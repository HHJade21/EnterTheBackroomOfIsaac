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

    [Header("Collision Settings")]
    [Tooltip("플레이어와 충돌 시 밀려나는 속도 배율 (낮을수록 천천히 밀림)")]
    [SerializeField] private float pushBackSpeed = 0.3f; // 플레이어가 움직일 때 밀려나는 속도

    private float lastAttackTime;
    private Rigidbody2D rb;
    private bool isCollidingWithPlayer = false; // 플레이어와 충돌 중인지 여부

    private void Awake()
    {
        // Rigidbody2D 설정: 벽 충돌 감지를 위해 kinematic = false로 설정
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = false; // 물리 충돌 감지 가능
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 회전 방지
            rb.linearDamping = 10f; // 자연스럽게 멈추도록 높은 마찰력 설정
            rb.gravityScale = 0f; // 중력 비활성화
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

    private void FixedUpdate()
    {
        if (enemyData == null || target == null || rb == null) return;

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
        if (!isCollidingWithPlayer)
        {
            // 이동: MovePosition을 사용하여 벽 충돌 감지
            Vector2 direction = toTarget.normalized;
            Vector2 moveDelta = direction * enemyData.moveSpeed * Time.fixedDeltaTime;
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

    /// <summary>
    /// 플레이어 총알과의 충돌 감지 메소드: "Bullet_Player" 태그를 가진 오브젝트와 충돌 시 데미지를 받습니다.
    /// </summary>
    /// <param name="other">충돌한 오브젝트의 Collider2D</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어 총알 태그 확인
        if (other.CompareTag("Bullet_Player"))
        {
            // 데미지 1 적용
            ApplyDamage(1);

            // 총알 파괴
            Destroy(other.gameObject);
        }
    }

    /// <summary>
    /// 플레이어와의 충돌 시작 감지: 플레이어와 충돌 시 자연스럽게 멈추도록 처리합니다.
    /// </summary>
    /// <param name="collision">충돌 정보</param>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isCollidingWithPlayer = true;
        }
    }

    /// <summary>
    /// 플레이어와의 충돌 지속 감지: 플레이어가 움직일 경우 천천히 밀려나도록 처리합니다.
    /// </summary>
    /// <param name="collision">충돌 정보</param>
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 플레이어의 현재 속도 확인
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null && playerRb.linearVelocity.magnitude > 0.1f)
            {
                // 플레이어가 움직이고 있으면 천천히 밀려남
                Vector2 pushDirection = (transform.position - collision.transform.position).normalized;
                rb.linearVelocity = playerRb.linearVelocity * pushBackSpeed;
            }
            else
            {
                // 플레이어가 정지해 있으면 자연스럽게 멈춤
                rb.linearVelocity *= 0.9f;
            }
        }
    }

    /// <summary>
    /// 플레이어와의 충돌 종료 감지: 충돌이 끝나면 다시 이동 가능하도록 처리합니다.
    /// </summary>
    /// <param name="collision">충돌 정보</param>
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isCollidingWithPlayer = false;
        }
    }

    /// <summary>
    /// 데미지 적용 메소드: 체력을 감소시키고, 체력이 0 이하가 되면 사망 처리합니다.
    /// </summary>
    /// <param name="amount">받을 데미지량</param>
    public void ApplyDamage(int amount)
    {
        currentHp -= amount;
        if (currentHp <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 사망 처리 메소드: 적 오브젝트를 파괴합니다.
    /// </summary>
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


