using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 젤다 스타일의 보스 체력바 HUD 컨트롤러.
/// - 구성: 배경(Background) / 지연 잔상(White Bar) / 실제 체력(Fill)
/// - 기능: 즉각적인 체력 반영 후 하얀색 바가 천천히 줄어드는 연출
/// </summary>
public class BossHUDController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("보스 HUD 전체를 담는 캔버스 그룹 (페이드 효과용)")]
    public CanvasGroup canvasGroup;
    
    [Tooltip("보스 이름 텍스트")]
    public TextMeshProUGUI nameText;
    
    [Tooltip("보스 설명 텍스트 (옵션)")]
    public TextMeshProUGUI descriptionText;

    [Header("Health Bar Settings")]
    [Tooltip("실제 체력을 표시하는 이미지 (가장 앞쪽, 보통 빨간색)")]
    public Image hpFillImage;
    
    [Tooltip("데미지를 입었을 때 남는 잔상 이미지 (중간, 보통 하얀색)")]
    public Image hpDelayedImage;
    
    [Tooltip("잔상 바가 줄어드는 속도")]
    public float delayLerpSpeed = 3f;
    
    [Tooltip("데미지를 입은 후 잔상 바가 줄어들기 시작할 때까지의 대기 시간")]
    public float damageStallDuration = 0.5f;

    [Header("Appearance")]
    [Tooltip("보스 등장 시 페이드 인 시간")]
    public float fadeInDuration = 1.0f;
    [Tooltip("보스 처치 시 페이드 아웃 시간")]
    public float fadeOutDuration = 2.0f;

    // 내부 상태 변수
    private EnemyController targetBoss;
    private float currentFillAmount;
    private float targetFillAmount;
    private float stallTimer = 0f;
    private bool isBossDead = false;

    // 싱글톤 인스턴스
    public static BossHUDController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 시작 시 숨김 처리
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (targetBoss == null)
        {
            // 보스가 할당되었다가 사라진 경우 (죽음 등) 처리
            if (canvasGroup.gameObject.activeSelf && !isBossDead)
            {
                OnBossDefeated(); 
            }
            return;
        }

        UpdateHealthValues();
        UpdateUI();
    }

    /// <summary>
    /// 보스 정보를 받아 HUD를 초기화하고 표시합니다.
    /// </summary>
    public void Initialize(EnemyController boss, string name, string description = "")
    {
        targetBoss = boss;
        isBossDead = false;

        // 텍스트 설정
        if (nameText != null) nameText.text = name;
        if (descriptionText != null) descriptionText.text = description;

        // 체력바 초기화 (꽉 찬 상태)
        currentFillAmount = 1f;
        targetFillAmount = 1f;

        if (hpFillImage != null) hpFillImage.fillAmount = 1f;
        if (hpDelayedImage != null) hpDelayedImage.fillAmount = 1f;

        // HUD 표시 (페이드 인)
        // 중요: 비활성화된 오브젝트에서는 코루틴을 시작할 수 없으므로 먼저 활성화해야 함
        if (canvasGroup != null)
        {
            canvasGroup.gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }
        
        StartCoroutine(FadeInRoutine());
    }

    private void UpdateHealthValues()
    {
        if (targetBoss == null) return;

        // 현재 체력 비율 계산 (0 ~ 1)
        float hpRatio = targetBoss.CurrentHP / targetBoss.MaxHP;
        
        // 체력이 줄어들었는지 감지 (데미지 피격)
        if (hpRatio < targetFillAmount)
        {
            stallTimer = damageStallDuration; // 줄어들기 전 대기 시간 리셋
        }

        targetFillAmount = hpRatio;

        // 보스가 죽었는지 체크
        if (targetBoss.CurrentHP <= 0 && !isBossDead)
        {
            isBossDead = true;
            OnBossDefeated();
        }
    }

    private void UpdateUI()
    {
        // 1. 실제 체력바 (즉시 반영)
        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = targetFillAmount;
        }

        // 2. 잔상 바 (딜레이 후 부드럽게 감소)
        if (hpDelayedImage != null)
        {
            if (stallTimer > 0)
            {
                stallTimer -= Time.deltaTime;
            }
            else
            {
                // 현재 보여지는 잔상 바가 목표치보다 크면 줄어듦
                if (hpDelayedImage.fillAmount > targetFillAmount)
                {
                    hpDelayedImage.fillAmount = Mathf.Lerp(hpDelayedImage.fillAmount, targetFillAmount, Time.deltaTime * delayLerpSpeed);
                }
            }
        }
    }

    public void OnBossDefeated()
    {
        StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        if (canvasGroup == null) yield break;

        canvasGroup.gameObject.SetActive(true);
        float t = 0f;
        
        while (t < 1f)
        {
            t += Time.deltaTime / fadeInDuration;
            canvasGroup.alpha = t;
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutRoutine()
    {
        if (canvasGroup == null) yield break;

        float t = 1f;
        
        while (t > 0f)
        {
            t -= Time.deltaTime / fadeOutDuration;
            canvasGroup.alpha = t;
            yield return null;
        }
        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(false);
    }
}
