using System.Collections;
using UnityEngine;

[System.Serializable]
public class PanelAnimationTarget
{
    public RectTransform panel;
    public Vector2 startAnchorPosition;
    public Vector2 endAnchorPosition;
}

public class PanelGroupAnimator : MonoBehaviour
{
    [Header("Animation Targets")]
    [Tooltip("애니메이션을 적용할 패널들의 배열")]
    public PanelAnimationTarget[] animationTargets;

    [Header("Animation Settings")]
    [Tooltip("애니메이션 지속 시간 (초)")]
    public float animationDuration = 0.5f;

    private Coroutine animationCoroutine;

    /// <summary>
    /// 패널들을 활성화하고 나타나는 애니메이션을 재생합니다.
    /// </summary>
    public void AnimateOn()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        // 모든 패널을 활성화하고 시작 위치로 설정
        foreach (var target in animationTargets)
        {
            if (target.panel != null)
            {
                target.panel.gameObject.SetActive(true);
                target.panel.anchoredPosition = target.startAnchorPosition;
            }
        }

        animationCoroutine = StartCoroutine(AnimatePanels(true));
    }

    /// <summary>
    /// 패널들이 사라지는 애니메이션을 재생하고 비활성화합니다.
    /// </summary>
    public void AnimateOff()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        // 애니메이션 시작 전에 모든 패널이 활성화 되어있는지 확인
        foreach (var target in animationTargets)
        {
            if (target.panel != null)
            {
                target.panel.gameObject.SetActive(true);
            }
        }

        animationCoroutine = StartCoroutine(AnimatePanels(false));
    }

    private IEnumerator AnimatePanels(bool isAnimatingOn)
    {
        float elapsedTime = 0f;

        // 각 패널의 애니메이션 시작/끝 위치 설정
        Vector2[] fromPositions = new Vector2[animationTargets.Length];
        Vector2[] toPositions = new Vector2[animationTargets.Length];

        for (int i = 0; i < animationTargets.Length; i++)
        {
            if (animationTargets[i].panel != null)
            {
                fromPositions[i] = isAnimatingOn ? animationTargets[i].startAnchorPosition : animationTargets[i].endAnchorPosition;
                toPositions[i] = isAnimatingOn ? animationTargets[i].endAnchorPosition : animationTargets[i].startAnchorPosition;
            }
        }

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime; // Time.timeScale에 영향받지 않도록
            float t = Mathf.Clamp01(elapsedTime / animationDuration);
            float smoothT = Mathf.SmoothStep(0.0f, 1.0f, t);

            for (int i = 0; i < animationTargets.Length; i++)
            {
                if (animationTargets[i].panel != null)
                {
                    animationTargets[i].panel.anchoredPosition = Vector2.Lerp(fromPositions[i], toPositions[i], smoothT);
                }
            }

            yield return null;
        }

        // 애니메이션 종료 후 최종 위치 고정 및 비활성화 처리
        for (int i = 0; i < animationTargets.Length; i++)
        {
            if (animationTargets[i].panel != null)
            {
                animationTargets[i].panel.anchoredPosition = toPositions[i];
                if (!isAnimatingOn)
                {
                    animationTargets[i].panel.gameObject.SetActive(false);
                }
            }
        }
        
        animationCoroutine = null;
    }
}