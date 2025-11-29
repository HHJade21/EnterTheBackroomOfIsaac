using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ArrayLabelAttribute))]
public class ArrayLabelPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ArrayLabelAttribute arrayLabelAttribute = (ArrayLabelAttribute)attribute;
        
        // 배열 요소인 경우 (부모가 배열일 때)
        if (property.propertyPath.Contains(".Array.data["))
        {
            // 배열 인덱스 추출
            int startIndex = property.propertyPath.LastIndexOf('[') + 1;
            int endIndex = property.propertyPath.LastIndexOf(']');
            if (startIndex > 0 && endIndex > startIndex)
            {
                string indexStr = property.propertyPath.Substring(startIndex, endIndex - startIndex);
                if (int.TryParse(indexStr, out int index))
                {
                    // 인덱스에 해당하는 라벨 사용
                    if (arrayLabelAttribute != null && index < arrayLabelAttribute.labels.Length)
                    {
                        label.text = arrayLabelAttribute.labels[index];
                    }
                }
            }
        }
        else if (property.isArray)
        {
            // 배열 자체인 경우 - 기본 동작
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }
        
        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}

