using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


// 각 색상별 하트 스프라이트 세트를 위한 직렬화 가능 클래스
[System.Serializable]
public class HeartSpriteSet
{
    public Sprite fullHeartSprite;
    public Sprite halfHeartSprite;
}

// Controls in-dungeon HUD elements
public class DungeonHUDController : MonoBehaviour
{
    [Header("Component References")]
    public GameObject player;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI swapText;

    [Header("Effects")]
    [Tooltip("전체 HUD를 포함하는 최상위 RectTransform")]
    public RectTransform hudContainer;
    [Tooltip("플레이어 움직임에 대한 관성 효과의 강도")]
    [Range(0f, 10f)]
    public float inertiaStrength = 1.5f;
    [Tooltip("관성 효과가 원래 위치로 돌아오는 부드러움. 값이 높을수록 빨리 따라옵니다.")]
    [Range(0.1f, 20f)]
    public float inertiaSmoothing = 5f;

    [Header("HP Hearts")]
    [Tooltip("체력 하트 아이콘 프리팹 (Image 컴포넌트를 포함해야 함)")]
    public GameObject heartPrefab;
    [Tooltip("하트 아이콘들이 생성될 부모 컨테이너. HorizontalLayoutGroup을 포함해야 합니다.")]
    public RectTransform heartsContainer;
    
    [Tooltip("모든 색상이 공유하는 비어 있는 하트 스프라이트")]
    public Sprite emptyHeartSprite;
    [Tooltip("CMYK 색상 순서(Key, Cyan, Magenta, Yellow)에 따른 하트 스프라이트 세트")]
    public HeartSpriteSet[] cmykHeartSprites = new HeartSpriteSet[4];

    [Header("HP Background Objects")]
    [Tooltip("최대 체력이 3 이하일 때 활성화할 배경 GameObject")]
    public GameObject hpBackgroundObject_3;
    [Tooltip("최대 체력이 4일 때 활성화할 배경 GameObject")]
    public GameObject hpBackgroundObject_4;
    [Tooltip("최대 체력이 5 이상일 때 활성화할 배경 GameObject")]
    public GameObject hpBackgroundObject_5;

    // --- Private members ---
    private PlayerController playerController;
    private WeaponController weaponController;
    private Animator playerAnimator;

    private readonly List<Image> heartIcons = new List<Image>();

    private int lastMaxHP = -1;
    private int lastCurrentHP = -1;
    private CMYKColor lastPlayerColor = (CMYKColor)(-1);

    private Coroutine shakeCoroutine;
    private Vector3 originalHudPos;
    
    // For manual velocity calculation
    private Vector2 lastPlayerPosition;
    private Vector2 manualVelocity;

    void Start()
    {
        if (hudContainer != null)
        {
            originalHudPos = hudContainer.anchoredPosition;
        }

        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            playerAnimator = player.GetComponentInChildren<Animator>();
            lastPlayerPosition = player.transform.position; // 초기 위치 저장
            
            if (playerController != null)
            {
                weaponController = playerController.weaponController;
            }
        }

        if (playerController != null && playerAnimator != null)
        {
            InitializeUI();
        }
    }
    
    // 데이터 업데이트는 Update에서
    void Update()
    {
        if (playerController == null || playerAnimator == null) return;
        
        bool maxHpChanged = playerController.maxHP != lastMaxHP;
        bool currentHpChanged = playerController.currentHP != lastCurrentHP;
        CMYKColor currentPlayerColor = (CMYKColor)playerAnimator.GetInteger("CMYK");
        bool playerColorChanged = currentPlayerColor != lastPlayerColor;

        if (maxHpChanged)
        {
            UpdateHeartContainers();
        }
        if (currentHpChanged || maxHpChanged || playerColorChanged)
        {
            UpdateHeartFill();
        }

        UpdateAmmoUI();
        UpdateSwapUI();
    }

    // 시각적 효과 및 위치 업데이트는 LateUpdate에서
    void LateUpdate()
    {
        if (player == null) return;

        // 위치 변화를 기반으로 수동으로 속도 계산
        manualVelocity = ((Vector2)player.transform.position - lastPlayerPosition) / Time.deltaTime;
        lastPlayerPosition = player.transform.position;
        
        ApplyInertiaEffect();
    }

    public void TriggerShake()
    {
        if (hudContainer == null) return;
        
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(ShakeCoroutine(0.2f, 10.0f));
    }
    
    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            hudContainer.anchoredPosition = originalHudPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            
            yield return null;
        }

        hudContainer.anchoredPosition = originalHudPos;
        shakeCoroutine = null;
    }
    
    private void ApplyInertiaEffect()
    {
        if (hudContainer == null || shakeCoroutine != null) return;

        Vector2 inertiaOffset = manualVelocity * inertiaStrength;
        Vector2 targetPos = originalHudPos - new Vector3(inertiaOffset.x, inertiaOffset.y, 0);
        
        hudContainer.anchoredPosition = Vector2.Lerp(hudContainer.anchoredPosition, targetPos, Time.deltaTime * inertiaSmoothing);
    }
    
    private void InitializeUI()
    {
        UpdateHeartContainers();
        UpdateHeartFill();
        lastMaxHP = playerController.maxHP;
        lastCurrentHP = playerController.currentHP;
        lastPlayerColor = (CMYKColor)playerAnimator.GetInteger("CMYK");
    }

    void UpdateHeartContainers()
    {
        if (heartPrefab == null || heartsContainer == null) return;

        foreach (Transform child in heartsContainer)
        {
            Destroy(child.gameObject);
        }
        heartIcons.Clear();

        int totalHeartContainers = Mathf.CeilToInt((float)playerController.maxHP / 2);

        for (int i = 0; i < totalHeartContainers; i++)
        {
            GameObject heartGO = Instantiate(heartPrefab, heartsContainer);
            heartGO.SetActive(true);
            Image heartImage = heartGO.GetComponent<Image>();
            if (heartImage != null)
            {
                heartIcons.Add(heartImage);
            }
        }
        
        UpdateBackgroundObjects();
        lastMaxHP = playerController.maxHP;
    }
    
    private void UpdateBackgroundObjects()
    {
        hpBackgroundObject_3?.SetActive(false);
        hpBackgroundObject_4?.SetActive(false);
        hpBackgroundObject_5?.SetActive(false);

        GameObject activeBackgroundObject = null;
        if (playerController.maxHP <= 6) activeBackgroundObject = hpBackgroundObject_3;
        else if (playerController.maxHP <= 8) activeBackgroundObject = hpBackgroundObject_4;
        else activeBackgroundObject = hpBackgroundObject_5;
        
        if (activeBackgroundObject != null)
        {
            activeBackgroundObject.SetActive(true);
        }
    }

    void UpdateHeartFill()
    {
        CMYKColor currentPlayerColor = (CMYKColor)playerAnimator.GetInteger("CMYK");
        if (cmykHeartSprites.Length <= (int)currentPlayerColor || cmykHeartSprites[(int)currentPlayerColor] == null) return;

        HeartSpriteSet currentSpriteSet = cmykHeartSprites[(int)currentPlayerColor];
        if(emptyHeartSprite == null || currentSpriteSet.fullHeartSprite == null || currentSpriteSet.halfHeartSprite == null) return;

        for (int i = 0; i < heartIcons.Count; i++)
        {
            Image heartImage = heartIcons[i];
            int maxHpForThisHeart = (i + 1) * 2;

            if (playerController.currentHP >= maxHpForThisHeart)
            {
                heartImage.sprite = currentSpriteSet.fullHeartSprite;
            }
            else if (playerController.currentHP == maxHpForThisHeart - 1)
            {
                heartImage.sprite = currentSpriteSet.halfHeartSprite;
            }
            else
            {
                heartImage.sprite = emptyHeartSprite;
            }
        }
        lastCurrentHP = playerController.currentHP;
        lastPlayerColor = currentPlayerColor;
    }

    void UpdateAmmoUI()
    {
        if (weaponController != null && ammoText != null)
        {
            ammoText.text = weaponController.IsAmmoWeapon 
                ? string.Format("{0}/{1}\nammo", weaponController.CurrentBulletCount, weaponController.MaxBulletCount) 
                : string.Format("∞\nammo");
        }
    }

    void UpdateSwapUI()
    {
        if (playerController != null && swapText != null)
        {
            swapText.text = string.Format("Swap: {0}\nCharge: {1:F1}/{2:F1}", playerController.swapCount, playerController.swapCharge, playerController.swapChargeMax);
        }
    }
}
