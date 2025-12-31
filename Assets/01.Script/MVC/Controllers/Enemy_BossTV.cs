using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 보스 TV: 5가지 공격 패턴을 가진 정적 보스 적
/// - 이동하지 않음
/// - 다양한 탄막 패턴으로 플레이어를 공격
/// </summary>
public class Enemy_BossTV : EnemyController
{
    [Header("Boss TV Settings")]
    [Tooltip("보스가 이동하지 않음")]
    public bool isStationary = true;
    
    [Header("Projectile Prefabs")]
    [Tooltip("기본 투사체 프리팹 (패턴 1, 2, 3에서 사용)")]
    public GameObject projectilePrefab;
    
    [Tooltip("Reticle 프리팹 (패턴 4, 5에서 사용)")]
    public GameObject reticlePrefab;
    
    [Tooltip("Bomb 프리팹 (패턴 4, 5에서 사용)")]
    public GameObject bombPrefab;
    
    [Header("Pattern 1 Settings")]
    [Tooltip("패턴 1 지속 시간 (초)")]
    public float pattern1Duration = 7f;
    
    [Tooltip("패턴 1 발사 간격 (초)")]
    public float pattern1FireInterval = 0.15f;
    
    [Tooltip("패턴 1 빈 공간 각도 범위 (도)")]
    public float pattern1GapAngle = 45f;
    
    [Tooltip("패턴 1 탄막 발사 각도 간격 (도)")]
    public float pattern1AngleInterval = 5f;
    
    [Tooltip("패턴 1 빈 공간 각도 변화 속도")]
    public float pattern1GapRotationSpeed = 0.1f;
    
    [Tooltip("패턴 1 초기 빈 공간 각도 오프셋 (도)")]
    public float pattern1InitialGapOffset = 15f;
    
    [Header("Pattern 2 Settings")]
    [Tooltip("패턴 2 지속 시간 (초)")]
    public float pattern2Duration = 5f;
    
    [Tooltip("패턴 2 발사 간격 (초)")]
    public float pattern2FireInterval = 0.05f;
    
    [Tooltip("패턴 2 총 회전 각도 (도)")]
    public float pattern2TotalRotation = 1080f;
    
    [Tooltip("패턴 2 초기 각도 (도, 랜덤)")]
    public float pattern2InitialAngle = 0f;
    
    [Header("Pattern 3 Settings")]
    [Tooltip("패턴 3 지속 시간 (초)")]
    public float pattern3Duration = 6f;
    
    [Tooltip("패턴 3 발사 간격 (초)")]
    public float pattern3FireInterval = 0.1f;
    
    [Tooltip("패턴 3 각도 분산 범위 (도)")]
    public float pattern3SpreadAngle = 30f;
    
    [Tooltip("패턴 3 탄환 개수 (한 번에 발사)")]
    public int pattern3ProjectileCount = 10;
    
    [Tooltip("패턴 3 각 탄환 발사 간 최소 딜레이 (초)")]
    public float pattern3MinFireDelay = 0f;
    
    [Tooltip("패턴 3 각 탄환 발사 간 최대 딜레이 (초)")]
    public float pattern3MaxFireDelay = 0.05f;
    
    [Tooltip("패턴 3 원형 탄막 발사 간격 (초)")]
    public float pattern3CircleFireInterval = 1f;
    
    [Tooltip("패턴 3 원형 탄막 각도 간격 (도)")]
    public float pattern3CircleAngleInterval = 15f;
    
    [Header("Pattern 4 Settings")]
    [Tooltip("패턴 4 반복 횟수")]
    public int pattern4RepeatCount = 4;
    
    [Tooltip("패턴 4 Reticle 표시 시간 (초)")]
    public float pattern4ReticleDuration = 0.5f;
    
    [Header("Pattern 5 Settings")]
    [Tooltip("패턴 5 스폰 위치 개수")]
    public int pattern5SpawnCount = 20;
    
    [Tooltip("패턴 5 Reticle 표시 시간 (초)")]
    public float pattern5ReticleDuration = 0.5f;
    
    // 패턴 상태 변수
    private bool isPatternActive = false;
    private Coroutine currentPatternCoroutine;
    private Coroutine patternSequenceCoroutine; // 패턴 시퀀스 코루틴 참조
    private float currentGapAngle = 0f; // 패턴 1의 현재 빈 공간 각도
    
    [Header("Pattern Sequence Settings")]
    [Tooltip("패턴 1 후 대기 시간 (초)")]
    public float delayAfterPattern1 = 2f;
    
    [Tooltip("패턴 2 후 대기 시간 (초)")]
    public float delayAfterPattern2 = 1.5f;
    
    [Tooltip("패턴 3 후 대기 시간 (초)")]
    public float delayAfterPattern3 = 2f;
    
    protected override void Start()
    {
        base.Start();
        
        // 보스는 이동하지 않음
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic; // 물리 시뮬레이션 비활성화
        }
        
        // 초기 빈 공간 각도 설정 (플레이어 방향 기준 오프셋)
        if (target != null)
        {
            Vector2 toPlayer = ((Vector2)target.position - (Vector2)transform.position).normalized;
            float playerAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            currentGapAngle = playerAngle + pattern1InitialGapOffset;
        }
        
        // 패턴 시퀀스 시작
        StartPatternSequence();
    }
    
    /// <summary>
    /// 패턴 시퀀스를 시작합니다: 패턴 1 → 대기 → 패턴 2 → 대기 → 패턴 3 → 대기 → 반복
    /// </summary>
    public void StartPatternSequence()
    {
        if (patternSequenceCoroutine != null)
        {
            StopCoroutine(patternSequenceCoroutine);
        }
        
        patternSequenceCoroutine = StartCoroutine(PatternSequenceCoroutine());
    }
    
    /// <summary>
    /// 패턴 시퀀스 코루틴: 패턴들을 순차적으로 반복 실행
    /// </summary>
    private IEnumerator PatternSequenceCoroutine()
    {
        while (!isDead)
        {
            // 패턴 1 시작
            StartPattern1();
            
            // 패턴 1이 완료될 때까지 대기
            while (isPatternActive)
            {
                yield return null;
            }
            
            // 패턴 1 후 대기
            yield return new WaitForSeconds(delayAfterPattern1);
            
            // 패턴 2 시작
            StartPattern2();
            
            // 패턴 2가 완료될 때까지 대기
            while (isPatternActive)
            {
                yield return null;
            }
            
            // 패턴 2 후 대기
            yield return new WaitForSeconds(delayAfterPattern2);
            
            // 패턴 3 시작
            StartPattern3();
            
            // 패턴 3이 완료될 때까지 대기
            while (isPatternActive)
            {
                yield return null;
            }
            
            // 패턴 3 후 대기
            yield return new WaitForSeconds(delayAfterPattern3);
            
            // 반복 (while 루프가 계속됨)
        }
    }
    
    protected override void FixedUpdate()
    {
        if (enemyData == null || target == null || isDead) return;
        
        // 보스는 이동하지 않음
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        
        // 공격 범위 체크는 기본 클래스 로직을 사용하지 않음 (패턴 기반 공격)
        // TryAttack은 오버라이드하여 사용하지 않음
    }
    
    protected override void TryAttack(Vector2 direction)
    {
        // Enemy_BossTV는 TryAttack을 사용하지 않고 패턴 기반 공격을 사용
        // 기본 클래스의 TryAttack은 호출하지 않음
    }
    
    #region Pattern 1: 360도 탄막 (삼각함수 그래프처럼 변화하는 빈 공간)
    
    /// <summary>
    /// 패턴 1 시작: 360도 전방위 탄막을 약 7초간 연사
    /// 빈 공간이 삼각함수 그래프처럼 서서히 변화함
    /// </summary>
    public void StartPattern1()
    {
        if (isPatternActive) return;
        
        if (currentPatternCoroutine != null)
        {
            StopCoroutine(currentPatternCoroutine);
        }
        
        // 현재 플레이어 위치를 기준으로 초기 빈 공간 각도 설정
        if (target != null)
        {
            Vector2 toPlayer = ((Vector2)target.position - (Vector2)transform.position).normalized;
            float playerAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            currentGapAngle = playerAngle + pattern1InitialGapOffset;
            
            // 0 ~ 360도 범위로 정규화
            while (currentGapAngle < 0f) currentGapAngle += 360f;
            while (currentGapAngle >= 360f) currentGapAngle -= 360f;
        }
        
        currentPatternCoroutine = StartCoroutine(Pattern1Coroutine());
    }
    
    /// <summary>
    /// 패턴 1 코루틴: 360도 탄막 연사
    /// </summary>
    private IEnumerator Pattern1Coroutine()
    {
        isPatternActive = true;
        float elapsedTime = 0f;
        float lastFireTime = 0f;
        
        while (elapsedTime < pattern1Duration)
        {
            elapsedTime += Time.deltaTime;
            
            // 발사 간격 체크
            if (elapsedTime - lastFireTime >= pattern1FireInterval)
            {
                // 삼각함수 그래프처럼 변화하는 빈 공간 각도 계산
                float gapCenterAngle = CalculateGapAngle(elapsedTime);
                
                // 360도 전방위로 탄막 발사 (빈 공간 제외)
                FirePattern1Bullets(gapCenterAngle);
                
                lastFireTime = elapsedTime;
            }
            
            yield return null;
        }
        
        isPatternActive = false;
        currentPatternCoroutine = null;
    }
    
    /// <summary>
    /// 삼각함수 그래프처럼 변화하는 빈 공간 각도를 계산
    /// </summary>
    private float CalculateGapAngle(float elapsedTime)
    {
        // Sin 그래프를 사용하여 부드럽게 변화 (0 ~ 360도 범위)
        float sinValue = Mathf.Sin(elapsedTime * pattern1GapRotationSpeed);
        float gapAngle = currentGapAngle + (sinValue * 180f); // -180 ~ +180도 범위로 확장
        
        // 0 ~ 360도 범위로 정규화
        while (gapAngle < 0f) gapAngle += 360f;
        while (gapAngle >= 360f) gapAngle -= 360f;
        
        return gapAngle;
    }
    
    /// <summary>
    /// 패턴 1 탄막 발사 (빈 공간 제외)
    /// </summary>
    private void FirePattern1Bullets(float gapCenterAngle)
    {
        // 360도 범위를 pattern1AngleInterval 간격으로 순회
        for (float angle = 0f; angle < 360f; angle += pattern1AngleInterval)
        {
            // 빈 공간 각도 범위 체크
            float gapStart = gapCenterAngle - (pattern1GapAngle / 2f);
            float gapEnd = gapCenterAngle + (pattern1GapAngle / 2f);
            
            // 0 ~ 360도 범위 정규화
            while (gapStart < 0f) gapStart += 360f;
            while (gapEnd >= 360f) gapEnd -= 360f;
            
            // 빈 공간 범위에 포함되는지 체크 (경계 처리)
            bool isInGap = false;
            if (gapStart <= gapEnd)
            {
                // 일반 케이스: gapStart < gapEnd
                isInGap = (angle >= gapStart && angle <= gapEnd);
            }
            else
            {
                // 경계 케이스: gapStart > gapEnd (360도를 넘어감)
                isInGap = (angle >= gapStart || angle <= gapEnd);
            }
            
            // 빈 공간이 아니면 탄막 발사
            if (!isInGap)
            {
                FireBulletAtAngle(angle);
            }
        }
    }
    
    #endregion
    
    #region Pattern 2: 회전하는 일직선 연사
    
    /// <summary>
    /// 패턴 2 시작: 일정 속도로 회전하는 일직선 연사
    /// </summary>
    public void StartPattern2()
    {
        if (isPatternActive) return;
        
        if (currentPatternCoroutine != null)
        {
            StopCoroutine(currentPatternCoroutine);
        }
        
        // 초기 각도 랜덤 설정
        pattern2InitialAngle = Random.Range(0f, 360f);
        
        currentPatternCoroutine = StartCoroutine(Pattern2Coroutine());
    }
    
    /// <summary>
    /// 패턴 2 코루틴: 회전하는 일직선 연사
    /// </summary>
    private IEnumerator Pattern2Coroutine()
    {
        isPatternActive = true;
        float elapsedTime = 0f;
        float lastFireTime = 0f;
        
        while (elapsedTime < pattern2Duration)
        {
            elapsedTime += Time.deltaTime;
            
            // 현재 각도 계산 (일정한 속도로 회전)
            float rotationProgress = elapsedTime / pattern2Duration; // 0 ~ 1
            float currentAngle = pattern2InitialAngle + (pattern2TotalRotation * rotationProgress);
            
            // 발사 간격 체크
            if (elapsedTime - lastFireTime >= pattern2FireInterval)
            {
                FireBulletAtAngle(currentAngle);
                lastFireTime = elapsedTime;
            }
            
            yield return null;
        }
        
        isPatternActive = false;
        currentPatternCoroutine = null;
    }
    
    #endregion
    
    #region Pattern 3: 플레이어 방향 집중 사격 (Enemy_Siren 방식)
    
    /// <summary>
    /// 패턴 3 시작: 플레이어 방향으로 넓은 범위 탄막
    /// </summary>
    public void StartPattern3()
    {
        if (isPatternActive) return;
        
        if (currentPatternCoroutine != null)
        {
            StopCoroutine(currentPatternCoroutine);
        }
        
        currentPatternCoroutine = StartCoroutine(Pattern3Coroutine());
    }
    
    /// <summary>
    /// 패턴 3 코루틴: 플레이어 방향 집중 사격 + 1초마다 원형 탄막
    /// </summary>
    private IEnumerator Pattern3Coroutine()
    {
        isPatternActive = true;
        float elapsedTime = 0f;
        float lastFireTime = 0f;
        float lastCircleFireTime = 0f;
        
        while (elapsedTime < pattern3Duration)
        {
            elapsedTime += Time.deltaTime;
            
            // 플레이어 방향 집중 사격 발사 간격 체크
            if (elapsedTime - lastFireTime >= pattern3FireInterval)
            {
                if (target != null)
                {
                    StartCoroutine(FirePattern3Spread());
                }
                lastFireTime = elapsedTime;
            }
            
            // 원형 탄막 발사 간격 체크 (1초마다)
            if (elapsedTime - lastCircleFireTime >= pattern3CircleFireInterval)
            {
                FirePattern3Circle();
                lastCircleFireTime = elapsedTime;
            }
            
            yield return null;
        }
        
        isPatternActive = false;
        currentPatternCoroutine = null;
    }
    
    /// <summary>
    /// 패턴 3 탄막 발사 (Enemy_Siren 방식, 더 넓고 촘촘함)
    /// </summary>
    private IEnumerator FirePattern3Spread()
    {
        if (target == null) yield break;
        
        // 플레이어 방향 계산
        Vector2 toPlayer = ((Vector2)target.position - (Vector2)transform.position).normalized;
        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        
        // 각 탄환 발사
        for (int i = 0; i < pattern3ProjectileCount; i++)
        {
            // ±pattern3SpreadAngle 범위 내 랜덤 각도
            float randomSpread = Random.Range(-pattern3SpreadAngle / 2f, pattern3SpreadAngle / 2f);
            float finalAngle = baseAngle + randomSpread;
            
            FireBulletAtAngle(finalAngle);
            
            // 각 탄환 발사 간 랜덤 딜레이
            float delay = Random.Range(pattern3MinFireDelay, pattern3MaxFireDelay);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            else
            {
                yield return null;
            }
        }
    }
    
    /// <summary>
    /// 패턴 3 원형 탄막 발사 (360도 전방위)
    /// </summary>
    private void FirePattern3Circle()
    {
        // 360도 범위를 pattern3CircleAngleInterval 간격으로 순회하며 발사
        for (float angle = 0f; angle < 360f; angle += pattern3CircleAngleInterval)
        {
            FireBulletAtAngle(angle);
        }
    }
    
    #endregion
    
    #region Pattern 4: Reticle → Bomb
    
    /// <summary>
    /// 패턴 4 시작: 플레이어 위치에 Reticle → Bomb
    /// </summary>
    public void StartPattern4()
    {
        if (isPatternActive) return;
        
        if (currentPatternCoroutine != null)
        {
            StopCoroutine(currentPatternCoroutine);
        }
        
        currentPatternCoroutine = StartCoroutine(Pattern4Coroutine());
    }
    
    /// <summary>
    /// 패턴 4 코루틴: Reticle → Bomb 반복
    /// </summary>
    private IEnumerator Pattern4Coroutine()
    {
        isPatternActive = true;
        
        for (int i = 0; i < pattern4RepeatCount; i++)
        {
            if (target != null)
            {
                Vector2 targetPosition = target.position;
                
                // Reticle 생성
                GameObject reticle = Instantiate(reticlePrefab, targetPosition, Quaternion.identity);
                
                // 0.5초 대기
                yield return new WaitForSeconds(pattern4ReticleDuration);
                
                // Bomb 생성 (Reticle 위치에)
                if (reticle != null)
                {
                    Instantiate(bombPrefab, targetPosition, Quaternion.identity);
                    Destroy(reticle); // Reticle 제거
                }
            }
        }
        
        isPatternActive = false;
        currentPatternCoroutine = null;
    }
    
    #endregion
    
    #region Pattern 5: 여러 위치에 패턴 4 적용
    
    /// <summary>
    /// 패턴 5 시작: BossRoom 범위 내 랜덤 위치에 Reticle → Bomb
    /// </summary>
    public void StartPattern5()
    {
        if (isPatternActive) return;
        
        if (currentPatternCoroutine != null)
        {
            StopCoroutine(currentPatternCoroutine);
        }
        
        currentPatternCoroutine = StartCoroutine(Pattern5Coroutine());
    }
    
    /// <summary>
    /// 패턴 5 코루틴: 여러 위치에 Reticle → Bomb
    /// </summary>
    private IEnumerator Pattern5Coroutine()
    {
        isPatternActive = true;
        
        // BossRoom 범위 내 랜덤 위치 생성 (균일하게 분포)
        List<Vector2> spawnPositions = GeneratePattern5Positions();
        
        // 모든 위치에 동시에 Reticle 생성
        List<GameObject> reticles = new List<GameObject>();
        foreach (Vector2 pos in spawnPositions)
        {
            if (reticlePrefab != null)
            {
                GameObject reticle = Instantiate(reticlePrefab, pos, Quaternion.identity);
                reticles.Add(reticle);
            }
        }
        
        // 0.5초 대기
        yield return new WaitForSeconds(pattern5ReticleDuration);
        
        // 모든 위치에 Bomb 생성
        foreach (Vector2 pos in spawnPositions)
        {
            if (bombPrefab != null)
            {
                Instantiate(bombPrefab, pos, Quaternion.identity);
            }
        }
        
        // 모든 Reticle 제거
        foreach (GameObject reticle in reticles)
        {
            if (reticle != null)
            {
                Destroy(reticle);
            }
        }
        
        isPatternActive = false;
        currentPatternCoroutine = null;
    }
    
    /// <summary>
    /// 패턴 5용 스폰 위치 생성 (균일하게 분포)
    /// </summary>
    private List<Vector2> GeneratePattern5Positions()
    {
        List<Vector2> positions = new List<Vector2>();
        
        if (roomController == null)
        {
            Debug.LogWarning("Enemy_BossTV: roomController가 없어 패턴 5 위치를 생성할 수 없습니다.");
            return positions;
        }
        
        // BossRoom의 범위를 추정 (실제 구현 필요 시 RoomController에서 범위 정보 가져오기)
        // 임시로 현재 위치 기준으로 일정 범위 내에서 생성
        Vector2 center = transform.position;
        
        // 균일하게 분포시키기 위한 격자 기반 배치
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(pattern5SpawnCount));
        float gridSpacing = 3f; // 격자 간격 (조정 필요)
        float totalSize = (gridSize - 1) * gridSpacing;
        Vector2 startPos = center - new Vector2(totalSize / 2f, totalSize / 2f);
        
        // 격자에 배치하되 약간의 랜덤 오프셋 추가
        int count = 0;
        for (int x = 0; x < gridSize && count < pattern5SpawnCount; x++)
        {
            for (int y = 0; y < gridSize && count < pattern5SpawnCount; y++)
            {
                Vector2 gridPos = startPos + new Vector2(x * gridSpacing, y * gridSpacing);
                // 약간의 랜덤 오프셋 추가
                Vector2 randomOffset = Random.insideUnitCircle * (gridSpacing * 0.3f);
                positions.Add(gridPos + randomOffset);
                count++;
            }
        }
        
        return positions;
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// 특정 각도로 투사체를 발사하는 헬퍼 메서드
    /// </summary>
    private void FireBulletAtAngle(float angleDegrees)
    {
        if (projectilePrefab == null) return;
        
        // 각도를 라디안으로 변환
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
        
        // 투사체 생성
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        
        // 투사체 회전 설정
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, angleDegrees);
        
        // 투사체 속도 설정
        Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
        if (projectileRb != null && enemyData != null)
        {
            projectileRb.linearVelocity = direction * enemyData.projectileSpeed;
        }
        
        // 투사체 생존 시간 설정
        if (enemyData != null)
        {
            Destroy(projectile, enemyData.projectileLifetime);
        }
    }
    
    #endregion
}

