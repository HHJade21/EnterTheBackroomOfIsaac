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
    
    [Tooltip("원형 발사 시 투사체 개수")]
    [SerializeField] private int projectileCount = 6;
    
    // 상태 변수
    private bool isDiving = false; // 현재 Diving 상태인지 여부
    private Vector2 diveDirection = Vector2.zero; // Diving 이동 방향
    private float diveStartTime = 0f; // Diving 시작 시간
    private float targetDiveDistance = 0f; // 목표 Diving 거리 (플레이어로부터)
    private Vector2 diveStartPosition = Vector2.zero; // Diving 시작 위치
    private bool isInAttackSequence = false; // 공격 시퀀스 중인지 여부
    
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
        
        // 현재 플레이어로부터의 거리 계산
        Vector2 toPlayer = (Vector2)target.position - (Vector2)transform.position;
        float currentDistanceFromPlayer = toPlayer.magnitude;
        
        // 목표 거리 이상 멀어졌고, 아직 시간이 남았다면 랜덤 방향으로 변경
        if (currentDistanceFromPlayer >= targetDiveDistance && elapsedTime < diveDuration)
        {
            // 랜덤 방향으로 변경
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            diveDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
        }
        
        // Diving 이동 계속
        if(animator.GetBool("Move"))
        {
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
        
        // 플레이어에게서 멀어지는 방향 계산
        Vector2 toPlayer = (Vector2)target.position - (Vector2)transform.position;
        diveDirection = -toPlayer.normalized;
        
        // 목표 거리 설정 (플레이어로부터의 거리, 1/3로 줄임)
        targetDiveDistance = Random.Range(diveMinDistance, diveMaxDistance);
        

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
    /// Jump 애니메이션과 함께 원형으로 투사체를 발사합니다.
    /// 이 메소드는 애니메이션 이벤트에서 호출됩니다.
    /// </summary>
    public void OnJumpAttack()
    {
        if (enemyData == null || enemyData.projectilePrefab == null) return;
        
        // 플레이어 방향 계산
        Vector2 toPlayer = target != null 
            ? ((Vector2)target.position - (Vector2)transform.position).normalized 
            : Vector2.up;
        
        // 원형으로 투사체 발사
        float angleStep = 360f / projectileCount;
        
        // 플레이어 방향의 각도 계산
        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        
        for (int i = 0; i < projectileCount; i++)
        {
            // 각 투사체의 각도 계산 (플레이어 방향을 기준으로 원형 배치)
            float angle = (baseAngle + i * angleStep) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            
            // 투사체 생성
            GameObject projectile = Instantiate(enemyData.projectilePrefab, transform.position, Quaternion.identity);
            
            Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
            if (projectileRb != null)
            {
                projectileRb.linearVelocity = direction * enemyData.projectileSpeed;
            }
            
            Destroy(projectile, enemyData.projectileLifetime);
        }
        
        // 공격 후 딜레이 시작
        StartCoroutine(PostAttackDelayRoutine());
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

