using UnityEngine;
using System.Collections;

public class RoomController : MonoBehaviour
{
    public bool isCleared = false;
    public bool isClosed = false;

    public CMYKColor roomColor;
    
    [Header("Door Settings")]
    public GameObject[] doors; // Room의 자식 Door 오브젝트

    [Header("Dungeon Controller")]
    public DungeonController dungeonController;

    [Header("Player")]
    public GameObject player;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(!isCleared && !isClosed && other.CompareTag("PlayerFeet"))
        {
            CloseRoom();
            StartCoroutine(_tmpWaitAndClear());
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
    /// - 방의 collider 범위 안에서 랜덤하게 5~8개의 위치 지정
    /// - 플레이어로부터 거리가 10 이상인 위치에만 적 생성
    /// </summary>
    private void SpawnEnemiesInRoom()
    {
        if (dungeonController == null) return;
        if (player == null) return;

        // 방의 Collider2D 가져오기
        Collider2D roomCollider = GetComponent<Collider2D>();
        if (roomCollider == null) return;

        // 스폰할 적의 개수 (5~8개)
        int enemyCount = Random.Range(5, 9);
        
        // 플레이어 위치
        Vector3 playerPos = player.transform.position;
        float minDistanceFromPlayer = 10f;

        // 스폰 시도 횟수 제한 (무한 루프 방지)
        int maxAttempts = enemyCount * 10;
        int attempts = 0;
        int spawnedCount = 0;

        while (spawnedCount < enemyCount && attempts < maxAttempts)
        {
            attempts++;

            // 방의 bounds 내에서 랜덤 위치 생성
            Bounds bounds = roomCollider.bounds;
            Vector3 randomPos = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                0f
            );

            // Collider 내부에 있는지 확인
            if (!roomCollider.OverlapPoint(randomPos)) continue;

            // 플레이어로부터의 거리 확인
            float distanceToPlayer = Vector3.Distance(randomPos, playerPos);
            if (distanceToPlayer < minDistanceFromPlayer) continue;

            // 적 스폰을 위한 임시 Transform 생성
            GameObject tempTransformObj = new GameObject("TempSpawnPoint");
            tempTransformObj.transform.position = randomPos;
            tempTransformObj.transform.rotation = Quaternion.identity;

            // 적 스폰
            dungeonController.SpawnEnemy(tempTransformObj.transform);

            // 임시 오브젝트 제거
            Destroy(tempTransformObj);

            spawnedCount++;
        }

        if (spawnedCount < enemyCount)
        {
            Debug.LogWarning($"RoomController: Only spawned {spawnedCount} out of {enemyCount} enemies due to space constraints.");
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
        OpenRoom();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
