using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("Tile Settings")]
    [Tooltip("바닥에 깔 타일 프리팹 (SpriteRenderer 포함 GameObject)")]
    public GameObject tilePrefab;

    [Tooltip("가로 타일 개수")]
    public int width = 16;

    [Tooltip("세로 타일 개수")]
    public int height = 9;

    [Tooltip("타일 한 칸의 월드 크기 (스프라이트 픽셀 퍼 유닛과 일치)")]
    public float tileSize = 1f;

    [Tooltip("프리팹 스프라이트의 실제 크기로 tileSize 자동 설정")]
    public bool autoTileSizeFromPrefab = true;

    [Header("Camera Fit (1920x1080 기준)")]
    [Tooltip("카메라를 1920x1080 기준으로 세팅하고 화면을 타일로 꽉 채움")]
    public bool fillCameraToResolution = true;

    [Tooltip("목표 해상도 가로 픽셀")]
    public int targetWidthPx = 1920;

    [Tooltip("목표 해상도 세로 픽셀")]
    public int targetHeightPx = 1080;

    [Header("Placement")] 
    [Tooltip("타일 배치 시작 좌표 (좌하단 기준)")]
    public Vector2 origin = Vector2.zero;

    [Tooltip("타일들을 담을 부모 트랜스폼 (비우면 자동생성)")]
    public Transform tilesRoot;

    [Header("Wall Settings")]
    [Tooltip("Ground 주변에 배치할 벽 프리팹")]
    public GameObject wallPrefab;

    [Tooltip("벽 생성을 활성화할지 여부")]
    public bool generateWalls = true;

    [Tooltip("벽들을 담을 부모 트랜스폼 (비우면 자동생성)")]
    public Transform wallsRoot;

    void Start()
    {
        if (autoTileSizeFromPrefab)
        {
            TryAutoSetTileSize();
        }

        if (fillCameraToResolution)
        {
            FitCameraAndGridToResolution();
        }
        GenerateFloorTiles();
        
        if (generateWalls)
        {
            GenerateWalls();
        }
    }

    public void GenerateFloorTiles()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("StageManager: tilePrefab 이(가) 비어있습니다. 인스펙터에서 지정하세요.");
            return;
        }

        EnsureTilesRoot();
        ClearChildren(tilesRoot);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 pos = new Vector3(origin.x + x * tileSize, origin.y + y * tileSize, 0f);
                GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity, tilesRoot);
                tile.name = $"Tile_{x}_{y}";
            }
        }
    }

    private void EnsureTilesRoot()
    {
        if (tilesRoot != null) return;
        GameObject root = new GameObject("TilesRoot");
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;
        tilesRoot = root.transform;
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child.gameObject);
            else
                Destroy(child.gameObject);
            #else
            Destroy(child.gameObject);
            #endif
        }
    }

    private void TryAutoSetTileSize()
    {
        if (tilePrefab == null) return;
        var spriteRenderer = tilePrefab.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        var sprite = spriteRenderer.sprite;
        float worldSizeX = sprite.rect.width / sprite.pixelsPerUnit;
        tileSize = worldSizeX;
    }

    private void FitCameraAndGridToResolution()
    {
        var cam = Camera.main;
        if (cam == null) return;

        // 카메라를 직교로 고정하고 목표 해상도 높이에 맞춘다.
        cam.orthographic = true;

        // tilePrefab의 스프라이트 PPU를 사용하여 orthographicSize 결정
        float ppu = 100f;
        var sr = tilePrefab != null ? tilePrefab.GetComponentInChildren<SpriteRenderer>() : null;
        if (sr != null && sr.sprite != null)
        {
            ppu = sr.sprite.pixelsPerUnit;
        }

        cam.orthographicSize = targetHeightPx / (2f * ppu);

        // 화면 월드 크기 계산
        float viewHeight = cam.orthographicSize * 2f;
        float viewWidth = viewHeight * cam.aspect;

        // 좌하단 원점을 카메라 화면 좌하단으로 설정
        Vector3 c = cam.transform.position;
        origin = new Vector2(c.x - viewWidth * 0.5f, c.y - viewHeight * 0.5f);

        // 화면을 빈틈없이 덮도록 그리드 크기 산정(여유 1칸)
        int tilesX = Mathf.CeilToInt(viewWidth / tileSize) + 1;
        int tilesY = Mathf.CeilToInt(viewHeight / tileSize) + 1;

        width = Mathf.Max(1, tilesX);
        height = Mathf.Max(1, tilesY);
    }

    public void GenerateWalls()
    {
        if (wallPrefab == null)
        {
            Debug.LogWarning("StageManager: wallPrefab이 비어있습니다. 벽 생성을 건너뜁니다.");
            return;
        }

        EnsureWallsRoot();
        ClearChildren(wallsRoot);

        // 아래쪽 벽 (왼쪽 모서리 포함)
        for (int x = -1; x <= width; x++)
        {
            Vector3 pos = new Vector3(origin.x + x * tileSize, origin.y - tileSize, 0f);
            GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity, wallsRoot);
            wall.name = $"Wall_Bottom_{x}";
        }

        // 위쪽 벽 (왼쪽 모서리 포함)
        for (int x = -1; x <= width; x++)
        {
            Vector3 pos = new Vector3(origin.x + x * tileSize, origin.y + height * tileSize, 0f);
            GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity, wallsRoot);
            wall.name = $"Wall_Top_{x}";
        }

        // 왼쪽 벽 (아래쪽과 위쪽 모서리 제외, 이미 배치됨)
        for (int y = 0; y < height; y++)
        {
            Vector3 pos = new Vector3(origin.x - tileSize, origin.y + y * tileSize, 0f);
            GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity, wallsRoot);
            wall.name = $"Wall_Left_{y}";
        }

        // 오른쪽 벽 (아래쪽과 위쪽 모서리 제외, 이미 배치됨)
        for (int y = 0; y < height; y++)
        {
            Vector3 pos = new Vector3(origin.x + width * tileSize, origin.y + y * tileSize, 0f);
            GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity, wallsRoot);
            wall.name = $"Wall_Right_{y}";
        }
    }

    private void EnsureWallsRoot()
    {
        if (wallsRoot != null) return;
        GameObject root = new GameObject("WallsRoot");
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;
        wallsRoot = root.transform;
    }
}
