using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Controls in-dungeon HUD elements
// Responsibilities:
// - Display player hp, ammo/gauge, current weapon
// - Show boss hp bar in boss rooms
// - Subscribe to events from controllers to update UI

public class DungeonHUDController : MonoBehaviour
{
    // [Bindings] UI references for bars/text/icons
    public TextMeshProUGUI hpText;
    [Header("Ammo")]
    public TextMeshProUGUI ammoText;//일단 얘만 쓰기
    public GameObject player;
    [Header("Weapon")]
    public TextMeshProUGUI weaponText;
    [Header("HP")]
    public Image hpBar;
    [Header("Ammo")]
    public Image ammoBar;
    [Header("Weapon")]
    public Image weaponIcon;
    [Header("Swap")]
    public TextMeshProUGUI swapText;
    // [Update] Methods to refresh UI based on events
    void Update()
    {
        if (player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            WeaponController weaponController = playerController != null ? playerController.weaponController : null;
            
            if (weaponController != null && ammoText != null)
            {
                if (weaponController.IsAmmoWeapon)
                {
                    ammoText.text = weaponController.CurrentBulletCount + "/" + weaponController.MaxBulletCount + "\nammo";
                }
                else
                {
                    ammoText.text = "∞\nammo";
                }
            }
            
            if (playerController != null && swapText != null)
            {
                swapText.text = $"Swap: {playerController.swapCount}\nCharge: {playerController.swapCharge:F1}/{playerController.swapChargeMax:F1}";
            }
        }
    }
}


