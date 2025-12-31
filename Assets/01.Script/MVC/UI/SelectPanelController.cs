using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 선택 패널 컨트롤러: 마우스 호버 시 버튼 위치를 위로 부드럽게 이동시킵니다.
/// </summary>
public class SelectPanelController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    [Tooltip("마우스 호버 시 위로 이동할 Y축 오프셋 (픽셀 단위)")]
    [SerializeField] private float hoverOffsetY = 30f;
    
    [Tooltip("이동 속도 (값이 클수록 빠름)")]
    [SerializeField] private float moveSpeed = 10f;
    
    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;
    private Vector2 targetPosition;
    private Coroutine moveCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalAnchoredPosition = rectTransform.anchoredPosition;
            targetPosition = originalAnchoredPosition;
        }
    }

    /// <summary>
    /// 마우스가 버튼 위에 올라왔을 때 호출됩니다.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (rectTransform != null)
        {
            targetPosition = originalAnchoredPosition + new Vector2(0f, hoverOffsetY);
            StartMoveCoroutine();
        }
    }

    /// <summary>
    /// 마우스가 버튼에서 벗어났을 때 호출됩니다.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (rectTransform != null)
        {
            targetPosition = originalAnchoredPosition;
            StartMoveCoroutine();
        }
    }

    /// <summary>
    /// 이동 코루틴 시작
    /// </summary>
    private void StartMoveCoroutine()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        moveCoroutine = StartCoroutine(MoveToTargetCoroutine());
    }

    /// <summary>
    /// 목표 위치로 부드럽게 이동하는 코루틴
    /// </summary>
    private IEnumerator MoveToTargetCoroutine()
    {
        if (rectTransform == null) yield break;

        while (Vector2.Distance(rectTransform.anchoredPosition, targetPosition) > 0.1f)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(
                rectTransform.anchoredPosition,
                targetPosition,
                Time.unscaledDeltaTime * moveSpeed
            );
            yield return null;
        }

        // 정확히 목표 위치로 설정
        rectTransform.anchoredPosition = targetPosition;
        moveCoroutine = null;
    }
}

