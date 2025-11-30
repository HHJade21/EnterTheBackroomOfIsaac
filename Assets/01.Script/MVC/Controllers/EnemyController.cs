using UnityEngine;

// Base controller for enemies (melee and ranged)
// Responsibilities:
// - Hold EnemyData ScriptableObject and drive AI behaviour using those stats
// - Move towards player and decide when to attack
// - Trigger projectile spawn for ranged enemies using data-defined prefabs
// - Handle damage and death logic (placeholder for now)
// Extension:
// - Derived classes implement DoAttack() (melee or ranged)
// SOLID:
// - OCP: Extend for new enemy types without modifying base

public class EnemyController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private EnemyData enemyData;    // 공유 가능한 스크립터블 오브젝트

    [Header("Runtime State")]
    [SerializeField] private Transform target;       // 추적할 대상 (보통 Player)
    [SerializeField] private float currentHp;        // 실시간 체력

    private float lastAttackTime;
    private Rigidbody2D rb;

    private void Awake()
    {
        // Rigidbody2D가 있으면 kinematic으로 설정하여 물리 충돌 무시
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = true; // 물리 충돌 무시, Transform으로 직접 이동 가능
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 회전 방지
        }

        if (enemyData != null)
        {
            currentHp = enemyData.maxHp;
        }
    }

    private void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    private void Update()
    {
        if (enemyData == null || target == null) return;

        Vector2 toTarget = target.position - transform.position;
        float sqrDistance = toTarget.sqrMagnitude;

        // 범위 밖이면 추적하지 않음
        if (sqrDistance > enemyData.detectionRange * enemyData.detectionRange)
        {
            return;
        }

        // 이동
        Vector2 direction = toTarget.normalized;
        transform.position += (Vector3)(direction * enemyData.moveSpeed * Time.deltaTime);

        // 공격 범위에 들어오면 공격 시도
        if (sqrDistance <= enemyData.attackRange * enemyData.attackRange)
        {
            TryAttack(direction);
        }
    }

    private void TryAttack(Vector2 direction)
    {
        if (enemyData == null) return;
        if (Time.time < lastAttackTime + enemyData.attackCooldown) return;

        lastAttackTime = Time.time;

        if (enemyData.isRanged && enemyData.projectilePrefab != null)
        {
            var projectile = Instantiate(enemyData.projectilePrefab, transform.position, Quaternion.identity);

        var rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * enemyData.projectileSpeed;
        }

            Destroy(projectile, enemyData.projectileLifetime);
        }
        else
        {
            // TODO: 근접 공격 데미지 적용 (예: PlayerController의 TakeDamage 호출)
        }
    }

    public void ApplyDamage(int amount)
    {
        currentHp -= amount;
        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // TODO: 사망 이펙트, 드랍, 이벤트 호출 등 구현
        Destroy(gameObject);
    }

    #region Configuration Helpers

    public void SetTarget(Transform newTarget) => target = newTarget;

    public void Configure(EnemyData data)
    {
        enemyData = data;
        currentHp = enemyData != null ? enemyData.maxHp : 1f;
    }

    public EnemyData GetEnemyData() => enemyData;

    #endregion
}


