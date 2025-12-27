using UnityEngine;

// Enemy_Venus: 플레이어를 직접 공격하지 않고, 공격 주기마다 플레이어 근처에 Enemy_Vine을 소환하는 정지형 적
// - 이동하지 않음
// - 공격 주기마다 플레이어 근처 위치에 Enemy_Vine 소환
// - 피격과 사망 방식은 EnemyController와 동일
public class Enemy_Venus : EnemyController
{
    [Header("Venus Specific")]
    [Tooltip("소환할 Enemy_Vine 프리팹 (인스펙터에서 할당)")]
    [SerializeField] private GameObject vinePrefab;
    
    [Tooltip("플레이어로부터 소환 위치까지의 최소 거리")]
    [SerializeField] private float minSpawnDistance = 2f;
    
    [Tooltip("플레이어로부터 소환 위치까지의 최대 거리")]
    [SerializeField] private float maxSpawnDistance = 5f;
    
    [Tooltip("소환 위치를 찾기 위한 최대 시도 횟수")]
    [SerializeField] private int maxSpawnAttempts = 10;

    protected override void FixedUpdate()
    {
        if (enemyData == null || target == null || rb == null || isDead) return;

        Vector2 toTarget = target.position - transform.position;
        float sqrDistance = toTarget.sqrMagnitude;

        // 범위 밖이면 아무것도 하지 않음
        if (sqrDistance > enemyData.detectionRange * enemyData.detectionRange)
        {
            rb.linearVelocity = Vector2.zero;
            if (animator != null)
            {
                animator.SetBool("Move", false);
            }
            return;
        }

        // 이동하지 않음 - 속도를 0으로 유지
        rb.linearVelocity = Vector2.zero;
        if (animator != null)
        {
            animator.SetBool("Move", false);
        }

        // 공격 범위에 들어오면 소환 시도 (공격 대신)
        if (sqrDistance <= enemyData.attackRange * enemyData.attackRange)
        {
            Vector2 direction = toTarget.normalized;
            TrySpawnVine(direction);
        }
    }

    /// <summary>
    /// 공격 시도 메소드 오버라이드: Enemy_Venus는 직접 공격하지 않음
    /// </summary>
    /// <param name="direction">공격 방향 (사용하지 않음)</param>
    protected override void TryAttack(Vector2 direction)
    {
        // Enemy_Venus는 직접 플레이어를 공격하지 않음
        // 소환은 TrySpawnVine에서 처리됨
    }

    /// <summary>
    /// 플레이어 근처 위치에 Enemy_Vine을 소환합니다.
    /// </summary>
    /// <param name="directionToPlayer">플레이어 방향 (사용하지 않지만 TryAttack과 호환성을 위해 유지)</param>
    private void TrySpawnVine(Vector2 directionToPlayer)
    {
        if (enemyData == null) return;
        if (Time.time < lastAttackTime + enemyData.attackCooldown) return;
        if (vinePrefab == null)
        {
            Debug.LogWarning("Enemy_Venus: vinePrefab이 할당되지 않았습니다.");
            return;
        }

        lastAttackTime = Time.time;

        // 플레이어 근처에 적절한 소환 위치 찾기
        Vector2 spawnPosition = FindSpawnPosition();
        
        // Enemy_Vine 인스턴스 생성
        GameObject vineInstance = Instantiate(vinePrefab, spawnPosition, Quaternion.identity);
        
        // EnemyController 컴포넌트가 있다면 roomController 설정
        EnemyController vineController = vineInstance.GetComponent<EnemyController>();
        if (vineController != null && roomController != null)
        {
            vineController.roomController = roomController;
            // RoomController의 enemies 리스트에 추가
            if (roomController.enemies != null)
            {
                roomController.enemies.Add(vineInstance);
            }
        }
    }

    /// <summary>
    /// 플레이어 근처에 적절한 소환 위치를 찾습니다.
    /// </summary>
    /// <returns>소환 위치 (플레이어 근처, 벽과 겹치지 않는 위치)</returns>
    private Vector2 FindSpawnPosition()
    {
        if (target == null) return transform.position;

        Vector2 playerPosition = target.position;
        
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            // 플레이어 주변 랜덤 각도와 거리로 위치 생성
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
            
            Vector2 candidatePosition = playerPosition + new Vector2(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance
            );

            // 벽과 겹치지 않는지 확인 (간단한 Raycast 체크)
            Collider2D wallCheck = Physics2D.OverlapCircle(candidatePosition, 0.5f, LayerMask.GetMask("Wall"));
            if (wallCheck == null)
            {
                return candidatePosition;
            }
        }

        // 모든 시도가 실패하면 플레이어 위치에서 약간 떨어진 기본 위치 반환
        Vector2 fallbackPosition = playerPosition + Random.insideUnitCircle.normalized * minSpawnDistance;
        return fallbackPosition;
    }
}

