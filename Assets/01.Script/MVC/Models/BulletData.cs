using UnityEngine;
[CreateAssetMenu(fileName = "BulletData", menuName = "EnterTheBackroomOfIsaac/Data/Bullet")]
public class BulletData : ScriptableObject
{
    public enum WeaponElement
    {
         Key = 0,      // 검정
         Cyan = 1,     // 청록
         Magenta = 2,  // 자홍
         Yellow = 3,    // 노랑
    }
    public WeaponElement weaponElement;
    public float damage = 1f;
}


