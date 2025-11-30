using UnityEngine;

/// <summary>
/// 타겟 샷 교체 스킬: 가장 가까운 적에게 데미지를 입힙니다.
/// </summary>
[CreateAssetMenu(fileName = "TargetShotSwapSkill", menuName = "EnterTheBackroomOfIsaac/Skill/TargetShot")]
public class TargetShotSwapSkillData : WeaponSwapSkillData
{
    [Header("Target Shot Settings")]
    [Tooltip("적에게 입히는 데미지")]
    public int damage = 10;
    [Tooltip("최대 탐지 거리 (0이면 무제한)")]
    public float maxRange = 0f; // 0이면 무제한
    [Tooltip("적 레이어 마스크")]
    public LayerMask enemyLayer = 1 << 6; // 기본값: Layer 6 (Enemy)

    /// <summary>
    /// 타겟 샷 스킬 실행: 가장 가까운 적을 찾아 데미지를 입힙니다.
    /// </summary>
    public override void Execute(WeaponController weaponController, PlayerController playerController)
    {
        if (playerController == null) return;

        Vector2 playerPos = playerController.transform.position;

        // 가장 가까운 적 찾기
        EnemyController closestEnemy = FindClosestEnemy(playerPos);

        if (closestEnemy != null)
        {
            // 데미지 적용
            closestEnemy.ApplyDamage(damage);
            Debug.Log($"TargetShotSwapSkill: 가장 가까운 적에게 {damage} 데미지 적용");
        }
        else
        {
            Debug.Log("TargetShotSwapSkill: 주변에 적이 없습니다.");
        }
    }

    /// <summary>
    /// 가장 가까운 적을 찾는 헬퍼 메소드
    /// </summary>
    /// <param name="fromPosition">시작 위치</param>
    /// <returns>가장 가까운 적의 EnemyController, 없으면 null</returns>
    private EnemyController FindClosestEnemy(Vector2 fromPosition)
    {
        EnemyController closestEnemy = null;
        float closestDistance = maxRange > 0f ? maxRange : float.MaxValue;

        // 씬의 모든 EnemyController 찾기
        EnemyController[] allEnemies = FindObjectsOfType<EnemyController>();

        foreach (EnemyController enemy in allEnemies)
        {
            if (enemy == null) continue;

            // 레이어 마스크 확인
            if (enemyLayer != 0 && ((1 << enemy.gameObject.layer) & enemyLayer) == 0)
            {
                continue;
            }

            float distance = Vector2.Distance(fromPosition, enemy.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }
}

