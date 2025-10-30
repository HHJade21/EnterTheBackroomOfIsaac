using UnityEngine;

// Mediates between PlayerController and IWeapon implementation
// Responsibilities:
// - Hold reference to equipped IWeapon
// - Translate player input into TryFire/Reload calls respecting attack speed
// - Apply player attack power to final damage output
// - Notify UI about ammo/gauge changes
// SOLID:
// - DIP: depend on IWeapon abstraction

public class WeaponController : MonoBehaviour
{
    // [Equip] Methods to equip/unequip weapons
    // [Fire] Rate limit using attack speed from PlayerStats
    // [Reload] Trigger reload or recharge depending on weapon type
}


