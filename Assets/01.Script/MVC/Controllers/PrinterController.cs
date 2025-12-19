using UnityEngine;

/// <summary>
/// 프린터 컨트롤러: Bullet_Player 태그를 가진 오브젝트와 충돌 시 무기를 생성합니다.
/// </summary>
public class PrinterController : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("무기가 생성될 위치를 가진 Transform (인스펙터에서 다른 오브젝트를 드래그 앤 드롭으로 할당)")]
    [SerializeField]
    private Transform weaponSpawnTransform;
    
    [Tooltip("Transform이 할당되지 않은 경우 사용할 기본 위치")]
    [SerializeField]
    private Vector3 defaultSpawnPosition = Vector3.zero;
    
    /// <summary>
    /// 무기 생성 위치를 반환합니다. Transform이 할당되어 있으면 Transform의 위치를, 없으면 기본 위치를 반환합니다.
    /// </summary>
    private Vector3 GetWeaponSpawnPosition()
    {
        return weaponSpawnTransform != null ? weaponSpawnTransform.position : defaultSpawnPosition;
    }
    
    private WeaponController weaponController;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // WeaponController 찾기
        weaponController = FindObjectOfType<WeaponController>();
        if (weaponController == null)
        {
            Debug.LogWarning("PrinterController: WeaponController를 찾을 수 없습니다.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    /// <summary>
    /// Bullet_Player 태그를 가진 오브젝트와 충돌 시 호출됩니다.
    /// </summary>
    /// <param name="other">충돌한 오브젝트의 Collider2D</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Bullet_Player 태그 확인
        if (other.CompareTag("Bullet_Player"))
        {
            // Bullet 오브젝트 삭제
            Destroy(other.gameObject);
            
            // WeaponController가 있으면 무기 생성
            if (weaponController != null)
            {
                weaponController.DevTool_DropNewWeapon(GetWeaponSpawnPosition());
            }
            else
            {
                Debug.LogWarning("PrinterController: WeaponController가 없어 무기를 생성할 수 없습니다.");
            }
        }
    }
}
