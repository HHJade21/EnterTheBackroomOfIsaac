using UnityEngine;
using System.Collections;

// Enemy_Siren: 플레이어에게서 멀어지다가 일정 거리 이동 후 원형으로 투사체를 발사하는 원거리 적
// - Diving 상태로 플레이어에게서 멀어짐
// - 일정 거리 이동 후 Jump 애니메이션과 함께 원형으로 6발 발사
// - Jump 후 Standing 애니메이션으로 약 2초 대기
// - Standing 후 Dive → Appear → Moving으로 전환되어 다시 Diving 시작
// - Diving 상태일 때는 플레이어 투사체로부터 데미지를 받지 않음
public class Enemy_Siren : EnemyController
{
    [Header("Siren Specific")]
    [Tooltip("Diving 상태에서 사용할 콜라이더")]
    [SerializeField] private Collider2D divingCollider;
    
    [Tooltip("Jump 상태에서 사용할 콜라이더")]
    [SerializeField] private Collider2D jumpCollider;
    
    [Tooltip("Diving 상태로 이동할 최소 거리 (목표 거리)")]
    [SerializeField] private float diveMinDistance = 2f; // 기존 5f의 1/3
    
    [Tooltip("Diving 상태로 이동할 최대 거리 (목표 거리)")]
    [SerializeField] private float diveMaxDistance = 4f; // 기존 8f의 1/3
    
    [Tooltip("Diving 상태 지속 시간 (초)")]
    [SerializeField] private float diveDuration = 2f;
    
    [Tooltip("발사 후 다음 Diving까지의 딜레이 시간")]
    [SerializeField] private float postAttackDelay = 1f;
    
    [Tooltip("발사할 투사체 개수")]
    [SerializeField] private int projectileCount = 5;
    
    [Tooltip("플레이어 방향 기준 각도 분산 범위 (도 단위)")]
    [SerializeField] private float spreadAngle = 20f;
    
    [Tooltip("각 탄환 발사 간 최소 딜레이 (초)")]
    [SerializeField] private float minFireDelay = 0f;
    
    [Tooltip("각 탄환 발사 간 최대 딜레이 (초)")]
    [SerializeField] private float maxFireDelay = 0.1f;
    
    // 상태 변수
    private bool isDiving = false; // 현재 Diving 상태인지 여부
    private Vector2 diveDirection = Vector2.zero; // Diving 이동 방향
    private float diveStartTime = 0f; // Diving 시작 시간
    private float targetDiveDistance = 0f; // 목표 Diving 거리 (플레이어로부터)
    private Vector2 diveStartPosition = Vector2.zero; // Diving 시작 위치
    private bool isInAttackSequence = false; // 공격 시퀀스 중인지 여부
    private bool isMovingAway = false; // 멀어지는 상태인지 여부 (true: 멀어짐, false: 다가옴)
    private Coroutine distanceCheckCoroutine; // 거리 체크 코루틴 참조
    
    protected override void Start()
    {
        base.Start();
        
        // 콜라이더 초기 설정: diving 콜라이더만 활성화 (초기 상태)
        if (divingCollider != null)
        {
            divingCollider.enabled = false;
        }
        if (jumpCollider != null)
        {
            jumpCollider.enabled = true;
        }
        
        // target이 설정될 때까지 대기 후 Diving 시작
        if (target != null)
        {
            StartDiving();
        }
        else
        {
            // target이 아직 null이면 다음 프레임에 다시 시도
            StartCoroutine(WaitForTargetAndStartDiving());
        }
    }
    
    /// <summary>
    /// target이 설정될 때까지 대기한 후 Diving을 시작합니다.
    /// </summary>
    private IEnumerator WaitForTargetAndStartDiving()
    {
        // target이 설정될 때까지 대기
        while (target == null)
        {
            yield return null;
        }
        
        // target이 설정되면 Diving 시작
        StartDiving();
        
        // 거리 체크 코루틴 시작
        if (distanceCheckCoroutine == null)
        {
            distanceCheckCoroutine = StartCoroutine(DistanceCheckRoutine());
        }
    }
    
    protected override void FixedUpdate()
    {
        if (enemyData == null || target == null || rb == null || isDead) return;
        
        Vector2 toTarget = target.position - transform.position;
        float sqrDistance = toTarget.sqrMagnitude;
        
        // 범위 밖이면 추적하지 않음
        if (sqrDistance > enemyData.detectionRange * enemyData.detectionRange)
        {
            rb.linearVelocity = Vector2.zero;
            if (animator != null)
            {
                animator.SetBool("Move", false);
            }
            return;
        }
        
        // 공격 시퀀스 중이면 이동하지 않음
        if (isInAttackSequence)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        // Diving 상태일 때
        if (isDiving)
        {
            UpdateDivingMovement();
        }
        else
        {
            // Diving이 아닐 때는 일반 이동 (Jump 상태에서 공격 대기)
            rb.linearVelocity = Vector2.zero;
        }
    }
    
    /// <summary>
    /// 0.5초 주기로 플레이어와의 거리를 확인하고 이동 상태를 업데이트하는 코루틴
    /// </summary>
    private IEnumerator DistanceCheckRoutine()
    {
        while (!isDead && target != null)
        {
            // Diving 상태일 때만 거리 체크
            if (isDiving && !isInAttackSequence)
            {
                Vector2 toPlayer = (Vector2)target.position - (Vector2)transform.position;
                float currentDistanceFromPlayer = toPlayer.magnitude;
                
                // 최대 사거리보다 가까우면 '멀어지는' 상태
                // 최대 사거리보다 멀면 '다가오는' 상태
                if (currentDistanceFromPlayer < targetDiveDistance)
                {
                    isMovingAway = true; // 멀어지는 상태
                    diveDirection = -toPlayer.normalized;
                }
                else
                {
                    isMovingAway = false; // 다가오는 상태
                    diveDirection = toPlayer.normalized;
                }
            }
            
            // 0.5초 대기
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    /// <summary>
    /// Diving 상태의 이동을 업데이트합니다.
    /// </summary>
    private void UpdateDivingMovement()
    {
        if (diveDirection == Vector2.zero || target == null) return;
        
        // 경과 시간 확인
        float elapsedTime = Time.time - diveStartTime;
        
        // 2초가 지났으면 Diving 종료
        if (elapsedTime >= diveDuration)
        {
            EndDiving();
            StartAttackSequence();
            return;
        }
        
        // Diving 이동 계속 (방향은 DistanceCheckRoutine에서 업데이트됨)
        Vector2 moveDelta = diveDirection * enemyData.moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + moveDelta);
        // 스프라이트 방향 설정
        if (moveDelta.x > 0)
        {
            spriteRenderer.flipX = true;
        }
        else
        {
            spriteRenderer.flipX = false;
        }
    }
    
    /// <summary>
    /// Diving 상태를 시작합니다.
    /// </summary>
    private void StartDiving()
    {
        if (target == null) return;
        
        isDiving = true;
        diveStartPosition = transform.position;
        diveStartTime = Time.time;
        
        // 목표 거리 설정 (플레이어로부터의 거리)
        targetDiveDistance = Random.Range(diveMinDistance, diveMaxDistance);
        
        // 초기 거리 체크하여 방향 설정
        Vector2 toPlayer = (Vector2)target.position - (Vector2)transform.position;
        float currentDistanceFromPlayer = toPlayer.magnitude;
        
        if (currentDistanceFromPlayer < targetDiveDistance)
        {
            // 현재 거리가 목표 거리보다 가까우면 멀어지는 방향
            isMovingAway = true;
            diveDirection = -toPlayer.normalized;
        }
        else
        {
            // 현재 거리가 목표 거리보다 멀면 다가오는 방향
            isMovingAway = false;
            diveDirection = toPlayer.normalized;
        }
    }

    public void StartMoving()
    {
        // 애니메이션: Moving 상태 유지 (코드에서 자동으로 Moving 상태로 전환됨)
        if (animator != null)
        {
            animator.SetBool("Move", true);
        }
    }
    
    /// <summary>
    /// Diving 상태를 종료합니다.
    /// </summary>
    private void EndDiving()
    {
        isDiving = false;
        
        // 콜라이더 전환: Diving → Jump
        if (divingCollider != null)
        {
            divingCollider.enabled = false;
        }
        if (jumpCollider != null)
        {
            jumpCollider.enabled = true;
        }
    }
    
    /// <summary>
    /// 공격 시퀀스를 시작합니다.
    /// </summary>
    private void StartAttackSequence()
    {
        isInAttackSequence = true;
        
        // 애니메이션 트리거: Moving → Disappear로 전환
        if (animator != null)
        {
            animator.SetBool("Move", false);
            animator.SetTrigger("StartAttack");
        }
    }
    
    /// <summary>
    /// Jump 애니메이션과 함께 플레이어 방향으로 탄환 뭉치를 발사합니다.
    /// 이 메소드는 애니메이션 이벤트에서 호출됩니다.
    /// </summary>
    public void OnJumpAttack()
    {
        if (enemyData == null || enemyData.projectilePrefab == null) return;
        
        // 플레이어 방향으로 탄환 뭉치 발사 코루틴 시작
        StartCoroutine(FireSpreadProjectiles());
        
        // 공격 후 딜레이 시작
        StartCoroutine(PostAttackDelayRoutine());
    }
    
    /// <summary>
    /// 플레이어 방향으로 분산된 탄환 뭉치를 발사하는 코루틴.
    /// 각 탄환은 플레이어 방향 기준 ±20도 범위 내 랜덤 각도로 발사되며,
    /// 발사 시간에 미세한 오차가 있어 무질서한 탄환 덩어리처럼 보입니다.
    /// </summary>
    private IEnumerator FireSpreadProjectiles()
    {
        // 플레이어 방향 계산
        Vector2 toPlayer = target != null 
            ? ((Vector2)target.position - (Vector2)transform.position).normalized 
            : Vector2.up;
        
        // 플레이어 방향의 기본 각도 계산 (라디안)
        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x);
        
        // 각 탄환 발사
        for (int i = 0; i < projectileCount; i++)
        {
            // ±spreadAngle 범위 내 랜덤 각도 계산
            float randomSpread = Random.Range(-spreadAngle, spreadAngle) * Mathf.Deg2Rad;
            float finalAngle = baseAngle + randomSpread;
            
            // 발사 방향 계산
            Vector2 direction = new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));
            
            // 투사체 생성
            GameObject projectile = Instantiate(enemyData.projectilePrefab, transform.position, Quaternion.identity);

            projectile.transform.rotation = Quaternion.Euler(0f, 0f, finalAngle * Mathf.Rad2Deg);
            
            Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
            if (projectileRb != null)
            {
                projectileRb.linearVelocity = direction * enemyData.projectileSpeed;
            }
            
            Destroy(projectile, enemyData.projectileLifetime);
            
            // 다음 탄환 발사까지 랜덤 딜레이 (미세한 오차)
            float delay = Random.Range(minFireDelay, maxFireDelay);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            else
            {
                yield return null; // 최소 딜레이가 0이면 한 프레임만 대기
            }
        }
    }
    
    /// <summary>
    /// 공격 후 딜레이 코루틴: 약 1초 후 공격 시퀀스를 종료합니다.
    /// 실제 Diving 시작은 OnDiveStart() 애니메이션 이벤트에서 처리됩니다.
    /// </summary>
    private IEnumerator PostAttackDelayRoutine()
    {
        yield return new WaitForSeconds(postAttackDelay);
        
        // 공격 시퀀스 종료 (Diving은 OnDiveStart() 애니메이션 이벤트에서 시작됨)
        isInAttackSequence = false;
    }
    
    /// <summary>
    /// Dive 애니메이션이 시작될 때 호출됩니다 (애니메이션 이벤트).
    /// Diving 상태를 활성화하고 콜라이더를 전환합니다.
    /// </summary>
    public void OnDiveStart()
    {
        // Diving 상태 시작 (방향과 시간 설정 포함)
        StartDiving();
        
        // 콜라이더 전환: Jump → Diving
        if (jumpCollider != null)
        {
            jumpCollider.enabled = false;
        }
        if (divingCollider != null)
        {
            divingCollider.enabled = true;
        }
    }
    
    /// <summary>
    /// Jump 애니메이션이 시작될 때 호출됩니다 (애니메이션 이벤트).
    /// Diving 상태를 비활성화하고 콜라이더를 전환합니다.
    /// </summary>
    public void OnJumpStart()
    {
        isDiving = false;
        
        // 콜라이더 전환: Diving → Jump
        if (divingCollider != null)
        {
            divingCollider.enabled = false;
        }
        if (jumpCollider != null)
        {
            jumpCollider.enabled = true;
        }
    }
    
    /// <summary>
    /// 플레이어 총알과의 충돌 감지 메소드: Diving 상태일 때는 데미지를 받지 않습니다.
    /// </summary>
    /// <param name="other">충돌한 오브젝트의 Collider2D</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Diving 상태일 때는 플레이어 투사체로부터 데미지를 받지 않음
        if (isDiving && other.CompareTag("Bullet_Player"))
        {
            return;
        }
        
        // 플레이어 총알 태그 확인
        if (other.CompareTag("Bullet_Player"))
        {
            // 총알의 속성 가져오기
            WeaponData.WeaponElement bulletElement = WeaponData.WeaponElement.Cyan; // 기본값
            BulletController bulletController = other.GetComponent<BulletController>();
            if (bulletController != null)
            {
                bulletElement = bulletController.weaponElement;
            }
            
            // 총알의 데미지 가져오기
            int damage = 1; // 기본값
            if (bulletController != null)
            {
                damage = Mathf.RoundToInt(bulletController.damage);
            }
            animator.SetTrigger("Hit");
            // 데미지 적용 (속성 포함)
            ApplyDamage(damage, bulletElement);
            
            // 총알은 BulletController에서 자동으로 파괴됨
        }
    }
    
    /// <summary>
    /// 공격 시도 메소드 오버라이드: Enemy_Siren은 TryAttack을 사용하지 않고 애니메이션 이벤트로 공격합니다.
    /// </summary>
    /// <param name="direction">공격 방향 (사용하지 않음)</param>
    protected override void TryAttack(Vector2 direction)
    {
        // Enemy_Siren은 TryAttack을 사용하지 않음
        // 공격은 OnJumpAttack()에서 처리됨
    }
}

