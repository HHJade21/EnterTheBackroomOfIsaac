using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

public enum CMYKColor
{
    Key = 0,      // 검정
    K = 0,
    Black = 0,
    Cyan = 1,     // 청록
    C = 1,
    Magenta = 2,  // 자홍
    M = 2,
    Yellow = 3,    // 노랑
    Y = 3,
}

public class PlayerController : MonoBehaviour
{
    // ========== References ==========
    public WeaponController weaponController;
    public SpriteRenderer spriteRenderer;
    private Animator animator;
    Rigidbody2D rigid;

    // ========== Movement Settings ==========
    public Vector2 inputVec;
    public float speed = 5f;
    private Vector2 lastMoveDirection;  // 입력이 0일 때도 방향 유지

    // ========== HP & Status ==========
    public int maxHP = 10;
    public int currentHP = 10;
    private bool isDead = false;
    private bool isInvincible;          // 구르는 동안 무적
    public bool isDashing = false;      // 돌진 중 여부 (WeaponController에서 설정)

    // ========== Swap System ==========
    public int swapCount=2;
    public float swapCharge=0f;
    [HideInInspector] public float swapChargeMax=5f;

    // ========== Roll Settings ==========
    [Header("Roll Settings")]
    public float rollSpeed = 12f;       // 구르기 속도 (이동 속도보다 빠르게)
    public float rollDuration = 0.2f;   // 구르기 지속 시간 (초)
    public float rollCooldown = 0.6f;   // 구르기 쿨다운 (초)
    public AudioClip rollSound; 

    [Header("Roll Visuals")]
    public Transform spriteRoot;         // 회전시킬 스프라이트 루트(보통 자식 트랜스폼)
    public bool rotateDuringRoll = true; // 구르는 동안 회전 여부
    public float rollSpinDegrees = 360f; // 구르기 1회전 각도

    private bool isRolling;
    private float lastRollTime;
    private Vector2 rollDirection;

    // ========== Warp Settings ==========
    [Header("Warp Settings")]
    [ArrayLabel("Key", "Cyan", "Magenta", "Yellow")]
    public GameObject[] warpEffectPrefabs;  // 워프 시작 위치에 생성할 애니메이션 프리팹

    // ========== Trail/Sande Settings ==========
    [Header("Trail Settings")]
    public float trailSpawnInterval = 0.05f; // trail 생성 주기 (초)
    private bool isSande = false;
    private Coroutine trailSpawnCoroutine; // trail 생성 코루틴 참조
    private Vector2 lastTrailPosition; // 마지막 Trail 생성 위치

    // ========== Combat Settings ==========
    [Header("Combat Settings")]
    [Tooltip("피격 판정용 Trigger Collider (별도로 설정)")]
    public Collider2D hitboxCollider; // 피격 판정용 Collider (Trigger)
    
    [Header("Collision Settings")]
    [Tooltip("벽 충돌용 콜라이더 (Feet 오브젝트의 콜라이더)")]
    public Collider2D wallCollider; // 벽 충돌용 콜라이더 (Feet에 있는 콜라이더)
    
    private float knockbackForce = 0f;
    private Vector2 knockbackDirection;

    // ========== Status Multipliers ==========
    [Header("Status Multipliers")]
    [Tooltip("공격 속도 배율 (기본값: 1.0, 낮을수록 빠름)")]
    public float attackSpeedMultiplier = 1f;
    public float attackRangeMultiplier = 1f;
    public float attackDamageMultiplier = 1f;
    [Tooltip("재장전 속도 배율 (기본값: 1.0, 낮을수록 빠름)")]
    public float reloadSpeedMultiplier = 1f;
    public float moveSpeedMultiplier = 1f;
    public float rollSpeedMultiplier = 1f;
    public float rollDurationMultiplier = 1f;
    public float rollCooldownMultiplier = 1f;
    public float swapCooldownMultiplier = 1f;
    
    // 기본 배율 값 저장 (리셋용)
    private const float DEFAULT_MULTIPLIER = 1f;

    // ========== Interaction Settings ==========
    [Header("Interaction Settings")]
    public GameObject targetItemPrefab;
    public float targetItemDistance = 1.5f;
    public GameObject InteractionText;

    // ========== UI Settings ==========
    [Header("Pause panel")]
    public GameObject pausePanel;

    // ========== Weapon Related ==========
    private Coroutine autoFireCoroutine; // 자동 발사 코루틴 참조

    // [주석 처리됨] 무기 관련 변수들은 WeaponController로 이동했습니다.
    // public int maxBulletCount = 10;
    // public int currentBulletCount = 10;
    // public float attackCooldown = 0.2f;
    // public float reloadTime = 0.6f;
    // public AudioClip fireSound;
    // public AudioClip reloadSound;
    // public SpriteRenderer weaponIconRenderer;
    // public float weaponIconDistance = 0.7f;
    // public float weaponIconFollowSpeed = 10f;
    // public float weaponIconRotationOffset = 0f;

    // [주석 처리됨] 발사 및 재장전 관련 변수들은 WeaponController로 이동했습니다.
    // private float lastFireTime;
    // private bool isReloading;

    // ========== Unity Lifecycle ==========
    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // 회전할 때 히트박스도 움직이면 벽에 걸리니까 스프라이트를 자식으로 보내서 그림만 회전하게 만듬
        animator = GetComponentInChildren<Animator>();
        
        // Feet 오브젝트의 콜라이더 자동 찾기 (수동 설정 안 했을 경우)
        if (wallCollider == null)
        {
            Transform feetTransform = transform.Find("Feet");
            if (feetTransform != null)
            {
                wallCollider = feetTransform.GetComponent<Collider2D>();
            }
        }
    }

    private void Start()
    {
        if (weaponController != null)
        {
            weaponController.SyncWeaponStats(forceResetAmmo: true);
            weaponController.UpdateWeaponIconSprite();
            weaponController.UpdateWeaponIconTransform(transform.position, true);
        }
    }

    private void Update()
    {
        // 무기 아이콘 위치 업데이트 (WeaponController에서 처리)
        if (weaponController != null)
        {
            weaponController.UpdateWeaponIconTransform(transform.position);
            
            // ChargeDash 무기 타입이고 마우스를 누르고 있으면 충전 업데이트
            if (weaponController.CurrentWeapon != null && 
                weaponController.CurrentWeapon.weaponType == WeaponData.WeaponType.ChargeDash &&
                Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Vector2 dir = ((Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - (Vector2)transform.position).normalized;
                weaponController.UpdateChargeDash(dir);
            }
            
            // ChargeFire 무기 타입이고 마우스를 누르고 있으면 충전 업데이트
            if (weaponController.CurrentWeapon != null && 
                weaponController.CurrentWeapon.weaponType == WeaponData.WeaponType.ChargeFire &&
                Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Vector2 dir = ((Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - (Vector2)transform.position).normalized;
                weaponController.UpdateChargeFire(dir);
            }
        }
        
        // SwapCount가 2 미만일 시 swapCharge가 시간에 따라 서서히 증가
        if (swapCount < 2)
        {
            swapCharge += Time.deltaTime;
            
            // swapCharge가 swapChargeMax와 같아지면, swapCharge가 0으로 초기화되고, swapCount가 1 증가
            if (swapCharge >= swapChargeMax)
            {
                swapCharge = 0f;
                swapCount++;
            }
        }
        else
        {
            // swapCount가 2 이상이면 swapCharge를 0으로 유지
            swapCharge = 0f;
        }
        
        DetectNearbyWeapons();
    }

    /************************************ FixedUpdate 잘보이라고 어그로끄는용 ************************************/
    
    void FixedUpdate()
    {
        if (isDead) return;
        if (isRolling)
        {
            Vector2 rollVec = rollDirection * rollSpeed * Time.fixedDeltaTime;
            rigid.MovePosition(rigid.position + rollVec);
            return;
        }
        if (isDashing)
        {
            // 돌진 중에는 일반 이동 건너뛰기 (WeaponController의 ChargeDashRoutine에서 이동 처리)
            return;
        }
        if(knockbackForce > 0f){
            Vector2 knockbackVec = knockbackDirection * knockbackForce * Time.fixedDeltaTime;
            rigid.MovePosition(rigid.position + knockbackVec);
            knockbackForce -= Time.fixedDeltaTime * 20f;
            if(knockbackForce <= 0f){
                knockbackForce = 0f;
            }
            return;
        }

        Vector2 nextVec = inputVec.normalized * speed * Time.fixedDeltaTime;
        if(isSande && Time.timeScale > 0){
            nextVec /= Time.timeScale;
            nextVec *= 1.5f;
        }
        rigid.MovePosition(rigid.position + nextVec);
        if(inputVec.x != 0){
            spriteRenderer.flipX = inputVec.x > 0;
        }
        animator.SetFloat("Speed", nextVec.magnitude);
    }

    // ========== Input Handlers ==========
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

    public void OnFireContext(InputAction.CallbackContext context)
    {
        if (weaponController == null) return;
        if (weaponController.CurrentWeapon == null) return;
        
        // 발사 방향 계산
        Vector2 dir = ((Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - (Vector2)transform.position).normalized;
        
        // 발사 오리진 위치 (무기 아이콘이 있으면 아이콘 위치, 없으면 플레이어 위치)
        Transform fireOrigin = weaponController.weaponIconRenderer != null 
            ? weaponController.weaponIconRenderer.transform 
            : transform;
        
        // ChargeDash 무기 타입인 경우 특별 처리
        if (weaponController.CurrentWeapon.weaponType == WeaponData.WeaponType.ChargeDash)
        {
            if (context.started)
            {
                // 마우스를 누르기 시작: 충전 시작
                weaponController.StartChargeDash(dir, fireOrigin);
            }
            else if (context.canceled)
            {
                // 마우스를 떼는 순간: 돌진 실행
                weaponController.ChargeDashAttack(dir, fireOrigin);
            }
            return;
        }
        
        // ChargeFire 무기 타입인 경우 특별 처리
        if (weaponController.CurrentWeapon.weaponType == WeaponData.WeaponType.ChargeFire)
        {
            if (context.started)
            {
                // 마우스를 누르기 시작: 충전 시작
                weaponController.StartChargeFire(dir, fireOrigin);
            }
            else if (context.canceled)
            {
                // 마우스를 떼는 순간: 레이저 빔 발사
                weaponController.ChargeFireAttack(dir, fireOrigin);
            }
            return;
        }
        
        // autoFire가 true인 경우 자동 발사 처리
        if (weaponController.CurrentWeapon.autoFire)
        {
            if (context.started)
            {
                // 마우스를 누르기 시작: 자동 발사 코루틴 시작
                if (autoFireCoroutine == null)
                {
                    // 무기 아이콘 애니메이터에 Attacking = true 설정 (Attack 애니메이션 루프 재생)
                    if (weaponController.CurrentWeapon.animatorController != null && weaponController.weaponIconRenderer != null)
                    {
                        Animator weaponAnimator = weaponController.weaponIconRenderer.GetComponent<Animator>();
                        if (weaponAnimator != null)
                        {
                            weaponAnimator.SetBool("Attacking", true);
                        }
                    }
                    
                    autoFireCoroutine = StartCoroutine(AutoFireRoutine());
                }
            }
            else if (context.canceled)
            {
                // 마우스를 떼는 순간: 자동 발사 코루틴 중지 및 Attacking = false 설정
                if (autoFireCoroutine != null)
                {
                    StopCoroutine(autoFireCoroutine);
                    autoFireCoroutine = null;
                }
                
                // 무기 아이콘 애니메이터에 Attacking = false 설정 (Idle로 복귀)
                if (weaponController.CurrentWeapon != null && weaponController.CurrentWeapon.animatorController != null && weaponController.weaponIconRenderer != null)
                {
                    Animator weaponAnimator = weaponController.weaponIconRenderer.GetComponent<Animator>();
                    if (weaponAnimator != null)
                    {
                        weaponAnimator.SetBool("Attacking", false);
                    }
                }
            }
            return;
        }
        
        // 일반 공격은 performed일 때만 처리
        if (!context.performed) return;
        
        // WeaponController에서 발사 처리
        weaponController.OnFire(dir, fireOrigin, transform.position);
    }

    // Input System에서 "Reload" 액션에 매핑 (Invoke Unity Events 모드)
    public void OnReloadContext(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (weaponController == null) return;
        
        // WeaponController에서 재장전 처리
        weaponController.Reload(transform.position);
    }

    public void OnSwapContext(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (weaponController == null) return;
        
        // swapCount가 0 이하일 경우 Swap을 사용할 수 없음
        if (swapCount <= 0) return;
        
        // WeaponController에서 무기 교체 처리
        if (weaponController.SwapWeapon())
        {
            // swap 사용 시 swapCount가 1 감소
            swapCount--;
            
            // 무기 교체 성공 시 색상 변경
            ChangeColor((int)weaponController.GetCurrentWeaponData().element);
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

    public void OnWarpContext(InputAction.CallbackContext context) //Roll 대체 할 수도 있는거
    {
        if (!context.performed) return;
        if (Time.time < lastRollTime + rollCooldown) return;

        rollDirection = (inputVec.sqrMagnitude > 0.0001f ? inputVec : (lastMoveDirection.sqrMagnitude > 0 ? lastMoveDirection : Vector2.right)).normalized;
        AudioSource.PlayClipAtPoint(rollSound, transform.position);
        StartCoroutine(WarpEffectRoutine(spriteRenderer.flipX));
        StartCoroutine(WarpRoutine());
    }

    public void OnSandeContext(InputAction.CallbackContext context)
    {
        // 빌드 상태에서는 Sande 기능 비활성화
        if (!Application.isEditor)
        {
            return;
        }
        
        switch (context.phase)
        {
        case InputActionPhase.Performed:
            isSande = true;
            Time.timeScale = 0.5f;
            lastTrailPosition = transform.position; // 초기 위치 저장
            // 벽 충돌 콜라이더 비활성화
            if (wallCollider != null)
            {
                wallCollider.enabled = false;
            }
            // 일정 주기마다 trail 생성하는 코루틴 시작 (중복 방지)
            if (trailSpawnCoroutine == null)
            {
                trailSpawnCoroutine = StartCoroutine(TrailSpawnRoutine());
            }
            break;

        case InputActionPhase.Canceled:
            isSande = false;
            Time.timeScale = 1f;
            // 벽 충돌 콜라이더 다시 활성화
            if (wallCollider != null)
            {
                wallCollider.enabled = true;
            }
            break;

        }
    }

    /// <summary>
    /// 상호작용 입력 처리 메소드: E키로 targetItem과 상호작용합니다.
    /// - 무기인 경우: WeaponController의 인벤토리에 추가만 하고 자동 장착하지 않음, 프리팹 파괴
    /// - 일반 아이템인 경우: 추후 구현 예정
    /// </summary>
    public void OnInteractContext(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (targetItemPrefab == null) return;
        if (weaponController == null) return;
        
        // targetItem이 무기인지 확인 (newWeapon 컴포넌트가 있는지 확인)
        newWeapon weaponComponent = targetItemPrefab.GetComponent<newWeapon>();
        if (weaponComponent != null)
        {
            // 무기인 경우: itemID로 WeaponData 가져오기
            WeaponData weaponData = weaponController.GetWeaponDataByID(weaponComponent.itemID);
            if (weaponData != null)
            {
                // 무기 추가 시도 (자동 장착하지 않음)
                bool success = weaponController.AddWeapon(weaponData, makeCurrent: false);
                if (success)
                {
                    // 획득 성공 시 무기 프리팹 파괴
                    Destroy(targetItemPrefab);
                    targetItemPrefab = null;
                }
            }
        }
        // 일반 아이템인 경우는 추후 구현 예정
    }

    public void OnPauseContext(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
    }

    // ========== Weapon Methods ==========
    public void EquipWeapon(WeaponData data)
    {
        if (weaponController == null) return;
        if (weaponController.AddWeapon(data, true))
        {
            weaponController.SyncWeaponStats(forceResetAmmo: true);
            weaponController.UpdateWeaponIconSprite();
            weaponController.UpdateWeaponIconTransform(transform.position, true);
        }
    }
    
    /// <summary>
    /// 자동 발사 코루틴: 마우스를 누르고 있는 동안 fireCooldown 간격으로 자동 발사합니다.
    /// </summary>
    private System.Collections.IEnumerator AutoFireRoutine()
    {
        while (true)
        {
            if (weaponController == null || weaponController.CurrentWeapon == null)
            {
                autoFireCoroutine = null;
                yield break;
            }
            
            // 발사 방향 계산
            Vector2 dir = ((Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - (Vector2)transform.position).normalized;
            
            // 발사 오리진 위치
            Transform fireOrigin = weaponController.weaponIconRenderer != null 
                ? weaponController.weaponIconRenderer.transform 
                : transform;
            
            // WeaponController에서 발사 처리
            weaponController.OnFire(dir, fireOrigin, transform.position);
            
            // fireCooldown만큼 대기 (WeaponController의 attackCooldown은 이미 attackSpeedMultiplier가 적용됨)
            yield return new WaitForSeconds(weaponController.attackCooldown);
        }
    }

    // ========== Roll & Movement Methods ==========
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

    System.Collections.IEnumerator WarpRoutine()
    {
        isRolling = true;
        isInvincible = true;
        lastRollTime = Time.time;

        spriteRenderer.enabled = false;
        
        float endTime = Time.time + rollDuration;
        while (Time.time < endTime)
        {
            yield return null;
        }
        isRolling = false;
        isInvincible = false;
        spriteRenderer.enabled = true;
        animator.SetTrigger("Warp");
        
    }

    System.Collections.IEnumerator WarpEffectRoutine(bool isFlip)
    {
        // 워프 시작 위치 저장
        Vector2 warpStartPosition = rigid.position;
        
        int cmyk = animator.GetInteger("CMYK");
        // 워프 시작 위치에 이펙트 생성
        if (warpEffectPrefabs[cmyk] != null)
        {
            GameObject warpEffect = Instantiate(warpEffectPrefabs[cmyk], warpStartPosition, Quaternion.identity);
            
            // 애니메이션 길이만큼 기다린 후 파괴
            Animator effectAnimator = warpEffect.GetComponent<Animator>();
            SpriteRenderer effectSpriteRenderer = warpEffect.GetComponent<SpriteRenderer>();
            effectSpriteRenderer.flipX = isFlip;
            if (effectAnimator != null)
            {
                // 한 프레임 기다려서 Animator가 초기화되도록 함
                yield return null;
                
                // Animator의 현재 클립 길이 가져오기
                AnimatorStateInfo stateInfo = effectAnimator.GetCurrentAnimatorStateInfo(0);
                float animationLength = stateInfo.length;
                
                if (animationLength > 0f)
                {
                    yield return new WaitForSeconds(animationLength);
                }
                else
                {
                    // 길이를 가져올 수 없으면 기본 시간(1초) 후 파괴
                    yield return new WaitForSeconds(1f);
                }
            }
            else
            {
                // Animator가 없으면 기본 시간(1초) 후 파괴
                yield return new WaitForSeconds(1f);
            }
            
            Destroy(warpEffect);
        }
    }

    // ========== Trail/Sande Methods ==========
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

    // ========== Interaction Methods ==========
    // 주변 무기 감지 및 targetItemPrefab 할당
    private void DetectNearbyWeapons()
    {
        // 모든 newWeapon 컴포넌트를 가진 GameObject 찾기
        newWeapon[] weapons = FindObjectsOfType<newWeapon>();
        
        if (weapons == null || weapons.Length == 0)
        {
            // 주변에 무기가 없으면 targetItemPrefab 초기화
            targetItemPrefab = null;
            UpdateInteractionText();
            return;
        }
        
        Vector2 playerPos = transform.position;
        GameObject closestWeapon = null;
        float closestDistance = float.MaxValue;
        
        // 가장 가까운 무기 찾기
        foreach (newWeapon weapon in weapons)
        {
            if (weapon == null || weapon.gameObject == null) continue;
            
            Vector2 weaponPos = weapon.transform.position;
            float distance = Vector2.Distance(playerPos, weaponPos);
            
            // targetItemDistance 내에 있고, 가장 가까운 무기인지 확인
            if (distance <= targetItemDistance && distance < closestDistance)
            {
                closestDistance = distance;
                closestWeapon = weapon.gameObject;
            }
        }
        
        // 가장 가까운 무기를 targetItemPrefab에 할당
        targetItemPrefab = closestWeapon;
        UpdateInteractionText();
    }
    
    // InteractionText 활성/비활성화 및 위치 업데이트
    private void UpdateInteractionText()
    {
        if (InteractionText == null) return;
        
        if (targetItemPrefab != null)
        {
            // targetItemPrefab이 있으면 InteractionText 활성화 및 위치 설정
            InteractionText.SetActive(true);
            
            // UI 요소의 위치를 설정하기 위해 RectTransform 사용
            RectTransform rectTransform = InteractionText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 월드 좌표를 스크린 좌표로 변환
                Vector3 worldPos = targetItemPrefab.transform.position;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                
                // Canvas를 찾아서 좌표 변환
                Canvas canvas = InteractionText.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    // 스크린 좌표를 Canvas의 로컬 좌표로 변환
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvas.transform as RectTransform,
                        screenPos,
                        canvas.worldCamera,
                        out Vector2 localPoint
                    );
                    
                    // RectTransform의 anchoredPosition 설정
                    rectTransform.anchoredPosition = localPoint;
                }
                else
                {
                    // Canvas를 찾을 수 없으면 스크린 좌표를 직접 사용
                    rectTransform.position = screenPos;
                }
            }
        }
        else
        {
            // targetItemPrefab이 null이면 InteractionText 비활성화
            InteractionText.SetActive(false);
        }
    }

    // ========== Animation & Visual Methods ==========
    public void Ayaya(){
        animator.SetTrigger("Aya");
        Knockback(7f, spriteRenderer.flipX ? Vector2.left : Vector2.right);
        
    }

    public void Death(){
        animator.SetTrigger("Death");
        isDead = true;
    }

    public void Revive(){
        isDead = false;
        animator.SetTrigger("Change");
    }

    public void ChangeColor(CMYKColor color){
        animator.SetInteger("CMYK", (int)color);
        animator.SetTrigger("Change");
    }

    public void ChangeColor(int colorIndex){ //이거 있는 이유: 버튼에 함수 할당할때 enum은 안보임 이슈...
        ChangeColor((CMYKColor)colorIndex);
    }

    private void Knockback(float force, Vector2 direction){
        animator.SetTrigger("Aya");
        StartCoroutine(HitRoutine());
        knockbackForce = force;
        knockbackDirection = direction;
    }

    System.Collections.IEnumerator HitRoutine(){
        float n = 0.5f;
        while(n < 1f){
            spriteRenderer.color = new Color(1f, n, n, 1f);
            n += 0.1f;
            yield return new WaitForSeconds(0.05f);
        }
        spriteRenderer.color = Color.white;
    }

    // ========== UI Methods ==========
    public void Resume()
    {
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
    }
    public void GoToTitle()
    {
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
        
        // GameManager.Instance가 null인지 확인
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadTitle();
        }
        else
        {
            // GameManager가 없으면 직접 씬 로드
            Debug.LogWarning("GameManager.Instance가 null입니다. 직접 씬을 로드합니다.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
        }
    }

    // ========== Multiplier Control Methods (Buff/Debuff System) ==========
    #region Multiplier Control Methods (Buff/Debuff System)
    
    /// <summary>
    /// 공격 속도 배율 설정 메소드: 버프/디버프 시스템에서 사용합니다.
    /// </summary>
    /// <param name="value">설정할 배율 값 (1.0이 기본값)</param>
    public void SetAttackSpeedMultiplier(float value)
    {
        attackSpeedMultiplier = Mathf.Max(0f, value);
    }
    
    /// <summary>
    /// 공격 속도 배율 곱하기 메소드: 기존 배율에 곱하여 적용합니다. (중첩 버프용)
    /// </summary>
    /// <param name="multiplier">곱할 배율 값</param>
    public void MultiplyAttackSpeedMultiplier(float multiplier)
    {
        attackSpeedMultiplier *= Mathf.Max(0f, multiplier);
    }
    
    /// <summary>
    /// 공격 속도 배율 리셋 메소드: 기본값(1.0)으로 복구합니다.
    /// </summary>
    public void ResetAttackSpeedMultiplier()
    {
        attackSpeedMultiplier = DEFAULT_MULTIPLIER;
    }
    
    /// <summary>
    /// 공격 범위 배율 설정 메소드: 버프/디버프 시스템에서 사용합니다.
    /// </summary>
    /// <param name="value">설정할 배율 값 (1.0이 기본값)</param>
    public void SetAttackRangeMultiplier(float value)
    {
        attackRangeMultiplier = Mathf.Max(0f, value);
    }
    
    /// <summary>
    /// 공격 범위 배율 곱하기 메소드: 기존 배율에 곱하여 적용합니다. (중첩 버프용)
    /// </summary>
    /// <param name="multiplier">곱할 배율 값</param>
    public void MultiplyAttackRangeMultiplier(float multiplier)
    {
        attackRangeMultiplier *= Mathf.Max(0f, multiplier);
    }
    
    /// <summary>
    /// 공격 범위 배율 리셋 메소드: 기본값(1.0)으로 복구합니다.
    /// </summary>
    public void ResetAttackRangeMultiplier()
    {
        attackRangeMultiplier = DEFAULT_MULTIPLIER;
    }
    
    /// <summary>
    /// 공격 데미지 배율 설정 메소드: 버프/디버프 시스템에서 사용합니다.
    /// </summary>
    /// <param name="value">설정할 배율 값 (1.0이 기본값)</param>
    public void SetAttackDamageMultiplier(float value)
    {
        attackDamageMultiplier = Mathf.Max(0f, value);
    }
    
    /// <summary>
    /// 공격 데미지 배율 곱하기 메소드: 기존 배율에 곱하여 적용합니다. (중첩 버프용)
    /// </summary>
    /// <param name="multiplier">곱할 배율 값</param>
    public void MultiplyAttackDamageMultiplier(float multiplier)
    {
        attackDamageMultiplier *= Mathf.Max(0f, multiplier);
    }
    
    /// <summary>
    /// 공격 데미지 배율 리셋 메소드: 기본값(1.0)으로 복구합니다.
    /// </summary>
    public void ResetAttackDamageMultiplier()
    {
        attackDamageMultiplier = DEFAULT_MULTIPLIER;
    }
    
    /// <summary>
    /// 재장전 속도 배율 설정 메소드: 버프/디버프 시스템에서 사용합니다.
    /// </summary>
    /// <param name="value">설정할 배율 값 (1.0이 기본값)</param>
    public void SetReloadSpeedMultiplier(float value)
    {
        reloadSpeedMultiplier = Mathf.Max(0f, value);
    }
    
    /// <summary>
    /// 재장전 속도 배율 곱하기 메소드: 기존 배율에 곱하여 적용합니다. (중첩 버프용)
    /// </summary>
    /// <param name="multiplier">곱할 배율 값</param>
    public void MultiplyReloadSpeedMultiplier(float multiplier)
    {
        reloadSpeedMultiplier *= Mathf.Max(0f, multiplier);
    }
    
    /// <summary>
    /// 재장전 속도 배율 리셋 메소드: 기본값(1.0)으로 복구합니다.
    /// </summary>
    public void ResetReloadSpeedMultiplier()
    {
        reloadSpeedMultiplier = DEFAULT_MULTIPLIER;
    }
    
    /// <summary>
    /// 이동 속도 배율 설정 메소드: 버프/디버프 시스템에서 사용합니다.
    /// </summary>
    /// <param name="value">설정할 배율 값 (1.0이 기본값)</param>
    public void SetMoveSpeedMultiplier(float value)
    {
        moveSpeedMultiplier = Mathf.Max(0f, value);
    }
    
    /// <summary>
    /// 이동 속도 배율 곱하기 메소드: 기존 배율에 곱하여 적용합니다. (중첩 버프용)
    /// </summary>
    /// <param name="multiplier">곱할 배율 값</param>
    public void MultiplyMoveSpeedMultiplier(float multiplier)
    {
        moveSpeedMultiplier *= Mathf.Max(0f, multiplier);
    }
    
    /// <summary>
    /// 이동 속도 배율 리셋 메소드: 기본값(1.0)으로 복구합니다.
    /// </summary>
    public void ResetMoveSpeedMultiplier()
    {
        moveSpeedMultiplier = DEFAULT_MULTIPLIER;
    }
    
    /// <summary>
    /// 구르기 속도 배율 설정 메소드: 버프/디버프 시스템에서 사용합니다.
    /// </summary>
    /// <param name="value">설정할 배율 값 (1.0이 기본값)</param>
    public void SetRollSpeedMultiplier(float value)
    {
        rollSpeedMultiplier = Mathf.Max(0f, value);
    }
    
    /// <summary>
    /// 구르기 속도 배율 곱하기 메소드: 기존 배율에 곱하여 적용합니다. (중첩 버프용)
    /// </summary>
    /// <param name="multiplier">곱할 배율 값</param>
    public void MultiplyRollSpeedMultiplier(float multiplier)
    {
        rollSpeedMultiplier *= Mathf.Max(0f, multiplier);
    }
    
    /// <summary>
    /// 구르기 속도 배율 리셋 메소드: 기본값(1.0)으로 복구합니다.
    /// </summary>
    public void ResetRollSpeedMultiplier()
    {
        rollSpeedMultiplier = DEFAULT_MULTIPLIER;
    }
    
    /// <summary>
    /// 구르기 지속 시간 배율 설정 메소드: 버프/디버프 시스템에서 사용합니다.
    /// </summary>
    /// <param name="value">설정할 배율 값 (1.0이 기본값)</param>
    public void SetRollDurationMultiplier(float value)
    {
        rollDurationMultiplier = Mathf.Max(0f, value);
    }
    
    /// <summary>
    /// 구르기 지속 시간 배율 곱하기 메소드: 기존 배율에 곱하여 적용합니다. (중첩 버프용)
    /// </summary>
    /// <param name="multiplier">곱할 배율 값</param>
    public void MultiplyRollDurationMultiplier(float multiplier)
    {
        rollDurationMultiplier *= Mathf.Max(0f, multiplier);
    }
    
    /// <summary>
    /// 구르기 지속 시간 배율 리셋 메소드: 기본값(1.0)으로 복구합니다.
    /// </summary>
    public void ResetRollDurationMultiplier()
    {
        rollDurationMultiplier = DEFAULT_MULTIPLIER;
    }
    
    /// <summary>
    /// 구르기 쿨다운 배율 설정 메소드: 버프/디버프 시스템에서 사용합니다.
    /// </summary>
    /// <param name="value">설정할 배율 값 (1.0이 기본값)</param>
    public void SetRollCooldownMultiplier(float value)
    {
        rollCooldownMultiplier = Mathf.Max(0f, value);
    }
    
    /// <summary>
    /// 구르기 쿨다운 배율 곱하기 메소드: 기존 배율에 곱하여 적용합니다. (중첩 버프용)
    /// </summary>
    /// <param name="multiplier">곱할 배율 값</param>
    public void MultiplyRollCooldownMultiplier(float multiplier)
    {
        rollCooldownMultiplier *= Mathf.Max(0f, multiplier);
    }
    
    /// <summary>
    /// 구르기 쿨다운 배율 리셋 메소드: 기본값(1.0)으로 복구합니다.
    /// </summary>
    public void ResetRollCooldownMultiplier()
    {
        rollCooldownMultiplier = DEFAULT_MULTIPLIER;
    }
    
    /// <summary>
    /// 무기 교체 쿨다운 배율 설정 메소드: 버프/디버프 시스템에서 사용합니다.
    /// </summary>
    /// <param name="value">설정할 배율 값 (1.0이 기본값)</param>
    public void SetSwapCooldownMultiplier(float value)
    {
        swapCooldownMultiplier = Mathf.Max(0f, value);
    }
    
    /// <summary>
    /// 무기 교체 쿨다운 배율 곱하기 메소드: 기존 배율에 곱하여 적용합니다. (중첩 버프용)
    /// </summary>
    /// <param name="multiplier">곱할 배율 값</param>
    public void MultiplySwapCooldownMultiplier(float multiplier)
    {
        swapCooldownMultiplier *= Mathf.Max(0f, multiplier);
    }
    
    /// <summary>
    /// 무기 교체 쿨다운 배율 리셋 메소드: 기본값(1.0)으로 복구합니다.
    /// </summary>
    public void ResetSwapCooldownMultiplier()
    {
        swapCooldownMultiplier = DEFAULT_MULTIPLIER;
    }
    
    /// <summary>
    /// 모든 배율을 기본값(1.0)으로 리셋하는 메소드: 버프/디버프가 모두 해제될 때 사용합니다.
    /// </summary>
    public void ResetAllMultipliers()
    {
        attackSpeedMultiplier = DEFAULT_MULTIPLIER;
        attackRangeMultiplier = DEFAULT_MULTIPLIER;
        attackDamageMultiplier = DEFAULT_MULTIPLIER;
        reloadSpeedMultiplier = DEFAULT_MULTIPLIER;
        moveSpeedMultiplier = DEFAULT_MULTIPLIER;
        rollSpeedMultiplier = DEFAULT_MULTIPLIER;
        rollDurationMultiplier = DEFAULT_MULTIPLIER;
        rollCooldownMultiplier = DEFAULT_MULTIPLIER;
        swapCooldownMultiplier = DEFAULT_MULTIPLIER;
    }
    
    #endregion

    // ========== Damage and Death System ==========
    #region Damage and Death System
    
    /// <summary>
    /// 피격 판정 처리 메소드: Bullet_Enemy 태그를 가진 오브젝트와 충돌 시 데미지를 받습니다.
    /// </summary>
    /// <param name="other">충돌한 오브젝트의 Collider2D</param>
    void OnTriggerEnter2D(Collider2D other)
    {
        // 무적 상태(구르기 중)이면 피격 무시
        if (isInvincible) return;
        
        // 사망 상태면 피격 무시
        if (isDead) return;

        // 적의 총알과 충돌했는지 확인
        if (other.CompareTag("Bullet_Enemy"))
        {
            // 데미지 1 적용 (추후 데미지량을 투사체에서 가져오도록 확장 가능)
            TakeDamage(1);
            
            // 적의 총알 파괴
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Enemy"))
        {
            TakeDamage(1);//데미지 가져와서 적용하도록 할 것.
        }
    }
    
    /// <summary>
    /// 데미지 적용 메소드: 체력을 감소시키고, 체력이 0 이하가 되면 사망 처리합니다.
    /// </summary>
    /// <param name="amount">받을 데미지량</param>
    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        currentHP = Mathf.Max(0, currentHP); // 음수 방지
        
        Debug.Log($"플레이어 피격: {amount} 데미지 받음. 현재 HP: {currentHP}/{maxHP}");
        
        // HP가 0 이하가 되면 사망
        if (currentHP <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 사망 처리 메소드: 플레이어가 사망했을 때 호출됩니다.
    /// </summary>
    private void Die()
    {
        isDead = true;
        Debug.Log("플레이어 사망");
        animator.SetTrigger("Death");
        // TODO: 사망 처리 로직 구현
    }
    
    #endregion

    // [Combat] Handle fire, reload, skill cooldowns, projectile size modifier
    // [Roll] Implement roll state, duration, cooldown, i-frames
    
    // [Interaction] Detect interactables and invoke their Interact()
    // [Damage] Calculate final damage taken using defense stat

    // [주석 처리됨] 이 메서드들은 WeaponController로 이동했습니다.
    // System.Collections.IEnumerator ReloadRoutine()
    // {
    //     isReloading = true; // 재장전 상태 시작
    //     
    //     // reloadTime만큼 대기
    //     yield return new WaitForSeconds(reloadTime);
    //     
    //     // 탄약을 최대치로 복구
    //     currentBulletCount = maxBulletCount;
    //     
    //     isReloading = false; // 재장전 상태 종료
    // }

    // private void SyncWeaponStatsFromData(bool forceResetAmmo = false)
    // {
    //     if (weaponController == null) return;
    //     var data = weaponController.CurrentWeapon;
    //     if (data == null) return;

    //     maxBulletCount = Mathf.Max(0, data.magazineSize);
    //     attackCooldown = data.fireCooldown;
    //     reloadTime = data.reloadTime;

    //     if (forceResetAmmo)
    //     {
    //         currentBulletCount = maxBulletCount;
    //     }
    //     else
    //     {
    //         currentBulletCount = Mathf.Clamp(currentBulletCount, 0, maxBulletCount);
    //         if (currentBulletCount == 0)
    //         {
    //             currentBulletCount = maxBulletCount;
    //         }
    //     }
    // }

    // [주석 처리됨] 이 메서드들은 WeaponController로 이동했습니다.
    // private void TryEquipWeaponSlot(int slotIndex)
    // {
    //     if (weaponController == null) return;
    //     if (weaponController.EquipWeaponByIndex(slotIndex))
    //     {
    //         SyncWeaponStatsFromData(forceResetAmmo: true);
    //         UpdateWeaponIconSprite();
    //         UpdateWeaponIconTransform(true);
    //     }
    // }

    // private void UpdateWeaponIconSprite()
    // {
    //     if (weaponIconRenderer == null) return;

    //     if (weaponController == null)
    //     {
    //         weaponIconRenderer.sprite = null;
    //         weaponIconRenderer.enabled = false;
    //         return;
    //     }

    //     var data = weaponController.CurrentWeapon;
    //     weaponIconRenderer.sprite = data != null ? data.icon : null;
    //     weaponIconRenderer.enabled = weaponIconRenderer.sprite != null;
    // }

    // private void UpdateWeaponIconTransform(bool snapImmediate = false)
    // {
    //     if (weaponIconRenderer == null || !weaponIconRenderer.enabled) return;

    //     Vector3 playerPos = transform.position;
    //     Vector3 direction = Vector3.right;

    //     if (Camera.main != null && Mouse.current != null)
    //     {
    //         Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    //         mouseWorld.z = playerPos.z;
    //         direction = (mouseWorld - playerPos);
    //     }

    //     if (direction.sqrMagnitude < 0.0001f)
    //     {
    //         direction = Vector3.right;
    //     }
    //     else
    //     {
    //         direction.Normalize();
    //     }

    //     Vector3 targetPos = playerPos + direction * weaponIconDistance;
    //     Transform iconTransform = weaponIconRenderer.transform;

    //     if (snapImmediate)
    //     {
    //         iconTransform.position = targetPos;
    //     }
    //     else
    //     {
    //         iconTransform.position = Vector3.Lerp(iconTransform.position, targetPos, weaponIconFollowSpeed * Time.deltaTime);
    //     }

    //     float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    //     iconTransform.rotation = Quaternion.Euler(0f, 0f, angle + weaponIconRotationOffset);

    //     // 왼쪽에 있을 경우 상하 반전
    //     weaponIconRenderer.flipY = direction.x < 0f;
    // }
}
