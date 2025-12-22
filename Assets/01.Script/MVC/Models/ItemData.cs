using System.Collections.Generic;
using UnityEngine;

// ScriptableObject that defines passive item configuration
// - Stores information about items that modify player stats when acquired
// - Items spawn as prefabs on the map and can be picked up via interaction
[CreateAssetMenu(fileName = "ItemData", menuName = "EnterTheBackroomOfIsaac/Data/Item")]
public class ItemData : ScriptableObject
{
    /// <summary>
    /// 변경할 스탯 타입
    /// </summary>
    public enum StatType
    {
        MaxHP,
        AttackSpeed,        // 공격 속도
        AttackRange,        // 공격 범위
        AttackDamage,       // 공격 데미지
        ReloadSpeed,        // 재장전 속도
        MoveSpeed,          // 이동 속도
        RollSpeed,          // 구르기 속도
        RollDuration,       // 구르기 지속 시간
        RollCooldown,       // 구르기 쿨다운
        SwapCooldown,       // 무기 교체 쿨다운
        BulletSpeed,        // 탄약 속도
        MaxAmmo,            // 최대 탄약 수
        InvincibilityDuration,  // 무적 시간 배율
    }

    /// <summary>
    /// 스탯 변경 방식
    /// </summary>
    public enum StatModifyType
    {
        Set,        // 값 설정 (기존 값 무시)
        Multiply,   // 값 곱하기 (기존 값에 곱함)
        Add,        // 값 더하기 (기존 값에 더함) - 필요시 사용
    }

    /// <summary>
    /// 스탯 변경 정보를 저장하는 구조체
    /// </summary>
    [System.Serializable]
    public class StatModifier
    {
        [Tooltip("변경할 스탯 타입")]
        public StatType statType = StatType.AttackSpeed;
        
        [Tooltip("변경 방식 (Set: 값 설정, Multiply: 값 곱하기, Add: 값 더하기)")]
        public StatModifyType modifyType = StatModifyType.Multiply;
        
        [Tooltip("변경할 값 (Set의 경우 설정값, Multiply의 경우 곱할 배율, Add의 경우 더할 값)")]
        public float value = 1.0f;
    }

    [Header("Meta")]
    [Tooltip("아이템 고유 번호")]
    public int itemID = 0;
    
    [Tooltip("아이템 이름 (UI 표시용)")]
    public string itemName = "Item";
    
    [Tooltip("아이템 설명 (UI 표시용)")]
    [TextArea(2, 4)]
    public string description = "Item Description";
    
    [Tooltip("아이템 아이콘 (UI 및 맵 표시용)")]
    public Sprite icon;

    [Header("Stat Modifiers")]
    [Tooltip("이 아이템이 적용할 스탯 변경 목록 (여러 개 가능)")]
    public List<StatModifier> statModifiers = new List<StatModifier>();

    /// <summary>
    /// 이 아이템의 효과를 PlayerController에 적용합니다.
    /// </summary>
    /// <param name="playerController">효과를 적용할 PlayerController</param>
    public void ApplyEffects(PlayerController playerController)
    {
        if (playerController == null) return;

        foreach (var modifier in statModifiers)
        {
            ApplyStatModifier(playerController, modifier);
        }
    }

    /// <summary>
    /// 개별 StatModifier를 PlayerController에 적용합니다.
    /// </summary>
    private void ApplyStatModifier(PlayerController playerController, StatModifier modifier)
    {
        switch (modifier.statType)
        {
            case StatType.MaxHP:
                if (modifier.modifyType == StatModifyType.Set)
                    playerController.SetMaxHP(Mathf.RoundToInt(modifier.value));
                else if (modifier.modifyType == StatModifyType.Multiply)
                    playerController.MultiplyMaxHP(modifier.value);
                else if (modifier.modifyType == StatModifyType.Add)
                    playerController.AddMaxHP(Mathf.RoundToInt(modifier.value));
                break;

            case StatType.AttackSpeed:
                if (modifier.modifyType == StatModifyType.Set)
                    playerController.SetAttackSpeedMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Multiply)
                    playerController.MultiplyAttackSpeedMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Add)
                    playerController.SetAttackSpeedMultiplier(playerController.attackSpeedMultiplier + modifier.value);
                break;

            case StatType.AttackRange:
                if (modifier.modifyType == StatModifyType.Set)
                    playerController.SetAttackRangeMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Multiply)
                    playerController.MultiplyAttackRangeMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Add)
                    playerController.SetAttackRangeMultiplier(playerController.attackRangeMultiplier + modifier.value);
                break;

            case StatType.AttackDamage:
                if (modifier.modifyType == StatModifyType.Set)
                    playerController.SetAttackDamageMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Multiply)
                    playerController.MultiplyAttackDamageMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Add)
                    playerController.SetAttackDamageMultiplier(playerController.attackDamageMultiplier + modifier.value);
                break;

            case StatType.ReloadSpeed:
                if (modifier.modifyType == StatModifyType.Set)
                    playerController.SetReloadSpeedMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Multiply)
                    playerController.MultiplyReloadSpeedMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Add)
                    playerController.SetReloadSpeedMultiplier(playerController.reloadSpeedMultiplier + modifier.value);
                break;

            case StatType.MoveSpeed:
                if (modifier.modifyType == StatModifyType.Set)
                    playerController.SetMoveSpeedMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Multiply)
                    playerController.MultiplyMoveSpeedMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Add)
                    playerController.SetMoveSpeedMultiplier(playerController.moveSpeedMultiplier + modifier.value);
                break;

            case StatType.RollSpeed:
                if (modifier.modifyType == StatModifyType.Set)
                    playerController.SetRollSpeedMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Multiply)
                    playerController.MultiplyRollSpeedMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Add)
                    playerController.SetRollSpeedMultiplier(playerController.rollSpeedMultiplier + modifier.value);
                break;

            case StatType.RollDuration:
                if (modifier.modifyType == StatModifyType.Set)
                    playerController.SetRollDurationMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Multiply)
                    playerController.MultiplyRollDurationMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Add)
                    playerController.SetRollDurationMultiplier(playerController.rollDurationMultiplier + modifier.value);
                break;

            case StatType.RollCooldown:
                if (modifier.modifyType == StatModifyType.Set)
                    playerController.SetRollCooldownMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Multiply)
                    playerController.MultiplyRollCooldownMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Add)
                    playerController.SetRollCooldownMultiplier(playerController.rollCooldownMultiplier + modifier.value);
                break;

            case StatType.SwapCooldown:
                if (modifier.modifyType == StatModifyType.Set)
                    playerController.SetSwapCooldownMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Multiply)
                    playerController.MultiplySwapCooldownMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Add)
                    playerController.SetSwapCooldownMultiplier(playerController.swapCooldownMultiplier + modifier.value);
                break;

            case StatType.BulletSpeed:
                if (playerController.weaponController == null) break;
                if (modifier.modifyType == StatModifyType.Set)
                    playerController.weaponController.SetProjectileSpeedMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Multiply)
                    playerController.weaponController.MultiplyProjectileSpeedMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Add)
                    playerController.weaponController.AddProjectileSpeedMultiplier(modifier.value);
                break;

            case StatType.MaxAmmo:
                if (playerController.weaponController == null) break;
                if (modifier.modifyType == StatModifyType.Set)
                    playerController.weaponController.SetMaxAmmo(Mathf.RoundToInt(modifier.value));
                else if (modifier.modifyType == StatModifyType.Multiply)
                    playerController.weaponController.MultiplyMaxAmmo(modifier.value);
                else if (modifier.modifyType == StatModifyType.Add)
                    playerController.weaponController.AddMaxAmmo(Mathf.RoundToInt(modifier.value));
                break;

            case StatType.InvincibilityDuration:
                if (modifier.modifyType == StatModifyType.Set)
                    playerController.SetInvincibilityMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Multiply)
                    playerController.MultiplyInvincibilityMultiplier(modifier.value);
                else if (modifier.modifyType == StatModifyType.Add)
                    playerController.SetInvincibilityMultiplier(playerController.invincibilityMultiplier + modifier.value);
                break;
        }
    }
}

