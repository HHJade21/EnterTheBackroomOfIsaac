using UnityEngine;

// 벽 가림 처리를 위한 별도 스크립트
// Player 아래의 Feet에 붙여서 사용
public class WallOcclusionHandler : MonoBehaviour
{
    [Tooltip("플레이어의 SpriteRenderer (PlayerController에서 가져올 수도 있음)")]
    public SpriteRenderer playerSpriteRenderer;
    
    [Tooltip("벽 뒤에 있을 때의 sortingOrder")]
    public int behindWallOrder = -1;
    
    [Tooltip("기본 sortingOrder")]
    public int defaultOrder = 1;

    private void Awake()
    {
        // PlayerController에서 spriteRenderer 가져오기
        if (playerSpriteRenderer == null)
        {
            PlayerController playerController = GetComponentInParent<PlayerController>();
            if (playerController != null)
            {
                playerSpriteRenderer = playerController.spriteRenderer;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("WallOcclusion"))
        {
            if (playerSpriteRenderer != null)
            {
                playerSpriteRenderer.sortingOrder = behindWallOrder; // 벽 앞으로
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("WallOcclusion"))
        {
            if (playerSpriteRenderer != null)
            {
                playerSpriteRenderer.sortingOrder = defaultOrder; // 원래대로
            }
        }
    }
}

