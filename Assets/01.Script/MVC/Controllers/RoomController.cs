using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RoomController : MonoBehaviour
{
    public bool isCleared = false;
    public bool isClosed = false;

    public CMYKColor roomColor;

    public int roomDepth = 0;

    
    
    [Header("Room Settings")]
    public GameObject[] doors; // Room의 자식 Door 오브젝트
    [ArrayLabel("ULRD", "LRD", "ULD", "LD", "ULR", "LR", "UL", "L", "URD", "RD", "UD", "D", "UR", "R", "U")]
    public Sprite[] wallSprites = new Sprite[15];
    [ArrayLabel("ULRD", "LRD", "ULD", "LD", "ULR", "LR", "UL", "L", "URD", "RD", "UD", "D", "UR", "R", "U")]
    public Sprite[] floorSprites = new Sprite[15];
    [ArrayLabel("ULRD", "LRD", "ULD", "LD", "ULR", "LR", "UL", "L", "URD", "RD", "UD", "D", "UR", "R", "U")]
    public Sprite[] borderSprites = new Sprite[15];

    private SpriteRenderer wallSpriteRenderer;
    private SpriteRenderer floorSpriteRenderer;
    private SpriteRenderer borderSpriteRenderer;

    private void Awake()
    {
        wallSpriteRenderer = transform.Find("Wall").GetComponentInChildren<SpriteRenderer>();
        floorSpriteRenderer = transform.Find("Floor").GetComponentInChildren<SpriteRenderer>();
        borderSpriteRenderer = transform.Find("Border").GetComponentInChildren<SpriteRenderer>();
    }

    [Header("Dungeon Controller")]
    public DungeonController dungeonController;

    [Header("Player")]
    [Tooltip("인스펙터에서 할당하지 않으면 Start()에서 자동으로 찾습니다.")]
    public GameObject player;

    [Header("Enemies")]
    [Tooltip("적이 스폰될 위치들의 리스트 (인스펙터에서 할당)")]
    public List<Transform> spawnPoints = new List<Transform>();
    public List<GameObject> enemies = new List<GameObject>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(!isCleared && !isClosed && other.CompareTag("PlayerFeet"))
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

            CloseRoom();
            // StartCoroutine(_tmpWaitAndClear());
        }
    }

    private IEnumerator _tmpWaitAndClear(){
        yield return new WaitForSeconds(100f);
        isCleared = true;
        OpenRoom();
    }

    private void CloseRoom(){
        isClosed = true;
        isCleared = false;
        foreach(var door in doors)
        {
            if(door != null) door.SetActive(true);
        }

        // 적 스폰 로직
        SpawnEnemiesInRoom();
    }

    /// <summary>
    /// 방 내부에 적을 스폰하는 메서드
    /// - spawnPoints 리스트에서 랜덤으로 enemyCount개의 좌표를 선택
    /// - 각 좌표에 대해 SpawnEnemy() 호출
    /// </summary>
    private void SpawnEnemiesInRoom()
    {
        if (dungeonController == null)
        {
            Debug.LogError("RoomController: DungeonController가 할당되지 않았습니다.");
            return;
        }

        // spawnPoints 리스트가 비어있거나 null인지 확인
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("RoomController: spawnPoints 리스트가 비어있습니다. 인스펙터에서 스폰 포인트를 할당해주세요.");
            return;
        }

        // 유효한 spawnPoint만 필터링 (null 제거)
        List<Transform> validSpawnPoints = spawnPoints.Where(sp => sp != null).ToList();
        
        if (validSpawnPoints.Count == 0)
        {
            Debug.LogWarning("RoomController: 유효한 spawnPoint가 없습니다.");
            return;
        }

        // 스폰할 적의 개수 (5~8개)
        int enemyCount = Random.Range(5, 9);
        
        // 스폰할 개수가 사용 가능한 스폰 포인트보다 많으면 제한
        int spawnCount = Mathf.Min(enemyCount, validSpawnPoints.Count);

        // 랜덤으로 스폰 포인트 선택 (중복 없이)
        List<Transform> selectedSpawnPoints = new List<Transform>();
        List<Transform> availablePoints = new List<Transform>(validSpawnPoints);

        for (int i = 0; i < spawnCount; i++)
        {
            if (availablePoints.Count == 0) break;

            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform selectedPoint = availablePoints[randomIndex];
            selectedSpawnPoints.Add(selectedPoint);
            availablePoints.RemoveAt(randomIndex); // 중복 선택 방지
        }

        // 선택된 스폰 포인트에 적 스폰
        foreach (Transform spawnPoint in selectedSpawnPoints)
        {
            enemies.Add(SpawnEnemy(spawnPoint));
        }

        Debug.Log($"RoomController: {selectedSpawnPoints.Count}개 적 스폰 완료.");
    }

    public GameObject SpawnEnemy(Transform transform)
    {
        GameObject enemyPrefab = null;
        switch(roomColor){
            case CMYKColor.Black:
                enemyPrefab = dungeonController.enemyPrefabK[Random.Range(0, dungeonController.enemyPrefabK.Count)];
                break;
            case CMYKColor.Cyan:
                enemyPrefab = dungeonController.enemyPrefabC[Random.Range(0, dungeonController.enemyPrefabC.Count)];
                break;
            case CMYKColor.Magenta:
                enemyPrefab = dungeonController.enemyPrefabM[Random.Range(0, dungeonController.enemyPrefabM.Count)];
                break;
            case CMYKColor.Yellow:
                enemyPrefab = dungeonController.enemyPrefabY[Random.Range(0, dungeonController.enemyPrefabY.Count)];
                break;
        }
        GameObject enemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        enemy.GetComponent<EnemyController>().roomController = this;
        return enemy;
    }

    public void OnEnemyDeath(EnemyController deadEnemy)
    {
        enemies.Remove(deadEnemy.gameObject);
        if(enemies.Count == 0 && isClosed)
        {
            isCleared = true;
            OpenRoom();
        }
    }

    private void OpenRoom(){
        isClosed = false;
        foreach(var door in doors)
        {
            if(door != null) door.SetActive(false);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // doors가 null이면 빈 배열 할당 (null 체크 방지)
        if(doors == null)
        {
            doors = new GameObject[0];
        }
        
        // 플레이어가 인스펙터에서 할당되지 않았다면 자동으로 찾기
        if (player == null)
        {
            FindPlayer();
        }
        
        OpenRoom();
    }

    /// <summary>
    /// 씬에서 플레이어 오브젝트를 자동으로 찾아서 할당합니다.
    /// PlayerController 컴포넌트를 가진 오브젝트를 찾습니다.
    /// </summary>
    private void FindPlayer()
    {
        PlayerController playerController = FindObjectOfType<PlayerController>();
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
                Debug.LogWarning("RoomController: 플레이어를 찾을 수 없습니다. 인스펙터에서 수동으로 할당해주세요.");
            }
        }
    }

    public void UpdateRoomSprites(){
        int doorState = 0;
        
        foreach(var door in doors){
            if(door == null) continue;
            
            SpriteRenderer doorRenderer = door.GetComponent<SpriteRenderer>();
            if(doorRenderer == null) continue;
            
            int sortingOrder = doorRenderer.sortingOrder;
            
            // 1:북, 2:동, 3:남, 4:서
            if(sortingOrder != -1 && sortingOrder >= 1 && sortingOrder <= 4){
                int bitIndex = sortingOrder - 1;
                doorState |= (1 << bitIndex);
            }
        }
        
        // 스프라이트 변경
        if(wallSpriteRenderer != null && doorState < wallSprites.Length && wallSprites[doorState] != null){
            wallSpriteRenderer.sprite = wallSprites[doorState];
        }
        if(floorSpriteRenderer != null && doorState < floorSprites.Length && floorSprites[doorState] != null){
            floorSpriteRenderer.sprite = floorSprites[doorState];
        }
        if(borderSpriteRenderer != null && doorState < borderSprites.Length && borderSprites[doorState] != null){
            borderSpriteRenderer.sprite = borderSprites[doorState];
        }
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
