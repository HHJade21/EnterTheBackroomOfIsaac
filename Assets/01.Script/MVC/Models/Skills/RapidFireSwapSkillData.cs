using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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
    
    [Header("UI Icon Settings")]
    [Tooltip("플레이어 머리 위에서 아이콘까지의 오프셋 (월드 좌표)")]
    public Vector3 iconOffset = new Vector3(0.5f, 1f, 0f);
    [Tooltip("깜빡이는 주기 (초)")]
    public float blinkInterval = 0.2f;

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

        // 스킬 아이콘 UI 생성 및 표시
        GameObject iconObject = null;
        Image iconImage = null;
        Coroutine blinkCoroutine = null;
        
        if (playerController.skillIcon != null && playerController.skillIcon.Count > 3 && playerController.skillIcon[3] != null)
        {
            // Canvas 찾기
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                // UI Image GameObject 생성
                iconObject = new GameObject("RapidFireSkillIcon");
                RectTransform rectTransform = iconObject.AddComponent<RectTransform>();
                iconImage = iconObject.AddComponent<Image>();
                
                // Image 설정
                iconImage.sprite = playerController.skillIcon[3];
                iconImage.preserveAspect = true;
                iconImage.type = Image.Type.Simple;
                
                // RectTransform 설정
                rectTransform.SetParent(canvas.transform, false);
                rectTransform.sizeDelta = new Vector2(50f, 50f);
                
                // 깜빡이는 코루틴 시작
                blinkCoroutine = playerController.StartCoroutine(BlinkIconCoroutine(iconImage, blinkInterval));
                
                // 아이콘 위치 업데이트 코루틴 시작
                playerController.StartCoroutine(UpdateIconPositionCoroutine(iconObject, playerController, iconOffset));
            }
        }

        // 지속 시간 대기
        yield return new WaitForSeconds(duration);

        // 원래 배율로 복구
        playerController.attackSpeedMultiplier = originalAttackMultiplier;
        playerController.reloadSpeedMultiplier = originalReloadMultiplier;

        // WeaponController의 스탯 재동기화 (원래 배율 적용)
        weaponController.SyncWeaponStats(forceResetAmmo: false);

        // 스킬 아이콘 제거
        if (iconObject != null)
        {
            if (blinkCoroutine != null)
            {
                playerController.StopCoroutine(blinkCoroutine);
            }
            Destroy(iconObject);
        }

        Debug.Log("RapidFireSwapSkill: 공격 속도 원래대로 복구");
    }

    /// <summary>
    /// 아이콘 깜빡이는 코루틴
    /// </summary>
    private IEnumerator BlinkIconCoroutine(Image iconImage, float interval)
    {
        while (iconImage != null && iconImage.gameObject != null)
        {
            // 반투명으로 변경
            Color color = iconImage.color;
            color.a = 0.3f;
            iconImage.color = color;
            
            yield return new WaitForSeconds(interval);
            
            if (iconImage == null || iconImage.gameObject == null) break;
            
            // 불투명으로 변경
            color = iconImage.color;
            color.a = 1f;
            iconImage.color = color;
            
            yield return new WaitForSeconds(interval);
        }
    }

    /// <summary>
    /// 아이콘 위치를 플레이어 머리 위로 업데이트하는 코루틴
    /// </summary>
    private IEnumerator UpdateIconPositionCoroutine(GameObject iconObject, PlayerController playerController, Vector3 offset)
    {
        RectTransform rectTransform = iconObject.GetComponent<RectTransform>();
        Canvas canvas = iconObject.GetComponentInParent<Canvas>();
        
        if (rectTransform == null || canvas == null) yield break;
        
        while (iconObject != null && playerController != null)
        {
            // 플레이어 머리 위 위치 계산 (월드 좌표)
            Vector3 worldPosition = playerController.transform.position + offset;
            
            // 월드 좌표를 스크린 좌표로 변환
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
            
            // 스크린 좌표를 Canvas의 로컬 좌표로 변환
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPos,
                canvas.worldCamera,
                out Vector2 localPoint
            );
            
            // RectTransform의 anchoredPosition 설정
            rectTransform.anchoredPosition = localPoint;
            
            yield return null;
        }
    }
}

