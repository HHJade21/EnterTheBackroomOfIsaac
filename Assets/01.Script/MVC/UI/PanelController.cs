using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class PanelController : MonoBehaviour
{

    public WeaponSlotUIController weaponSlotUIController;
    public GameObject selectPanel;
    public List<GameObject> selectOptions;
    public List<WeaponData> selectWeapons;
    public List<int> selectWeaponIDs;
    public Sprite selectIconBackgroundC;
    public Sprite selectIconBackgroundM;
    public Sprite selectIconBackgroundY;
    public Sprite selectIconBackgroundK;
    public List<Sprite> skillIcon;

    private void Start()
    {
        GameManager.Instance.panelController = this;
    }

    public void OpenSelectPanel(){
        Time.timeScale = 0;
        selectPanel.SetActive(true);
    }

    public void CloseSelectPanel(){
        Time.timeScale = 1;
        selectPanel.SetActive(false);
    }

    public void SetSelectPanelOptions(){
        WeaponController weaponController = GameManager.Instance.weaponController;
        if (weaponController == null)
        {
            Debug.LogWarning("PanelController: WeaponController를 찾을 수 없습니다.");
            return;
        }

        // 아직 드랍되지 않은 무기 인덱스들을 가져옴
        List<int> availableIndices = weaponController.GetAvailableWeaponIndices();
        
        // 사용 가능한 무기가 3개 미만이면 모든 사용 가능한 무기를 사용
        int selectCount = Mathf.Min(3, availableIndices.Count);
        
        if (selectCount == 0)
        {
            Debug.LogWarning("PanelController: 선택 가능한 무기가 없습니다.");
            selectWeapons?.Clear();
            return;
        }

        // 랜덤으로 세 개(또는 그 이하) 선택
        List<int> selectedIndices = new List<int>();
        List<int> tempIndices = new List<int>(availableIndices);
        
        for (int i = 0; i < selectCount; i++)
        {
            int randomIndex = Random.Range(0, tempIndices.Count);
            selectedIndices.Add(tempIndices[randomIndex]);
            tempIndices.RemoveAt(randomIndex);
        }

        // selectWeapons 리스트 초기화 및 선택된 WeaponData 추가
        if (selectWeapons == null)
        {
            selectWeapons = new List<WeaponData>();
        }
        else
        {
            selectWeapons.Clear();
        }

        if (selectWeaponIDs == null)
        {
            selectWeaponIDs = new List<int>();
        }
        else
        {
            selectWeaponIDs.Clear();
        }

        foreach (int index in selectedIndices)
        {
            WeaponData weaponData = weaponController.GetWeaponDataByID(index);
            if (weaponData != null)
            {
                selectWeapons.Add(weaponData);
                selectWeaponIDs.Add(index);
            }
        }

        // UI 업데이트: 각 selectOptions에 무기 정보 설정
        for (int i = 0; i < selectWeapons.Count && i < selectOptions.Count; i++)
        {
            if (selectOptions[i] == null || selectWeapons[i] == null) continue;

            // 이미지 설정: 자식 오브젝트에서 모든 Image 컴포넌트 찾기 (자기 자신 제외)
            List<Image> imageList = new List<Image>();
            foreach (Transform child in selectOptions[i].transform)
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
                switch (selectWeapons[i].element)
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
                imageList[1].sprite = selectWeapons[i].selectIcon;
            }

            // 텍스트 설정: 자식 오브젝트에서 모든 TextMeshProUGUI 컴포넌트 찾기
            List<TextMeshProUGUI> textList = new List<TextMeshProUGUI>();
            foreach (Transform child in selectOptions[i].transform)
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
                textList[0].text = "< " + selectWeapons[i].weaponName + " >";
                
                // 두 번째 텍스트는 detailDescription (존재하는 경우)
                if (textList.Count > 1)
                {
                    textList[1].text = selectWeapons[i].description;
                    if (textList.Count > 2)
                    {
                        textList[2].text = selectWeapons[i].detailDescription;
                    }
                }
            }
        }
    }

    public void SelectOption1(){
        SelectOption(0);
    }
    public void SelectOption2(){
        SelectOption(1);
    }
    public void SelectOption3(){
        SelectOption(2);
    }
    public void SelectOption(int index){
        WeaponController weaponController = GameManager.Instance.weaponController;
        PrinterController printerController = GameManager.Instance.printerController;
        if (weaponController != null)
        {
            int newWeaponIndex = selectWeaponIDs[index];
            //여기에 해당 무기 스폰 구현
            Vector3 spawnPos = printerController.weaponSpawnTransform != null ? printerController.weaponSpawnTransform.position : printerController.defaultSpawnPosition;
            weaponController.SpawnWeaponByIndex(newWeaponIndex, spawnPos);
        }
        CloseSelectPanel();
    }

    // public void SelectOption(){
    //     // WeaponController가 있으면 무기 생성
    //     WeaponController weaponController = GameManager.Instance.weaponController;
    //     PrinterController printerController = GameManager.Instance.printerController;
    //     if (weaponController != null)
    //     {
    //         Vector3 spawnPos = printerController.weaponSpawnTransform != null ? printerController.weaponSpawnTransform.position : printerController.defaultSpawnPosition;
    //         weaponController.DevTool_DropNewWeapon(spawnPos);
    //     }
    //     CloseSelectPanel();
    // }


}
