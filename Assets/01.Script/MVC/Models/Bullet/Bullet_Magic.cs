using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 마법 투사체: 생성 시 랜덤한 변형 중 하나를 선택하여 외형, 애니메이션, 데미지를 적용합니다.
/// </summary>
public class Bullet_Magic : BulletController
{
    [System.Serializable]
    public class VariantData
    {
        [Tooltip("변형의 스프라이트")]
        public Sprite sprite;
        
        [Tooltip("변형의 애니메이션 컨트롤러 (선택사항)")]
        public RuntimeAnimatorController animatorController;
        
        [Tooltip("변형의 데미지")]
        public float damage;
        
        [Tooltip("변형의 스케일 (1.0 = 기본 크기, 2.0 = 2배 크기). 0이면 전역 스케일 사용")]
        [Range(0f, 5f)]
        public float scale = 0f; // 0이면 전역 스케일 사용
    }
    
    [Header("Random Variant Settings")]
    [Tooltip("변형 데이터 리스트 (최대 3개)")]
    [SerializeField] private List<VariantData> variants = new List<VariantData>();
    
    [Tooltip("변형 인덱스 최대값 (0부터 시작, variants.Count - 1까지)")]
    [SerializeField] private int tmpIdxMax = 3;
    
    // [Header("Scale Settings")]
    // [Tooltip("스프라이트 전역 스케일 (1.0 = 기본 크기, 2.0 = 2배 크기). 변형별 스케일이 0이면 이 값을 사용")]
    // [Range(0.1f, 5f)]
    // [SerializeField] private float globalScale = 0.70f;
    
    private int tmpIdx = 0;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    
    /// <summary>
    /// Awake에서 랜덤 인덱스를 선택하고 데이터를 적용합니다.
    /// </summary>
    private void Awake()
    {
        // 컴포넌트 가져오기
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        
        // 변형 데이터가 없으면 경고하고 종료 (하지만 부모 클래스 초기화는 계속 진행)
        if (variants == null || variants.Count == 0)
        {
            Debug.LogError($"Bullet_Magic: variants 리스트가 비어있습니다! 프리팹 '{gameObject.name}'의 Inspector에서 variants를 설정해주세요. 투사체가 제대로 표시되지 않을 수 있습니다.");
            // variants가 비어있어도 부모 클래스의 초기화는 계속 진행되도록 return하지 않음
            // 대신 기본 동작을 하도록 함
            return;
        }
        
        // tmpIdxMax를 variants.Count - 1로 제한
        int maxIndex = Mathf.Min(tmpIdxMax, variants.Count - 1);
        
        // 랜덤 인덱스 선택 (0부터 maxIndex까지)
        int randomValue = Random.Range(0, 10);
        if(randomValue < 4)
        {
            tmpIdx = 0;
        }
        else if(randomValue < 7)
        {
            tmpIdx = 1;
        }
        else if(randomValue < 9)
        {
            tmpIdx = 2;
        }
        else
        {
            tmpIdx = 3;
        }
        
        // tmpIdx가 variants 범위를 벗어나면 0으로 설정
        if (tmpIdx >= variants.Count)
        {
            Debug.LogWarning($"Bullet_Magic: tmpIdx({tmpIdx})가 variants.Count({variants.Count})를 초과합니다. 0으로 설정합니다.");
            tmpIdx = 0;
        }
        
        // 선택된 변형 데이터 적용
        ApplyVariant(tmpIdx);
    }
    
    /// <summary>
    /// 선택된 인덱스의 변형 데이터를 자신에게 적용합니다.
    /// </summary>
    /// <param name="index">적용할 변형 인덱스</param>
    private void ApplyVariant(int index)
    {
        // 인덱스 범위 체크
        if (index < 0 || index >= variants.Count)
        {
            Debug.LogWarning($"Bullet_Magic: 인덱스 {index}가 범위를 벗어났습니다. (0 ~ {variants.Count - 1})");
            return;
        }
        
        VariantData selectedVariant = variants[index];
        
        if (selectedVariant == null)
        {
            Debug.LogWarning($"Bullet_Magic: 인덱스 {index}의 변형 데이터가 null입니다.");
            return;
        }
        
        // 스프라이트 적용
        if (spriteRenderer != null)
        {
            if (selectedVariant.sprite != null)
            {
                spriteRenderer.sprite = selectedVariant.sprite;
            }
            else
            {
                Debug.LogWarning($"Bullet_Magic: 인덱스 {index}의 variant sprite가 null입니다. 스프라이트가 표시되지 않을 수 있습니다.");
            }
        }
        else
        {
            Debug.LogWarning("Bullet_Magic: SpriteRenderer 컴포넌트를 찾을 수 없습니다!");
        }
        
        // 스케일 적용 (변형별 스케일이 0보다 크면 그것을 사용, 아니면 전역 스케일 사용)
        // float scaleToApply = selectedVariant.scale > 0f ? selectedVariant.scale : globalScale;
        // transform.localScale = Vector3.one * scaleToApply;
        
        // 애니메이션 컨트롤러 적용
        if (animator != null && selectedVariant.animatorController != null)
        {
            animator.runtimeAnimatorController = selectedVariant.animatorController;
        }
        
        // 데미지 적용 (bulletData가 있으면 덮어씌움)
        if (bulletData != null)
        {
            bulletData.damage = selectedVariant.damage;
        }
    }
    
    /// <summary>
    /// 현재 선택된 변형의 데미지를 반환합니다.
    /// </summary>
    public new float damage
    {
        get
        {
            if (variants != null && tmpIdx >= 0 && tmpIdx < variants.Count && variants[tmpIdx] != null)
            {
                return variants[tmpIdx].damage;
            }
            // 기본값: bulletData가 있으면 그 값을 사용, 없으면 1.0f
            return bulletData != null ? bulletData.damage : 1.0f;
        }
    }
}
