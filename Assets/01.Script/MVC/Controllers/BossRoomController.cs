using UnityEngine;
using System.Collections;

/// <summary>
/// 보스 방 컨트롤러: RoomController를 상속받아 보스 전용 기능을 추가합니다.
/// - 플레이어 입장 시 카메라 연출
/// - 보스 등장 애니메이션 및 스폰
/// </summary>
public class BossRoomController : RoomController
{
    [Header("Boss Settings")]
    [Tooltip("보스가 스폰될 위치 (인스펙터에서 할당)")]
    public Transform BossPos;
    
    [Tooltip("보스 프리팹 (인스펙터에서 할당, Idle 애니메이션을 재생하기 위해 사용)")]
    public GameObject BossEnemyPrefab;
    
    [Header("Camera Animation Settings")]
    [Tooltip("카메라 이동 속도")]
    public float cameraMoveSpeed = 5f;
    
    [Tooltip("카메라가 보스 위치에서 머무는 시간 (초)")]
    public float cameraStayDuration = 2f;

    [Header("UI Settings")]
    [Tooltip("표시할 보스 이름")]
    public string bossName = "UNKNOWN";
    [Tooltip("표시할 보스 설명 (비워두면 표시 안 함)")]
    public string bossDescription = "";
    
    private Camera mainCamera;
    private PlayerController playerController;
    private DungeonHUDController hudController;
    private GameObject bossPreviewInstance; // Idle 애니메이션만 재생하는 미리보기 인스턴스
    private bool hasBossSpawned = false;
    
    private new void Start()
    {
        // RoomController의 Start() 로직 수행 (doors 초기화, 플레이어 찾기, OpenRoom)
        if (doors == null)
        {
            doors = new GameObject[0];
        }
        
        // 플레이어가 인스펙터에서 할당되지 않았다면 자동으로 찾기
        if (player == null)
        {
            FindPlayerForBossRoom();
        }
        
        OpenRoomForBossRoom();
        
        // 카메라 찾기
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("BossRoomController: Main Camera를 찾을 수 없습니다!");
        }
        
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                hudController = playerController.hudController;
            }
        }
        
        // BossPos 위치에 보스 프리팹의 Idle 애니메이션 미리보기 생성
        if (BossPos != null && BossEnemyPrefab != null)
        {
            SpawnBossIdleAnimation();
        }
        else
        {
            Debug.LogWarning("BossRoomController: BossPos 또는 BossEnemyPrefab가 할당되지 않았습니다.");
        }
    }
    
    /// <summary>
    /// BossPos 위치에 보스 프리팹을 생성하고 Idle 애니메이션만 재생합니다.
    /// 실제 보스는 비활성화 상태로 두고, 나중에 SpawnBoss에서 활성화합니다.
    /// </summary>
    private void SpawnBossIdleAnimation()
    {
        if (BossEnemyPrefab == null || BossPos == null) return;
        
        // 보스 프리팹을 생성하되 비활성화 상태로 생성 (나중에 활성화하기 위해)
        bossPreviewInstance = Instantiate(BossEnemyPrefab, BossPos.position, Quaternion.identity);
        
        // EnemyController 비활성화 (실제 보스 행동 비활성화)
        EnemyController enemyController = bossPreviewInstance.GetComponent<EnemyController>();
        if (enemyController != null)
        {
            enemyController.enabled = false;
        }
        
        // 다른 컴포넌트들도 비활성화할 수 있음 (예: Rigidbody2D, Collider2D 등)
        // 하지만 Animator는 활성화 상태로 두어 Idle 애니메이션이 재생되도록 함
        
        // Animator가 있으면 Idle 애니메이션을 재생하도록 설정
        Animator animator = bossPreviewInstance.GetComponent<Animator>();
        if (animator != null)
        {
            // Animator Controller의 기본 상태가 Idle이면 자동으로 재생됨
            // 또는 명시적으로 Idle 파라미터를 설정하려면:
            // animator.SetBool("Idle", true); 또는 animator.SetTrigger("Idle");
            // 보스 프리팹의 Animator Controller 설정에 따라 조정 필요
        }
        else
        {
            Debug.LogWarning("BossRoomController: 보스 프리팹에 Animator 컴포넌트가 없습니다.");
        }
    }
    
    /// <summary>
    /// 보스 방 전용 CloseRoom 로직: 일반 적 스폰 대신 보스 등장 연출을 시작합니다.
    /// </summary>
    private void CloseBossRoom()
    {
        isClosed = true;
        isCleared = false;
        
        foreach (var door in doors)
        {
            if (door != null) door.SetActive(true);
        }
        
        // 보스 방에서는 일반 적 스폰 대신 보스 등장 연출 시작
        if (!hasBossSpawned)
        {
            StartCoroutine(BossIntroductionSequence());
        }
    }
    
    /// <summary>
    /// 보스 등장 연출 시퀀스: 카메라 이동 → 보스 스폰
    /// </summary>
    private IEnumerator BossIntroductionSequence()
    {
        Debug.Log("BossRoomController: 보스 등장 연출 시작");
        
        // HUD 패널 비활성화
        if (hudController != null && hudController.hudContainer != null)
        {
            hudController.hudContainer.gameObject.SetActive(false);
        }
        
        // 플레이어 조작 불가 (Time.timeScale을 0으로 설정하면 안 됨 - 카메라 이동을 위해)
        // 대신 플레이어 컨트롤러를 직접 비활성화하거나 다른 방법 사용
        bool wasPlayerEnabled = false;
        if (playerController != null)
        {
            wasPlayerEnabled = playerController.enabled;
            playerController.enabled = false;
        }
        
        // 카메라 컴포넌트 가져오기 (매번 가져오기)
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("BossRoomController: Main Camera를 찾을 수 없습니다!");
                yield break;
            }
        }
        
        // 카메라를 제어하는 다른 스크립트 비활성화 (예: CameraFollow 등)
        MonoBehaviour[] cameraScripts = mainCamera.GetComponents<MonoBehaviour>();
        bool[] cameraScriptsEnabled = new bool[cameraScripts.Length];
        for (int i = 0; i < cameraScripts.Length; i++)
        {
            if (cameraScripts[i] != null && cameraScripts[i] != this)
            {
                cameraScriptsEnabled[i] = cameraScripts[i].enabled;
                cameraScripts[i].enabled = false;
            }
        }
        
        // 카메라의 초기 위치 저장
        Vector3 initialCameraPos = mainCamera.transform.position;
        Debug.Log($"BossRoomController: 카메라 초기 위치: {initialCameraPos}");
        
        // 카메라를 보스 위치로 이동
        if (BossPos != null)
        {
            Vector3 bossCameraPos = new Vector3(BossPos.position.x, BossPos.position.y, mainCamera.transform.position.z);
            Debug.Log($"BossRoomController: 카메라를 보스 위치로 이동: {bossCameraPos}");
            yield return StartCoroutine(MoveCameraToPosition(mainCamera.transform, bossCameraPos, cameraMoveSpeed));
            Debug.Log($"BossRoomController: 카메라 보스 위치 도달 완료");
            
            // 보스 위치에서 잠시 머무름
            yield return new WaitForSeconds(cameraStayDuration);
            
            // 카메라를 플레이어 위치로 돌아감
            Vector3 playerCameraPos = new Vector3(player.transform.position.x, player.transform.position.y, mainCamera.transform.position.z);
            Debug.Log($"BossRoomController: 카메라를 플레이어 위치로 이동: {playerCameraPos}");
            yield return StartCoroutine(MoveCameraToPosition(mainCamera.transform, playerCameraPos, cameraMoveSpeed));
            Debug.Log($"BossRoomController: 카메라 플레이어 위치 도달 완료");
        }
        else
        {
            Debug.LogError("BossRoomController: BossPos가 할당되지 않았습니다!");
        }
        
        // 카메라 스크립트 다시 활성화
        for (int i = 0; i < cameraScripts.Length; i++)
        {
            if (cameraScripts[i] != null && cameraScripts[i] != this)
            {
                cameraScripts[i].enabled = cameraScriptsEnabled[i];
            }
        }
        
        // HUD 패널 활성화
        if (hudController != null && hudController.hudContainer != null)
        {
            hudController.hudContainer.gameObject.SetActive(true);
        }
        
        // 플레이어 조작 활성화
        if (playerController != null && wasPlayerEnabled)
        {
            playerController.enabled = true;
        }
        
        // 보스 Idle 애니메이션 중지 및 보스 스폰
        SpawnBoss();
    }
    
    /// <summary>
    /// 카메라를 특정 위치로 부드럽게 이동시키는 코루틴
    /// </summary>
    private IEnumerator MoveCameraToPosition(Transform cameraTransform, Vector3 targetPosition, float speed)
    {
        Vector3 startPosition = cameraTransform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        
        if (distance < 0.01f)
        {
            // 거리가 매우 가까우면 즉시 이동
            cameraTransform.position = targetPosition;
            yield break;
        }
        
        float duration = distance / speed;
        float elapsed = 0f;
        
        Debug.Log($"BossRoomController: 카메라 이동 시작 - 시작: {startPosition}, 목표: {targetPosition}, 거리: {distance}, 속도: {speed}, 예상 시간: {duration}");
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cameraTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        
        cameraTransform.position = targetPosition;
        Debug.Log($"BossRoomController: 카메라 이동 완료 - 최종 위치: {cameraTransform.position}");
    }
    
    /// <summary>
    /// 보스 미리보기 인스턴스를 실제 보스로 활성화합니다.
    /// </summary>
    private void SpawnBoss()
    {
        if (hasBossSpawned) return;
        
        if (bossPreviewInstance != null)
        {
            // 미리보기 인스턴스를 실제 보스로 활성화
            EnemyController enemyController = bossPreviewInstance.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.enabled = true;
                enemyController.roomController = this;
            }
            
            // 보스 스폰 완료 표시
            bossPreviewInstance = null; // 참조 초기화
            hasBossSpawned = true;

            // 보스 HUD 초기화 (싱글톤 사용)
            if (BossHUDController.Instance != null && enemyController != null)
            {
                BossHUDController.Instance.Initialize(enemyController, bossName, bossDescription);
            }
            
            Debug.Log("BossRoomController: 보스 스폰 완료.");
        }
        else
        {
            Debug.LogWarning("BossRoomController: 보스 미리보기 인스턴스가 없습니다. BossEnemyPrefab이 할당되었는지 확인해주세요.");
        }
    }
    
    /// <summary>
    /// RoomController의 OnTriggerEnter2D를 오버라이드하여 보스 방 전용 로직을 추가합니다.
    /// </summary>
    private new void OnTriggerEnter2D(Collider2D other)
    {
        if (!isCleared && !isClosed && other.CompareTag("PlayerFeet"))
        {
            // 플레이어가 아직 할당되지 않았다면 할당 (fallback)
            if (player == null)
            {
                if (other.transform.parent != null)
                {
                    player = other.transform.parent.gameObject;
                }
                else
                {
                    player = other.transform.root.gameObject;
                }
            }
            
            // 플레이어 컨트롤러 다시 찾기
            if (player != null)
            {
                playerController = player.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    hudController = playerController.hudController;
                }
            }
            
            // 보스 방 전용 CloseRoom 호출
            CloseBossRoom();
        }
    }
    
    /// <summary>
    /// 씬에서 플레이어 오브젝트를 자동으로 찾아서 할당합니다.
    /// RoomController.FindPlayer()와 동일한 로직입니다.
    /// </summary>
    private void FindPlayerForBossRoom()
    {
        PlayerController playerController = Object.FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.gameObject;
        }
        else
        {
            // PlayerController를 찾지 못한 경우 태그로 찾기 시도
            GameObject playerByTag = GameObject.FindWithTag("Player");
            if (playerByTag != null)
            {
                player = playerByTag;
            }
            else
            {
                Debug.LogWarning("BossRoomController: 플레이어를 찾을 수 없습니다. 인스펙터에서 수동으로 할당해주세요.");
            }
        }
    }
    
    /// <summary>
    /// 방의 문을 열어 플레이어가 출입할 수 있도록 합니다.
    /// RoomController.OpenRoom()과 동일한 로직입니다.
    /// </summary>
    private void OpenRoomForBossRoom()
    {
        isClosed = false;
        foreach (var door in doors)
        {
            if (door != null) door.SetActive(false);
        }
    }
}
