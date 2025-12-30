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
    
    [Tooltip("보스 프리팹 (인스펙터에서 할당)")]
    public GameObject BossEnemyPrefab;
    
    [Tooltip("보스 Idle 애니메이션을 재생할 임시 GameObject (인스펙터에서 할당, Animator 컴포넌트 필요)")]
    public GameObject BossIdleAnimationObject;
    
    [Header("Camera Animation Settings")]
    [Tooltip("카메라 이동 속도")]
    public float cameraMoveSpeed = 5f;
    
    [Tooltip("카메라가 보스 위치에서 머무는 시간 (초)")]
    public float cameraStayDuration = 2f;
    
    private Camera mainCamera;
    private PlayerController playerController;
    private DungeonHUDController hudController;
    private GameObject bossIdleAnimationInstance;
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
        
        // BossPos 위치에 Idle 애니메이션 GameObject 생성
        if (BossPos != null && BossIdleAnimationObject != null)
        {
            SpawnBossIdleAnimation();
        }
        else
        {
            Debug.LogWarning("BossRoomController: BossPos 또는 BossIdleAnimationObject가 할당되지 않았습니다.");
        }
    }
    
    /// <summary>
    /// BossPos 위치에 보스 Idle 애니메이션을 재생하는 GameObject를 생성합니다.
    /// </summary>
    private void SpawnBossIdleAnimation()
    {
        if (BossIdleAnimationObject == null || BossPos == null) return;
        
        bossIdleAnimationInstance = Instantiate(BossIdleAnimationObject, BossPos.position, Quaternion.identity);
        
        // Animator가 있으면 Idle 애니메이션이 자동으로 재생되도록 설정
        Animator animator = bossIdleAnimationInstance.GetComponent<Animator>();
        if (animator != null)
        {
            // Animator가 기본 상태로 Idle을 재생하도록 설정 (Animator Controller에서 설정되어야 함)
            animator.SetTrigger("Idle");
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
        
        // 카메라의 초기 위치 저장
        Vector3 initialCameraPos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        
        // 카메라를 보스 위치로 이동
        if (mainCamera != null && BossPos != null)
        {
            Vector3 bossCameraPos = new Vector3(BossPos.position.x, BossPos.position.y, mainCamera.transform.position.z);
            yield return StartCoroutine(MoveCameraToPosition(mainCamera.transform, bossCameraPos, cameraMoveSpeed));
            
            // 보스 위치에서 잠시 머무름
            yield return new WaitForSeconds(cameraStayDuration);
            
            // 카메라를 플레이어 위치로 돌아감
            Vector3 playerCameraPos = new Vector3(player.transform.position.x, player.transform.position.y, mainCamera.transform.position.z);
            yield return StartCoroutine(MoveCameraToPosition(mainCamera.transform, playerCameraPos, cameraMoveSpeed));
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
        float duration = distance / speed;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            cameraTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        
        cameraTransform.position = targetPosition;
    }
    
    /// <summary>
    /// 보스 Idle 애니메이션을 중지하고 보스 프리팹을 스폰합니다.
    /// </summary>
    private void SpawnBoss()
    {
        if (hasBossSpawned) return;
        
        // 보스 Idle 애니메이션 중지 및 제거
        if (bossIdleAnimationInstance != null)
        {
            Destroy(bossIdleAnimationInstance);
            bossIdleAnimationInstance = null;
        }
        
        // 보스 프리팹 스폰
        if (BossEnemyPrefab != null && BossPos != null)
        {
            GameObject bossInstance = Instantiate(BossEnemyPrefab, BossPos.position, Quaternion.identity);
            
            // EnemyController에 roomController 할당
            EnemyController enemyController = bossInstance.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.roomController = this;
            }
            
            // 보스를 enemies 리스트에 추가
            enemies.Add(bossInstance);
            
            hasBossSpawned = true;
            
            Debug.Log("BossRoomController: 보스 스폰 완료.");
        }
        else
        {
            Debug.LogWarning("BossRoomController: BossEnemyPrefab 또는 BossPos가 할당되지 않았습니다.");
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
