using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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
    
    [Header("UI Icon Settings")]
    [Tooltip("아이콘 표시 시간 (초)")]
    public float iconDisplayDuration = 1f;
    [Tooltip("플레이어 머리 위에서 아이콘까지의 오프셋 (월드 좌표)")]
    public Vector3 iconOffset = new Vector3(0.5f, 1f, 0f);
    [Tooltip("깜빡이는 주기 (초)")]
    public float blinkInterval = 0.2f;

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
        
        // 스킬 아이콘 표시 코루틴 시작
        playerController.StartCoroutine(ShowIconRoutine(playerController));
    }

    /// <summary>
    /// 스킬 아이콘을 표시하는 코루틴
    /// </summary>
    private IEnumerator ShowIconRoutine(PlayerController playerController)
    {
        GameObject iconObject = null;
        Image iconImage = null;
        Coroutine blinkCoroutine = null;
        
        if (playerController.skillIcon != null && playerController.skillIcon.Count > 5 && playerController.skillIcon[5] != null)
        {
            // Canvas 찾기
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                // UI Image GameObject 생성
                iconObject = new GameObject("TargetShotSkillIcon");
                RectTransform rectTransform = iconObject.AddComponent<RectTransform>();
                iconImage = iconObject.AddComponent<Image>();
                
                // Image 설정
                iconImage.sprite = playerController.skillIcon[5];
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
        
        // 아이콘 표시 시간 대기
        yield return new WaitForSeconds(iconDisplayDuration);
        
        // 스킬 아이콘 제거
        if (iconObject != null)
        {
            if (blinkCoroutine != null)
            {
                playerController.StopCoroutine(blinkCoroutine);
            }
            Destroy(iconObject);
        }
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
        EnemyController[] allEnemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
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