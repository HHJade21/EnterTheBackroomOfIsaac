using UnityEngine;

/// <summary>
/// Inspector에서 변수명 대신 별명을 표시하는 속성
/// 사용법: [Label("별명")] public GameObject variableName;
/// </summary>
public class LabelAttribute : PropertyAttribute
{
    public string label;

    public LabelAttribute(string label)
    {
        this.label = label;
    }
}

