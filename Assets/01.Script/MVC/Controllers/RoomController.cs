using UnityEngine;

public class RoomController : MonoBehaviour
{
    public bool isCleared = false;

    [Header("Wall Settings")]
    [Tooltip("벽 프리팹")]
    public GameObject wallPrefab;
    [Tooltip("벽들을 담을 부모 트랜스폼 (비우면 자동생성)")]
    public Transform wallsRoot;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(!isCleared && other.CompareTag("Player"))
        {
            for(int x=-1; x<=3; x++)
            {
                for(int y=-1; y<=3; y++)
                {
                    if((x>-1 && x<3) && (y>-1 && y<3)) continue;
                    Vector3 pos = new Vector3(transform.position.x + x, transform.position.y + y, 0f);
                    GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity, this.transform);
                }
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
