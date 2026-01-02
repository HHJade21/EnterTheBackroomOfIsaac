using UnityEngine;

/// <summary>
/// 하트 아이템 컨트롤러: 플레이어와 충돌 시 체력을 1 회복시킵니다.
/// </summary>
public class HeartItemController : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    /// <summary>
    /// 플레이어와 충돌 시 호출됩니다.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어 태그 확인
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // PlayerController 찾기
        PlayerController playerController = other.GetComponent<PlayerController>();
        if (playerController == null)
        {
            playerController = other.GetComponentInParent<PlayerController>();
        }

        if (playerController == null)
        {
            Debug.LogWarning("HeartItemController: PlayerController를 찾을 수 없습니다.");
            return;
        }

        // 현재 HP가 최대 HP보다 낮은지 확인
        if (playerController.currentHP < playerController.maxHP)
        {
            // HP 1 증가 (최대 HP를 초과하지 않도록 제한)
            playerController.currentHP = Mathf.Min(playerController.currentHP + 1, playerController.maxHP);

            // 애니메이터 "used" 트리거 발동
            if (animator != null)
            {
                animator.SetTrigger("Used");
            }

            Debug.Log($"HeartItemController: 플레이어 체력 회복. 현재 HP: {playerController.currentHP}/{playerController.maxHP}");
        }
        // currentHP == maxHP인 경우 아무 일도 하지 않음
    }

    public void DestroyItem()
    {
        Destroy(gameObject);
    }
}

