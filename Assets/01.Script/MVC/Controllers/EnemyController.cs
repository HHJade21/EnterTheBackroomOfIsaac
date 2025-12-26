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
    private WeaponData.WeaponElement element;        // 적 속성

    [Header("Collision Settings")]
    [Tooltip("플레이어와 충돌 시 밀려나는 속도 배율 (낮을수록 천천히 밀림)")]
    [SerializeField] private float pushBackSpeed = 0.3f; // 플레이어가 움직일 때 밀려나는 속도

    [Tooltip("이 적이 속한 방의 RoomController")]
    public RoomController roomController;

    private float lastAttackTime;
    private Rigidbody2D rb;
    private bool isCollidingWithPlayer = false; // 플레이어와 충돌 중인지 여부
    private Animator animator;
    private Collider2D collider;
    private SpriteRenderer spriteRenderer;
    private bool isDead = false;

    private bool isStatue = false;
    private bool isStatueMoving = false;
    private Vector2 statueMoveDirection = Vector2.zero;

    private void Awake()
    {
        // Rigidbody2D 설정: 벽 충돌 감지를 위해 kinematic = false로 설정
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        collider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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
            element = enemyData.element;
            if (enemyData.enemyCategory == "Statue")
            {
                isStatue = true;
            }
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
                if(isStatue)
                {
                    StartCoroutine(StatueMoveRoutine());
                }
            }
        }
    }

    System.Collections.IEnumerator StatueMoveRoutine()
    {
        while (!isDead)
        {
            isStatueMoving = true;
            statueMoveDirection = (target.position - transform.position).normalized;
            yield return new WaitForSeconds(0.5f);
            isStatueMoving = false;
            animator.SetBool("Move", false);
            yield return new WaitForSeconds(Random.Range(2.5f, 3.5f));
        }
    }

    private void FixedUpdate()
    {
        if (enemyData == null || target == null || rb == null || isDead) return;

        Vector2 toTarget = target.position - transform.position;
        float sqrDistance = toTarget.sqrMagnitude;

        // 범위 밖이면 추적하지 않음
        if (sqrDistance > enemyData.detectionRange * enemyData.detectionRange)
        {
            // 추적 범위 밖이면 정지
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("Move", false);
            return;
        }

        // 플레이어와 충돌 중이 아닐 때만 이동
        if (!isCollidingWithPlayer && sqrDistance > enemyData.attackRange * enemyData.attackRange && !isStatue)
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
            animator.SetBool("Move", true);
        }
        else if (isStatue && isStatueMoving)
        {
            Vector2 moveDelta = statueMoveDirection * enemyData.moveSpeed * Time.fixedDeltaTime;
            if(moveDelta.x > 0)
            {
                spriteRenderer.flipX = true;
            }
            else
            {
                spriteRenderer.flipX = false;
            }
            rb.MovePosition(rb.position + moveDelta);
            animator.SetBool("Move", true);
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

    /// <summary>
    /// 플레이어 총알과의 충돌 감지 메소드: "Bullet_Player" 태그를 가진 오브젝트와 충돌 시 데미지를 받습니다.
    /// </summary>
    /// <param name="other">충돌한 오브젝트의 Collider2D</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
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
    /// <param name="attackerElement">공격자의 속성 (상성 계산용)</param>
    public void ApplyDamage(int amount, WeaponData.WeaponElement attackerElement = WeaponData.WeaponElement.Cyan)
    {
        // 상성 관계에 따른 데미지 배율 계산
        float damageMultiplier = CalculateElementMultiplier(attackerElement, element);
        int finalDamage = Mathf.RoundToInt(amount * damageMultiplier);
        
        currentHp -= finalDamage;
        if (currentHp <= 0)
        {
            StartCoroutine(DeathRoutine());
        }
    }
    
    /// <summary>
    /// 상성 관계에 따른 데미지 배율을 계산합니다.
    /// </summary>
    /// <param name="attackerElement">공격자의 속성</param>
    /// <param name="defenderElement">방어자의 속성</param>
    /// <returns>데미지 배율 (1.5배: 약점, 0.5배: 약함, 1.0배: 일반)</returns>
    private float CalculateElementMultiplier(WeaponData.WeaponElement attackerElement, WeaponData.WeaponElement defenderElement)
    {
        // Key 속성은 예외: 어떤 속성에도 강하지 않고 약하지 않음 (항상 1.0배)
        if (attackerElement == WeaponData.WeaponElement.Key || defenderElement == WeaponData.WeaponElement.Key)
        {
            return 1.0f;
        }
        
        // 상성 관계: Cyan -> Magenta -> Yellow -> Cyan
        // 공격자의 다음 속성이 방어자와 같으면 약점 (1.5배)
        // 방어자의 다음 속성이 공격자와 같으면 약함 (0.5배)
        
        int attackerValue = (int)attackerElement;
        int defenderValue = (int)defenderElement;
        
        // 공격자의 다음 속성 계산 (Cyan(1) -> Magenta(2) -> Yellow(3) -> Cyan(1))
        int attackerNext = ((attackerValue - 1 + 1) % 3) + 1; // -1로 0-based로 변환, +1로 다음, %3로 순환, +1로 다시 1-based
        
        // 방어자의 다음 속성 계산
        int defenderNext = ((defenderValue - 1 + 1) % 3) + 1;
        
        // 약점: 공격자의 다음 속성이 방어자와 같으면 1.5배
        if (attackerNext == defenderValue)
        {
            return 1.5f;
        }
        
        // 약함: 방어자의 다음 속성이 공격자와 같으면 0.5배
        if (defenderNext == attackerValue)
        {
            return 0.5f;
        }
        
        // 그 외는 1.0배
        return 1.0f;
    }

    /// <summary>
    /// 사망 처리 메소드: 적 오브젝트를 파괴합니다.
    /// </summary>
    System.Collections.IEnumerator DeathRoutine()
    {
        // TODO: 사망 이펙트, 드랍, 이벤트 호출 등 구현
        isDead = true;
        gameObject.tag = "Corpse";
        roomController.OnEnemyDeath(this);
        collider.enabled = false;
        animator.SetTrigger("Death");
        yield return new WaitForSeconds(3f);
        for(int i = 0; i < 100; i++)
        {
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 1f - i * 0.01f);
            yield return new WaitForSeconds(0.02f);
        }
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


