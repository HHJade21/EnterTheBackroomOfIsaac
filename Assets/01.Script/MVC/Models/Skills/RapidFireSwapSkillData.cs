using System.Collections;
using UnityEngine;

/// <summary>
/// 신속 발사 교체 스킬: 3초 동안 attackCooldown과 reloadTime을 1/4로 감소시킵니다.
/// </summary>
[CreateAssetMenu(fileName = "RapidFireSwapSkill", menuName = "EnterTheBackroomOfIsaac/Skill/RapidFire")]
public class RapidFireSwapSkillData : WeaponSwapSkillData
{
    [Header("Rapid Fire Settings")]
    [Tooltip("버프 지속 시간 (초)")]
    public float duration = 3f;
    [Tooltip("공격 속도 배율 (기본값: 0.25 = 1/4)")]
    public float speedMultiplier = 0.25f;

    /// <summary>
    /// 신속 발사 스킬 실행: 3초 동안 공격 속도와 재장전 속도를 4배 증가시킵니다.
    /// </summary>
    public override void Execute(WeaponController weaponController, PlayerController playerController)
    {
        if (playerController == null) return;

        // PlayerController에서 코루틴 실행
        playerController.StartCoroutine(RapidFireRoutine(playerController, weaponController));
    }

    /// <summary>
    /// 신속 발사 버프 코루틴: 일정 시간 동안 공격 속도를 증가시킵니다.
    /// </summary>
    private IEnumerator RapidFireRoutine(PlayerController playerController, WeaponController weaponController)
    {
        // 원래 배율 저장
        float originalAttackMultiplier = playerController.attackSpeedMultiplier;
        float originalReloadMultiplier = playerController.reloadSpeedMultiplier;

        // 배율 적용 (낮을수록 빠름)
        playerController.attackSpeedMultiplier = speedMultiplier;
        playerController.reloadSpeedMultiplier = speedMultiplier;

        // WeaponController의 스탯 재동기화 (배율 적용)
        weaponController.SyncWeaponStats(forceResetAmmo: false);

        Debug.Log($"RapidFireSwapSkill: 공격 속도 {(1f / speedMultiplier)}배 증가 시작 (지속 시간: {duration}초)");

        // 지속 시간 대기
        yield return new WaitForSeconds(duration);

        // 원래 배율로 복구
        playerController.attackSpeedMultiplier = originalAttackMultiplier;
        playerController.reloadSpeedMultiplier = originalReloadMultiplier;

        // WeaponController의 스탯 재동기화 (원래 배율 적용)
        weaponController.SyncWeaponStats(forceResetAmmo: false);

        Debug.Log("RapidFireSwapSkill: 공격 속도 원래대로 복구");
    }
}

