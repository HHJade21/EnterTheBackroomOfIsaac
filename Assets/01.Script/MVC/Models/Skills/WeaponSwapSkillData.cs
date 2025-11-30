using UnityEngine;

/// <summary>
/// 무기 교체 스킬 데이터의 추상 기본 클래스
/// - 각 스킬은 이 클래스를 상속받아 독립적으로 구현합니다.
/// - ScriptableObject로 데이터와 로직을 함께 관리합니다.
/// - Execute() 메소드는 반드시 구현해야 합니다 (추상 메소드).
/// </summary>
public abstract class WeaponSwapSkillData : ScriptableObject
{
    /// <summary>
    /// 스킬 실행 메소드: 무기 교체 시 호출됩니다.
    /// 각 스킬 클래스에서 반드시 이 메소드를 구현해야 합니다.
    /// </summary>
    /// <param name="weaponController">무기 컨트롤러 참조</param>
    /// <param name="playerController">플레이어 컨트롤러 참조</param>
    public abstract void Execute(WeaponController weaponController, PlayerController playerController);
}

