using UnityEngine;

public class BulletController : MonoBehaviour
{
    public BulletData bulletData;
    public float damage => bulletData.damage;
    public BulletData.WeaponElement weaponElement => bulletData.weaponElement;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
