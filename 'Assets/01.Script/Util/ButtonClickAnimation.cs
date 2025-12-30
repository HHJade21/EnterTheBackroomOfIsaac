'''using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼 클릭 시 이미지를 잠시 바꿨다가 되돌리는 간단한 애니메이션 연출을 담당합니다.
/// Image 컴포넌트가 있는 게임 오브젝트에 붙여서 사용합니다.
/// </summary>
[RequireComponent(typeof(Image))]
public class ButtonClickAnimation : MonoBehaviour
{
    [Tooltip("클릭 시 보여줄 이미지")]
    [SerializeField] private Sprite animatedSprite;

    [Tooltip("이미지가 바뀌어있을 시간 (초)")]
    [SerializeField] private float animationDuration = 3.0f;

    private Image buttonImage;
    private Sprite originalSprite;

    private Coroutine animationCoroutine;

    private void Awake()
    {
        buttonImage = GetComponent<Image>();
        // 원본 스프라이트는 시작 시점에 한 번만 저장합니다.
        originalSprite = buttonImage.sprite;
    }

    /// <summary>
    /// 애니메이션을 재생합니다.
    /// </summary>
    public void Animate()
    {
        if (animatedSprite == null)
        {
            Debug.LogWarning("ButtonClickAnimation: animatedSprite가 할당되지 않았습니다.", this);
            return;
        }

        if (buttonImage == null)
        {
            return;
        }

        // 이미 코루틴이 실행 중이면 중지합니다.
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        // 어떤 상태였든, 애니메이션을 시작하기 전에 항상 원본 이미지로 초기화합니다.
        // 이렇게 하면 여러 번 클릭해도 항상 동일한 상태에서 시작합니다.
        buttonImage.sprite = originalSprite;

        animationCoroutine = StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        // 애니메이션 스프라이트로 변경
        buttonImage.sprite = animatedSprite;

        // 지정된 시간만큼 대기
        yield return new WaitForSeconds(animationDuration);

        // 원래 스프라이트로 복구
        buttonImage.sprite = originalSprite;

        // 코루틴 참조 해제
        animationCoroutine = null;
    }
}
''