using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(LabelAttribute))]
public class LabelPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 배열 요소인 경우 Label을 적용하지 않음 (ArrayLabel이 처리)
        if (property.propertyPath.Contains(".Array.data["))
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }
        
        LabelAttribute labelAttribute = (LabelAttribute)attribute;
        EditorGUI.PropertyField(position, property, new GUIContent(labelAttribute.label), true);
    }
}

