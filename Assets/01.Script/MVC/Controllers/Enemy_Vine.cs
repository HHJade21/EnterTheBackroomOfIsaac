using UnityEngine;
using System.Collections;

// Enemy_Vine: Enemy_Venus에 의해 소환되는 근접공격형 몬스터
// - 생성 시 Awake 애니메이션 재생 후 움직임 시작
// - 공격 시 애니메이터의 "Attack" 트리거 설정
public class Enemy_Vine : EnemyController
{
    private bool isAwakeAnimationFinished = false;
    private const string AWAKE_STATE_NAME = "Awake";
    private const string ATTACK_TRIGGER_NAME = "Attack";

    protected override void Start()
    {
        // 부모 클래스의 Start 호출
        base.Start();
        
        // Awake 애니메이션 종료 감지 시작
        StartCoroutine(WaitForAwakeAnimation());
    }

    /// <summary>
    /// Awake 애니메이션이 끝날 때까지 대기하는 코루틴
    /// </summary>
    private IEnumerator WaitForAwakeAnimation()
    {
        if (animator == null)
        {
            Debug.LogWarning("Enemy_Vine: Animator가 할당되지 않았습니다.");
            isAwakeAnimationFinished = true;
            yield break;
        }

        // Awake 상태로 전환될 때까지 대기
        yield return null;
        
        // Awake 애니메이션이 재생 중인지 확인
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // Awake 상태가 아니면 이미 Idle 상태로 전환된 것이므로 바로 활성화
        if (!stateInfo.IsName(AWAKE_STATE_NAME))
        {
            isAwakeAnimationFinished = true;
            yield break;
        }

        // Awake 애니메이션이 끝날 때까지 대기
        while (stateInfo.IsName(AWAKE_STATE_NAME) && stateInfo.normalizedTime < 1.0f)
        {
            yield return null;
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        }

        // Awake 애니메이션 종료
        isAwakeAnimationFinished = true;
    }

    protected override void FixedUpdate()
    {
        // Awake 애니메이션이 끝나지 않았으면 움직이지 않음
        if (!isAwakeAnimationFinished)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }

        // Awake 애니메이션이 끝났으면 이동 및 공격 로직 실행 (Move 파라미터 제외)
        if (enemyData == null || target == null || rb == null || isDead) return;

        Vector2 toTarget = target.position - transform.position;
        float sqrDistance = toTarget.sqrMagnitude;

        // 범위 밖이면 추적하지 않음
        if (sqrDistance > enemyData.detectionRange * enemyData.detectionRange)
        {
            // 추적 범위 밖이면 정지
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // 플레이어와 충돌 중이 아닐 때만 이동
        if (!isCollidingWithPlayer && sqrDistance > enemyData.attackRange * enemyData.attackRange)
        {
            // 이동: MovePosition을 사용하여 벽 충돌 감지
            Vector2 direction = toTarget.normalized;
            Vector2 moveDelta = direction * enemyData.moveSpeed * Time.fixedDeltaTime;
            if(moveDelta.x > 0)
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

        // 공격 범위에 들어오면 공격 시도
        if (sqrDistance <= enemyData.attackRange * enemyData.attackRange)
        {
            Vector2 direction = toTarget.normalized;
            TryAttack(direction);
        }
    }

    /// <summary>
    /// 공격 시도 메소드 오버라이드: 공격 시 "Attack" 트리거 설정
    /// </summary>
    /// <param name="direction">공격 방향</param>
    protected override void TryAttack(Vector2 direction)
    {
        if (enemyData == null) return;
        if (Time.time < lastAttackTime + enemyData.attackCooldown) return;

        lastAttackTime = Time.time;

        // 애니메이터에 "Attack" 트리거 설정
        if (animator != null)
        {
            animator.SetTrigger(ATTACK_TRIGGER_NAME);
        }

        // 근접 공격: 플레이어에게 데미지 적용
        if (target != null)
        {
            PlayerController playerController = target.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // 적의 속성으로 플레이어에게 데미지 적용
                playerController.TakeDamage(Mathf.RoundToInt(enemyData.contactDamage), element);
            }
        }
    }
}

