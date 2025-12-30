using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using TMPro;
// Mediates between PlayerController and weapon ScriptableObject data
// Responsibilities:
// - Manage up to three WeaponData references the player can carry
// - Spawn projectiles using prefab/settings defined in the currently equipped WeaponData
// - Provide helper accessors for UI/logic to query weapon stats
// - Manage weapon stats (ammo, cooldown, reload)
// - Manage weapon icon display

public class WeaponController : MonoBehaviour
{
    // ========== Constants ==========
    private const int allWeaponsCount = 8;//구현된 모든 무기 종류의 개수를 여기 표시
    private const int MaxWeapons = 2;//플레이어가 소지할 수 있는 최대 무기 개수

    // ========== Weapon Data ==========
    [Header("Data")]
    [SerializeField] private List<WeaponData> allWeapons = new List<WeaponData>(allWeaponsCount);
    [SerializeField] private List<WeaponData> ownedWeapons = new List<WeaponData>(MaxWeapons);
    [SerializeField] private WeaponData currentWeapon;
    [SerializeField] private List<bool> droppedWeapons = new List<bool>(allWeaponsCount);//이번 게임에서 한 번이라도 드랍된 무기들은 여기서 1로 바뀌고 다시는 등장하지 않음.
    [SerializeField] private WeaponData temporaryWeapon;
    public GameObject weaponPrefab;

    // ========== Weapon Stats ==========
    [Header("Weapon Stats")]
    public int[] maxBulletCount = new int[2] {10, 10};        // 최대 탄약 수
    public int[] currentBulletCount = new int[2] {10, 10};    // 현재 탄약 수
    private bool[] isAmmoWeapon = new bool[2] {true, true};
    public float attackCooldown = 0.2f;    // 발사 쿨다운
    public float reloadTime = 0.6f;        // 재장전 시간
    private float lastFireTime;            // 마지막 발사 시간
    private bool isReloading;              // 재장전 중 여부
    public int multiBulletCount = 2;     // 산탄 공격 탄약 수
    public int multiBulletSpread = 60;     // 산탄 공격 탄약 분산
    [Tooltip("각 탄환 발사 간 최소 딜레이 (초)")]
    public float multiBulletMinFireDelay = 0f;
    [Tooltip("각 탄환 발사 간 최대 딜레이 (초)")]
    public float multiBulletMaxFireDelay = 0.1f;
    [Tooltip("투사체 속도 배율 (기본값: 1.0, 아이템 효과용)")]
    public float projectileSpeedMultiplier = 1f;  // 투사체 속도 배율

    // ========== Weapon Icon ==========
    [Header("Weapon Icon")]
    [Tooltip("무기 아이콘을 표시할 Renderer (자동으로 찾거나 생성됩니다)")]
    public SpriteRenderer weaponIconRenderer;   // 현재 무기 아이콘을 표시할 Renderer
    [Tooltip("플레이어로부터 아이콘까지의 거리")]
    public float weaponIconDistance = 0.7f;     // 플레이어로부터의 거리
    [Tooltip("아이콘이 목표 위치를 따라가는 속도")]
    public float weaponIconFollowSpeed = 10f;   // 추적 보간 속도
    [Tooltip("스프라이트가 왼쪽을 바라보고 있을 때 필요한 회전 오프셋 (도 단위, 기본 180도는 자동 적용됨)")]
    public float weaponIconRotationOffset = 0f; // 스프라이트 기본 방향 보정 (추가 미세 조정용)

    // ========== Audio ==========
    [Header("Audio")]
    public AudioClip fireSound;
    public AudioClip reloadSound;

    // ========== Melee Attack ==========
    [Header("Melee Attack")]
    [Tooltip("근접 공격용 Collider (일시적으로 활성화됨)")]
    public Collider2D meleeAttackCollider;
    [Tooltip("근접 공격 이펙트 스프라이트 렌더러 (일시적으로 활성화됨)")]
    public SpriteRenderer meleeEffectRenderer;
    [Tooltip("근접 공격 지속 시간 (초)")]
    public float meleeAttackDuration = 0.2f;

    // ========== Charge Dash Attack ==========
    [Header("Charge Dash Attack")]
    [Tooltip("최대 충전 시간 (초)")]
    public float maxChargeTime = 2f;
    [Tooltip("돌진 거리")]
    public float dashDistance = 5f;
    [Tooltip("돌진 속도")]
    public float dashSpeed = 20f;
    
    public float currentChargeTime = 0f;
    private bool isCharging = false;
    private bool isDashing = false;
    private Vector2 dashDirection;
    private GameObject dashFireEffect;
    private Coroutine chargeDashCoroutine;

    // ========== Charge Fire Attack ==========
    [Header("Charge Fire Attack")]
    [Tooltip("최대 충전 시간 (초)")]
    public float maxChargeFireTime = 2f;
    [Tooltip("레이저 빔 최대 거리")]
    public float maxMultiDistance = 20f;
    
    public float currentChargeFireTime = 0f;
    private bool isChargingFire = false;
    private Vector2 chargeFireDirection;

    // ========== References ==========
    public WeaponSlotUIController weaponSlotUIController;
    public PlayerController playerController;
    
    // ========== Reload UI ==========
    [Header("Reload UI")]
    [Tooltip("재장전 중 표시할 텍스트 (플레이어 머리 위에 표시)")]
    public TextMeshPro reloadText; // 월드 스페이스 TextMeshPro
    [Tooltip("플레이어 머리 위에서 텍스트까지의 오프셋 (Y축)")]
    public float reloadTextOffsetY = 1.5f;

    // ========== Properties ==========
    public WeaponData CurrentWeapon => currentWeapon;
    public IReadOnlyList<WeaponData> OwnedWeapons => ownedWeapons;
    public int CurrentWeaponIndex => ownedWeapons.IndexOf(currentWeapon);
    public bool IsReloading => isReloading;
    public bool IsCharging => isCharging || isChargingFire; // ChargeDash 또는 ChargeFire 충전 중인지 확인
    
    // 현재 무기의 탄창 정보를 반환하는 프로퍼티
    public int CurrentBulletCount => GetCurrentBulletCount();
    public int MaxBulletCount => GetMaxBulletCount();
    public bool IsAmmoWeapon => GetIsAmmoWeapon();
    
    // 슬롯별 탄창 정보를 반환하는 메서드
    public int GetCurrentBulletCount(int slotIndex = -1)
    {
        if (currentBulletCount == null) return 0;
        int index = slotIndex >= 0 ? slotIndex : CurrentWeaponIndex;
        if (index < 0 || index >= MaxWeapons || index >= currentBulletCount.Length) return 0;
        return currentBulletCount[index];
    }
    
    public int GetMaxBulletCount(int slotIndex = -1)
    {
        if (maxBulletCount == null) return 0;
        int index = slotIndex >= 0 ? slotIndex : CurrentWeaponIndex;
        if (index < 0 || index >= MaxWeapons || index >= maxBulletCount.Length) return 0;
        return maxBulletCount[index];
    }
    
    public bool GetIsAmmoWeapon(int slotIndex = -1)
    {
        if (isAmmoWeapon == null) return true;
        int index = slotIndex >= 0 ? slotIndex : CurrentWeaponIndex;
        if (index < 0 || index >= MaxWeapons || index >= isAmmoWeapon.Length) return true;
        return isAmmoWeapon[index];
    }

    // ========== Unity Lifecycle ==========
    /// <summary>
    /// 초기화 메소드: 게임 시작 시 무기 인벤토리와 드랍 리스트를 설정합니다.
    /// - 드랍된 무기 리스트 초기화
    /// - 현재 무기가 인벤토리에 없으면 추가
    /// - 현재 무기가 없으면 첫 번째 무기를 장착
    /// - 인벤토리 크기 제한 확인
    /// </summary>
    private void Awake()
    {
        GameManager.Instance.weaponController = this;
        // 배열 초기화 (null이거나 크기가 맞지 않으면 초기화)
        if (maxBulletCount == null || maxBulletCount.Length != MaxWeapons)
        {
            maxBulletCount = new int[MaxWeapons];
            for (int i = 0; i < MaxWeapons; i++)
            {
                maxBulletCount[i] = 10;
            }
        }
        
        if (currentBulletCount == null || currentBulletCount.Length != MaxWeapons)
        {
            currentBulletCount = new int[MaxWeapons];
            for (int i = 0; i < MaxWeapons; i++)
            {
                currentBulletCount[i] = 10;
            }
        }
        
        if (isAmmoWeapon == null || isAmmoWeapon.Length != MaxWeapons)
        {
            isAmmoWeapon = new bool[MaxWeapons];
            for (int i = 0; i < MaxWeapons; i++)
            {
                isAmmoWeapon[i] = true;
            }
        }
        
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
        playerController = GameManager.Instance.playerController;
        // 근접 공격용 컴포넌트 자동 찾기 또는 생성
        EnsureMeleeAttackComponents();
        
        // 재장전 텍스트 초기화
        EnsureReloadText();

        // 초기 무기 스탯 동기화
        if (currentWeapon != null)
        {
            int slotIndex = CurrentWeaponIndex;
            if (slotIndex >= 0 && slotIndex < MaxWeapons && 
                isAmmoWeapon != null && slotIndex < isAmmoWeapon.Length)
            {
                // 초기 무기의 isAmmoWeapon 설정
                isAmmoWeapon[slotIndex] = currentWeapon.isAmmoWeapon;
            }
            SyncWeaponStats(forceResetAmmo: true);
            UpdateWeaponIconSprite();
        }
    }
    
    private void Update()
    {
        // 재장전 중일 때 텍스트 위치 업데이트
        if (isReloading && reloadText != null && reloadText.gameObject.activeSelf)
        {
            UpdateReloadTextPosition();
        }
    }

    // ========== Weapon Selection & Drop ==========
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
    public int SelectNewWeapon(){
        EnsureDroppedWeaponList();
        int itemID = RandomWeapon();
        return itemID;
    }
    public void SpawnNewWeapon(int itemID, Vector3? spawnPosition = null)
    {
        Vector3 position = spawnPosition ?? new Vector3(0, 0, 0);
        GameObject newWeapon = Instantiate(weaponPrefab, position, Quaternion.identity);
        newWeapon.GetComponent<SpriteRenderer>().sprite = allWeapons[itemID].icon;
        newWeapon.GetComponent<newWeapon>().itemID = itemID;
        droppedWeapons[itemID] = true;
    }
    public void DevTool_DropNewWeapon(Vector3? spawnPosition = null){
        int newID = SelectNewWeapon();
        SpawnNewWeapon(newID, spawnPosition);
    }

    public void DevTool_DropNewWeapon_DefaultPos() // Vector3? << 물음표 들어가면 OnClick에서 안 떠서 임시로 만듬
    {
        DevTool_DropNewWeapon((Vector3?)null);
    }

    /// <summary>
    /// 특정 인덱스의 무기를 필드 위에 생성하는 메소드: allWeapons[idx]에 해당하는 무기를 생성합니다.
    /// </summary>
    /// <param name="idx">생성할 무기의 인덱스 (allWeapons 리스트의 인덱스)</param>
    /// <param name="spawnPosition">생성 위치 (null이면 기본값 사용)</param>
    public void SpawnWeaponByIndex(int idx, Vector3? spawnPosition = null)
    {
        if (idx < 0 || idx >= allWeapons.Count)
        {
            Debug.LogWarning($"WeaponController: SpawnWeaponByIndex 실패 - 인덱스 {idx}가 범위를 벗어났습니다. (allWeapons.Count: {allWeapons.Count})");
            return;
        }

        SpawnNewWeapon(idx, spawnPosition);
    }

    // ========== Weapon Management ==========
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
                //Debug.LogWarning($"Weapon inventory full ({MaxWeapons}). Cannot add {data.name}.");
                //return false;
                temporaryWeapon = data;
                OpenweaponSlotUIController(ownedWeapons[0], ownedWeapons[1], data);
            }
            else
            {
                ownedWeapons.Add(data);
                // 새 무기를 추가한 슬롯의 탄창 정보 초기화
                int newSlotIndex = ownedWeapons.Count - 1;
                if (newSlotIndex >= 0 && newSlotIndex < MaxWeapons &&
                    isAmmoWeapon != null && newSlotIndex < isAmmoWeapon.Length &&
                    maxBulletCount != null && newSlotIndex < maxBulletCount.Length &&
                    currentBulletCount != null && newSlotIndex < currentBulletCount.Length)
                {
                    isAmmoWeapon[newSlotIndex] = data.isAmmoWeapon;
                    maxBulletCount[newSlotIndex] = data.magazineSize;
                    currentBulletCount[newSlotIndex] = data.magazineSize; // 처음 추가 시 최대치로 설정
                }
            }
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
        if (data == null)
        {
            Debug.LogWarning($"WeaponController: EquipWeaponByIndex 실패 - ownedWeapons[{index}]가 null 입니다.");
            return false;
        }
        if (data == currentWeapon) return false;

        SetCurrentWeapon(data);
        return true;
    }

    /// <summary>
    /// 현재 무기 설정 메소드: 내부적으로 현재 무기를 변경합니다.
    /// - 무기 교체 시 교체 스킬을 실행합니다.
    /// - 이전 무기의 탄창 정보를 저장하고, 새 무기의 탄창 정보를 불러옵니다.
    /// </summary>
    /// <param name="data">설정할 무기 데이터</param>
    private void SetCurrentWeapon(WeaponData data)
    {
        // 이전 무기의 슬롯 인덱스 저장 (currentWeapon이 바뀌기 전에)
        int previousIndex = CurrentWeaponIndex;
        
        // 무기 교체 시 교체 스킬 실행
        if (data != null && data.swapSkillData != null)
        {
            if (playerController != null)
            {
                data.swapSkillData.Execute(this, playerController);
            }
        }

        // 새 무기의 슬롯 인덱스 찾기 (currentWeapon 변경 전)
        int newIndex = ownedWeapons.IndexOf(data);
        
        // currentWeapon 변경
        currentWeapon = data;
        
        // 새 무기가 유효한 슬롯에 있는 경우
        if (newIndex >= 0 && newIndex < MaxWeapons && 
            isAmmoWeapon != null && newIndex < isAmmoWeapon.Length)
        {
            // 새 무기의 isAmmoWeapon 설정
            isAmmoWeapon[newIndex] = data != null ? data.isAmmoWeapon : true;
        }
        
        // SyncWeaponStats에서 현재 슬롯의 탄창 정보를 업데이트 (저장된 값 유지)
        SyncWeaponStats(forceResetAmmo: false);
        UpdateWeaponIconSprite();
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
            SyncWeaponStats(forceResetAmmo: false); // 탄약은 저장된 값 유지
            UpdateWeaponIconSprite();

            if (playerController == null)
            {
                playerController = GameManager.Instance.playerController;
            }

            if (playerController != null &&
                slotIndex >= 0 && slotIndex < ownedWeapons.Count &&
                ownedWeapons[slotIndex] != null)
            {
                playerController.ChangeColor((int)ownedWeapons[slotIndex].element);
            }
            return true;
        }
        return false;
    }

    public int GetWeaponCount()
    {
        return ownedWeapons.Count;
    }

    public WeaponData GetCurrentWeaponData()
    {
        return currentWeapon;
    }

    // ========== Weapon Slot UI ==========
    public void OpenweaponSlotUIController(WeaponData slot1Weapon, WeaponData slot2Weapon, WeaponData newWeapon){
        weaponSlotUIController.OpenNewWeaponPanel(slot1Weapon, slot2Weapon, newWeapon);
    }
    public void CloseweaponSlotUIController(){
        weaponSlotUIController.CloseNewWeaponPanel();
    }
    public void ChangeSlot1Weapon(){
        int newID = ownedWeapons[0].itemID;
        SpawnNewWeapon(newID);
        ownedWeapons[0] = temporaryWeapon;
        // 슬롯 0의 탄창 정보를 새 무기로 초기화
        if (temporaryWeapon != null &&
            isAmmoWeapon != null && 0 < isAmmoWeapon.Length &&
            maxBulletCount != null && 0 < maxBulletCount.Length &&
            currentBulletCount != null && 0 < currentBulletCount.Length)
        {
            isAmmoWeapon[0] = temporaryWeapon.isAmmoWeapon;
            maxBulletCount[0] = temporaryWeapon.magazineSize;
            currentBulletCount[0] = temporaryWeapon.magazineSize;
        }
        temporaryWeapon = null;
        CloseweaponSlotUIController();
        TryEquipWeaponSlot(0);
    }
    public void ChangeSlot2Weapon(){
        int newID = ownedWeapons[1].itemID;
        SpawnNewWeapon(newID);
        ownedWeapons[1] = temporaryWeapon;
        // 슬롯 1의 탄창 정보를 새 무기로 초기화
        if (temporaryWeapon != null &&
            isAmmoWeapon != null && 1 < isAmmoWeapon.Length &&
            maxBulletCount != null && 1 < maxBulletCount.Length &&
            currentBulletCount != null && 1 < currentBulletCount.Length)
        {
            isAmmoWeapon[1] = temporaryWeapon.isAmmoWeapon;
            maxBulletCount[1] = temporaryWeapon.magazineSize;
            currentBulletCount[1] = temporaryWeapon.magazineSize;
        }
        temporaryWeapon = null;
        CloseweaponSlotUIController();
        TryEquipWeaponSlot(1);
    }

    // ========== Attack Methods ==========
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
            case WeaponData.WeaponType.Multi:
                MultiAttack(dir, startPoint);
                break;
            case WeaponData.WeaponType.ChargeFire:
                ChargeFireAttack(dir, startPoint);
                break;
            case WeaponData.WeaponType.ChargeDash:
                ChargeDashAttack(dir, startPoint);
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

        // 무기 아이콘 애니메이터에 Attack 애니메이션 강제 재생 (AutoFire 무기가 아닌 경우에만)
        if (!currentWeapon.autoFire && currentWeapon.animatorController != null && weaponIconRenderer != null)
        {
            Animator weaponAnimator = weaponIconRenderer.GetComponent<Animator>();
            if (weaponAnimator != null)
            {
                // Play()를 사용하여 현재 애니메이션을 중단하고 Attack 애니메이션을 처음부터 강제 재생
                weaponAnimator.Play("Attack", 0, 0f);
                
                // meleeAttackDuration 후 Idle로 돌아가도록 코루틴 시작
                StartCoroutine(ResetWeaponAnimatorToIdle(weaponAnimator, meleeAttackDuration));
            }
        }

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

        // 무기 아이콘 애니메이터에 Attack 애니메이션 강제 재생 (AutoFire 무기가 아닌 경우에만)
        if (!currentWeapon.autoFire && currentWeapon.animatorController != null && weaponIconRenderer != null)
        {
            Animator weaponAnimator = weaponIconRenderer.GetComponent<Animator>();
            if (weaponAnimator != null)
            {
                // Play()를 사용하여 현재 애니메이션을 중단하고 Attack 애니메이션을 처음부터 강제 재생
                weaponAnimator.Play("Attack", 0, 0f);
                
                // lifetime 후 Idle로 돌아가도록 코루틴 시작
                StartCoroutine(ResetWeaponAnimatorToIdle(weaponAnimator, currentWeapon.projectileLifetime));
            }
        }
        
        dir = dir.normalized;
        GameObject projectile = Instantiate(currentWeapon.projectilePrefab, startPoint.position, startPoint.rotation);
        
        if (projectile == null)
        {
            Debug.LogError($"WeaponController.FireAttack: 투사체 생성 실패! 무기: {currentWeapon.weaponName}, 프리팹: {currentWeapon.projectilePrefab}");
            return;
        }
        
        projectile.transform.up = dir;

        // 총알의 속성을 무기의 속성으로 설정
        BulletController bulletController = projectile.GetComponent<BulletController>();
        if (bulletController != null)
        {
            bulletController.weaponElement = currentWeapon.element;
        }
        else
        {
            Debug.LogWarning($"WeaponController.FireAttack: BulletController 컴포넌트를 찾을 수 없습니다! 무기: {currentWeapon.weaponName}, 투사체: {projectile.name}");
        }

        var rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = dir * currentWeapon.projectileSpeed * projectileSpeedMultiplier;
        }
        else
        {
            Debug.LogWarning($"WeaponController.FireAttack: Rigidbody2D 컴포넌트를 찾을 수 없습니다! 무기: {currentWeapon.weaponName}, 투사체: {projectile.name}");
        }

        Destroy(projectile, currentWeapon.projectileLifetime);
    }

    /// <summary>
    /// 산탄 공격 메소드: Enemy_Siren과 같은 방식으로 플레이어 방향 기준 분산 각도 내 랜덤한 각도로 순차 발사합니다.
    /// </summary>
    /// <param name="dir">공격 방향 (정규화됨)</param>
    /// <param name="startPoint">공격 시작 위치와 회전</param>
    private void MultiAttack(Vector2 dir, Transform startPoint)
    {
        if (currentWeapon == null) return;
        if (currentWeapon.projectilePrefab == null) return;

        // 무기 아이콘 애니메이터에 Attack 애니메이션 강제 재생 (AutoFire 무기가 아닌 경우에만)
        if (!currentWeapon.autoFire && currentWeapon.animatorController != null && weaponIconRenderer != null)
        {
            Animator weaponAnimator = weaponIconRenderer.GetComponent<Animator>();
            if (weaponAnimator != null)
            {
                // Play()를 사용하여 현재 애니메이션을 중단하고 Attack 애니메이션을 처음부터 강제 재생
                weaponAnimator.Play("Attack", 0, 0f);
                
                // lifetime 후 Idle로 돌아가도록 코루틴 시작
                StartCoroutine(ResetWeaponAnimatorToIdle(weaponAnimator, currentWeapon.projectileLifetime));
            }
        }

        // 순차 발사 코루틴 시작
        StartCoroutine(MultiAttackCoroutine(dir, startPoint));
    }
    
    /// <summary>
    /// 산탄 공격을 순차적으로 발사하는 코루틴 (Enemy_Siren 방식)
    /// </summary>
    private IEnumerator MultiAttackCoroutine(Vector2 dir, Transform startPoint)
    {
        dir = dir.normalized;
        
        // 공격 방향의 기본 각도 계산 (라디안)
        float baseAngle = Mathf.Atan2(dir.y, dir.x);

        int tmp = Random.Range(0, 10);
        multiBulletCount = 2;
        if(tmp<4) tmp = multiBulletCount;
        else if(tmp<7) tmp = multiBulletCount + 1;
        else if(tmp<9) tmp = multiBulletCount + 2;
        else tmp = multiBulletCount + 3;

        int tmpSpread = tmp * multiBulletSpread;
        Debug.Log("tmp: " + tmp);
        Debug.Log("multiBulletCount: " + multiBulletCount);
        Debug.Log("multiBulletSpread: " + multiBulletSpread);
        // Debug.Log("baseAngle: " + baseAngle);
        // Debug.Log("dir: " + dir);
        // Debug.Log("startPoint: " + startPoint.position);
        // Debug.Log("currentWeapon.projectilePrefab: " + currentWeapon.projectilePrefab);
        // Debug.Log("currentWeapon.element: " + currentWeapon.element);
        // 각 탄환 발사
        for (int i = 0; i < tmp; i++)
        {
            // ±multiBulletSpread/2 범위 내 랜덤 각도 계산
            float randomSpread = Random.Range(-multiBulletSpread / 2f, multiBulletSpread / 2f) * Mathf.Deg2Rad;
            float finalAngle = baseAngle + randomSpread;
            
            // 발사 방향 계산
            Vector2 direction = new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));
            
            // 투사체 생성
            GameObject projectile = Instantiate(currentWeapon.projectilePrefab, startPoint.position, Quaternion.identity);
            
            // 총알의 속성을 무기의 속성으로 설정
            BulletController bulletController = projectile.GetComponent<BulletController>();
            if (bulletController != null)
            {
                bulletController.weaponElement = currentWeapon.element;
            }
            
            // 회전 설정
            projectile.transform.rotation = Quaternion.Euler(0f, 0f, finalAngle * Mathf.Rad2Deg);
            
            Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
            if (projectileRb != null)
            {
                projectileRb.linearVelocity = direction * currentWeapon.projectileSpeed * projectileSpeedMultiplier;
            }
            
            Destroy(projectile, currentWeapon.projectileLifetime);
            
            // 다음 탄환 발사까지 랜덤 딜레이 (미세한 오차)
            float delay = Random.Range(multiBulletMinFireDelay, multiBulletMaxFireDelay);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            else
            {
                yield return null; // 최소 딜레이가 0이면 한 프레임만 대기
            }
        }
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
    /// 무기 애니메이터를 Idle 상태로 리셋하는 코루틴: projectileLifetime 후에 실행됩니다.
    /// </summary>
    /// <param name="animator">리셋할 Animator</param>
    /// <param name="lifetime">대기 시간 (초)</param>
    private IEnumerator ResetWeaponAnimatorToIdle(Animator animator, float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        
        // Attack 트리거를 리셋 (다음 공격을 위해)
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
        }
    }

    // ========== Charge Dash Attack ==========
    /// <summary>
    /// 충전 대시 공격 시작 메소드: 마우스를 누르고 있는 동안 충전을 시작합니다.
    /// </summary>
    /// <param name="dir">공격 방향 (정규화됨)</param>
    /// <param name="startPoint">공격 시작 위치와 회전</param>
    public void StartChargeDash(Vector2 dir, Transform startPoint)
    {
        if (isDashing || isCharging) return;
        Animator weaponAnimator = weaponIconRenderer.GetComponent<Animator>();
        if (weaponAnimator != null)
        {
            weaponAnimator.SetBool("Attacking", true);
        }
        isCharging = true;
        currentChargeTime = 0f;
        dashDirection = dir.normalized;
    }

    /// <summary>
    /// 충전 대시 공격 업데이트 메소드: 마우스를 누르고 있는 동안 호출되어 충전 시간을 증가시킵니다.
    /// </summary>
    /// <param name="dir">공격 방향 (정규화됨)</param>
    public void UpdateChargeDash(Vector2 dir)
    {
        if (!isCharging || isDashing) return;
        
        // 충전 시간 증가
        currentChargeTime += Time.deltaTime;
        Animator weaponAnimator = weaponIconRenderer.GetComponent<Animator>();
        if (currentChargeTime > maxChargeTime)
        {
            currentChargeTime = maxChargeTime;
            
            // ChargeMax 파라미터가 false라면 true로 변경
            if (weaponAnimator != null && !weaponAnimator.GetBool("ChargeMax"))
            {
                weaponAnimator.SetBool("ChargeMax", true);
            }
        }
        
        // 방향 업데이트
        dashDirection = dir.normalized;
    }

    /// <summary>
    /// 충전 대시 공격 실행 메소드: 마우스를 떼는 순간 호출되어 돌진을 시작합니다.
    /// </summary>
    /// <param name="dir">공격 방향 (정규화됨)</param>
    /// <param name="startPoint">공격 시작 위치와 회전</param>
    public void ChargeDashAttack(Vector2 dir, Transform startPoint)
    {
        if (!isCharging || isDashing) return;
        
        isCharging = false;
        dashDirection = dir.normalized;
        
        // 돌진 코루틴 시작
        if (chargeDashCoroutine != null)
        {
            StopCoroutine(chargeDashCoroutine);
        }
        Animator weaponAnimator = weaponIconRenderer.GetComponent<Animator>();
        if (weaponAnimator != null && weaponAnimator.GetBool("ChargeMax"))
        {
            weaponAnimator.SetBool("ChargeMax", false);
        }
        if (weaponAnimator != null)
        {
            weaponAnimator.SetBool("Attacking", false);
        }
        chargeDashCoroutine = StartCoroutine(ChargeDashRoutine(startPoint));
    }

    /// <summary>
    /// 충전 대시 공격 취소 메소드: 충전 중인 공격을 취소합니다.
    /// </summary>
    public void CancelChargeDash()
    {
        if (isCharging)
        {
            isCharging = false;
            currentChargeTime = 0f;
        }
    }

    /// <summary>
    /// 충전 대시 공격 코루틴: 돌진을 실행하고 이펙트와 콜라이더를 관리합니다.
    /// </summary>
    private IEnumerator ChargeDashRoutine(Transform startPoint)
    {
        isDashing = true;
        
        if (playerController == null)
        {
            isDashing = false;
            yield break;
        }
        
        // PlayerController의 isDashing 플래그 설정
        playerController.isDashing = true;
        
        Rigidbody2D playerRb = playerController.GetComponent<Rigidbody2D>();
        if (playerRb == null)
        {
            isDashing = false;
            playerController.isDashing = false;
            yield break;
        }
        
        // meleeAttackCollider가 없으면 자동으로 찾거나 생성
        EnsureMeleeAttackComponents();
        
        // fireEffect 생성
        if (currentWeapon != null && currentWeapon.fireEffect != null)
        {
            dashFireEffect = new GameObject("DashFireEffect");
            dashFireEffect.transform.SetParent(transform);
            SpriteRenderer effectRenderer = dashFireEffect.AddComponent<SpriteRenderer>();
            effectRenderer.sprite = currentWeapon.fireEffect;
            effectRenderer.sortingOrder = 15;
            
            // 이펙트 회전 설정
            float angle = Mathf.Atan2(dashDirection.y, dashDirection.x) * Mathf.Rad2Deg;
            dashFireEffect.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        
        // meleeAttackCollider 활성화 및 위치 설정
        if (meleeAttackCollider != null)
        {
            meleeAttackCollider.enabled = true;
        }
        
        // 돌진 거리 계산 (충전 시간에 비례)
        float chargeRatio = Mathf.Clamp01(currentChargeTime / maxChargeTime);
        float actualDashDistance = dashDistance * (0.5f + chargeRatio * 0.5f); // 최소 50%, 최대 100%
        
        Vector2 startPosition = playerRb.position;
        Vector2 targetPosition = startPosition + dashDirection * actualDashDistance;
        float remainingDistance = actualDashDistance;
        
        // 돌진 실행
        while (remainingDistance > 0.1f)
        {
            float moveDistance = dashSpeed * Time.fixedDeltaTime;
            if (moveDistance > remainingDistance)
            {
                moveDistance = remainingDistance;
            }
            
            Vector2 nextPosition = playerRb.position + dashDirection * moveDistance;
            playerRb.MovePosition(nextPosition);
            
            // fireEffect 위치 업데이트
            if (dashFireEffect != null)
            {
                dashFireEffect.transform.position = playerRb.position;
            }
            
            // meleeAttackCollider 위치 업데이트
            if (meleeAttackCollider != null)
            {
                meleeAttackCollider.transform.position = playerRb.position;
            }
            
            remainingDistance -= moveDistance;
            yield return new WaitForFixedUpdate();
        }
        
        // 최종 위치 설정
        playerRb.MovePosition(targetPosition);
        
        // fireEffect 제거
        if (dashFireEffect != null)
        {
            Destroy(dashFireEffect);
            dashFireEffect = null;
        }
        
        // meleeAttackCollider 비활성화
        if (meleeAttackCollider != null)
        {
            meleeAttackCollider.enabled = false;
        }
        
        // 충전 시간 리셋
        currentChargeTime = 0f;
        isDashing = false;
        
        // PlayerController의 isDashing 플래그 해제
        if (playerController != null)
        {
            playerController.isDashing = false;
        }
        
        chargeDashCoroutine = null;
    }

    // ========== Charge Fire Attack ==========
    /// <summary>
    /// 충전 발사 공격 시작 메소드: 마우스를 누르고 있는 동안 충전을 시작합니다.
    /// </summary>
    /// <param name="dir">공격 방향 (정규화됨)</param>
    /// <param name="startPoint">공격 시작 위치와 회전</param>
    public void StartChargeFire(Vector2 dir, Transform startPoint)
    {
        if (isChargingFire) return;
        
        isChargingFire = true;
        currentChargeFireTime = 0f;
        chargeFireDirection = dir.normalized;
    }

    /// <summary>
    /// 충전 발사 공격 업데이트 메소드: 마우스를 누르고 있는 동안 호출되어 충전 시간을 증가시킵니다.
    /// </summary>
    /// <param name="dir">공격 방향 (정규화됨)</param>
    public void UpdateChargeFire(Vector2 dir)
    {
        if (!isChargingFire) return;
        
        // 충전 시간 증가
        currentChargeFireTime += Time.deltaTime;
        if (currentChargeFireTime > maxChargeFireTime)
        {
            currentChargeFireTime = maxChargeFireTime;
        }
        
        // 방향 업데이트
        chargeFireDirection = dir.normalized;
    }

    /// <summary>
    /// 충전 발사 공격 실행 메소드: 마우스를 떼는 순간 호출되어 레이저 빔을 발사합니다.
    /// </summary>
    /// <param name="dir">공격 방향 (정규화됨)</param>
    /// <param name="startPoint">공격 시작 위치와 회전</param>
    public void ChargeFireAttack(Vector2 dir, Transform startPoint)
    {
        if (!isChargingFire) return;
        
        isChargingFire = false;
        chargeFireDirection = dir.normalized;
        
        // 레이저 빔 프리팹이 없으면 리턴
        if (currentWeapon == null || currentWeapon.projectilePrefab == null)
        {
            currentChargeFireTime = 0f;
            return;
        }
        
        // 레이저 빔 생성
        GameObject MultiBeam = Instantiate(currentWeapon.projectilePrefab, startPoint.position, Quaternion.identity);
        LaserBeamController beamController = MultiBeam.GetComponent<LaserBeamController>();
        
        if (beamController != null)
        {
            // 데미지 계산 (WeaponData의 baseDamage 사용)
            float damage = currentWeapon.baseDamage;
            
            // PlayerController의 attackDamageMultiplier 적용
            if (playerController != null)
            {
                damage *= playerController.attackDamageMultiplier;
            }
            
            // 레이저 빔 초기화 (속성 포함)
            beamController.Initialize(startPoint.position, chargeFireDirection, maxMultiDistance, damage, currentWeapon.element);
        }
        else
        {
            Debug.LogWarning("ChargeFireAttack: LaserBeamController 컴포넌트가 없습니다.");
        }
        
        // 발사 사운드 재생
        if (currentWeapon.fireSound != null)
        {
            AudioSource.PlayClipAtPoint(currentWeapon.fireSound, startPoint.position);
        }
        
        // 충전 시간 리셋
        currentChargeFireTime = 0f;
    }

    /// <summary>
    /// 충전 발사 공격 취소 메소드: 충전 중인 공격을 취소합니다.
    /// </summary>
    public void CancelChargeFire()
    {
        if (isChargingFire)
        {
            isChargingFire = false;
            currentChargeFireTime = 0f;
        }
    }

    // ========== Fire & Reload ==========
    /// <summary>
    /// 발사 가능 여부 체크 메소드: 현재 발사 가능한지 확인합니다.
    /// </summary>
    /// <returns>발사 가능 여부</returns>
    public bool CanFire()
    {
        if (currentWeapon == null) return false;
        if (isReloading) return false;
        
        int slotIndex = CurrentWeaponIndex;
        if (slotIndex < 0 || slotIndex >= MaxWeapons) return false;
        
        // 배열 null 체크 및 길이 체크
        if (isAmmoWeapon == null || slotIndex >= isAmmoWeapon.Length) return false;
        if (currentBulletCount == null || slotIndex >= currentBulletCount.Length) return false;
        
        // 탄약 무기가 아니면 탄약 체크 스킵
        if (!isAmmoWeapon[slotIndex]) return true;
        
        if (currentBulletCount[slotIndex] <= 0) return false;
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

        // 발사 시간 기록 및 탄약 감소 (isAmmoWeapon이 true인 경우에만 탄약 소모)
        lastFireTime = Time.time;
        
        int slotIndex = CurrentWeaponIndex;
        if (slotIndex >= 0 && slotIndex < MaxWeapons && 
            isAmmoWeapon != null && slotIndex < isAmmoWeapon.Length &&
            currentBulletCount != null && slotIndex < currentBulletCount.Length &&
            isAmmoWeapon[slotIndex] && currentWeapon != null && 
            (currentWeapon.weaponType == WeaponData.WeaponType.Fire || 
             currentWeapon.weaponType == WeaponData.WeaponType.Multi))
        {
            currentBulletCount[slotIndex]--;
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
        // 이미 재장전 중이면 무시
        if (isReloading) return false;
        
        int slotIndex = CurrentWeaponIndex;
        if (slotIndex < 0 || slotIndex >= MaxWeapons) return false;
        
        // 배열 null 체크 및 길이 체크
        if (isAmmoWeapon == null || slotIndex >= isAmmoWeapon.Length) return false;
        if (currentBulletCount == null || slotIndex >= currentBulletCount.Length) return false;
        if (maxBulletCount == null || slotIndex >= maxBulletCount.Length) return false;
        
        // 탄약 무기가 아니면 재장전 불가
        if (!isAmmoWeapon[slotIndex]) return false;
        
        // 탄약이 최대면 무시
        if (currentBulletCount[slotIndex] >= maxBulletCount[slotIndex]) return false;

        // 재장전 텍스트 표시
        if (reloadText != null)
        {
            reloadText.gameObject.SetActive(true);
            reloadText.text = "재장전";
            UpdateReloadTextPosition();
        }

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
        
        int slotIndex = CurrentWeaponIndex;
        if (slotIndex < 0 || slotIndex >= MaxWeapons)
        {
            isReloading = false;
            yield break;
        }
        
        // 배열 null 체크 및 길이 체크
        if (currentBulletCount == null || slotIndex >= currentBulletCount.Length ||
            maxBulletCount == null || slotIndex >= maxBulletCount.Length)
        {
            isReloading = false;
            yield break;
        }

        // reloadTime만큼 대기
        yield return new WaitForSeconds(reloadTime);

        // 탄약을 최대치로 복구
        currentBulletCount[slotIndex] = maxBulletCount[slotIndex];

        isReloading = false; // 재장전 상태 종료
        
        // 재장전 텍스트 숨기기
        if (reloadText != null)
        {
            reloadText.gameObject.SetActive(false);
        }
    }

    // ========== Weapon Stats ==========
    /// <summary>
    /// 무기 스탯 동기화 메소드: 현재 무기의 데이터로부터 스탯을 동기화합니다.
    /// - attackCooldown과 reloadTime은 PlayerController의 배율을 적용하여 계산됩니다.
    /// - 현재 무기의 슬롯 인덱스에 해당하는 배열 요소를 업데이트합니다.
    /// </summary>
    /// <param name="forceResetAmmo">탄약을 강제로 최대치로 리셋할지 여부</param>
    public void SyncWeaponStats(bool forceResetAmmo = false)
    {
        if (currentWeapon == null) return;

        int slotIndex = CurrentWeaponIndex;
        if (slotIndex < 0 || slotIndex >= MaxWeapons) return;
        
        // 배열 null 체크 및 길이 체크
        if (maxBulletCount == null || maxBulletCount.Length <= slotIndex) return;
        if (currentBulletCount == null || currentBulletCount.Length <= slotIndex) return;

        // 현재 슬롯의 최대 탄약 수 업데이트
        maxBulletCount[slotIndex] = Mathf.Max(0, currentWeapon.magazineSize);
        
        // PlayerController의 배율을 적용하여 계산
        float attackMultiplier = playerController != null ? playerController.attackSpeedMultiplier : 1f;
        float reloadMultiplier = playerController != null ? playerController.reloadSpeedMultiplier : 1f;
        
        attackCooldown = currentWeapon.fireCooldown * attackMultiplier;
        reloadTime = currentWeapon.reloadTime * reloadMultiplier;

        if (forceResetAmmo)
        {
            // 강제 리셋 시 최대치로 설정
            currentBulletCount[slotIndex] = maxBulletCount[slotIndex];
        }
        else
        {
            // 저장된 탄약 수를 유지하되, 최대치를 초과하지 않도록 클램프
            currentBulletCount[slotIndex] = Mathf.Clamp(currentBulletCount[slotIndex], 0, maxBulletCount[slotIndex]);
            // 탄약이 0이면 최대치로 복구 (첫 장착 시)
            if (currentBulletCount[slotIndex] == 0 && maxBulletCount[slotIndex] > 0)
            {
                currentBulletCount[slotIndex] = maxBulletCount[slotIndex];
            }
        }
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
    /// 아직 드랍되지 않은 무기의 인덱스 목록을 반환하는 메소드: droppedWeapons에서 false인 인덱스들을 반환합니다.
    /// </summary>
    /// <returns>아직 드랍되지 않은 무기의 인덱스 목록</returns>
    public List<int> GetAvailableWeaponIndices()
    {
        EnsureDroppedWeaponList();
        List<int> availableIndices = new List<int>();
        
        for (int i = 0; i < droppedWeapons.Count && i < allWeapons.Count; i++)
        {
            if (!droppedWeapons[i])
            {
                availableIndices.Add(i);
            }
        }
        
        return availableIndices;
    }

    /// <summary>
    /// 투사체 속도 배율 설정 메소드: 아이템 시스템에서 사용합니다.
    /// </summary>
    /// <param name="value">설정할 배율 값 (1.0이 기본값)</param>
    public void SetProjectileSpeedMultiplier(float value)
    {
        projectileSpeedMultiplier = Mathf.Max(0f, value);
    }
    
    /// <summary>
    /// 투사체 속도 배율 곱하기 메소드: 기존 배율에 곱하여 적용합니다.
    /// </summary>
    /// <param name="multiplier">곱할 배율 값</param>
    public void MultiplyProjectileSpeedMultiplier(float multiplier)
    {
        projectileSpeedMultiplier *= Mathf.Max(0f, multiplier);
    }
    
    /// <summary>
    /// 투사체 속도 배율 더하기 메소드: 기존 배율에 더합니다.
    /// </summary>
    /// <param name="value">더할 값</param>
    public void AddProjectileSpeedMultiplier(float value)
    {
        projectileSpeedMultiplier = Mathf.Max(0f, projectileSpeedMultiplier + value);
    }
    
    /// <summary>
    /// 최대 탄약 수 설정 메소드: 아이템 시스템에서 사용합니다. (현재 장착된 무기의 슬롯에 적용)
    /// </summary>
    /// <param name="value">설정할 최대 탄약 수</param>
    public void SetMaxAmmo(int value)
    {
        int slotIndex = CurrentWeaponIndex;
        if (slotIndex < 0 || slotIndex >= MaxWeapons) return;
        if (maxBulletCount == null || slotIndex >= maxBulletCount.Length) return;
        
        int oldMaxAmmo = maxBulletCount[slotIndex];
        maxBulletCount[slotIndex] = Mathf.Max(1, value);
        
        // 최대 탄창이 증가한 만큼 현재 탄약도 증가 (비율 유지가 아님)
        if (currentBulletCount != null && slotIndex < currentBulletCount.Length)
        {
            int maxAmmoIncrease = maxBulletCount[slotIndex] - oldMaxAmmo;
            if (maxAmmoIncrease > 0)
            {
                // 최대 탄창이 증가한 만큼 현재 탄약도 증가
                currentBulletCount[slotIndex] += maxAmmoIncrease;
            }
            // 최대 탄창이 감소한 경우는 현재 탄약을 최대치로 제한
            currentBulletCount[slotIndex] = Mathf.Clamp(currentBulletCount[slotIndex], 0, maxBulletCount[slotIndex]);
        }
    }
    
    /// <summary>
    /// 최대 탄약 수 곱하기 메소드: 기존 최대 탄약 수에 곱하여 적용합니다.
    /// </summary>
    /// <param name="multiplier">곱할 배율 값</param>
    public void MultiplyMaxAmmo(float multiplier)
    {
        int slotIndex = CurrentWeaponIndex;
        if (slotIndex < 0 || slotIndex >= MaxWeapons) return;
        if (maxBulletCount == null || slotIndex >= maxBulletCount.Length) return;
        
        SetMaxAmmo(Mathf.RoundToInt(maxBulletCount[slotIndex] * multiplier));
    }
    
    /// <summary>
    /// 최대 탄약 수 더하기 메소드: 기존 최대 탄약 수에 더합니다.
    /// </summary>
    /// <param name="value">더할 값</param>
    public void AddMaxAmmo(int value)
    {
        int slotIndex = CurrentWeaponIndex;
        if (slotIndex < 0 || slotIndex >= MaxWeapons) return;
        if (maxBulletCount == null || slotIndex >= maxBulletCount.Length) return;
        
        SetMaxAmmo(maxBulletCount[slotIndex] + value);
    }

    // ========== Weapon Icon ==========
    /// <summary>
    /// 무기 아이콘 스프라이트 및 애니메이터 업데이트 메소드: 현재 무기의 아이콘과 애니메이터를 표시합니다.
    /// </summary>
    public void UpdateWeaponIconSprite()
    {
        // weaponIconRenderer가 없으면 자동으로 찾거나 생성
        if (weaponIconRenderer == null)
        {
            EnsureWeaponIconRenderer();
        }

        if (weaponIconRenderer == null) return;

        // Animator 컴포넌트 가져오기 (한 번만 선언)
        Animator animator = weaponIconRenderer.GetComponent<Animator>();

        if (currentWeapon == null)
        {
            weaponIconRenderer.sprite = null;
            weaponIconRenderer.enabled = false;
            
            // 애니메이터도 비활성화
            if (animator != null)
            {
                animator.runtimeAnimatorController = null;
            }
            return;
        }

        // currentWeapon.icon을 weaponIconRenderer.sprite에 설정
        weaponIconRenderer.sprite = currentWeapon.icon;
        weaponIconRenderer.enabled = weaponIconRenderer.sprite != null;
        
        // 애니메이터 컨트롤러 업데이트
        if (animator != null)
        {
            animator.runtimeAnimatorController = currentWeapon.animatorController;
        }
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
            if (weaponIconRenderer != null)
            {
                // Animator 컴포넌트 확인 및 추가
                EnsureWeaponIconAnimator(weaponIconRenderer.gameObject);
                return;
            }
        }

        // 전체 하이어라키에서 "WeaponIcon" 이름으로 찾기 (같은 부모 기준)
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.gameObject.name == "WeaponIcon")
            {
                weaponIconRenderer = renderer;
                // Animator 컴포넌트 확인 및 추가
                EnsureWeaponIconAnimator(weaponIconRenderer.gameObject);
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
        
        // Animator 컴포넌트도 추가 (애니메이션 사용 가능하도록)
        EnsureWeaponIconAnimator(weaponIconObj);
    }
    
    /// <summary>
    /// 무기 아이콘에 Animator 컴포넌트가 있는지 확인하고 없으면 추가하는 헬퍼 메소드
    /// </summary>
    /// <param name="weaponIconObj">무기 아이콘 GameObject</param>
    private void EnsureWeaponIconAnimator(GameObject weaponIconObj)
    {
        if (weaponIconObj == null) return;
        
        // Animator 컴포넌트가 없으면 추가
        if (weaponIconObj.GetComponent<Animator>() == null)
        {
            weaponIconObj.AddComponent<Animator>();
        }
    }
    
    /// <summary>
    /// 재장전 텍스트 자동 찾기 또는 생성 메소드: reloadText가 없으면 자동으로 찾거나 생성합니다.
    /// </summary>
    private void EnsureReloadText()
    {
        // 이미 할당되어 있으면 무시
        if (reloadText != null) return;
        
        // 자식 오브젝트에서 "ReloadText" 이름으로 찾기
        Transform reloadTextTransform = transform.Find("ReloadText");
        if (reloadTextTransform != null)
        {
            reloadText = reloadTextTransform.GetComponent<TextMeshPro>();
            if (reloadText != null)
            {
                reloadText.gameObject.SetActive(false); // 초기에는 비활성화
                return;
            }
        }
        
        // 찾지 못했으면 새로 생성
        GameObject reloadTextObj = new GameObject("ReloadText");
        reloadTextObj.transform.SetParent(transform);
        reloadTextObj.transform.localPosition = Vector3.zero;
        reloadText = reloadTextObj.AddComponent<TextMeshPro>();
        
        // 텍스트 설정
        reloadText.text = "재장전";
        reloadText.fontSize = 2f;
        reloadText.alignment = TextAlignmentOptions.Center;
        reloadText.color = Color.white;
        
        // 초기에는 비활성화
        reloadTextObj.SetActive(false);
    }
    
    /// <summary>
    /// 재장전 텍스트 위치 업데이트 메소드: 플레이어 머리 위에 표시되도록 위치를 업데이트합니다.
    /// </summary>
    private void UpdateReloadTextPosition()
    {
        if (reloadText == null || playerController == null) return;
        
        // 플레이어 위치 가져오기
        Vector3 playerPosition = playerController.transform.position;
        
        // 플레이어 머리 위에 텍스트 배치
        reloadText.transform.position = playerPosition + Vector3.up * reloadTextOffsetY;
        
        // 카메라를 향하도록 회전 (Billboard 효과)
        if (Camera.main != null)
        {
            reloadText.transform.LookAt(Camera.main.transform);
            reloadText.transform.Rotate(0f, 180f, 0f); // 텍스트가 뒤집히지 않도록
        }
    }
}
