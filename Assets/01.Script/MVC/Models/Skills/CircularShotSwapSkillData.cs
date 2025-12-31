using System.Collections;
using UnityEngine;

/// <summary>
/// 원형 탄막 교체 스킬: 플레이어의 360도 전방위로 원형 탄막을 2회 발사합니다.
/// </summary>
[CreateAssetMenu(fileName = "CircularShotSwapSkill", menuName = "EnterTheBackroomOfIsaac/Skill/CircularShot")]
public class CircularShotSwapSkillData : WeaponSwapSkillData
{
    [Header("Circular Shot Settings")]
    [Tooltip("발사 횟수")]
    public int fireCount = 2;
    [Tooltip("각 발사 사이의 딜레이 (초)")]
    public float fireDelay = 0.2f;
    [Tooltip("탄막 각도 간격 (도)")]
    public float angleInterval = 15f;
    [Tooltip("발사 위치 오프셋 (플레이어 중심에서의 상대 위치)")]
    public Vector3 fireOffset = Vector3.zero;

    /// <summary>
    /// 원형 탄막 스킬 실행: 360도 전방위로 탄막을 2회 발사합니다.
    /// </summary>
    public override void Execute(WeaponController weaponController, PlayerController playerController)
    {
        if (weaponController == null || playerController == null) return;
        if (weaponController.CurrentWeapon == null) return;
        if (weaponController.CurrentWeapon.projectilePrefab == null) return;

        // 코루틴 시작
        playerController.StartCoroutine(CircularShotRoutine(weaponController, playerController));
    }

    /// <summary>
    /// 원형 탄막 발사 코루틴
    /// </summary>
    private IEnumerator CircularShotRoutine(WeaponController weaponController, PlayerController playerController)
    {
        WeaponData currentWeapon = weaponController.CurrentWeapon;
        
        // fireCount 횟수만큼 발사
        for (int i = 0; i < fireCount; i++)
        {
            // 플레이어 위치 + 오프셋
            Vector3 firePosition = playerController.transform.position + fireOffset;
            
            // 360도 원형 탄막 발사
            FireCircularBullets(currentWeapon, firePosition, weaponController);
            
            // 마지막 발사가 아니면 딜레이
            if (i < fireCount - 1)
            {
                yield return new WaitForSeconds(fireDelay);
            }
        }
    }

    /// <summary>
    /// 360도 원형 탄막 발사
    /// </summary>
    private void FireCircularBullets(WeaponData weaponData, Vector3 firePosition, WeaponController weaponController)
    {
        // 360도 범위를 angleInterval 간격으로 순회하며 발사
        for (float angle = 0f; angle < 360f; angle += angleInterval)
        {
            FireBulletAtAngle(weaponData, firePosition, angle, weaponController);
        }
    }

    /// <summary>
    /// 특정 각도로 투사체를 발사하는 헬퍼 메서드
    /// </summary>
    private void FireBulletAtAngle(WeaponData weaponData, Vector3 firePosition, float angleDegrees, WeaponController weaponController)
    {
        if (weaponData.projectilePrefab == null) return;

        // 각도를 라디안으로 변환
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));

        // 투사체 생성
        GameObject projectile = Instantiate(weaponData.projectilePrefab, firePosition, Quaternion.identity);

        // 투사체 회전 설정
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, angleDegrees);

        // 투사체 속성 설정
        BulletController bulletController = projectile.GetComponent<BulletController>();
        if (bulletController != null)
        {
            bulletController.weaponElement = weaponData.element;
        }

        // 투사체 속도 설정
        Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
        if (projectileRb != null)
        {
            float projectileSpeed = weaponData.projectileSpeed * weaponController.projectileSpeedMultiplier;
            projectileRb.linearVelocity = direction * projectileSpeed;
        }

        // 투사체 생존 시간 설정
        Destroy(projectile, weaponData.projectileLifetime);
    }
}

