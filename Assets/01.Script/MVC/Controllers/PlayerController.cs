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

    private bool isRolling;
    private bool isInvincible;          // 구르는 동안 무적
    private float lastRollTime;
    private Vector2 rollDirection;
    private Vector2 lastMoveDirection;  // 입력이 0일 때도 방향 유지

    // 발사 및 재장전 관련 변수
    private float lastFireTime;         // 마지막 발사 시간
    private bool isReloading;            // 재장전 중 여부

    Rigidbody2D rigid;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void OnMove(InputValue value)
    {
        inputVec = value.Get<Vector2>();
        if (inputVec.sqrMagnitude > 0.0001f)
        {
            lastMoveDirection = inputVec.normalized;
        }
    }

    // Input System에서 "Roll" 액션에 매핑 (PlayerInput Send Messages 모드)
    void OnRoll(InputValue value)
    {
        if (!value.isPressed) return;
        if (isRolling) return;
        if (Time.time < lastRollTime + rollCooldown) return;

        // 입력이 없을 때는 마지막 이동 방향으로 구르기, 없으면 우측 기본
        rollDirection = (inputVec.sqrMagnitude > 0.0001f ? inputVec : (lastMoveDirection.sqrMagnitude > 0 ? lastMoveDirection : Vector2.right)).normalized;
        bool isClockwise;
        if(inputVec.x == 0){
            isClockwise = inputVec.y < 0;
        }else{
            isClockwise = inputVec.x > 0;
        }
        StartCoroutine(RollRoutine(isClockwise));
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
        rigid.MovePosition(rigid.position + nextVec);
    }


    void OnFire(InputValue value)
    {
        // 버튼을 눌렀을 때만 발사 (버튼을 떼면 중단)
        if (!value.isPressed) return;
        
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
    
    // Input System에서 "Reload" 액션에 매핑 (PlayerInput Send Messages 모드)
    void OnReload(InputValue value)
    {
        // 버튼을 눌렀을 때만 재장전 시작
        if (!value.isPressed) return;
        
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
