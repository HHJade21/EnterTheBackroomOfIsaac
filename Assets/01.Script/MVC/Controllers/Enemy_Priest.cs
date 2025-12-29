using UnityEngine;
using System.Collections;

// Enemy_Priest: 원거리 몬스터
// - 기본적으로 Standing 상태 (이동하지 않음)
// - 플레이어가 공격 사정거리 밖에 있으면 사정거리까지 걸어감
// - 공격을 하지 않은 채로 2초 이상 있었고, 플레이어가 사정거리 이내에 있다면 공격 시작
// - 공격: 360도 전방위 원형 탄막, 1초에 한 번씩 5초 동안 총 5번 발사
// - 공격이 끝나면 2초 동안 공격하지 않음
public class Enemy_Priest : EnemyController
{
    [Header("Priest Specific")]
    [Tooltip("원형 탄막 발사 시 투사체 개수")]
    [SerializeField] private int projectileCount = 24; // 360도 촘촘하게
    
    [Tooltip("공격 후 대기 시간 (초)")]
    [SerializeField] private float attackCooldown = 2f;
    
    [Tooltip("공격 시작 전 대기 시간 (초)")]
    [SerializeField] private float attackStartDelay = 2f;
    
    [Tooltip("공격 지속 시간 (초)")]
    [SerializeField] private float attackDuration = 5f;
    
    [Tooltip("공격 발사 간격 (초)")]
    [SerializeField] private float attackInterval = 1f;
    
    // 상태 변수
    private bool isAttacking = false; // 현재 공격 중인지 여부
    private float lastAttackEndTime = 0f; // 마지막 공격 종료 시간
    private Coroutine attackCoroutine = null; // 공격 코루틴 참조
    
    protected override void FixedUpdate()
    {
        if (enemyData == null || target == null || rb == null || isDead) return;
        
        Vector2 toTarget = target.position - transform.position;
        float sqrDistance = toTarget.sqrMagnitude;
        float distance = Mathf.Sqrt(sqrDistance);
        
        // 범위 밖이면 추적하지 않음
        if (sqrDistance > enemyData.detectionRange * enemyData.detectionRange)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        // 공격 중이면 이동하지 않음
        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        // 공격 사정거리 체크
        bool isInAttackRange = distance <= enemyData.attackRange;
        
        // 공격 조건 체크: 사정거리 이내이고, 마지막 공격 종료 후 2초 이상 지났고, 공격 시작 전 대기 시간(2초)이 지났으면 공격 시작
        float timeSinceLastAttack = Time.time - lastAttackEndTime;
        bool canAttack = isInAttackRange && 
                        timeSinceLastAttack >= attackCooldown && 
                        timeSinceLastAttack >= attackStartDelay &&
                        !isAttacking;
        
        if (canAttack)
        {
            animator.SetBool("Move", false);
            animator.SetTrigger("StartAttack");
        }
        // 공격 사정거리 밖이면 플레이어 쪽으로 이동
        else if (!isInAttackRange)
        {
            animator.SetBool("Move", true);
            MoveTowardsPlayer(toTarget);
        }
        else
        {
            animator.SetBool("Move", false);
            // 공격 사정거리 이내이지만 공격 조건을 만족하지 않으면 정지
            rb.linearVelocity = Vector2.zero;
        }
    }
    
    /// <summary>
    /// 플레이어 쪽으로 이동합니다.
    /// </summary>
    private void MoveTowardsPlayer(Vector2 toTarget)
    {
        // 플레이어와 충돌 중이 아닐 때만 이동
        if (!isCollidingWithPlayer)
        {
            Vector2 direction = toTarget.normalized;
            Vector2 moveDelta = direction * enemyData.moveSpeed * Time.fixedDeltaTime;
            
            // 스프라이트 방향 설정
            if (moveDelta.x > 0)
            {
                spriteRenderer.flipX = true;
            }
            else
            {
                spriteRenderer.flipX = false;
            }
            
            rb.MovePosition(rb.position + moveDelta);
        }
        else
        {
            // 플레이어와 충돌 중일 때는 속도를 줄여서 자연스럽게 멈춤
            rb.linearVelocity *= 0.9f;
        }
    }
    
    /// <summary>
    /// 공격을 시작합니다.
    /// </summary>
    private void StartAttack()
    {
        if (isAttacking) return;
        
        isAttacking = true;
        
        // 공격 코루틴 시작
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }
        animator.SetBool("Attacking", true);
        attackCoroutine = StartCoroutine(AttackRoutine());
    }
    
    /// <summary>
    /// 공격 루틴: 5초 동안 1초마다 원형 탄막 발사 (총 5번)
    /// </summary>
    private IEnumerator AttackRoutine()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < attackDuration)
        {
            // 원형 탄막 발사
            FireCircularProjectiles();
            
            // 1초 대기
            yield return new WaitForSeconds(attackInterval);
            elapsedTime += attackInterval;
        }
        animator.SetBool("Attacking", false);
    }
    
    /// <summary>
    /// 공격을 종료합니다.
    /// </summary>
    private void EndAttack()
    {
        isAttacking = false;
        lastAttackEndTime = Time.time;
        
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
    }
    
    /// <summary>
    /// 360도 전방위로 촘촘하게 원형 탄막을 발사합니다.
    /// </summary>
    private void FireCircularProjectiles()
    {
        if (enemyData == null || enemyData.projectilePrefab == null) return;
        
        // 360도를 투사체 개수로 나눔
        float angleStep = 360f / projectileCount;
        
        for (int i = 0; i < projectileCount; i++)
        {
            // 각 투사체의 각도 계산
            float angle = i * angleStep * Mathf.Deg2Rad;
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
    }
    
    /// <summary>
    /// 공격 시도 메소드 오버라이드: Enemy_Priest는 자체 공격 로직을 사용합니다.
    /// </summary>
    /// <param name="direction">공격 방향 (사용하지 않음)</param>
    protected override void TryAttack(Vector2 direction)
    {
        // Enemy_Priest는 FixedUpdate에서 자체적으로 공격을 처리하므로 여기서는 아무것도 하지 않음
    }
}
