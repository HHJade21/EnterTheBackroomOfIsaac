using UnityEngine;

// ScriptableObject that defines weapon configuration
// - Keeps weapon stats decoupled from prefabs allowing designers to tweak values easily
[CreateAssetMenu(fileName = "WeaponData", menuName = "EnterTheBackroomOfIsaac/Data/Weapon")]
public class WeaponData : ScriptableObject
{
    public enum WeaponElement
    {
        Key = 0,      // 검정
        Cyan = 1,     // 청록
        Magenta = 2,  // 자홍
        Yellow = 3,    // 노랑
    }

    public enum WeaponType
    {
        Melee = 0,    // 근접 공격
        Fire = 1,     // 발사 공격 (투사체)
        Multi = 2,   // 산탄 공격
        ChargeFire = 3,   // 차지 공격
        ChargeDash = 4,   // 차지 대시 공격
    }

    [Header("Meta")]
    public int itemID = 0;   // 무기 고유 번호
    public string weaponName = "Weapon";   // UI 및 디버깅 표시 이름
    public string description = "Description";   // UI 및 디버깅 표시 설명
    public Sprite icon;                    // UI 아이콘
    public WeaponElement element = WeaponElement.Cyan; // 무기 속성
    public WeaponType weaponType = WeaponType.Fire;   // 무기 타입
    public bool autoFire = false; // 자동 발사 여부

    [Header("Projectile")]
    public GameObject projectilePrefab;    // 발사할 투사체 프리팹
    public float projectileSpeed = 12f;    // 투사체 속도
    public float projectileLifetime = 1.5f;// 투사체 생존 시간

    [Header("Combat")]
    public float baseDamage = 1f;          // 기본 피해량
    public float fireCooldown = 0.2f;      // 발사 간 딜레이
    public int magazineSize = 10;          // 탄창 탄약 수
    public float reloadTime = 0.6f;        // 재장전 소요 시간
    public AudioClip fireSound;            // 공격 사운드 (모든 타입 공통)
    public AudioClip reloadSound;            // 재장전 사운드 (모든 타입 공통)
    public Sprite fireEffect;              // 공격 이펙트 스프라이트 (근접 공격용)

    [Header("Swap Skill")]
    [Tooltip("무기 교체 시 발동하는 스킬 (null이면 스킬 없음)")]
    public WeaponSwapSkillData swapSkillData;
}

