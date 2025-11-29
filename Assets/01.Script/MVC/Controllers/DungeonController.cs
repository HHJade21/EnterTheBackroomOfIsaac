using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// Controls dungeon stage flow and room transitions
// Responsibilities:
// - Generate/track rooms (2 normal, 1 boss) and connections
// - Handle entering rooms, spawning enemies, and locking doors during combat
// - Detect room clear and open exits; transition to boss room
// - Signal GameManager when stage completed or player died
// SOLID:
// - SRP: Focus on dungeon logic; use factories/services for spawning

public class DungeonController : MonoBehaviour
{
    [Header("Enemy Spawning")]
    public GameObject enemyPrefab;

    [Header("Corridor")]
    public Transform corridorParent;
    [ArrayLabel("─", "│", "┌", "┐", "└", "┘")] [SerializeField]
    private GameObject[] corridorPrefab;

    [Header("Room Prefab")]
    public GameObject startRoomPrefab;
    public GameObject[] normalRoomPrefabs;

    [Header("Rooms")]
    public int minRooms = 1;
    public int maxRooms = 10;
    public List<GameObject> rooms = new List<GameObject>();


    // [Rooms] Data for current room, neighbors, and visited state
    // [Spawning] Trigger enemy waves and boss spawn
    public void SpawnEnemy(Transform transform)
    {
        Instantiate(enemyPrefab, transform.position, Quaternion.identity);
    }
    // [State] Track combat active/cleared flags

    void Start(){
        GenerateDungeon();
    }

    private void GenerateDungeon(){
        GameObject startRoom = Instantiate(startRoomPrefab, transform.position, Quaternion.identity, transform);
        startRoom.GetComponent<RoomController>().isCleared = true;
        rooms.Add(startRoom);
        //while(rooms.Count < minRooms){
            // ToList()로 복사본 만들어서 순회 (foreach 중 리스트 수정 가능)
            foreach(var room in rooms.ToList()){
                SpreadRoom(room);
            }
        //}
    }

    private void SpreadRoom(GameObject room){
        RoomController roomController = room.GetComponent<RoomController>();
        if(roomController == null){
            return;
        }
        foreach(var door in roomController.doors){
            if(door == null){
                continue;
            }
            
            SpriteRenderer doorRenderer = door.GetComponent<SpriteRenderer>();
            if(doorRenderer == null || doorRenderer.sortingOrder == -1){
                continue;
            }
            
            int doorDirection = doorRenderer.sortingOrder; // 1:북, 2:동, 3:남, 4:서
            
            // 복도 생성 (나중에 여러 칸 생성 로직 추가 가능)
            Vector3 corridorEndPosition = CreateCorridor(door.transform.position, doorDirection);
            
            // 복도 끝에 방 붙이기
            AttachRoomToCorridor(corridorEndPosition, doorDirection);
            
            doorRenderer.sortingOrder = -1;
        }
    }
    
    /// <summary>
    /// 복도 생성 (나중에 여러 칸 생성 로직으로 확장 가능)
    /// </summary>
    /// <param name="startPosition">복도 시작 위치 (문 위치)</param>
    /// <param name="direction">복도 방향 (1:북, 2:동, 3:남, 4:서)</param>
    /// <returns>복도 끝 위치</returns>
    private Vector3 CreateCorridor(Vector3 startPosition, int direction){
        Vector3 corridorPosition = startPosition;
        Vector3 corridorMove = Vector3.zero;
        int corridorIndex = 0;
        
        switch(direction){
            case 1: // 북
                corridorMove.y += 1;
                corridorIndex = 1; // 세로 복도
                break;
            case 2: // 동
                corridorMove.x += 1;
                corridorIndex = 0; // 가로 복도
                break;
            case 3: // 남
                corridorMove.y -= 1;
                corridorIndex = 1; // 세로 복도
                break;
            case 4: // 서
                corridorMove.x -= 1;
                corridorIndex = 0; // 가로 복도
                break;
        }
        corridorPosition += corridorMove;
        // 복도 생성 (나중에 여러 칸 생성 로직으로 확장)
        Instantiate(corridorPrefab[corridorIndex], corridorPosition, Quaternion.identity, corridorParent);
        corridorPosition += corridorMove;
        Instantiate(corridorPrefab[corridorIndex], corridorPosition, Quaternion.identity, corridorParent);
        corridorPosition += corridorMove;
        // 복도 끝 위치 반환 (지금은 1칸이므로 복도 위치 = 끝 위치)
        return corridorPosition;
    }
    
    /// <summary>
    /// 복도 끝에 방 붙이기
    /// </summary>
    /// <param name="corridorEndPosition">복도 끝 위치</param>
    /// <param name="corridorDirection">복도 방향 (1:북, 2:동, 3:남, 4:서)</param>
    private void AttachRoomToCorridor(Vector3 corridorEndPosition, int corridorDirection){
        // 복도 방향의 반대편 문이 필요함
        // 1(북) → 3(남), 2(동) → 4(서), 3(남) → 1(북), 4(서) → 2(동)
        int requiredDoorDirection = GetOppositeDirection(corridorDirection);
        
        // 방 프리팹에서 해당 방향의 문 찾기
        GameObject roomPrefab = normalRoomPrefabs[0];
        GameObject targetDoor = FindDoorInPrefab(normalRoomPrefabs[0], requiredDoorDirection);
        
        if(targetDoor == null){
            Debug.LogWarning($"방 프리팹에 {requiredDoorDirection} 방향 문이 없습니다.");
            return;
        }
        
        // 문의 로컬 위치 기준으로 방 중심 위치 계산
        Vector3 doorLocalPosition = targetDoor.transform.localPosition;
        Vector3 roomCenterPosition = corridorEndPosition - doorLocalPosition;
        
        // 방 생성
        GameObject newRoom = Instantiate(roomPrefab, roomCenterPosition, Quaternion.identity, transform);

        // 생성한 방의 스크립트에 데이터 할당 (방 깊이, 던전 컨트롤러)
        RoomController newRoomController = newRoom.GetComponent<RoomController>();
        if(newRoomController != null){
            newRoomController.dungeonController = this;
        }
        
        // 생성된 방의 연결된 문의 sortingOrder를 -1로 설정
        GameObject connectedDoor = FindDoorInPrefab(newRoom, requiredDoorDirection);
        if(connectedDoor != null){
            SpriteRenderer doorRenderer = connectedDoor.GetComponent<SpriteRenderer>();
            if(doorRenderer != null){
                doorRenderer.sortingOrder = -1;
            }
        }
        
        rooms.Add(newRoom);
    }
    
    private int GetOppositeDirection(int direction){
        switch(direction){
            case 1: return 3; // 북 → 남
            case 2: return 4; // 동 → 서
            case 3: return 1; // 남 → 북
            case 4: return 2; // 서 → 동
            default: return -1;
        }
    }
    
    /// <summary>
    /// 프리팹에서 특정 방향의 문 찾기
    /// </summary>
    private GameObject FindDoorInPrefab(GameObject prefab, int doorDirection){
        RoomController roomController = prefab.GetComponent<RoomController>();
        if(roomController == null || roomController.doors == null){
            return null;
        }
        
        foreach(var door in roomController.doors){
            if(door == null) continue;
            
            SpriteRenderer doorRenderer = door.GetComponent<SpriteRenderer>();
            //Debug.Log(doorRenderer.sortingOrder);
            if(true || doorRenderer != null && doorRenderer.sortingOrder == doorDirection){
                return door;
            }
        }
        
        return null;
    }
}


