using UnityEngine;

/// <summary>
/// 맵에 생성된 아이템 프리팹에 붙는 컴포넌트
/// Weapon과 유사하게 바닥에 떨어진 아이템의 정보를 저장합니다.
/// </summary>
public class newItem : MonoBehaviour
{
    [Tooltip("바닥에 떨어진 이 아이템의 아이템 고유 번호")]
    public int itemID;
    
    [Tooltip("아이템 데이터 참조 (선택사항, itemID로도 조회 가능)")]
    public ItemData itemData;
}

