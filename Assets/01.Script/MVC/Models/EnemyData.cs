using UnityEngine;

// ScriptableObject that defines shared enemy parameters
// - Stores stats so multiple enemy instances can reuse the same configuration
// - Keeps tuning data outside of prefabs for easy balancing
[CreateAssetMenu(fileName = "EnemyData", menuName = "EnterTheBackroomOfIsaac/Data/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("Meta")]
    public string enemyName = "Enemy";   // 디버깅 및 UI 표시용 이름

    [Header("Stats")]
    public int maxHp = 10;               // 최대 체력
    public float moveSpeed = 2f;         // 이동 속도
    public int contactDamage = 1;        // 플레이어와 충돌 시 입히는 피해
    public float attackCooldown = 1.5f;  // 공격 쿨다운
    public bool isRanged = false;        // 원거리 공격 여부

    [Header("Detection")]
    public float detectionRange = 6f;    // 플레이어 추적 시작 거리
    public float attackRange = 2f;       // 공격 가능 거리 (근접/원거리 모두 사용)

    [Header("Ranged Attack")]
    public GameObject projectilePrefab;  // 원거리 투사체 프리팹
    public float projectileSpeed = 7f;   // 투사체 속도
    public float projectileLifetime = 2f;// 투사체 생존 시간
}

