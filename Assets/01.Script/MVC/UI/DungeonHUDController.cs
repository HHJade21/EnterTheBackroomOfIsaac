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
    // [Update] Methods to refresh UI based on events
    void Update()
    {
        ammoText.text = player.GetComponent<PlayerController>().currentBulletCount + "/" + player.GetComponent<PlayerController>().maxBulletCount + "\nammo";
    }
}


