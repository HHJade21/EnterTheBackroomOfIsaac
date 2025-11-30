using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Mediates between PlayerController and weapon ScriptableObject data
// Responsibilities:
// - Manage up to three WeaponData references the player can carry
// - Spawn projectiles using prefab/settings defined in the currently equipped WeaponData
// - Provide helper accessors for UI/logic to query weapon stats
// - Manage weapon stats (ammo, cooldown, reload)
// - Manage weapon icon display

public class WeaponController : MonoBehaviour
{
    private const int allWeaponsCount = 17;//구현된 모든 무기 종류의 개수를 여기 표시
    private const int MaxWeapons = 17;//플레이어가 소지할 수 있는 최대 무기 개수

    [Header("Data")]
    [SerializeField] private List<WeaponData> allWeapons = new List<WeaponData>(allWeaponsCount);
    [SerializeField] private List<WeaponData> ownedWeapons = new List<WeaponData>(MaxWeapons);
    [SerializeField] private WeaponData currentWeapon;
    [SerializeField] private List<bool> droppedWeapons = new List<bool>(allWeaponsCount);//이번 게임에서 한 번이라도 드랍된 무기들은 여기서 1로 바뀌고 다시는 등장하지 않음.

    [Header("Weapon Stats")]
    public int maxBulletCount = 10;        // 최대 탄약 수
    public int currentBulletCount = 10;    // 현재 탄약 수
    public float attackCooldown = 0.2f;    // 발사 쿨다운
    public float reloadTime = 0.6f;        // 재장전 시간
    private float lastFireTime;            // 마지막 발사 시간
    private bool isReloading;              // 재장전 중 여부

    [Header("Weapon Icon")]
    [Tooltip("무기 아이콘을 표시할 Renderer (자동으로 찾거나 생성됩니다)")]
    public SpriteRenderer weaponIconRenderer;   // 현재 무기 아이콘을 표시할 Renderer
    [Tooltip("플레이어로부터 아이콘까지의 거리")]
    public float weaponIconDistance = 0.7f;     // 플레이어로부터의 거리
    [Tooltip("아이콘이 목표 위치를 따라가는 속도")]
    public float weaponIconFollowSpeed = 10f;   // 추적 보간 속도
    [Tooltip("스프라이트가 왼쪽을 바라보고 있을 때 필요한 회전 오프셋 (도 단위, 기본 180도는 자동 적용됨)")]
    public float weaponIconRotationOffset = 0f; // 스프라이트 기본 방향 보정 (추가 미세 조정용)

    [Header("Audio")]
    public AudioClip fireSound;
    public AudioClip reloadSound;

    [Header("Melee Attack")]
    [Tooltip("근접 공격용 Collider (일시적으로 활성화됨)")]
    public Collider2D meleeAttackCollider;
    [Tooltip("근접 공격 이펙트 스프라이트 렌더러 (일시적으로 활성화됨)")]
    public SpriteRenderer meleeEffectRenderer;
    [Tooltip("근접 공격 지속 시간 (초)")]
    public float meleeAttackDuration = 0.2f;

    public WeaponData CurrentWeapon => currentWeapon;
    public IReadOnlyList<WeaponData> OwnedWeapons => ownedWeapons;
    public int CurrentWeaponIndex => ownedWeapons.IndexOf(currentWeapon);
    public GameObject weaponPrefab;
    public bool IsReloading => isReloading;
    public int CurrentBulletCount => currentBulletCount;
    public int MaxBulletCount => maxBulletCount;

    /// <summary>
    /// 초기화 메소드: 게임 시작 시 무기 인벤토리와 드랍 리스트를 설정합니다.
    /// - 드랍된 무기 리스트 초기화
    /// - 현재 무기가 인벤토리에 없으면 추가
    /// - 현재 무기가 없으면 첫 번째 무기를 장착
    /// - 인벤토리 크기 제한 확인
    /// </summary>
    private void Awake()
    {
        //기본무기 드랍체크(중복으로 안 뜨게)
        EnsureDroppedWeaponList();

        // 무기 아이콘 SpriteRenderer 자동 찾기 또는 생성
        EnsureWeaponIconRenderer();

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

    private void Start()
    {
        // 근접 공격용 컴포넌트 자동 찾기 또는 생성
        EnsureMeleeAttackComponents();

        // 초기 무기 스탯 동기화
        if (currentWeapon != null)
        {
            SyncWeaponStats(forceResetAmmo: true);
            UpdateWeaponIconSprite();
        }
    }

    /// <summary>
    /// 랜덤 무기 선택 메소드: 아직 드랍되지 않은 무기 중에서 랜덤으로 하나를 선택합니다.
    /// - 드랍된 무기 리스트 확인
    /// - 드랍되지 않은 무기를 찾을 때까지 반복 (최대 100회)
    /// - 선택된 무기의 인덱스를 반환
    /// </summary>
    /// <returns>선택된 무기의 인덱스</returns>
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
    
    /// <summary>
    /// 새 무기 드랍 메소드: 바닥에 새로운 무기 프리팹을 생성합니다.
    /// - 랜덤으로 무기 선택
    /// - 무기 프리팹을 인스턴스화
    /// - 선택된 무기의 아이콘과 ID 설정
    /// - 해당 무기를 드랍된 목록에 추가 (중복 방지)
    /// </summary>
    public void SpawnNewWeapon()
    {
        EnsureDroppedWeaponList();
        int itemID = RandomWeapon();
        GameObject newWeapon = Instantiate(weaponPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        newWeapon.GetComponent<SpriteRenderer>().sprite = allWeapons[itemID].icon;
        newWeapon.GetComponent<newWeapon>().itemID = itemID;
        droppedWeapons[itemID] = true;
    }

    /// <summary>
    /// 무기 추가 메소드: 플레이어의 인벤토리에 새로운 무기를 추가합니다.
    /// - 인벤토리가 가득 찬 경우 추가 실패
    /// - 이미 가지고 있는 무기는 추가하지 않음
    /// - makeCurrent가 true이면 추가한 무기를 즉시 장착
    /// </summary>
    /// <param name="data">추가할 무기 데이터</param>
    /// <param name="makeCurrent">추가 후 즉시 장착할지 여부 (기본값: true)</param>
    /// <returns>추가 성공 여부</returns>
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

    /// <summary>
    /// 무기 장착 메소드: 인벤토리에 있는 무기를 현재 무기로 장착합니다.
    /// - 인벤토리에 없는 무기는 장착 불가
    /// </summary>
    /// <param name="data">장착할 무기 데이터</param>
    /// <returns>장착 성공 여부</returns>
    public bool EquipWeapon(WeaponData data)
    {
        if (data == null) return false;
        if (!ownedWeapons.Contains(data)) return false;

        SetCurrentWeapon(data);
        return true;
    }

    public int GetWeaponCount()
    {
        return ownedWeapons.Count;
    }

    public WeaponData GetCurrentWeaponData()
    {
        return currentWeapon;
    }

    /// <summary>
    /// 인덱스로 무기 장착 메소드: 인벤토리의 특정 인덱스에 있는 무기를 장착합니다.
    /// - 인덱스 범위 확인
    /// - 이미 장착 중인 무기는 장착하지 않음
    /// </summary>
    /// <param name="index">장착할 무기의 인벤토리 인덱스 (0~2)</param>
    /// <returns>장착 성공 여부</returns>
    public bool EquipWeaponByIndex(int index)
    {
        if (index < 0 || index >= ownedWeapons.Count) return false;

        var data = ownedWeapons[index];
        if (data == currentWeapon) return false;

        SetCurrentWeapon(data);
        return true;
    }

    /// <summary>
    /// 현재 무기 설정 메소드: 내부적으로 현재 무기를 변경합니다.
    /// </summary>
    /// <param name="data">설정할 무기 데이터</param>
    private void SetCurrentWeapon(WeaponData data)
    {
        currentWeapon = data;
        SyncWeaponStats(forceResetAmmo: true);
        UpdateWeaponIconSprite();
    }

    /// <summary>
    /// 드랍된 무기 리스트 관리 메소드: 드랍된 무기 추적 리스트를 allWeapons 리스트 크기에 맞춰 동기화합니다.
    /// - allWeapons와 droppedWeapons 리스트 초기화 확인
    /// - droppedWeapons 크기를 allWeapons 크기에 맞춤
    /// - 첫 번째 무기(기본 무기)는 항상 드랍된 것으로 표시 (중복 방지)
    /// </summary>
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

    /// <summary>
    /// 무기 타입에 따른 공격 메소드: 현재 무기의 타입에 맞는 공격을 수행합니다.
    /// </summary>
    /// <param name="dir">공격 방향 (정규화됨)</param>
    /// <param name="startPoint">공격 시작 위치와 회전</param>
    public void Attack(Vector2 dir, Transform startPoint)
    {
        if (currentWeapon == null) return;

        switch (currentWeapon.weaponType)
        {
            case WeaponData.WeaponType.Melee:
                MeleeAttack(dir, startPoint);
                break;
            case WeaponData.WeaponType.Fire:
                FireAttack(dir, startPoint);
                break;
            case WeaponData.WeaponType.Laser:
                LaserAttack(dir, startPoint);
                break;
        }
    }

    /// <summary>
    /// 근접 공격 메소드: 플레이어 주변, 마우스 방향에 collider와 이펙트를 일시적으로 활성화합니다.
    /// </summary>
    /// <param name="dir">공격 방향 (정규화됨)</param>
    /// <param name="startPoint">공격 시작 위치와 회전</param>
    private void MeleeAttack(Vector2 dir, Transform startPoint)
    {
        if (currentWeapon == null) return;

        // Collider 활성화
        if (meleeAttackCollider != null)
        {
            meleeAttackCollider.enabled = true;
            // Collider 위치를 무기 아이콘 위치로 설정
            meleeAttackCollider.transform.position = startPoint.position;
            // Collider 회전을 공격 방향으로 설정
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            meleeAttackCollider.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // 이펙트 스프라이트 활성화
        if (meleeEffectRenderer != null && currentWeapon.fireEffect != null)
        {
            meleeEffectRenderer.enabled = true;
            meleeEffectRenderer.sprite = currentWeapon.fireEffect;
            meleeEffectRenderer.transform.position = startPoint.position;
            // 이펙트 회전을 공격 방향으로 설정
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            meleeEffectRenderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // 사운드 재생
        if (currentWeapon.fireSound != null)
        {
            AudioSource.PlayClipAtPoint(currentWeapon.fireSound, startPoint.position);
        }

        // 일정 시간 후 비활성화
        StartCoroutine(DisableMeleeAttackAfterDelay());
    }

    /// <summary>
    /// 근접 공격 비활성화 코루틴: 일정 시간 후 collider와 이펙트를 비활성화합니다.
    /// </summary>
    private IEnumerator DisableMeleeAttackAfterDelay()
    {
        yield return new WaitForSeconds(meleeAttackDuration);

        if (meleeAttackCollider != null)
        {
            meleeAttackCollider.enabled = false;
        }

        if (meleeEffectRenderer != null)
        {
            meleeEffectRenderer.enabled = false;
        }
    }

    /// <summary>
    /// 발사 공격 메소드: 현재 장착된 무기로 투사체를 발사합니다.
    /// - 현재 무기와 투사체 프리팹 확인
    /// - 지정된 위치와 방향으로 투사체 생성
    /// - 투사체에 속도 적용
    /// - 투사체 수명 시간 후 자동 파괴
    /// </summary>
    /// <param name="dir">발사 방향 (정규화됨)</param>
    /// <param name="startPoint">발사 시작 위치와 회전</param>
    private void FireAttack(Vector2 dir, Transform startPoint)
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

    /// <summary>
    /// 레이저 공격 메소드: 추후 구현 예정입니다.
    /// </summary>
    /// <param name="dir">공격 방향 (정규화됨)</param>
    /// <param name="startPoint">공격 시작 위치와 회전</param>
    private void LaserAttack(Vector2 dir, Transform startPoint)
    {
        // TODO: 레이저 공격 구현 예정
    }

    /// <summary>
    /// 발사 메소드: 현재 장착된 무기로 투사체를 발사합니다. (하위 호환성을 위해 유지)
    /// </summary>
    /// <param name="dir">발사 방향 (정규화됨)</param>
    /// <param name="startPoint">발사 시작 위치와 회전</param>
    [System.Obsolete("Use Attack() method instead. This method is kept for backward compatibility.")]
    public void Fire(Vector2 dir, Transform startPoint)
    {
        FireAttack(dir, startPoint);
    }

    /// <summary>
    /// 발사 쿨다운 반환 메소드: 현재 무기의 발사 쿨다운 시간을 반환합니다.
    /// </summary>
    /// <returns>발사 쿨다운 시간 (초), 무기가 없으면 0.2초</returns>
    public float GetFireCooldown() => currentWeapon != null ? currentWeapon.fireCooldown : 0.2f;
    
    /// <summary>
    /// 재장전 시간 반환 메소드: 현재 무기의 재장전 시간을 반환합니다.
    /// </summary>
    /// <returns>재장전 시간 (초), 무기가 없으면 0.6초</returns>
    public float GetReloadTime() => currentWeapon != null ? currentWeapon.reloadTime : 0.6f;
    
    /// <summary>
    /// 탄창 크기 반환 메소드: 현재 무기의 탄창 크기를 반환합니다.
    /// </summary>
    /// <returns>탄창 크기, 무기가 없으면 0</returns>
    public int GetMagazineSize() => currentWeapon != null ? currentWeapon.magazineSize : 0;
    
    /// <summary>
    /// 기본 데미지 반환 메소드: 현재 무기의 기본 데미지를 반환합니다.
    /// </summary>
    /// <returns>기본 데미지, 무기가 없으면 0</returns>
    public float GetBaseDamage() => currentWeapon != null ? currentWeapon.baseDamage : 0f;
    
    /// <summary>
    /// 아이콘 반환 메소드: 현재 무기의 아이콘 스프라이트를 반환합니다.
    /// </summary>
    /// <returns>무기 아이콘 스프라이트, 무기가 없으면 null</returns>
    public Sprite GetIcon() => currentWeapon != null ? currentWeapon.icon : null;
    
    /// <summary>
    /// 무기 이름 반환 메소드: 현재 무기의 이름을 반환합니다.
    /// </summary>
    /// <returns>무기 이름, 무기가 없으면 "Weapon"</returns>
    public string GetWeaponName() => currentWeapon != null ? currentWeapon.weaponName : "Weapon";
    
    /// <summary>
    /// 아이템 ID로 무기 데이터 반환 메소드: itemID를 사용하여 allWeapons 리스트에서 WeaponData를 가져옵니다.
    /// </summary>
    /// <param name="itemID">무기의 아이템 ID</param>
    /// <returns>해당 ID의 WeaponData, 없으면 null</returns>
    public WeaponData GetWeaponDataByID(int itemID)
    {
        if (itemID < 0 || itemID >= allWeapons.Count) return null;
        return allWeapons[itemID];
    }

    /// <summary>
    /// 무기 스탯 동기화 메소드: 현재 무기의 데이터로부터 스탯을 동기화합니다.
    /// </summary>
    /// <param name="forceResetAmmo">탄약을 강제로 최대치로 리셋할지 여부</param>
    public void SyncWeaponStats(bool forceResetAmmo = false)
    {
        if (currentWeapon == null) return;

        maxBulletCount = Mathf.Max(0, currentWeapon.magazineSize);
        attackCooldown = currentWeapon.fireCooldown;
        reloadTime = currentWeapon.reloadTime;

        if (forceResetAmmo)
        {
            currentBulletCount = maxBulletCount;
        }
        else
        {
            currentBulletCount = Mathf.Clamp(currentBulletCount, 0, maxBulletCount);
            if (currentBulletCount == 0)
            {
                currentBulletCount = maxBulletCount;
            }
        }
    }

    /// <summary>
    /// 발사 가능 여부 체크 메소드: 현재 발사 가능한지 확인합니다.
    /// </summary>
    /// <returns>발사 가능 여부</returns>
    public bool CanFire()
    {
        if (currentWeapon == null) return false;
        if (isReloading) return false;
        if (currentBulletCount <= 0) return false;
        if (Time.time < lastFireTime + attackCooldown) return false;
        return true;
    }

    /// <summary>
    /// 발사 처리 메소드: 무기 타입에 맞는 공격을 처리하고 탄약을 감소시킵니다.
    /// </summary>
    /// <param name="dir">공격 방향</param>
    /// <param name="startPoint">공격 시작 위치</param>
    /// <param name="fireOrigin">공격 오리진 위치 (오디오용)</param>
    /// <returns>공격 성공 여부</returns>
    public bool OnFire(Vector2 dir, Transform startPoint, Vector3 fireOrigin)
    {
        if (!CanFire()) return false;

        // 무기 타입에 따른 공격 실행
        Attack(dir, startPoint);

        // 발사 시간 기록 및 탄약 감소 (발사 타입만 탄약 소모)
        lastFireTime = Time.time;
        if (currentWeapon != null && currentWeapon.weaponType == WeaponData.WeaponType.Fire)
        {
            currentBulletCount--;
        }

        // 발사 사운드 재생 (WeaponData의 fireSound가 있으면 우선 사용, 없으면 기본 fireSound 사용)
        AudioClip soundToPlay = (currentWeapon != null && currentWeapon.fireSound != null) 
            ? currentWeapon.fireSound 
            : fireSound;
        if (soundToPlay != null)
        {
            AudioSource.PlayClipAtPoint(soundToPlay, fireOrigin);
        }

        return true;
    }

    /// <summary>
    /// 재장전 시작 메소드: 재장전을 시작합니다.
    /// </summary>
    /// <returns>재장전 시작 성공 여부</returns>
    public bool Reload(Vector3 reloadOrigin)
    {
        // 이미 재장전 중이거나 탄약이 최대면 무시
        if (isReloading) return false;
        if (currentBulletCount >= maxBulletCount) return false;

        // 재장전 시작
        StartCoroutine(ReloadRoutine());

        // 재장전 사운드 재생
        if (reloadSound != null)
        {
            AudioSource.PlayClipAtPoint(reloadSound, reloadOrigin);
        }

        return true;
    }

    /// <summary>
    /// 재장전 코루틴: 재장전 시간만큼 대기 후 탄약을 복구합니다.
    /// </summary>
    private IEnumerator ReloadRoutine()
    {
        isReloading = true; // 재장전 상태 시작

        // reloadTime만큼 대기
        yield return new WaitForSeconds(reloadTime);

        // 탄약을 최대치로 복구
        currentBulletCount = maxBulletCount;

        isReloading = false; // 재장전 상태 종료
    }

    /// <summary>
    /// 무기 교체 메소드: 다음 무기로 교체합니다.
    /// </summary>
    /// <returns>교체 성공 여부</returns>
    public bool SwapWeapon()
    {
        if (ownedWeapons.Count <= 1) return false;

        int currentIndex = CurrentWeaponIndex;
        int nextIndex = (currentIndex + 1) % ownedWeapons.Count;

        return TryEquipWeaponSlot(nextIndex);
    }

    /// <summary>
    /// 슬롯으로 무기 장착 시도 메소드: 특정 슬롯의 무기를 장착합니다.
    /// </summary>
    /// <param name="slotIndex">장착할 슬롯 인덱스</param>
    /// <returns>장착 성공 여부</returns>
    public bool TryEquipWeaponSlot(int slotIndex)
    {
        if (EquipWeaponByIndex(slotIndex))
        {
            SyncWeaponStats(forceResetAmmo: true);
            UpdateWeaponIconSprite();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 근접 공격용 컴포넌트 자동 찾기 또는 생성 메소드: meleeAttackCollider와 meleeEffectRenderer가 없으면 자동으로 찾거나 생성합니다.
    /// </summary>
    private void EnsureMeleeAttackComponents()
    {
        // Collider 찾기 또는 생성
        if (meleeAttackCollider == null)
        {
            // 자식 오브젝트에서 "MeleeAttackCollider" 이름으로 찾기
            Transform colliderTransform = transform.Find("MeleeAttackCollider");
            if (colliderTransform != null)
            {
                meleeAttackCollider = colliderTransform.GetComponent<Collider2D>();
            }

            // 찾지 못했으면 새로 생성
            if (meleeAttackCollider == null)
            {
                GameObject colliderObj = new GameObject("MeleeAttackCollider");
                colliderObj.transform.SetParent(transform);
                colliderObj.transform.localPosition = Vector3.zero;
                meleeAttackCollider = colliderObj.AddComponent<CircleCollider2D>();
                meleeAttackCollider.isTrigger = true;
                meleeAttackCollider.enabled = false; // 기본적으로 비활성화
            }
        }

        // Effect Renderer 찾기 또는 생성
        if (meleeEffectRenderer == null)
        {
            // 자식 오브젝트에서 "MeleeEffectRenderer" 이름으로 찾기
            Transform effectTransform = transform.Find("MeleeEffectRenderer");
            if (effectTransform != null)
            {
                meleeEffectRenderer = effectTransform.GetComponent<SpriteRenderer>();
            }

            // 찾지 못했으면 새로 생성
            if (meleeEffectRenderer == null)
            {
                GameObject effectObj = new GameObject("MeleeEffectRenderer");
                effectObj.transform.SetParent(transform);
                effectObj.transform.localPosition = Vector3.zero;
                meleeEffectRenderer = effectObj.AddComponent<SpriteRenderer>();
                meleeEffectRenderer.enabled = false; // 기본적으로 비활성화
                meleeEffectRenderer.sortingOrder = 15; // 무기 아이콘보다 앞에 표시
            }
        }
    }

    /// <summary>
    /// 무기 아이콘 SpriteRenderer 자동 찾기 또는 생성 메소드: weaponIconRenderer가 없으면 자동으로 찾거나 생성합니다.
    /// </summary>
    private void EnsureWeaponIconRenderer()
    {
        // 이미 할당되어 있으면 무시
        if (weaponIconRenderer != null) return;

        // 자식 오브젝트에서 "WeaponIcon" 이름으로 찾기
        Transform weaponIconTransform = transform.Find("WeaponIcon");
        if (weaponIconTransform != null)
        {
            weaponIconRenderer = weaponIconTransform.GetComponent<SpriteRenderer>();
            if (weaponIconRenderer != null) return;
        }

        // 전체 하이어라키에서 "WeaponIcon" 이름으로 찾기 (같은 부모 기준)
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.gameObject.name == "WeaponIcon")
            {
                weaponIconRenderer = renderer;
                return;
            }
        }

        // 찾지 못했으면 새로 생성
        GameObject weaponIconObj = new GameObject("WeaponIcon");
        weaponIconObj.transform.SetParent(transform);
        weaponIconObj.transform.localPosition = Vector3.zero;
        weaponIconRenderer = weaponIconObj.AddComponent<SpriteRenderer>();
        
        // Sorting Layer 설정 (플레이어보다 앞에 표시되도록)
        weaponIconRenderer.sortingOrder = 10;
    }

    /// <summary>
    /// 무기 아이콘 스프라이트 업데이트 메소드: 현재 무기의 아이콘을 표시합니다.
    /// </summary>
    public void UpdateWeaponIconSprite()
    {
        // weaponIconRenderer가 없으면 자동으로 찾거나 생성
        if (weaponIconRenderer == null)
        {
            EnsureWeaponIconRenderer();
        }

        if (weaponIconRenderer == null) return;

        if (currentWeapon == null)
        {
            weaponIconRenderer.sprite = null;
            weaponIconRenderer.enabled = false;
            return;
        }

        // currentWeapon.icon을 weaponIconRenderer.sprite에 설정
        weaponIconRenderer.sprite = currentWeapon.icon;
        weaponIconRenderer.enabled = weaponIconRenderer.sprite != null;
    }

    /// <summary>
    /// 무기 아이콘 위치 및 회전 업데이트 메소드: 플레이어 위치와 마우스 커서를 기준으로 아이콘을 배치합니다.
    /// </summary>
    /// <param name="playerPos">플레이어 위치</param>
    /// <param name="snapImmediate">즉시 이동할지 여부</param>
    public void UpdateWeaponIconTransform(Vector3 playerPos, bool snapImmediate = false)
    {
        if (weaponIconRenderer == null || !weaponIconRenderer.enabled) return;

        Vector3 direction = Vector3.right;

        if (Camera.main != null && UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
            mouseWorld.z = playerPos.z;
            direction = (mouseWorld - playerPos);
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.right;
        }
        else
        {
            direction.Normalize();
        }

        Vector3 targetPos = playerPos + direction * weaponIconDistance;
        Transform iconTransform = weaponIconRenderer.transform;

        if (snapImmediate)
        {
            iconTransform.position = targetPos;
        }
        else
        {
            iconTransform.position = Vector3.Lerp(iconTransform.position, targetPos, weaponIconFollowSpeed * Time.deltaTime);
        }

        // Atan2는 오른쪽(1,0)을 0도로 계산하지만, 스프라이트는 왼쪽을 기본 방향으로 함
        // 따라서 180도를 더해서 올바른 방향으로 회전시킴
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180f;
        iconTransform.rotation = Quaternion.Euler(0f, 0f, angle + weaponIconRotationOffset);

        // 왼쪽에 있을 경우 상하 반전
        weaponIconRenderer.flipY = direction.x > 0f;
    }
}