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
    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;

    public void Fire(Vector2 dir, Transform startPoint)
    {
        GameObject bullet = Instantiate(bulletPrefab, startPoint.position, Quaternion.identity);
        bullet.transform.up = dir;
        bullet.GetComponent<Rigidbody2D>().linearVelocity = dir * bulletSpeed;
        Destroy(bullet, 1f);
    }
}

