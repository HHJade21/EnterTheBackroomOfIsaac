using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class WeaponSlotUIController : MonoBehaviour
{
    public GameObject newWeaponPanel;
    public Image Slot1Image;
    public Image Slot2Image;
    public Image newWeaponImage;
    public TextMeshProUGUI Slot1Name;
    public TextMeshProUGUI Slot2Name;
    public TextMeshProUGUI newWeaponName;
    public TextMeshProUGUI Slot1Description;
    public TextMeshProUGUI Slot2Description;
    public TextMeshProUGUI newWeaponDescription;
    


    public void OpenNewWeaponPanel(WeaponData slot1Weapon, WeaponData slot2Weapon, WeaponData newWeapon){
        Time.timeScale = 0;
        newWeaponPanel.SetActive(true);
        Slot1Image.sprite = slot1Weapon.icon;
        Slot2Image.sprite = slot2Weapon.icon;
        newWeaponImage.sprite = newWeapon.icon;
        Slot1Name.text = slot1Weapon.weaponName;
        Slot2Name.text = slot2Weapon.weaponName;
        newWeaponName.text = newWeapon.weaponName;
        Slot1Description.text = slot1Weapon.description;
        Slot2Description.text = slot2Weapon.description;
        newWeaponDescription.text = newWeapon.description;
    }
    public void CloseNewWeaponPanel(){
        Time.timeScale = 1;
        newWeaponPanel.SetActive(false);
    }
}
