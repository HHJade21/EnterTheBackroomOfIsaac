using System.Collections;
using UnityEngine;

/// <summary>
/// 유도탄 컨트롤러: 가장 가까운 적을 추적하며 이동합니다.
/// - 0.5초마다 가장 가까운 적을 탐색하여 타겟 지정/변경
/// - 타겟이 없거나 벽이 있으면 직선 이동
/// - 그렇지 않으면 타겟 방향으로 이동
/// </summary>
public class GuidedProjectileController : MonoBehaviour
{
    [Header("Target Search Settings")]
    [Tooltip("타겟 탐색 주기 (초)")]
    public float targetSearchInterval = 0.3f;
    
    [Tooltip("최대 탐지 거리 (0이면 무제한)")]
    public float maxDetectionRange = 0f; // 0이면 무제한
    
    [Tooltip("벽 레이어 마스크")]
    public LayerMask wallLayerMask = 1 << 8; // 기본값: Layer 8 (Wall)
    
    [Tooltip("적 레이어 마스크")]
    public LayerMask enemyLayerMask = 1 << 6; // 기본값: Layer 6 (Enemy)
    
    private Transform currentTarget = null;
    private Vector2 initialDirection; // 초기 발사 방향
    private Rigidbody2D rb;
    private Coroutine targetSearchCoroutine;
    private bool isGuided = false; // 유도 모드인지 여부
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogWarning("GuidedProjectileController: Rigidbody2D가 없습니다!");
        }
    }
    
    private void Start()
    {
        // 초기 발사 방향 저장 (Rigidbody2D의 현재 속도 방향)
        if (rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            initialDirection = rb.linearVelocity.normalized;
        }
        else
        {
            // Rigidbody2D 속도가 없으면 transform.up 방향 사용
            initialDirection = transform.up;
        }
        
        // 타겟 탐색 코루틴 시작
        targetSearchCoroutine = StartCoroutine(TargetSearchRoutine());
    }
    
    private void FixedUpdate()
    {
        if (rb == null) return;
        
        Vector2 moveDirection;
        
        // 타겟이 있고 유도 모드인 경우
        if (isGuided && currentTarget != null)
        {
            Vector2 toTarget = (currentTarget.position - transform.position);
            moveDirection = toTarget.normalized;
        }
        else
        {
            // 타겟이 없거나 유도 모드가 아닌 경우: 초기 방향으로 직선 이동
            moveDirection = initialDirection;
        }
        
        // 현재 속도 유지하면서 방향만 변경
        float currentSpeed = rb.linearVelocity.magnitude;
        if (currentSpeed < 0.1f)
        {
            // 속도가 없으면 기본 속도 사용 (WeaponData의 projectileSpeed를 사용하려면 외부에서 설정 필요)
            currentSpeed = 3f; // 기본값
        }
        
        rb.linearVelocity = moveDirection * currentSpeed;
        
        // 회전 업데이트
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
    
    /// <summary>
    /// 타겟 탐색 코루틴: 0.5초마다 가장 가까운 적을 찾습니다.
    /// </summary>
    private IEnumerator TargetSearchRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(targetSearchInterval);
            
            // 가장 가까운 적 찾기
            Transform newTarget = FindClosestEnemy();
            
            // 타겟이 변경되었거나 새로 찾은 경우
            if (newTarget != currentTarget)
            {
                currentTarget = newTarget;
                
                // 타겟이 있고 벽이 없으면 유도 모드 활성화
                if (currentTarget != null)
                {
                    isGuided = !IsWallBetween(transform.position, currentTarget.position);
                }
                else
                {
                    isGuided = false;
                }
            }
            else if (currentTarget != null)
            {
                // 기존 타겟이 있으면 벽 체크 업데이트
                isGuided = !IsWallBetween(transform.position, currentTarget.position);
            }
        }
    }
    
    /// <summary>
    /// 가장 가까운 적을 찾는 메소드
    /// </summary>
    /// <returns>가장 가까운 적의 Transform, 없으면 null</returns>
    private Transform FindClosestEnemy()
    {
        Transform closestEnemy = null;
        float closestDistance = maxDetectionRange > 0f ? maxDetectionRange : float.MaxValue;
        
        // 씬의 모든 EnemyController 찾기
        EnemyController[] allEnemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        
        foreach (EnemyController enemy in allEnemies)
        {
            if (enemy == null || enemy.gameObject == null || enemy.gameObject.tag != "Enemy") continue;
            
            // 레이어 마스크 확인
            if (enemyLayerMask != 0 && ((1 << enemy.gameObject.layer) & enemyLayerMask) == 0)
            {
                continue;
            }
            
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }
        
        return closestEnemy;
    }
    
    /// <summary>
    /// 두 지점 사이에 벽이 있는지 확인하는 메소드
    /// </summary>
    /// <param name="from">시작 위치</param>
    /// <param name="to">목표 위치</param>
    /// <returns>벽이 있으면 true, 없으면 false</returns>
    private bool IsWallBetween(Vector2 from, Vector2 to)
    {
        Vector2 direction = to - from;
        float distance = direction.magnitude;
        
        if (distance < 0.1f) return false;
        
        direction.Normalize();
        
        // Raycast로 벽 체크
        RaycastHit2D hit = Physics2D.Raycast(from, direction, distance, wallLayerMask);
        
        return hit.collider != null;
    }
    
    private void OnDestroy()
    {
        // 코루틴 정리
        if (targetSearchCoroutine != null)
        {
            StopCoroutine(targetSearchCoroutine);
        }
    }
}
