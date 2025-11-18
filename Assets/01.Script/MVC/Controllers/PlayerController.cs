using UnityEngine;
using UnityEngine.InputSystem;

// Orchestrates player behavior (Controller in MVC)
// Responsibilities:
// - Read input (WASD, LMB fire, RMB reload, Space roll, E interact, Q skill)
// - Move player using physics/CharacterController
// - Manage rolling with temporary invincibility frames
// - Coordinate with IWeapon for firing/reloading and attack speed
// - Interact with IInteractable objects
// - Apply damage via IDamageable and PlayerStats (hp/defense)
// - Emit events for UI (hp change, ammo change)
// SOLID:
// - SRP: Only orchestrates; no rendering, no direct input system details
// - DIP: Depend on abstractions (IWeapon, IInteractable, IDamageable)

public class PlayerController : MonoBehaviour
{
    // [References] Link to PlayerView, PlayerStats, current IWeapon
    public WeaponController weaponController;
    public Vector2 inputVec;
    public float speed = 5f;
    public SpriteRenderer spriteRenderer;

    [Header("Weapon Settings")]
    public int currentWeaponIndex = 0;//현재 무기 번호 - 해당 번호의 스크립터블 오브젝트를 불러와 무기 프리팹에 덮어씌움.
    public int maxBulletCount = 10;//이하 데이터들은 나중에 리스트로 재구성할 것.
    public int currentBulletCount = 10;
    public float attackCooldown = 0.2f;
    public float reloadTime = 0.6f;
    //얘네 나중에 weaponData로 옮겨야 함
    public AudioClip fireSound;
    public AudioClip reloadSound;

    [Header("Weapon Icon")]
    public SpriteRenderer weaponIconRenderer;   // 현재 무기 아이콘을 표시할 Renderer
    [Tooltip("플레이어로부터 아이콘까지의 거리")]
    public float weaponIconDistance = 0.7f;     // 플레이어로부터의 거리
    [Tooltip("아이콘이 목표 위치를 따라가는 속도")]
    public float weaponIconFollowSpeed = 10f;   // 추적 보간 속도
    [Tooltip("스프라이트가 오른쪽을 바라보고 있을 때 필요한 회전 오프셋 (도 단위)")]
    public float weaponIconRotationOffset = 0f; // 스프라이트 기본 방향 보정

    [Header("Roll Settings")]
    public float rollSpeed = 12f;       // 구르기 속도 (이동 속도보다 빠르게)
    public float rollDuration = 0.2f;   // 구르기 지속 시간 (초)
    public float rollCooldown = 0.6f;   // 구르기 쿨다운 (초)
    public AudioClip rollSound; 

    [Header("Roll Visuals")]
    public Transform spriteRoot;         // 회전시킬 스프라이트 루트(보통 자식 트랜스폼)
    public bool rotateDuringRoll = true; // 구르는 동안 회전 여부
    public float rollSpinDegrees = 360f; // 구르기 1회전 각도

    [Header("Trail Settings")]
    public float trailSpawnInterval = 0.05f; // trail 생성 주기 (초)

    [Header("Combat Settings")]
    [Tooltip("피격 판정용 Trigger Collider (별도로 설정)")]
    public Collider2D hitboxCollider; // 피격 판정용 Collider (Trigger)
    
    private bool isRolling;
    private bool isInvincible;          // 구르는 동안 무적
    private float lastRollTime;
    private Vector2 rollDirection;
    private Vector2 lastMoveDirection;  // 입력이 0일 때도 방향 유지
    private bool isSande = false;
    private Coroutine trailSpawnCoroutine; // trail 생성 코루틴 참조
    private Vector2 lastTrailPosition; // 마지막 Trail 생성 위치
    private Animator animator;

    // 발사 및 재장전 관련 변수
    private float lastFireTime;         // 마지막 발사 시간
    private bool isReloading;            // 재장전 중 여부

    Rigidbody2D rigid;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // 회전할 때 히트박스도 움직이면 벽에 걸리니까 스프라이트를 자식으로 보내서 그림만 회전하게 만듬
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        SyncWeaponStatsFromData(forceResetAmmo: true);
        UpdateWeaponIconSprite();
        UpdateWeaponIconTransform(true);
    }

    // Unity Events 방식 전용 메서드 (Invoke Unity Events 모드에서 사용)
    public void OnMoveContext(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled)
        {
            inputVec = context.ReadValue<Vector2>();
            if (inputVec.sqrMagnitude > 0.0001f)
            {
                lastMoveDirection = inputVec.normalized;
                if(inputVec.x != 0){
                    spriteRenderer.flipX = inputVec.x > 0;
                }
            }
        }
    }

    public void OnRollContext(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (isRolling) return;
        if (Time.time < lastRollTime + rollCooldown) return;

        rollDirection = (inputVec.sqrMagnitude > 0.0001f ? inputVec : (lastMoveDirection.sqrMagnitude > 0 ? lastMoveDirection : Vector2.right)).normalized;
        
        StartCoroutine(RollRoutine(spriteRenderer.flipX));
        AudioSource.PlayClipAtPoint(rollSound, transform.position);
    }

    public void OnFireContext(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (weaponController == null || weaponController.CurrentWeapon == null) return;
        
        // 재장전 중이면 발사 불가
        if (isReloading) return;
        
        // 탄약이 없으면 발사 불가
        if (currentBulletCount <= 0) return;
        
        // 발사 쿨다운 체크
        if (Time.time < lastFireTime + attackCooldown) return;
        
        // 발사 실행
        Vector2 dir = ((Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - (Vector2)transform.position).normalized;
        Transform fireOrigin = weaponIconRenderer != null ? weaponIconRenderer.transform : transform;
        weaponController.Fire(dir, fireOrigin);
        
        // 발사 시간 기록 및 탄약 감소
        lastFireTime = Time.time;
        currentBulletCount--;
        if (fireSound != null)
        {
            AudioSource.PlayClipAtPoint(fireSound, transform.position);
        }
    }

    public void EquipWeapon(WeaponData data)
    {
        if (weaponController == null) return;
        if (weaponController.AddWeapon(data, true))
        {
            SyncWeaponStatsFromData(forceResetAmmo: true);
            UpdateWeaponIconSprite();
            UpdateWeaponIconTransform(true);
        }
    }

    public void OnRunContext(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
        case InputActionPhase.Performed:
            speed = 7.5f;
            break;

        case InputActionPhase.Canceled:
            speed = 5f;
            break;

        }
    }

    public void OnSandeContext(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
        case InputActionPhase.Performed:
            isSande = true;
            Time.timeScale = 0.5f;
            lastTrailPosition = transform.position; // 초기 위치 저장
            // 일정 주기마다 trail 생성하는 코루틴 시작 (중복 방지)
            if (trailSpawnCoroutine == null)
            {
                trailSpawnCoroutine = StartCoroutine(TrailSpawnRoutine());
            }
            break;

        case InputActionPhase.Canceled:
            isSande = false;
            Time.timeScale = 1f;
            break;

        }
    }
    
    // 일정 주기마다 trail을 생성하는 코루틴
    System.Collections.IEnumerator TrailSpawnRoutine()
    {
        while (isSande)
        {
            Vector2 currentPosition = transform.position;
            // 위치가 다르면 Trail 생성
            if (currentPosition != lastTrailPosition)
            {
                CreateTrail();
                lastTrailPosition = currentPosition;
            }
            yield return new WaitForSeconds(trailSpawnInterval * Time.timeScale);
        }
        trailSpawnCoroutine = null; // 종료 시 참조 초기화
    }

    private void Update()
    {
        HandleWeaponSwapInput();
        UpdateWeaponIconTransform();
    }

    void FixedUpdate()
    {
        if (isRolling)
        {
            Vector2 rollVec = rollDirection * rollSpeed * Time.fixedDeltaTime;
            rigid.MovePosition(rigid.position + rollVec);
            return;
        }

        Vector2 nextVec = inputVec.normalized * speed * Time.fixedDeltaTime;
        if(isSande && Time.timeScale > 0){
            nextVec /= Time.timeScale;
            nextVec *= 1.5f;
        }
        rigid.MovePosition(rigid.position + nextVec);
        animator.SetFloat("Speed", nextVec.magnitude);
    }
    
    void CreateTrail()
    {
        GameObject trail = new GameObject("Trail"); // 잔상 오브젝트 생성
        SpriteRenderer trailSprite = trail.AddComponent<SpriteRenderer>(); // SpriteRenderer 추가
        trailSprite.sprite = spriteRenderer.sprite; // 현재 스프라이트 복사
        trailSprite.color = new Color(1f, 0.5f, 0.5f, 1f); // 색상 설정
        trail.transform.position = transform.position;
        trail.transform.rotation = transform.rotation;
        trail.transform.localScale = transform.localScale;
        trailSprite.flipX = spriteRenderer.flipX;
        
        // trail을 원본보다 뒤에 렌더링 (sortingOrder를 낮춤)
        trailSprite.sortingOrder = spriteRenderer.sortingOrder - 1;
        StartCoroutine(TrailControlRoutine(trailSprite, trail));
    }
    // Input System에서 "Reload" 액션에 매핑 (Invoke Unity Events 모드)
    public void OnReloadContext(InputAction.CallbackContext context)
    {
        // 버튼을 눌렀을 때만 재장전 시작?
        if (!context.performed) return;
        
        // 이미 재장전 중이거나 탄약이 최대면 무시
        if (isReloading) return;
        if (currentBulletCount >= maxBulletCount) return;
        
        // 재장전 시작
        StartCoroutine(ReloadRoutine());
        if (reloadSound != null)
        {
            AudioSource.PlayClipAtPoint(reloadSound, transform.position);
        }
    }
    
    // [Combat] Handle fire, reload, skill cooldowns, projectile size modifier
    // [Roll] Implement roll state, duration, cooldown, i-frames
    
    // [Interaction] Detect interactables and invoke their Interact()
    // [Damage] Calculate final damage taken using defense stat

    System.Collections.IEnumerator TrailControlRoutine(SpriteRenderer trailSprite, GameObject trail){
        float startTime = Time.time; // trail 생성 시점 기록
        
        // 생성 시점의 색상 오프셋을 저장 (각 trail마다 다른 색상에서 시작)
        float colorOffset = (startTime * 0.1f) % 1f;
        
        while(isSande){
            // 생성 시점의 오프셋 + 현재 시간으로 색상 순환 (각 trail은 다른 색상에서 시작하지만 모두 순환)
            float hue = (colorOffset + (Time.time * 0.1f)) % 1f;
            Color rainbowColor = HSVToRGB(hue, 1f, 1f);
            
            trailSprite.color = new Color(rainbowColor.r, rainbowColor.g, rainbowColor.b, 0.2f);
            yield return null;
        }
        Destroy(trail);
    }
    
    // HSV to RGB 변환 헬퍼 함수
    Color HSVToRGB(float h, float s, float v)
    {
        h = Mathf.Clamp01(h);
        s = Mathf.Clamp01(s);
        v = Mathf.Clamp01(v);
        
        float c = v * s;
        float x = c * (1f - Mathf.Abs(((h * 6f) % 2f) - 1f));
        float m = v - c;
        
        float r = 0f, g = 0f, b = 0f;
        
        if (h < 1f / 6f)        { r = c; g = x; b = 0f; }
        else if (h < 2f / 6f)   { r = x; g = c; b = 0f; }
        else if (h < 3f / 6f)   { r = 0f; g = c; b = x; }
        else if (h < 4f / 6f)   { r = 0f; g = x; b = c; }
        else if (h < 5f / 6f)   { r = x; g = 0f; b = c; }
        else                    { r = c; g = 0f; b = x; }
        return new Color(r + m, g + m, b + m, 1f);
    }

    System.Collections.IEnumerator RollRoutine(bool isClockwise = true)
    {
        isRolling = true;
        isInvincible = true;
        lastRollTime = Time.time;

        Quaternion originalRotation = spriteRoot != null ? spriteRoot.localRotation : Quaternion.identity;

        float endTime = Time.time + rollDuration;
        while (Time.time < endTime)
        {
            if (rotateDuringRoll && spriteRoot != null)
            {
                float spinPerSecond = rollSpinDegrees / rollDuration; // 초당 회전 각도
                float delta = spinPerSecond * Time.deltaTime;
                spriteRoot.Rotate(0f, 0f, delta * (isClockwise ? -1 : 1), Space.Self); // 시계 방향 회전(-Z)
            }
            yield return null; // FixedUpdate에서 이동 처리
        }

        isRolling = false;
        isInvincible = false;

        if (spriteRoot != null)
        {
            spriteRoot.localRotation = originalRotation; // 원래 회전 복원
        }
    }
    
    // 재장전 코루틴
    System.Collections.IEnumerator ReloadRoutine()
    {
        isReloading = true; // 재장전 상태 시작
        
        // reloadTime만큼 대기
        yield return new WaitForSeconds(reloadTime);
        
        // 탄약을 최대치로 복구
        currentBulletCount = maxBulletCount;
        
        isReloading = false; // 재장전 상태 종료
    }

    private void SyncWeaponStatsFromData(bool forceResetAmmo = false)
    {
        if (weaponController == null) return;
        var data = weaponController.CurrentWeapon;
        if (data == null) return;

        maxBulletCount = Mathf.Max(0, data.magazineSize);
        attackCooldown = data.fireCooldown;
        reloadTime = data.reloadTime;

        if (forceResetAmmo)
        {
            currentBulletCount = maxBulletCount;
        }
        else
        {
            currentBulletCount = Mathf.Clamp(currentBulletCount, 0, maxBulletCount);
            if (currentBulletCount == 0)
            {
                currentBulletCount = maxBulletCount;
            }
        }
    }

    private void HandleWeaponSwapInput()
    {
        if (weaponController == null) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
        {
            TryEquipWeaponSlot(0);
        }
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
        {
            TryEquipWeaponSlot(1);
        }
        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
        {
            TryEquipWeaponSlot(2);
        }
    }

    private void TryEquipWeaponSlot(int slotIndex)
    {
        if (weaponController == null) return;
        if (weaponController.EquipWeaponByIndex(slotIndex))
        {
            SyncWeaponStatsFromData(forceResetAmmo: true);
            UpdateWeaponIconSprite();
            UpdateWeaponIconTransform(true);
        }
    }

    private void UpdateWeaponIconSprite()
    {
        if (weaponIconRenderer == null) return;

        if (weaponController == null)
        {
            weaponIconRenderer.sprite = null;
            weaponIconRenderer.enabled = false;
            return;
        }

        var data = weaponController.CurrentWeapon;
        weaponIconRenderer.sprite = data != null ? data.icon : null;
        weaponIconRenderer.enabled = weaponIconRenderer.sprite != null;
    }

    private void UpdateWeaponIconTransform(bool snapImmediate = false)
    {
        if (weaponIconRenderer == null || !weaponIconRenderer.enabled) return;

        Vector3 playerPos = transform.position;
        Vector3 direction = Vector3.right;

        if (Camera.main != null && Mouse.current != null)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mouseWorld.z = playerPos.z;
            direction = (mouseWorld - playerPos);
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.right;
        }
        else
        {
            direction.Normalize();
        }

        Vector3 targetPos = playerPos + direction * weaponIconDistance;
        Transform iconTransform = weaponIconRenderer.transform;

        if (snapImmediate)
        {
            iconTransform.position = targetPos;
        }
        else
        {
            iconTransform.position = Vector3.Lerp(iconTransform.position, targetPos, weaponIconFollowSpeed * Time.deltaTime);
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        iconTransform.rotation = Quaternion.Euler(0f, 0f, angle + weaponIconRotationOffset);

        // 왼쪽에 있을 경우 상하 반전
        weaponIconRenderer.flipY = direction.x < 0f;
    }

    // 피격 판정 처리 (Trigger Collider용 - hitboxCollider)
    void OnTriggerEnter2D(Collider2D other)
    {
        // 무적 상태(구르기 중)이면 피격 무시
        if (isInvincible) return;

        // 적이나 적의 총알과 충돌했는지 확인
        // 예: Enemy 태그나 EnemyProjectile 태그를 가진 오브젝트
        if (other.CompareTag("Enemy") || other.CompareTag("EnemyProjectile"))
        {
            // TODO: IDamageable 인터페이스를 통해 데미지 처리
            // 예: IDamageable damageable = GetComponent<IDamageable>();
            //     if (damageable != null) damageable.TakeDamage(...);
            
            Debug.Log($"플레이어 피격: {other.name}");
            
            // 적의 총알인 경우 파괴
            if (other.CompareTag("EnemyProjectile"))
            {
                Destroy(other.gameObject);
            }
        }
    }
}
