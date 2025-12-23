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

    [Header("Shake Effect")]
    [Tooltip("전체 HUD를 포함하는 최상위 RectTransform")]
    public RectTransform hudContainer;

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
    private Animator playerAnimator; // 플레이어 Animator 참조
    private readonly List<Image> heartIcons = new List<Image>();

    private int lastMaxHP = -1;
    private int lastCurrentHP = -1;
    private CMYKColor lastPlayerColor = (CMYKColor)(-1);

    private Coroutine shakeCoroutine;
    private Vector3 originalHudPos;

    void Start()
    {
        if (hudContainer != null)
        {
            originalHudPos = hudContainer.anchoredPosition;
        }

        // ... 기존 Start 로직 ...
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            playerAnimator = player.GetComponentInChildren<Animator>();
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

    void Update()
    {
        // ... 기존 Update 로직 ...
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

    /// <summary>
    /// 외부에서 호출하여 HUD 흔들림 효과를 시작합니다.
    /// </summary>
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
    
    // --- 기존 UI 업데이트 함수들 ---
    
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
        if (playerController.maxHP <= 3) activeBackgroundObject = hpBackgroundObject_3;
        else if (playerController.maxHP == 4) activeBackgroundObject = hpBackgroundObject_4;
        else activeBackgroundObject = hpBackgroundObject_5;
        
        if (activeBackgroundObject != null)
        {
            activeBackgroundObject.SetActive(true);
            Image backgroundImage = activeBackgroundObject.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.type = Image.Type.Sliced;
                
                Canvas.ForceUpdateCanvases(); 
                Vector2 newSize = heartsContainer.rect.size;
                backgroundImage.rectTransform.sizeDelta = newSize;
            }
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
                ? $"{weaponController.CurrentBulletCount}/{weaponController.MaxBulletCount}\n ammo" 
                : "∞\n ammo";
        }
    }

    void UpdateSwapUI()
    {
        if (playerController != null && swapText != null)
        {
            swapText.text = $"Swap: {playerController.swapCount}\nCharge: {playerController.swapCharge:F1}/{playerController.swapChargeMax:F1}";
        }
    }
}