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

    // --- Private Members ---
    private WeaponController weaponController;
    private Animator playerAnimator;
    private int lastWeaponIndex = -1;
    private bool isAnimating = false;
    private float displayedAmmoRatio; // For smooth animation

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
    }

    void Update()
    {
        if (weaponController == null || isAnimating)
        {
            return;
        }
        
        UpdateAmmoUI();

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
        displayedAmmoRatio = Mathf.Lerp(displayedAmmoRatio, targetAmmoRatio, Time.deltaTime * ammoBarSmoothing);
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
            elapsedTime += Time.unscaledDeltaTime;
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
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if(activeWeaponIcon.enabled) activeWeaponIcon.transform.localScale = originalScale;
        if(inactiveWeaponIcon.enabled) inactiveWeaponIcon.transform.localScale = originalScale;

        isAnimating = false;
    }
}
