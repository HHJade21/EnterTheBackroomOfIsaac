using UnityEngine;
[CreateAssetMenu(fileName = "BulletData", menuName = "EnterTheBackroomOfIsaac/Data/Bullet")]
public class BulletData : ScriptableObject
{
    public WeaponData.WeaponElement weaponElement; // WeaponData의 enum 사용
    public float damage = 1f;
}


