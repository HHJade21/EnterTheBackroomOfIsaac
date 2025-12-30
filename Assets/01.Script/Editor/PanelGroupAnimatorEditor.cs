using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PanelGroupAnimator))]
public class PanelGroupAnimatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 UI를 그립니다.
        DrawDefaultInspector();

        PanelGroupAnimator animator = (PanelGroupAnimator)target;

        if (animator.animationTargets == null || animator.animationTargets.Length == 0)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animation Helpers", EditorStyles.boldLabel);

        for (int i = 0; i < animator.animationTargets.Length; i++)
        {
            PanelAnimationTarget currentTarget = animator.animationTargets[i];
            if (currentTarget == null || currentTarget.panel == null)
            {
                continue;
            }

            EditorGUILayout.LabelField($"Target: {currentTarget.panel.name}", EditorStyles.miniBoldLabel);
            
            // 한 줄에 버튼들을 배치하기 위해 Horizontal 사용
            EditorGUILayout.BeginHorizontal();

            // 현재 위치를 시작 위치로 설정
            if (GUILayout.Button("Set Start"))
            {
                Undo.RecordObject(animator, "Set Start Position");
                currentTarget.startAnchorPosition = currentTarget.panel.anchoredPosition;
                EditorUtility.SetDirty(animator);
            }

            // 현재 위치를 끝 위치로 설정
            if (GUILayout.Button("Set End"))
            {
                Undo.RecordObject(animator, "Set End Position");
                currentTarget.endAnchorPosition = currentTarget.panel.anchoredPosition;
                EditorUtility.SetDirty(animator);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            // 시작 위치로 이동
            if (GUILayout.Button("Go To Start"))
            {
                Undo.RecordObject(currentTarget.panel, "Move to Start Position");
                currentTarget.panel.anchoredPosition = currentTarget.startAnchorPosition;
                EditorUtility.SetDirty(currentTarget.panel);
            }

            // 끝 위치로 이동
            if (GUILayout.Button("Go To End"))
            {
                Undo.RecordObject(currentTarget.panel, "Move to End Position");
                currentTarget.panel.anchoredPosition = currentTarget.endAnchorPosition;
                EditorUtility.SetDirty(currentTarget.panel);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
    }
}
