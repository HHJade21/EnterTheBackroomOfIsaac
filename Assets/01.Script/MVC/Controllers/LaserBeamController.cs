using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 레이저 빔 컨트롤러: SpriteRenderer를 사용하여 벽까지 관통하는 레이저 빔을 구현합니다.
/// - 벽을 만나기 전까지 모든 적을 관통하여 데미지를 줍니다.
/// - 0.5초 후 자동으로 삭제됩니다.
/// </summary>
public class LaserBeamController : MonoBehaviour
{
    [Header("Beam Settings")]
    [Tooltip("빔의 두께 (월드 단위)")]
    public float beamWidth = 0.1f;
    [Tooltip("데미지량")]
    public float damage = 1f;
    [Tooltip("벽 레이어 마스크")]
    public LayerMask wallLayerMask = 1 << 8; // 기본값: Layer 8 (Wall)
    [Tooltip("적 레이어 마스크")]
    public LayerMask enemyLayerMask = 1 << 6; // 기본값: Layer 6 (Enemy)
    [Tooltip("빔 지속 시간 (초)")]
    public float lifetime = 0.5f;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D beamCollider;
    private float originalSpriteHeight; // 스프라이트 원본 높이 (월드 단위)
    private HashSet<EnemyController> hitEnemies = new HashSet<EnemyController>(); // 이미 맞은 적 추적

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        beamCollider = GetComponent<BoxCollider2D>();

        if (spriteRenderer == null)
        {
            Debug.LogError("LaserBeamController: SpriteRenderer가 필요합니다.");
            enabled = false;
            return;
        }

        if (beamCollider == null)
        {
            beamCollider = gameObject.AddComponent<BoxCollider2D>();
            beamCollider.isTrigger = true;
        }

        // 스프라이트의 원본 높이 계산 (월드 단위)
        if (spriteRenderer.sprite != null)
        {
            originalSpriteHeight = spriteRenderer.sprite.bounds.size.y;
        }
        else
        {
            originalSpriteHeight = 1f; // 기본값
        }
    }

    /// <summary>
    /// 레이저 빔을 초기화하고 설정합니다.
    /// </summary>
    /// <param name="startPosition">발사 시작 위치</param>
    /// <param name="direction">발사 방향 (정규화됨)</param>
    /// <param name="maxDistance">최대 거리 (벽이 없을 경우)</param>
    /// <param name="beamDamage">데미지량</param>
    public void Initialize(Vector2 startPosition, Vector2 direction, float maxDistance, float beamDamage)
    {
        damage = beamDamage;
        
        // 방향으로 회전 (스프라이트가 위쪽을 향하고 있으므로 -90도 오프셋 추가)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Raycast로 벽까지 거리 계산
        RaycastHit2D hit = Physics2D.Raycast(startPosition, direction, maxDistance, wallLayerMask);
        float distance = hit.collider != null ? hit.distance : maxDistance;

        // 스프라이트 스케일 조정
        if (originalSpriteHeight > 0f)
        {
            float scaleY = distance / originalSpriteHeight;
            transform.localScale = new Vector3(beamWidth, scaleY, 1f);
        }

        // 빔의 위치 설정: 스프라이트의 pivot이 중앙이므로, 시작점에서 방향으로 distance/2만큼 이동
        // 이렇게 하면 빔의 시작점이 정확히 발사 위치가 됩니다
        transform.position = startPosition + direction * (distance / 2f);

        // Collider 크기 및 위치 조정
        if (beamCollider != null)
        {
            beamCollider.size = new Vector2(beamWidth, distance);
            // 스프라이트의 pivot이 중앙이므로, Collider의 offset은 0 (이미 위치를 조정했으므로)
            beamCollider.offset = Vector2.zero;
        }

        // lifetime 후 삭제
        Destroy(gameObject, lifetime);
    }

    /// <summary>
    /// 적과의 충돌 감지: OnTriggerEnter2D로 처리합니다.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 적 레이어 확인
        if (enemyLayerMask != 0 && ((1 << other.gameObject.layer) & enemyLayerMask) == 0)
        {
            return;
        }

        // EnemyController 컴포넌트 확인
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null && !hitEnemies.Contains(enemy))
        {
            // 이미 맞은 적이 아니면 데미지 적용
            hitEnemies.Add(enemy);
            enemy.ApplyDamage(Mathf.RoundToInt(damage));
        }
    }
}

