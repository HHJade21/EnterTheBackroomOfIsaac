using UnityEngine;

// ScriptableObject that defines weapon configuration
// - Keeps weapon stats decoupled from prefabs allowing designers to tweak values easily
[CreateAssetMenu(fileName = "WeaponData", menuName = "EnterTheBackroomOfIsaac/Data/Weapon")]
public class WeaponData : ScriptableObject
{
    public enum WeaponElement
    {
        Cyan,
        Magenta,
        Yellow,
        Key
    }

    [Header("Meta")]
    public string weaponName = "Weapon";   // UI 및 디버깅 표시 이름
    public Sprite icon;                    // UI 아이콘
    public WeaponElement element = WeaponElement.Cyan; // 무기 속성

    [Header("Projectile")]
    public GameObject projectilePrefab;    // 발사할 투사체 프리팹
    public float projectileSpeed = 12f;    // 투사체 속도
    public float projectileLifetime = 1.5f;// 투사체 생존 시간

    [Header("Combat")]
    public float baseDamage = 1f;          // 기본 피해량
    public float fireCooldown = 0.2f;      // 발사 간 딜레이
    public int magazineSize = 10;          // 탄창 탄약 수
    public float reloadTime = 0.6f;        // 재장전 소요 시간
}

