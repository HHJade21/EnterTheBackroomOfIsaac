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

    [Header("Roll Settings")]
    public float rollSpeed = 12f;       // 구르기 속도 (이동 속도보다 빠르게)
    public float rollDuration = 0.2f;   // 구르기 지속 시간 (초)
    public float rollCooldown = 0.6f;   // 구르기 쿨다운 (초)

    [Header("Roll Visuals")]
    public Transform spriteRoot;         // 회전시킬 스프라이트 루트(보통 자식 트랜스폼)
    public bool rotateDuringRoll = true; // 구르는 동안 회전 여부
    public float rollSpinDegrees = 360f; // 구르기 1회전 각도

    [Header("Trail Settings")]
    public float trailSpawnInterval = 0.05f; // trail 생성 주기 (초)

    private bool isRolling;
    private bool isInvincible;          // 구르는 동안 무적
    private float lastRollTime;
    private Vector2 rollDirection;
    private Vector2 lastMoveDirection;  // 입력이 0일 때도 방향 유지
    private bool isSande = false;
    private Coroutine trailSpawnCoroutine; // trail 생성 코루틴 참조

    // 발사 및 재장전 관련 변수
    private float lastFireTime;         // 마지막 발사 시간
    private bool isReloading;            // 재장전 중 여부

    Rigidbody2D rigid;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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
            }
        }
    }

    public void OnRollContext(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (isRolling) return;
        if (Time.time < lastRollTime + rollCooldown) return;

        rollDirection = (inputVec.sqrMagnitude > 0.0001f ? inputVec : (lastMoveDirection.sqrMagnitude > 0 ? lastMoveDirection : Vector2.right)).normalized;
        bool isClockwise;
        if(inputVec.x == 0){
            isClockwise = inputVec.y < 0;
        }else{
            isClockwise = inputVec.x > 0;
        }
        StartCoroutine(RollRoutine(isClockwise));
    }

    public void OnFireContext(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        
        // 재장전 중이면 발사 불가
        if (isReloading) return;
        
        // 탄약이 없으면 발사 불가
        if (currentBulletCount <= 0) return;
        
        // 발사 쿨다운 체크
        if (Time.time < lastFireTime + attackCooldown) return;
        
        // 발사 실행
        Vector2 dir = ((Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - (Vector2)transform.position).normalized;
        weaponController.Fire(dir, transform);
        
        // 발사 시간 기록 및 탄약 감소
        lastFireTime = Time.time;
        currentBulletCount--;
    }

    public void OnSandeContext(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
        case InputActionPhase.Performed:
            isSande = true;
            Time.timeScale = 0.5f;
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
            CreateTrail();
            yield return new WaitForSeconds(trailSpawnInterval * Time.timeScale);
        }
        trailSpawnCoroutine = null; // 종료 시 참조 초기화
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
}
