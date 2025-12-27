using UnityEngine;

public class BulletController : MonoBehaviour
{
    public BulletData bulletData;
    private WeaponData.WeaponElement _weaponElement; // 런타임에 설정 가능한 속성
    public bool isPlayerBullet = true;
    
    public float damage => bulletData != null ? bulletData.damage : 1f;
    public WeaponData.WeaponElement weaponElement
    {
        get
        {
            // 런타임에 설정된 element가 있으면 우선 사용, 없으면 bulletData의 element 사용
            if (bulletData != null && bulletData.weaponElement != WeaponData.WeaponElement.Cyan)
            {
                return bulletData.weaponElement;
            }
            return _weaponElement;
        }
        set => _weaponElement = value;
    }
    
    // Awake는 Instantiate 직후 즉시 호출됨
    void Awake()
    {
        // 디버깅: BulletController가 생성되었는지 확인
        // Debug.Log($"BulletController.Awake: {gameObject.name} 생성됨");
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // bulletData가 있으면 초기값 설정
        if (bulletData != null)
        {
            _weaponElement = bulletData.weaponElement;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    /// <summary>
    /// 충돌 감지 메소드: Enemy, Player, Wall 태그와 충돌 시 총알을 파괴합니다.
    /// </summary>
    /// <param name="other">충돌한 오브젝트의 Collider2D</param>
    void OnTriggerEnter2D(Collider2D other)
    {
        // Enemy, Player, Wall 태그와 충돌 시 총알 파괴
        if (isPlayerBullet && (other.CompareTag("Enemy") || other.CompareTag("Wall")))
        {
            //여기서 투사체 충돌 애니메이션 재생해주시면 됩니다.
            Destroy(gameObject);
        }
        else if (!isPlayerBullet && (other.CompareTag("Player") || other.CompareTag("Wall")))
        {
            Destroy(gameObject);
        }
    }
}

