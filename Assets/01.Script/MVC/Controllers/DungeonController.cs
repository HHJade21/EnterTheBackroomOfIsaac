using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// Controls dungeon stage flow and room transitions
// Responsibilities:
// - Generate/track rooms (2 normal, 1 boss) and connections
// - Detect room clear and open exits; transition to boss room
// - Signal GameManager when stage completed or player died
// SOLID:
// - SRP: Focus on dungeon logic; use factories/services for spawning

public class DungeonController : MonoBehaviour
{
    [Header("Enemy Spawning")]
    public List<GameObject> enemyPrefabC;
    public List<GameObject> enemyPrefabM;
    public List<GameObject> enemyPrefabY;
    public List<GameObject> enemyPrefabK;

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
    [Tooltip("충돌 체크 시 방 크기에 추가할 여유 공간")]
    public float roomCollisionMargin = 2f;
    [Tooltip("충돌 체크 범위를 시각적으로 표시")]
    public bool showCollisionBounds = true;
    
    // 충돌 체크 시 그릴 Bounds 저장 (Bounds, 충돌 여부)
    private List<(Bounds bounds, bool isColliding)> collisionBoundsToDraw = new List<(Bounds, bool)>();
    
    // 방향별 유효한 프리팹 리스트와 현재 인덱스 (1:북, 2:동, 3:남, 4:서)
    private Dictionary<int, (List<GameObject> prefabs, int currentIndex)> prefabsQueueByDirection = 
        new Dictionary<int, (List<GameObject>, int)>();


    // [State] Track combat active/cleared flags

    void Start(){
        InitializePrefabsByDirection();
        GenerateDungeon();
    }
    
    // 방향별 유효한 프리팹 리스트를 미리 구성 (랜덤 섞기 + 순환 큐 초기화)
    private void InitializePrefabsByDirection(){
        prefabsQueueByDirection.Clear();
        
        // 각 방향(1~4)별로 유효한 프리팹 찾기
        for(int direction = 1; direction <= 4; direction++){
            List<GameObject> validPrefabs = new List<GameObject>();
            foreach(var prefab in normalRoomPrefabs){
                if(FindDoorInPrefab(prefab, direction) != null){
                    validPrefabs.Add(prefab);
                }
            }
            
            // 프리팹을 랜덤으로 섞기
            for(int i = 0; i < validPrefabs.Count; i++){
                GameObject temp = validPrefabs[i];
                int randomIndex = Random.Range(i, validPrefabs.Count);
                validPrefabs[i] = validPrefabs[randomIndex];
                validPrefabs[randomIndex] = temp;
            }
            
            // 순환 큐 초기화 (프리팹 리스트, 현재 인덱스 0)
            prefabsQueueByDirection[direction] = (validPrefabs, 0);
            
            if(validPrefabs.Count == 0){
                Debug.LogWarning($"방향 {direction}의 문이 있는 방 프리팹이 없습니다!");
            }
        }
    }

    private void GenerateDungeon(){
        // 충돌 체크 시각화 리스트 초기화
        if(showCollisionBounds){
            collisionBoundsToDraw.Clear();
        }
        
        GameObject startRoom = Instantiate(startRoomPrefab, transform.position, Quaternion.identity, transform);
        startRoom.GetComponent<RoomController>().isCleared = true;
        startRoom.GetComponent<RoomController>().roomDepth = 0;
        startRoom.GetComponent<RoomController>().dungeonController = this;
        rooms.Add(startRoom);
        while(rooms.Count < minRooms){
            // ToList()로 복사본 만들어서 순회 (foreach 중 리스트 수정 가능)
            foreach(var room in rooms.ToList()){
                SpreadRoom(room);
            }
        }
    }

    private void SpreadRoom(GameObject room){
        RoomController roomController = room.GetComponent<RoomController>();
        if(roomController == null){
            return;
        }
        foreach(var door in roomController.doors){
            if(rooms.Count >= maxRooms){
                return;
            }
            if(door == null){
                continue;
            }
            
            SpriteRenderer doorRenderer = door.GetComponent<SpriteRenderer>();
            if(doorRenderer == null || doorRenderer.sortingOrder == -1){
                continue;
            }
            
            int chance = Random.Range(0, 100); // 대충 확률로 방 생성
            if(chance >= 25){
                continue;
            }
            
            int doorDirection = doorRenderer.sortingOrder; // 1:북, 2:동, 3:남, 4:서
            
            // 복도 생성 (복도의 끝에 방 생성 시도도 포함되어 있음)
            CreateCorridor(door.transform.position, doorDirection, roomController, doorRenderer);
            
        }
    }
    
    private void CreateCorridor(Vector3 startPosition, int direction, RoomController roomController, SpriteRenderer doorRenderer){
        Vector3 corridorPosition = startPosition;
        Vector3 corridorMove = Vector3.zero;
        int corridorIndex = 0;
        
        switch(direction){
            case 1: // 북
                corridorMove.y += 2;
                corridorIndex = 1; // 세로 복도
                break;
            case 2: // 동
                corridorMove.x += 2;
                corridorIndex = 0; // 가로 복도
                break;
            case 3: // 남
                corridorMove.y -= 2;
                corridorIndex = 1; // 세로 복도
                break;
            case 4: // 서
                corridorMove.x -= 2;
                corridorIndex = 0; // 가로 복도
                break;
        }
        List<GameObject> corridorList = new List<GameObject>();

        corridorPosition += corridorMove;
        // 복도 생성 (나중에 여러 칸 생성 로직으로 확장)
        corridorList.Add(Instantiate(corridorPrefab[corridorIndex], corridorPosition, Quaternion.identity, corridorParent));
        corridorPosition += corridorMove;
        corridorList.Add(Instantiate(corridorPrefab[corridorIndex], corridorPosition, Quaternion.identity, corridorParent));
        corridorPosition += corridorMove;
        corridorList.Add(Instantiate(corridorPrefab[corridorIndex], corridorPosition, Quaternion.identity, corridorParent));
        corridorPosition += corridorMove;
        corridorList.Add(Instantiate(corridorPrefab[corridorIndex], corridorPosition, Quaternion.identity, corridorParent));
        corridorPosition += corridorMove;
        corridorList.Add(Instantiate(corridorPrefab[corridorIndex], corridorPosition, Quaternion.identity, corridorParent));
        corridorPosition += corridorMove;

        GameObject newRoom = AttachRoomToCorridor(corridorPosition, direction);
        if(newRoom != null){
            newRoom.GetComponent<RoomController>().roomDepth = roomController.roomDepth + 1;
            if(rooms.Count < maxRooms){
                SpreadRoom(newRoom);
            }
            doorRenderer.sortingOrder = -1;
        }
        else{
            foreach(var corridor in corridorList){
                Destroy(corridor);
            }
        }
        return;
    }
    

    private GameObject AttachRoomToCorridor(Vector3 corridorEndPosition, int corridorDirection){
        // 복도 방향의 반대편 문이 필요함
        // 1(북) → 3(남), 2(동) → 4(서), 3(남) → 1(북), 4(서) → 2(동)
        int requiredDoorDirection = GetOppositeDirection(corridorDirection);
        
        // 미리 구성된 방향별 프리팹 큐 확인
        if(!prefabsQueueByDirection.ContainsKey(requiredDoorDirection)){
            Debug.LogWarning($"방향({requiredDoorDirection})의 문이 있는 방 프리팹이 없습니다!");
            return null;
        }
        
        var (prefabs, startIndex) = prefabsQueueByDirection[requiredDoorDirection];
        if(prefabs.Count == 0){
            Debug.LogWarning($"방향({requiredDoorDirection})의 문이 있는 방 프리팹이 없습니다!");
            return null;
        }
        
        // 최대 프리팹 개수의 절반만 시도 (무한 반복 방지 + 전부 도니까 가장 작은 방이 너무 많이 나옴)
        int maxAttempts = Mathf.Max(1, prefabs.Count / 2);
        int currentIndex = startIndex;
        
        for(int attempt = 0; attempt < maxAttempts; attempt++){
            // 현재 인덱스의 프리팹 가져오기
            GameObject roomPrefab = prefabs[currentIndex];
            GameObject targetDoor = FindDoorInPrefab(roomPrefab, requiredDoorDirection);
            
            if(targetDoor == null){
                // 다음 프리팹으로 이동
                currentIndex = (currentIndex + 1) % prefabs.Count;
                continue;
            }
            
            // 문의 로컬 위치 기준으로 방 중심 위치 계산
            Vector3 doorLocalPosition = targetDoor.transform.localPosition;
            Vector3 roomCenterPosition = corridorEndPosition - doorLocalPosition;
            
            // 충돌 체크 - 다른 방과 겹치면 다음 프리팹 시도
            if(CheckRoomCollision(roomPrefab, roomCenterPosition)){
                currentIndex = (currentIndex + 1) % prefabs.Count;
                continue;
            }
            
            // 충돌 없음! 방 생성 성공
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
            
            // 성공한 프리팹과 마지막 프리팹을 swap
            if(currentIndex < prefabs.Count - 1){
                GameObject temp = prefabs[currentIndex];
                prefabs[currentIndex] = prefabs[prefabs.Count - 1];
                prefabs[prefabs.Count - 1] = temp;
            }
            
            // 다음 시도를 위해 인덱스는 그대로 유지 (이미 마지막으로 이동했으므로)
            prefabsQueueByDirection[requiredDoorDirection] = (prefabs, currentIndex);
            
            return newRoom;
        }
        
        // 모든 프리팹을 시도했지만 실패
        Debug.LogWarning($"방향({requiredDoorDirection})의 모든 프리팹을 시도했지만 방 생성에 실패했습니다!");
        return null;
    }
    
    // 방이 다른 방과 충돌하는지 체크
    private bool CheckRoomCollision(GameObject roomPrefab, Vector3 roomCenterPosition){
        // 방 프리팹의 Bounds 가져오기
        Bounds newRoomBounds = GetPrefabBounds(roomPrefab, roomCenterPosition);
        
        bool isColliding = false;
        
        // 기존 방들과 충돌 체크
        foreach(var existingRoom in rooms){
            if(existingRoom == null) continue;
            
            Bounds existingBounds = GetObjectBounds(existingRoom);
            if(newRoomBounds.Intersects(existingBounds)){
                isColliding = true;
                break;
            }
        }
        
        // 충돌 체크 범위 시각화를 위해 저장
        if(showCollisionBounds){
            collisionBoundsToDraw.Add((newRoomBounds, isColliding));
        }
        
        return isColliding;
    }
    
    // 프리팹의 Bounds 계산 (생성 전)
    private Bounds GetPrefabBounds(GameObject prefab, Vector3 position){
        // GetObjectBounds와 동일한 방식으로 계산하기 위해 임시로 인스턴스화
        GameObject tempRoom = Instantiate(prefab, position, Quaternion.identity);
        Bounds bounds = GetObjectBounds(tempRoom);
        
        // 임시 오브젝트 제거
        #if UNITY_EDITOR
        DestroyImmediate(tempRoom);
        #else
        Destroy(tempRoom);
        #endif
        
        return bounds;
    }
    
    private Bounds GetObjectBounds(GameObject obj){
        Bounds bounds;
        
        // Floor 자식 오브젝트의 SpriteRenderer 찾기
        Transform floorTransform = obj.transform.Find("Floor");
        if(floorTransform != null){
            SpriteRenderer floorRenderer = floorTransform.GetComponent<SpriteRenderer>();
            if(floorRenderer != null){
                bounds = floorRenderer.bounds;
            }
            else {
                // Floor는 있는데 SpriteRenderer가 없으면 기본 크기
                bounds = new Bounds(obj.transform.position, Vector3.one * 10f);
            }
        }
        // Floor가 없으면 기본 크기
        else {
            bounds = new Bounds(obj.transform.position, Vector3.one * 10f);
        }
        
        // SpriteRenderer보다 조금 더 크게 계산 (여유 공간 추가)
        // roomCollisionMargin이 마이너스여도 최소 크기는 보장
        Vector3 marginOffset = Vector3.one * (roomCollisionMargin * 2f);
        Vector3 expandedSize = bounds.size + marginOffset;
        
        // 최소 크기 보장 (원본 크기의 50% 이하로 축소되지 않도록)
        Vector3 minSize = bounds.size * 0.5f;
        expandedSize.x = Mathf.Max(expandedSize.x, minSize.x);
        expandedSize.y = Mathf.Max(expandedSize.y, minSize.y);
        expandedSize.z = Mathf.Max(expandedSize.z, minSize.z);
        
        bounds.size = expandedSize;
        
        return bounds;
    }
    
    /// <summary>
    /// 복도와의 충돌 체크 (Polygon Collider 사용)
    /// </summary>
    private bool CheckCorridorCollision(Bounds roomBounds, GameObject corridor){
        // 복도에서 Polygon Collider 찾기
        PolygonCollider2D polygonCollider = corridor.GetComponent<PolygonCollider2D>();
        if(polygonCollider == null){
            polygonCollider = corridor.GetComponentInChildren<PolygonCollider2D>();
        }
        
        if(polygonCollider == null){
            // Polygon Collider가 없으면 bounds로 체크
            Bounds corridorBounds = GetObjectBounds(corridor);
            return roomBounds.Intersects(corridorBounds);
        }
        
        // Polygon Collider의 경로 가져오기
        Vector2[] points = polygonCollider.points;
        if(points == null || points.Length == 0){
            return false;
        }
        
        // Polygon Collider의 월드 좌표로 변환
        Vector2[] worldPoints = new Vector2[points.Length];
        for(int i = 0; i < points.Length; i++){
            worldPoints[i] = polygonCollider.transform.TransformPoint(points[i]);
        }
        
        // 방의 bounds와 Polygon Collider가 겹치는지 체크
        // 간단하게 bounds의 4개 모서리 점이 Polygon 안에 있는지, 또는 Polygon 점이 bounds 안에 있는지 확인
        Vector2 roomMin = new Vector2(roomBounds.min.x, roomBounds.min.y);
        Vector2 roomMax = new Vector2(roomBounds.max.x, roomBounds.max.y);
        
        // Polygon의 점이 bounds 안에 있는지 체크
        foreach(var point in worldPoints){
            if(point.x >= roomMin.x && point.x <= roomMax.x && 
               point.y >= roomMin.y && point.y <= roomMax.y){
                return true; // 충돌 발생
            }
        }
        
        // bounds의 모서리 점이 Polygon 안에 있는지 체크 (간단한 방법)
        Vector2[] roomCorners = new Vector2[]{
            roomMin,
            new Vector2(roomMax.x, roomMin.y),
            roomMax,
            new Vector2(roomMin.x, roomMax.y)
        };
        
        foreach(var corner in roomCorners){
            if(IsPointInPolygon(corner, worldPoints)){
                return true; // 충돌 발생
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 점이 Polygon 안에 있는지 체크 (Ray Casting 알고리즘)
    /// </summary>
    private bool IsPointInPolygon(Vector2 point, Vector2[] polygon){
        if(polygon == null || polygon.Length < 3) return false;
        
        bool inside = false;
        int j = polygon.Length - 1;
        
        for(int i = 0; i < polygon.Length; i++){
            if(((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
               (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)){
                inside = !inside;
            }
            j = i;
        }
        
        return inside;
    }
    
    /// <summary>
    /// 충돌 체크 범위를 시각적으로 표시 (Gizmos)
    /// </summary>
    private void OnDrawGizmos(){
        if(!showCollisionBounds) return;
        
        foreach(var (bounds, isColliding) in collisionBoundsToDraw){
            // 겹치는 경우 적색, 아니면 녹색
            Gizmos.color = isColliding ? new Color(1f, 0f, 0f, 0.3f) : new Color(0f, 1f, 0f, 0.3f);
            
            // 와이어프레임 박스 그리기
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            
            // 2D로 보이도록 사각형 그리기
            Vector3 size = bounds.size;
            Vector3 center = bounds.center;
            
            // 4개 모서리 선 그리기
            Gizmos.DrawLine(
                new Vector3(center.x - size.x/2, center.y - size.y/2, 0),
                new Vector3(center.x + size.x/2, center.y - size.y/2, 0)
            );
            Gizmos.DrawLine(
                new Vector3(center.x + size.x/2, center.y - size.y/2, 0),
                new Vector3(center.x + size.x/2, center.y + size.y/2, 0)
            );
            Gizmos.DrawLine(
                new Vector3(center.x + size.x/2, center.y + size.y/2, 0),
                new Vector3(center.x - size.x/2, center.y + size.y/2, 0)
            );
            Gizmos.DrawLine(
                new Vector3(center.x - size.x/2, center.y + size.y/2, 0),
                new Vector3(center.x - size.x/2, center.y - size.y/2, 0)
            );
        }
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
    
    // 프리팹에서 특정 방향의 문 찾기
    private GameObject FindDoorInPrefab(GameObject prefab, int doorDirection){
        RoomController roomController = prefab.GetComponent<RoomController>();
        if(roomController == null || roomController.doors == null){
            return null;
        }
        
        foreach(var door in roomController.doors){
            if(door == null) continue;
            
            SpriteRenderer doorRenderer = door.GetComponent<SpriteRenderer>();
            //Debug.Log(doorRenderer.sortingOrder);
            if(doorRenderer != null && doorRenderer.sortingOrder == doorDirection){
                return door;
            }
        }
        
        return null;
    }
}



