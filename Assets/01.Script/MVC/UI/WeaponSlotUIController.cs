using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
public class WeaponSlotUIController : MonoBehaviour
{
    public GameObject newWeaponPanel;
    public GameObject Slot1GameObject;
    public GameObject Slot2GameObject;
    public GameObject newWeaponGameObject;
    public Sprite selectIconBackgroundC;
    public Sprite selectIconBackgroundM;
    public Sprite selectIconBackgroundY;
    public Sprite selectIconBackgroundK;
    


    public void OpenNewWeaponPanel(WeaponData slot1Weapon, WeaponData slot2Weapon, WeaponData newWeapon){
        Time.timeScale = 0;
        newWeaponPanel.SetActive(true);
        
        // Slot1 설정
        SetWeaponSlotUI(Slot1GameObject, slot1Weapon);
        
        // Slot2 설정
        SetWeaponSlotUI(Slot2GameObject, slot2Weapon);
        
        // NewWeapon 설정
        SetWeaponSlotUI(newWeaponGameObject, newWeapon);
    }
    
    /// <summary>
    /// 주어진 GameObject의 자식 오브젝트들을 순회하여 무기 정보를 설정합니다.
    /// PanelController.SetSelectPanelOptions와 동일한 방식으로 구현되었습니다.
    /// </summary>
    /// <param name="slotGameObject">설정할 슬롯 GameObject</param>
    /// <param name="weaponData">설정할 무기 데이터</param>
    private void SetWeaponSlotUI(GameObject slotGameObject, WeaponData weaponData)
    {
        if (slotGameObject == null || weaponData == null) return;

        // 이미지 설정: 자식 오브젝트에서 모든 Image 컴포넌트 찾기 (자기 자신 제외)
        List<Image> imageList = new List<Image>();
        foreach (Transform child in slotGameObject.transform)
        {
            Image image = child.GetComponent<Image>();
            if (image != null)
            {
                imageList.Add(image);
            }
        }
        
        // 첫 번째 이미지: Element에 따른 배경 이미지
        if (imageList.Count > 0)
        {
            Sprite backgroundSprite = null;
            switch (weaponData.element)
            {
                case WeaponData.WeaponElement.Cyan:
                    backgroundSprite = selectIconBackgroundC;
                    break;
                case WeaponData.WeaponElement.Magenta:
                    backgroundSprite = selectIconBackgroundM;
                    break;
                case WeaponData.WeaponElement.Yellow:
                    backgroundSprite = selectIconBackgroundY;
                    break;
                case WeaponData.WeaponElement.Key:
                    backgroundSprite = selectIconBackgroundK;
                    break;
            }
            
            if (backgroundSprite != null)
            {
                imageList[0].sprite = backgroundSprite;
            }
        }
        
        // 두 번째 이미지: selectIcon
        if (imageList.Count > 1)
        {
            imageList[1].sprite = weaponData.selectIcon;
        }

        // 텍스트 설정: 자식 오브젝트에서 모든 TextMeshProUGUI 컴포넌트 찾기
        List<TextMeshProUGUI> textList = new List<TextMeshProUGUI>();
        foreach (Transform child in slotGameObject.transform)
        {
            TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                textList.Add(text);
            }
        }
        
        if (textList.Count > 0)
        {
            // 첫 번째 텍스트는 description
            textList[0].text = weaponData.weaponName;
            
            // 두 번째 텍스트는 detailDescription (존재하는 경우)
            if (textList.Count > 1)
            {
                textList[1].text = weaponData.description;
                
                if(textList.Count > 2)
                {
                    textList[2].text = weaponData.detailDescription;
                }
            }
        }
    }
    public void CloseNewWeaponPanel(){
        Time.timeScale = 1;
        newWeaponPanel.SetActive(false);
    }
}
