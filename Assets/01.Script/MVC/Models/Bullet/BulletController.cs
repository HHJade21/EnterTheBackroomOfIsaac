using UnityEngine;

public class BulletController : MonoBehaviour
{
    public BulletData bulletData;
    private WeaponData.WeaponElement _weaponElement; // 런타임에 설정 가능한 속성
    
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
}
