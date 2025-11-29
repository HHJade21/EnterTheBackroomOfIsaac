using UnityEngine;

/// <summary>
/// 배열 요소마다 별명을 지정하는 속성
/// 사용법: [ArrayLabel("요소1", "요소2", "요소3")] public GameObject[] array;
/// </summary>
public class ArrayLabelAttribute : PropertyAttribute
{
    public string[] labels;

    public ArrayLabelAttribute(params string[] labels)
    {
        this.labels = labels;
    }
}

