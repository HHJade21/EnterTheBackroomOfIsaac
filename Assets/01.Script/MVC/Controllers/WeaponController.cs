using System.Collections.Generic;
using UnityEngine;

// Mediates between PlayerController and weapon ScriptableObject data
// Responsibilities:
// - Manage up to three WeaponData references the player can carry
// - Spawn projectiles using prefab/settings defined in the currently equipped WeaponData
// - Provide helper accessors for UI/logic to query weapon stats

public class WeaponController : MonoBehaviour
{
    private const int MaxWeapons = 3;

    [Header("Data")]
    [SerializeField] private List<WeaponData> allWeapons = new List<WeaponData>();
    [SerializeField] private List<WeaponData> ownedWeapons = new List<WeaponData>(MaxWeapons);
    [SerializeField] private WeaponData currentWeapon;

    public WeaponData CurrentWeapon => currentWeapon;
    public IReadOnlyList<WeaponData> OwnedWeapons => ownedWeapons;
    public int CurrentWeaponIndex => ownedWeapons.IndexOf(currentWeapon);

    private void Awake()
    {
        if (currentWeapon != null && !ownedWeapons.Contains(currentWeapon))
        {
            if (ownedWeapons.Count < MaxWeapons)
            {
                ownedWeapons.Add(currentWeapon);
            }
        }

        if (currentWeapon == null && ownedWeapons.Count > 0)
        {
            currentWeapon = ownedWeapons[0];
        }

        if (ownedWeapons.Count > MaxWeapons)
        {
            ownedWeapons.RemoveRange(MaxWeapons, ownedWeapons.Count - MaxWeapons);
        }
    }

    public bool AddWeapon(WeaponData data, bool makeCurrent = true)
    {
        if (data == null) return false;

        if (!ownedWeapons.Contains(data))
        {
            if (ownedWeapons.Count >= MaxWeapons)
            {
                Debug.LogWarning($"Weapon inventory full ({MaxWeapons}). Cannot add {data.name}.");
                return false;
            }
            ownedWeapons.Add(data);
        }

        if (makeCurrent)
        {
            SetCurrentWeapon(data);
        }

        return true;
    }

    public bool EquipWeapon(WeaponData data)
    {
        if (data == null) return false;
        if (!ownedWeapons.Contains(data)) return false;

        SetCurrentWeapon(data);
        return true;
    }

    public bool EquipWeaponByIndex(int index)
    {
        if (index < 0 || index >= ownedWeapons.Count) return false;

        var data = ownedWeapons[index];
        if (data == currentWeapon) return false;

        SetCurrentWeapon(data);
        return true;
    }

    private void SetCurrentWeapon(WeaponData data)
    {
        currentWeapon = data;
    }

    public void Fire(Vector2 dir, Transform startPoint)
    {
        if (currentWeapon == null) return;
        if (currentWeapon.projectilePrefab == null) return;

        dir = dir.normalized;
        GameObject projectile = Instantiate(currentWeapon.projectilePrefab, startPoint.position, startPoint.rotation);
        projectile.transform.up = dir;

        var rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = dir * currentWeapon.projectileSpeed;
        }

        Destroy(projectile, currentWeapon.projectileLifetime);
    }

    public float GetFireCooldown() => currentWeapon != null ? currentWeapon.fireCooldown : 0.2f;
    public float GetReloadTime() => currentWeapon != null ? currentWeapon.reloadTime : 0.6f;
    public int GetMagazineSize() => currentWeapon != null ? currentWeapon.magazineSize : 0;
    public float GetBaseDamage() => currentWeapon != null ? currentWeapon.baseDamage : 0f;
    public Sprite GetIcon() => currentWeapon != null ? currentWeapon.icon : null;
    public string GetWeaponName() => currentWeapon != null ? currentWeapon.weaponName : "Weapon";
}