using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class BulletAssetGenerator : EditorWindow
{
    public enum RotateType { None, Clockwise_90, CounterClockwise_90, Rotate_180 }

    // --- 변수들 ---
    string bulletName = "NewBullet";
    string savePath = "Assets/GeneratedBullets";

    GameObject targetPrefab;
    AnimatorController baseController;

    // [설정] 날아가는 탄
    Texture2D flyTexture;
    int flyFrameCount = 1;
    RotateType flyRotation = RotateType.None;
    bool flyReverse = false;

    // [설정] 터지는 이펙트
    Texture2D explodeTexture;
    int explodeFrameCount = 3;
    float explodeSampleRate = 12f;
    RotateType explodeRotation = RotateType.None;
    bool explodeReverse = false;

    string originalFlyClipName = "Fly";
    string originalExplodeClipName = "Explode";

    Vector2 scrollPos;

    [MenuItem("Tools/Bullet Generator")]
    public static void ShowWindow()
    {
        GetWindow<BulletAssetGenerator>("Bullet Gen");
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("총알 에셋 생성기", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 0. 프리팹 & 경로
        GUI.color = new Color(0.8f, 1f, 0.8f);
        GUILayout.Label("0. 타겟 프리팹 (선택)", EditorStyles.boldLabel);
        targetPrefab = (GameObject)EditorGUILayout.ObjectField("타겟 프리팹", targetPrefab, typeof(GameObject), false);
        GUI.color = Color.white;

        EditorGUILayout.BeginHorizontal();
        savePath = EditorGUILayout.TextField("저장 위치", savePath);
        if (GUILayout.Button("폴더", GUILayout.Width(50)))
        {
            string path = EditorUtility.OpenFolderPanel("저장 폴더", "Assets", "");
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                savePath = "Assets" + path.Substring(Application.dataPath.Length);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 1. 기본 설정
        bulletName = EditorGUILayout.TextField("총알 이름", bulletName);
        baseController = (AnimatorController)EditorGUILayout.ObjectField("Base Controller", baseController, typeof(AnimatorController), false);
        originalFlyClipName = EditorGUILayout.TextField("Fly 클립명", originalFlyClipName);
        originalExplodeClipName = EditorGUILayout.TextField("Explode 클립명", originalExplodeClipName);

        EditorGUILayout.Space();
        GUILayout.Label("---------- 이미지 처리 설정 ----------", EditorStyles.boldLabel);

        // 2. Fly 설정
        GUILayout.Label("1. 날아가는(Fly) 텍스처", EditorStyles.label);
        flyTexture = (Texture2D)EditorGUILayout.ObjectField(flyTexture, typeof(Texture2D), false);

        EditorGUILayout.BeginHorizontal();
        flyFrameCount = EditorGUILayout.IntField("프레임 수", flyFrameCount);
        flyRotation = (RotateType)EditorGUILayout.EnumPopup(flyRotation, GUILayout.Width(100));
        flyReverse = EditorGUILayout.ToggleLeft("순서 반전", flyReverse, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 3. Explode 설정
        GUILayout.Label("2. 터지는(Explode) 텍스처", EditorStyles.label);
        explodeTexture = (Texture2D)EditorGUILayout.ObjectField(explodeTexture, typeof(Texture2D), false);

        EditorGUILayout.BeginHorizontal();
        explodeFrameCount = EditorGUILayout.IntField("프레임 수", explodeFrameCount);
        explodeRotation = (RotateType)EditorGUILayout.EnumPopup(explodeRotation, GUILayout.Width(100));
        explodeReverse = EditorGUILayout.ToggleLeft("순서 반전", explodeReverse, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        explodeSampleRate = EditorGUILayout.FloatField("폭발 FPS", explodeSampleRate);

        EditorGUILayout.Space();

        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("에셋 생성하기", GUILayout.Height(40)))
        {
            if (CheckInput()) CreateBulletAssets();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    bool CheckInput()
    {
        if (baseController == null || flyTexture == null || explodeTexture == null)
        {
            Debug.LogError("필수 항목이 비어있습니다.");
            return false;
        }
        return true;
    }

    void CreateBulletAssets()
    {
        string folderPath = $"{savePath}/{bulletName}";
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        // 1. 텍스처 처리
        Texture2D processedFlyTex = ProcessTexture(flyTexture, flyRotation, folderPath, "_fly_gen");
        Texture2D processedExpTex = ProcessTexture(explodeTexture, explodeRotation, folderPath, "_exp_gen");
        if (processedFlyTex == null || processedExpTex == null) return;

        // 2. 슬라이스
        bool flyIsVertical = (flyRotation == RotateType.Clockwise_90 || flyRotation == RotateType.CounterClockwise_90);
        bool expIsVertical = (explodeRotation == RotateType.Clockwise_90 || explodeRotation == RotateType.CounterClockwise_90);

        List<Sprite> flySprites = SliceTexture(processedFlyTex, flyFrameCount, flyIsVertical);
        List<Sprite> explodeSprites = SliceTexture(processedExpTex, explodeFrameCount, expIsVertical);

        if (flySprites == null || explodeSprites == null) return;

        // 3. 순서 반전
        if (flyReverse) flySprites.Reverse();
        if (explodeReverse) explodeSprites.Reverse();

        // 4. 애니메이션 생성
        AnimationClip flyClip = CreateClip(flySprites, 60, true);
        AssetDatabase.CreateAsset(flyClip, $"{folderPath}/{bulletName}_Fly.anim");

        AnimationClip explodeClip = CreateClip(explodeSprites, explodeSampleRate, false);
        AssetDatabase.CreateAsset(explodeClip, $"{folderPath}/{bulletName}_Explode.anim");

        // 5. 오버라이드 컨트롤러
        AnimatorOverrideController aoc = new AnimatorOverrideController(baseController);
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        aoc.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            if (overrides[i].Key.name == originalFlyClipName)
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, flyClip);
            else if (overrides[i].Key.name == originalExplodeClipName)
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, explodeClip);
        }
        aoc.ApplyOverrides(overrides);
        AssetDatabase.CreateAsset(aoc, $"{folderPath}/{bulletName}_Override.overrideController");

        // 6. 프리팹 적용 (경고창 부활!)
        if (targetPrefab != null) ApplyToPrefab(aoc);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = aoc;

        Debug.Log($"✅ {bulletName} 생성 완료!");
    }

    void ApplyToPrefab(AnimatorOverrideController newController)
    {
        string assetPath = AssetDatabase.GetAssetPath(targetPrefab);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
        Animator anim = prefabRoot.GetComponent<Animator>();
        if (anim == null) anim = prefabRoot.AddComponent<Animator>();

        // [복구됨] 이미 컨트롤러가 있다면 경고창 띄우기
        if (anim.runtimeAnimatorController != null)
        {
            bool confirm = EditorUtility.DisplayDialog(
                "덮어쓰기 경고",
                $"'{targetPrefab.name}'에 이미 컨트롤러가 있습니다.\n" +
                $"현재: {anim.runtimeAnimatorController.name}\n" +
                $"교체: {newController.name}\n\n" +
                "교체하시겠습니까?",
                "네, 교체합니다", // OK 버튼
                "아니요"        // Cancel 버튼
            );

            if (!confirm)
            {
                Debug.Log("🚫 사용자가 교체를 취소했습니다.");
                PrefabUtility.UnloadPrefabContents(prefabRoot); // 저장 안 하고 닫기
                return;
            }
        }

        anim.runtimeAnimatorController = newController;
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        Debug.Log("✅ 프리팹에 컨트롤러 적용 완료!");
    }

    // --- 유틸리티 함수들 ---
    List<Sprite> SliceTexture(Texture2D texture, int sliceCount, bool isVertical)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        importer.isReadable = true;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;

        SpriteMetaData[] metaData = new SpriteMetaData[sliceCount];
        float sliceWidth = isVertical ? texture.width : (float)texture.width / sliceCount;
        float sliceHeight = isVertical ? (float)texture.height / sliceCount : texture.height;

        for (int i = 0; i < sliceCount; i++)
        {
            float x = isVertical ? 0 : i * sliceWidth;
            float y = isVertical ? texture.height - (sliceHeight * (i + 1)) : 0;

            metaData[i] = new SpriteMetaData
            {
                name = $"{texture.name}_{i}",
                rect = new Rect(x, y, sliceWidth, sliceHeight),
                alignment = (int)SpriteAlignment.Center
            };
        }

        importer.spritesheet = metaData;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        List<Sprite> sprites = new List<Sprite>();
        foreach (Object asset in assets)
            if (asset is Sprite) sprites.Add(asset as Sprite);

        return sprites.OrderBy(s => s.name).ToList();
    }

    Texture2D ProcessTexture(Texture2D original, RotateType rotation, string savePath, string suffix)
    {
        if (rotation == RotateType.None) return original;
        string originalPath = AssetDatabase.GetAssetPath(original);
        TextureImporter importer = AssetImporter.GetAtPath(originalPath) as TextureImporter;
        bool wasReadable = importer.isReadable;
        if (!wasReadable) { importer.isReadable = true; importer.SaveAndReimport(); }

        Color32[] originalPixels = original.GetPixels32();
        int w = original.width; int h = original.height;
        if (!wasReadable) { importer.isReadable = false; importer.SaveAndReimport(); }

        int newW = w, newH = h;
        if (rotation == RotateType.Clockwise_90 || rotation == RotateType.CounterClockwise_90) { newW = h; newH = w; }

        Color32[] newPixels = new Color32[originalPixels.Length];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int newIndex = 0; int originalIndex = y * w + x;
                switch (rotation)
                {
                    case RotateType.Clockwise_90: newIndex = (w - 1 - x) * newW + y; break;
                    case RotateType.CounterClockwise_90: newIndex = x * newW + (h - 1 - y); break;
                    case RotateType.Rotate_180: newIndex = (h - 1 - y) * newW + (w - 1 - x); break;
                }
                newPixels[newIndex] = originalPixels[originalIndex];
            }
        }
        Texture2D newTex = new Texture2D(newW, newH);
        newTex.SetPixels32(newPixels); newTex.Apply();
        byte[] bytes = newTex.EncodeToPNG();
        string fileName = $"{bulletName}{suffix}.png";
        string fullPath = Path.Combine(savePath, fileName);
        File.WriteAllBytes(fullPath, bytes);
        AssetDatabase.Refresh();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
    }

    AnimationClip CreateClip(List<Sprite> sprites, float fps, bool loop)
    {
        AnimationClip clip = new AnimationClip(); clip.frameRate = fps;
        EditorCurveBinding spriteBinding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i * (1f / fps), value = sprites[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop; AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }
}