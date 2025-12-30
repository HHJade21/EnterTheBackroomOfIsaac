using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 버튼에 마우스가 호버링될 때 하위 오브젝트를 활성화/비활성화하는 컴포넌트
/// </summary>
public class ButtonHoverActivator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Settings")]
    [Tooltip("호버링 시 활성화할 하위 오브젝트들 (비어있으면 모든 직접 자식 오브젝트를 활성화)")]
    [SerializeField] private GameObject[] childObjects;
    
    [Tooltip("모든 직접 자식 오브젝트를 활성화할지 여부 (childObjects가 비어있을 때만 사용)")]
    [SerializeField] private bool activateAllChildren = true;
    
    [Header("Fade In")]
    [Tooltip("페이드인 지속 시간 (초)")]
    [SerializeField] private float fadeInDuration = 5.0f;
    
    [Header("Fade Out")]
    [Tooltip("화면 전체를 덮을 페이드아웃 이미지 (검은색 Image 컴포넌트)")]
    [SerializeField] private Image fadeOutImage;
    
    [Tooltip("페이드아웃 지속 시간 (초)")]
    [SerializeField] private float fadeOutDuration = 2.0f;
    
    [Tooltip("페이드아웃 중 다른 동작을 방지할지 여부 (Time.timeScale 조절)")]
    [SerializeField] private bool pauseGameDuringFadeOut = true;

    private Image buttonImage;
    private Color originalColor;
    private bool isFadingOut = false;
    
    private RectTransform rectTransform;
    private Vector2 originalSize;
    private Vector2 originalAnchoredPosition;

    private void Awake()
    {
        // Image 컴포넌트 가져오기
        buttonImage = GetComponent<Image>();
        
        if (buttonImage != null)
        {
            // 원래 색상 저장
            originalColor = buttonImage.color;
            
            // 처음에는 검은색으로 설정
            buttonImage.color = Color.black;
            
            // 페이드인 코루틴 시작
            StartCoroutine(FadeInCoroutine());
        }
        
        // RectTransform 가져오기
        rectTransform = GetComponent<RectTransform>();
        
        if (rectTransform != null)
        {
            // 원래 크기와 위치 저장
            originalSize = rectTransform.sizeDelta;
            originalAnchoredPosition = rectTransform.anchoredPosition;
        }
    }

    private void Start()
    {
        // childObjects가 비어있으면 모든 직접 자식 오브젝트를 찾아서 초기화
        if (childObjects == null || childObjects.Length == 0)
        {
            if (activateAllChildren)
            {
                int childCount = transform.childCount;
                if (childCount > 0)
                {
                    childObjects = new GameObject[childCount];
                    for (int i = 0; i < childCount; i++)
                    {
                        childObjects[i] = transform.GetChild(i).gameObject;
                    }
                }
            }
        }
        
        // 시작 시 모든 하위 오브젝트 비활성화
        SetChildrenActive(false);
    }

    /// <summary>
    /// 마우스가 버튼 위로 들어올 때 호출됩니다.
    /// </summary>
    /// <param name="eventData">이벤트 데이터</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        SetChildrenActive(true);
        
        // 크기와 위치 변경
        if (rectTransform != null)
        {
            // 크기를 15% 증가 (1.15배)
            rectTransform.sizeDelta = originalSize * 1.3f;
            
            // 오른쪽으로 70 이동
            Vector2 newPosition = originalAnchoredPosition;
            newPosition.x += 90f;
            rectTransform.anchoredPosition = newPosition;
        }
    }

    /// <summary>
    /// 마우스가 버튼에서 벗어날 때 호출됩니다.
    /// </summary>
    /// <param name="eventData">이벤트 데이터</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        SetChildrenActive(false);
        
        // 원래 크기와 위치로 복원
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = originalSize;
            rectTransform.anchoredPosition = originalAnchoredPosition;
        }
    }

    /// <summary>
    /// 하위 오브젝트들을 활성화/비활성화합니다.
    /// </summary>
    /// <param name="active">활성화 여부</param>
    private void SetChildrenActive(bool active)
    {
        if (childObjects == null) return;

        foreach (GameObject child in childObjects)
        {
            if (child != null)
            {
                child.SetActive(active);
            }
        }
    }

    /// <summary>
    /// 이미지를 검은색에서 원래 색상으로 페이드인하는 코루틴
    /// </summary>
    private IEnumerator FadeInCoroutine()
    {
        if (buttonImage == null) yield break;

        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeInDuration;
            
            // 검은색에서 원래 색상으로 보간
            buttonImage.color = Color.Lerp(Color.black, originalColor, t);
            
            yield return null;
        }

        // 최종적으로 원래 색상으로 설정 (보정)
        buttonImage.color = originalColor;
    }

    

    
}

