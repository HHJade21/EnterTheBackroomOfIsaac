using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// Controls the HUD for weapon slots, including swap animations and ammo display.
public class WeaponSlotHUDController : MonoBehaviour
{
    [Header("Core Reference")]
    [Tooltip("Reference to the PlayerController")]
    public PlayerController playerController;

    [Header("In-Game HUD References")]
    [Tooltip("Image component for the active weapon slot")]
    public Image activeWeaponIcon;
    [Tooltip("Image component for the inactive weapon slot")]
    public Image inactiveWeaponIcon;
    [Tooltip("GameObject container for the entire in-game weapon HUD")]
    public GameObject weaponHudContainer;

    [Header("Ammo Display")]
    public TextMeshProUGUI ammoText;
    [Tooltip("Image for the ammo bar, set to 'Filled' mode")]
    public Image ammoBarImage;
    [Tooltip("CMYK color-coded sprites for the ammo bar")]
    [ArrayLabel("Key", "Cyan", "Magenta", "Yellow")]
    public Sprite[] cmykAmmoBarSprites = new Sprite[4];

    [Header("Animation Settings")]
    [Tooltip("Duration of the weapon swap animation")]
    public float swapAnimationDuration = 0.3f;
    [Tooltip("The scale the icons shrink to during the animation (e.g., 0.1 for 10%)")]
    public float scaleDownFactor = 0.1f;
    [Tooltip("How smoothly the ammo bar animates")]
    public float ammoBarSmoothing = 10f;

    [Header("Swap Stamina")]
    [Tooltip("List of images for the swap charges (set to 'Filled' mode)")]
    public List<Image> swapChargeImages;
    private float swapFillDuration = 0.2f; // The time it takes for a charge UI to fill or empty.

    [Header("Swap Stamina Fade Out")]
    [Tooltip("The CanvasGroup containing all swap charge images. Used for fading.")]
    public CanvasGroup swapChargeCanvasGroup;
    [Tooltip("How long to wait after charges are full before starting to fade out.")]
    public float fadeOutDelay = 2f;
    [Tooltip("How long the fade-out animation takes.")]
    public float fadeOutDuration = 1f;
    [Tooltip("How long the fade-in animation takes.")]
    public float fadeInDuration = 0.2f;

    [Header("Player Follow Settings")]
    [Tooltip("How fast the UI follows the player.")]
    public float followSpeed = 10f;
    [Tooltip("The offset from the player's position.")]
    public Vector3 followOffset = new Vector3(0, 1.5f, 0);

    // --- Private Members ---
    private WeaponController weaponController;
    private Animator playerAnimator;
    private int lastWeaponIndex = -1;
    private bool isAnimating = false;
    private float displayedAmmoRatio; // For smooth animation
    private List<float> displayedFillAmounts;
    private float fullSinceTimestamp = -1f;

    void Start()
    {
        if (playerController != null)
        {
            weaponController = playerController.weaponController;
            playerAnimator = playerController.GetComponentInChildren<Animator>();
        }
        else
        {
             // Fallback to find the player if not set in inspector
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                playerController = player;
                weaponController = player.weaponController;
                playerAnimator = player.GetComponentInChildren<Animator>();
            }
        }

        if (weaponController == null)
        {
            Debug.LogError("WeaponSlotHUDController: Could not find PlayerController or WeaponController! Disabling HUD.");
            if (weaponHudContainer != null) weaponHudContainer.SetActive(false);
            return;
        }

        InitialUISetup();
        InitializeSwapFillAmounts(); // Initialize smooth fill amounts
    }

    void Update()
    {
        // Handle following the player
        if (swapChargeCanvasGroup != null && playerController != null && Camera.main != null)
        {
            // 1. Define the target position in WORLD space (e.g., above the player)
            Vector3 worldTargetPosition = playerController.transform.position + followOffset;

            // 2. Convert the world position to a screen position
            Vector3 screenTargetPosition = Camera.main.WorldToScreenPoint(worldTargetPosition);

            // 3. Smoothly move the UI element's screen position towards the target screen position
            // Use unscaledDeltaTime for smooth movement even when Time.timeScale is changed
            swapChargeCanvasGroup.transform.position = Vector3.Lerp(swapChargeCanvasGroup.transform.position, screenTargetPosition, Time.unscaledDeltaTime * followSpeed);
        }

        if (weaponController == null || isAnimating)
        {
            return;
        }
        
        UpdateAmmoUI();
        UpdateSwapStaminaUI();

        int currentWeaponCount = weaponController.OwnedWeapons.Count;
        int currentIndex = weaponController.CurrentWeaponIndex;

        // Update if the equipped weapon has changed (swap)
        if (lastWeaponIndex != -1 && currentIndex != lastWeaponIndex && currentWeaponCount > 1)
        {
            StartCoroutine(SwapAnimationRoutine());
        }
        else
        {
            // For other cases (like picking up a first/second weapon), just update sprites
            UpdateWeaponSlots();
        }
        
        lastWeaponIndex = currentIndex;
    }

    private void InitialUISetup()
    {
        UpdateWeaponSlots();
        lastWeaponIndex = weaponController.CurrentWeaponIndex;

        // Initialize ammo bar state
        if (weaponController.IsAmmoWeapon && weaponController.MaxBulletCount > 0)
        {
            displayedAmmoRatio = (float)weaponController.CurrentBulletCount / weaponController.MaxBulletCount;
        }
        else
        {
            displayedAmmoRatio = 1f;
        }
        ammoBarImage.fillAmount = displayedAmmoRatio;
    }

    private void InitializeSwapFillAmounts()
    {
        if (swapChargeImages != null)
        {
            displayedFillAmounts = new List<float>(swapChargeImages.Count);
            for (int i = 0; i < swapChargeImages.Count; i++)
            {
                float initialFill = 0f;
                if (playerController != null && playerController.swapChargeMax > 0)
                {
                    int currentCharges = playerController.swapCount;
                    float chargeProgress = playerController.swapCharge / playerController.swapChargeMax;
                    if (i < currentCharges) initialFill = 1f;
                    else if (i == currentCharges) initialFill = chargeProgress;
                    else initialFill = 0f;
                }
                displayedFillAmounts.Add(initialFill);
                if (swapChargeImages[i] != null) swapChargeImages[i].fillAmount = initialFill;
            }
        }
    }

    private void HandleFadeOut(int currentCharges)
    {
        if (swapChargeCanvasGroup == null) return;

        bool areAllChargesFull = currentCharges >= (swapChargeImages?.Count ?? 0);
        float targetAlpha = 1f;

        if (areAllChargesFull)
        {
            // Check if this is the first frame all charges are full
            if (fullSinceTimestamp < 0)
            {
                fullSinceTimestamp = Time.time;
            }

            // If the delay has passed, target alpha is 0 (fade out)
            if (Time.time > fullSinceTimestamp + fadeOutDelay)
            {
                targetAlpha = 0f;
            }
        }
        else
        {
            // If not full, reset the timer and target alpha is 1 (fade in)
            fullSinceTimestamp = -1f;
            targetAlpha = 1f;
        }

        // Determine the speed of the fade
        float fadeDuration = (targetAlpha == 0) ? fadeOutDuration : fadeInDuration;
        float fadeSpeed = (fadeDuration > 0) ? 1 / fadeDuration : float.MaxValue;

        // Smoothly move the alpha towards the target
        swapChargeCanvasGroup.alpha = Mathf.MoveTowards(swapChargeCanvasGroup.alpha, targetAlpha, fadeSpeed * Mathf.Min(Time.unscaledDeltaTime, 0.033f));
    }

    private void UpdateSwapStaminaUI()
    {
        if (playerController == null || swapChargeImages == null || swapChargeImages.Count == 0 || displayedFillAmounts == null || displayedFillAmounts.Count != swapChargeImages.Count)
        {
            // Re-initialize if mismatch (e.g., images added in editor during play mode)
            InitializeSwapFillAmounts();
            if (displayedFillAmounts == null || displayedFillAmounts.Count == 0) return; // Still null, can't proceed
        }

        int currentCharges = playerController.swapCount;
        float chargeProgress = 0;
        if (playerController.swapChargeMax > 0)
        {
            chargeProgress = playerController.swapCharge / playerController.swapChargeMax;
        }

        for (int i = 0; i < swapChargeImages.Count; i++)
        {
            if (swapChargeImages[i] == null) continue;

            float targetFillAmount;

            if (i < currentCharges)
            {
                // This is a full charge, target is 1
                targetFillAmount = 1;
            }
            else if (i == currentCharges)
            {
                // This is the charge that is currently regenerating, target is chargeProgress
                targetFillAmount = chargeProgress;
            }
            else
            {
                // This is an empty charge waiting for its turn, target is 0
                targetFillAmount = 0;
            }

            // If the target is lower (decreasing), snap instantly. Otherwise, animate smoothly.
            if (targetFillAmount < displayedFillAmounts[i])
            {
                displayedFillAmounts[i] = targetFillAmount;
            }
            else
            {
                // Move towards the target fill amount at a constant speed (for filling up)
                float step = (swapFillDuration > 0) ? (1 / swapFillDuration) : float.MaxValue;
                displayedFillAmounts[i] = Mathf.MoveTowards(displayedFillAmounts[i], targetFillAmount, step * Mathf.Min(Time.unscaledDeltaTime, 0.033f));
            }

            swapChargeImages[i].fillAmount = displayedFillAmounts[i];
        }

        // Handle fade-out logic
        HandleFadeOut(currentCharges);
    }

    private void UpdateAmmoUI()
    {
        if (weaponController == null || ammoText == null || ammoBarImage == null || playerAnimator == null) return;

        float targetAmmoRatio = 1f;

        if (weaponController.IsAmmoWeapon)
        {
            ammoText.text = string.Format("{0}/{1}", weaponController.CurrentBulletCount, weaponController.MaxBulletCount);
            
            if (weaponController.MaxBulletCount > 0)
            {
                targetAmmoRatio = (float)weaponController.CurrentBulletCount / weaponController.MaxBulletCount;
            }
        }
        else
        {
            ammoText.text = "∞";
            targetAmmoRatio = 1f;
        }
        
        // Smoothly animate the bar
        displayedAmmoRatio = Mathf.Lerp(displayedAmmoRatio, targetAmmoRatio, Mathf.Min(Time.unscaledDeltaTime, 0.033f) * ammoBarSmoothing);
        ammoBarImage.fillAmount = displayedAmmoRatio;
        
        // Update ammo bar color/sprite based on player's CMYK state
        int cmykIndex = playerAnimator.GetInteger("CMYK");
        if (cmykAmmoBarSprites.Length > cmykIndex && cmykAmmoBarSprites[cmykIndex] != null)
        {
            ammoBarImage.sprite = cmykAmmoBarSprites[cmykIndex];
        }
    }

    private void UpdateWeaponSlots()
    {
        IReadOnlyList<WeaponData> ownedWeapons = weaponController.OwnedWeapons;

        // Active weapon is always in the 'activeWeaponIcon' slot
        if (weaponController.CurrentWeapon != null)
        {
            activeWeaponIcon.sprite = weaponController.CurrentWeapon.icon;
            activeWeaponIcon.enabled = true;
        }
        else
        {
            activeWeaponIcon.enabled = false;
        }

        // Find the other weapon to display in the 'inactiveWeaponIcon' slot
        WeaponData otherWeapon = null;
        if (ownedWeapons.Count > 1)
        {
            int currentIndex = weaponController.CurrentWeaponIndex;
            // The other weapon is the one that is not the current one
            if(currentIndex == 0 && ownedWeapons.Count > 1)
                otherWeapon = ownedWeapons[1];
            else if (currentIndex > 0)
                otherWeapon = ownedWeapons[0];
        }

        if (otherWeapon != null)
        {
            inactiveWeaponIcon.sprite = otherWeapon.icon;
            inactiveWeaponIcon.enabled = true;
        }
        else
        {
            inactiveWeaponIcon.enabled = false;
        }
    }

    private IEnumerator SwapAnimationRoutine()
    {
        isAnimating = true;
        float halfDuration = swapAnimationDuration / 2f;
        float elapsedTime = 0f;

        Vector3 originalScale = activeWeaponIcon.transform.localScale;
        Vector3 targetScale = new Vector3(scaleDownFactor, scaleDownFactor, scaleDownFactor);

        // --- Shrink Phase ---
        while (elapsedTime < halfDuration)
        {
            float t = elapsedTime / halfDuration;
            if(activeWeaponIcon.enabled) activeWeaponIcon.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            if(inactiveWeaponIcon.enabled) inactiveWeaponIcon.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            elapsedTime += Mathf.Min(Time.unscaledDeltaTime, 0.033f);
            yield return null;
        }

        if(activeWeaponIcon.enabled) activeWeaponIcon.transform.localScale = targetScale;
        if(inactiveWeaponIcon.enabled) inactiveWeaponIcon.transform.localScale = targetScale;

        // --- Swap Sprites ---
        UpdateWeaponSlots();

        // --- Grow Phase ---
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            float t = elapsedTime / halfDuration;
            if(activeWeaponIcon.enabled) activeWeaponIcon.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            if(inactiveWeaponIcon.enabled) inactiveWeaponIcon.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            elapsedTime += Mathf.Min(Time.unscaledDeltaTime, 0.033f);
            yield return null;
        }

        if(activeWeaponIcon.enabled) activeWeaponIcon.transform.localScale = originalScale;
        if(inactiveWeaponIcon.enabled) inactiveWeaponIcon.transform.localScale = originalScale;

        isAnimating = false;
    }
}