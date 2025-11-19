using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Mediates between PlayerController and weapon ScriptableObject data
// Responsibilities:
// - Manage up to three WeaponData references the player can carry
// - Spawn projectiles using prefab/settings defined in the currently equipped WeaponData
// - Provide helper accessors for UI/logic to query weapon stats

public class WeaponController : MonoBehaviour
{
    private const int allWeaponsCount = 3;
    private const int MaxWeapons = 3;

    [Header("Data")]
    [SerializeField] private List<WeaponData> allWeapons = new List<WeaponData>(allWeaponsCount);
    [SerializeField] private List<WeaponData> ownedWeapons = new List<WeaponData>(MaxWeapons);
    [SerializeField] private WeaponData currentWeapon;
    [SerializeField] private List<bool> droppedWeapons = new List<bool>(allWeaponsCount);//이번 게임에서 한 번이라도 드랍된 무기들은 여기서 1로 바뀌고 다시는 등장하지 않음.

    public WeaponData CurrentWeapon => currentWeapon;
    public IReadOnlyList<WeaponData> OwnedWeapons => ownedWeapons;
    public int CurrentWeaponIndex => ownedWeapons.IndexOf(currentWeapon);
    public GameObject weaponPrefab;

    private void Awake()
    {
        //기본무기 드랍체크(중복으로 안 뜨게)
        EnsureDroppedWeaponList();


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

    //새 무기 생성 전에 랜덤으로 골라주는 메소드
    public int RandomWeapon()
    {
        EnsureDroppedWeaponList();
        int res = 0;
        int guard = 0;
        do
        {
            res = Random.Range(0, allWeapons.Count);
            guard++;
            if (guard > 100)
                break;
        } while (droppedWeapons[res]);
        return res;
    } 
    
    //새 무기 드랍 메소드
    public void SpawnNewWeapon()
    {
        EnsureDroppedWeaponList();
        int itemID = RandomWeapon();
        GameObject newWeapon = Instantiate(weaponPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        newWeapon.GetComponent<SpriteRenderer>().sprite = allWeapons[itemID].icon;
        newWeapon.GetComponent<newWeapon>().itemID = itemID;
        droppedWeapons[itemID] = true;
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

    private void EnsureDroppedWeaponList()
    {
        if (allWeapons == null) allWeapons = new List<WeaponData>();
        if (droppedWeapons == null) droppedWeapons = new List<bool>();

        while (droppedWeapons.Count < allWeapons.Count)
        {
            droppedWeapons.Add(false);
        }

        if (droppedWeapons.Count > allWeapons.Count)
        {
            droppedWeapons.RemoveRange(allWeapons.Count, droppedWeapons.Count - allWeapons.Count);
        }

        if (droppedWeapons.Count > 0)
        {
            droppedWeapons[0] = true;
        }
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