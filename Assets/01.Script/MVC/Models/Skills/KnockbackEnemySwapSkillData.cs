using UnityEngine;

/// <summary>
/// 적 넉백 교체 스킬: 주변 5f 거리 안에 있는 모든 적을 넉백시킵니다.
/// </summary>
[CreateAssetMenu(fileName = "KnockbackEnemySwapSkill", menuName = "EnterTheBackroomOfIsaac/Skill/KnockbackEnemy")]
public class KnockbackEnemySwapSkillData : WeaponSwapSkillData
{
    [Header("Knockback Settings")]
    [Tooltip("넉백 범위")]
    public float range = 5f;
    
    [Tooltip("넉백 힘")]
    public float knockbackForce = 10f;

    /// <summary>
    /// 적 넉백 스킬 실행: 주변 범위 안에 있는 모든 적을 넉백시킵니다.
    /// </summary>
    public override void Execute(WeaponController weaponController, PlayerController playerController)
    {
        if (playerController == null) return;

        Vector2 playerPos = playerController.transform.position;

        // 모든 "Enemy" 태그를 가진 오브젝트 찾기
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");

        int knockedBackCount = 0;

        foreach (GameObject enemyObj in allEnemies)
        {
            if (enemyObj == null) continue;

            // 플레이어로부터의 거리 계산
            Vector2 enemyPos = enemyObj.transform.position;
            float distance = Vector2.Distance(playerPos, enemyPos);

            // 범위 안에 있는 적만 넉백
            if (distance <= range)
            {
                // 넉백 방향 계산 (플레이어에서 적으로 가는 방향의 반대, 즉 적을 밀어내는 방향)
                Vector2 knockbackDirection = (enemyPos - playerPos).normalized;
                
                // Rigidbody2D를 통해 넉백 적용
                Rigidbody2D enemyRb = enemyObj.GetComponent<Rigidbody2D>();
                if (enemyRb != null)
                {
                    enemyRb.linearVelocity = knockbackDirection * knockbackForce;
                    knockedBackCount++;
                }
            }
        }

        Debug.Log($"KnockbackEnemySwapSkill: {knockedBackCount}개의 적을 넉백시켰습니다.");
    }
}

