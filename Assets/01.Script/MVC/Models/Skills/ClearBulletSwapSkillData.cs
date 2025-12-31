using UnityEngine;

/// <summary>
/// 투사체 제거 교체 스킬: 주변 10f 거리 안에 있는 모든 적 투사체를 파괴합니다.
/// </summary>
[CreateAssetMenu(fileName = "ClearBulletSwapSkill", menuName = "EnterTheBackroomOfIsaac/Skill/ClearBullet")]
public class ClearBulletSwapSkillData : WeaponSwapSkillData
{
    [Header("Clear Bullet Settings")]
    [Tooltip("투사체 제거 범위")]
    public float range = 10f;

    /// <summary>
    /// 투사체 제거 스킬 실행: 주변 범위 안에 있는 모든 적 투사체를 파괴합니다.
    /// </summary>
    public override void Execute(WeaponController weaponController, PlayerController playerController)
    {
        if (playerController == null) return;

        Vector2 playerPos = playerController.transform.position;

        // 모든 "Bullet_Enemy" 태그를 가진 오브젝트 찾기
        GameObject[] allBullets = GameObject.FindGameObjectsWithTag("Bullet_Enemy");

        int destroyedCount = 0;

        foreach (GameObject bullet in allBullets)
        {
            if (bullet == null) continue;

            // 플레이어로부터의 거리 계산
            Vector2 bulletPos = bullet.transform.position;
            float distance = Vector2.Distance(playerPos, bulletPos);

            // 범위 안에 있는 투사체만 파괴
            if (distance <= range)
            {
                Destroy(bullet);
                destroyedCount++;
            }
        }

        Debug.Log($"ClearBulletSwapSkill: {destroyedCount}개의 적 투사체를 파괴했습니다.");
    }
}

