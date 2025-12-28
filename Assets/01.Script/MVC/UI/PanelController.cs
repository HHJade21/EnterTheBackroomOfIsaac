using UnityEngine;

public class PanelController : MonoBehaviour
{
    public GameObject selectPanel;

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

    public void SelectOption(){
        // WeaponController가 있으면 무기 생성
        WeaponController weaponController = GameManager.Instance.weaponController;
        PrinterController printerController = GameManager.Instance.printerController;
        if (weaponController != null)
        {
            Vector3 spawnPos = printerController.weaponSpawnTransform != null ? printerController.weaponSpawnTransform.position : printerController.defaultSpawnPosition;
            weaponController.DevTool_DropNewWeapon(spawnPos);
        }
        CloseSelectPanel();
    }
}
