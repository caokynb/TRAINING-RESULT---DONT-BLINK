#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SetupAudioManager
{
    private const string PrefabDirectory = "Assets/04 PREFABS/Audio";
    private const string PrefabPath = "Assets/04 PREFABS/Audio/AudioManager.prefab";
    private const string SfxDirectory = "Assets/05 SFX";

    [MenuItem("Tools/Backrooms/Setup AudioManager (T11 & T12)", priority = 150)]
    public static void Setup()
    {
        if (!AssetDatabase.IsValidFolder(PrefabDirectory))
        {
            AssetDatabase.CreateFolder("Assets/04 PREFABS", "Audio");
        }

        GameObject tempGO = new GameObject("AudioManager");
        AudioManager manager = tempGO.AddComponent<AudioManager>();

        SerializedObject so = new SerializedObject(manager);

        AssignClip(so, "humAmbientClip", $"{SfxDirectory}/SFX_01.mp3");
        AssignClip(so, "lightFlickerClip", $"{SfxDirectory}/SFX_02.mp3");
        AssignClip(so, "monsterFootstepsClip", $"{SfxDirectory}/SFX_03.mp3");
        AssignClip(so, "blinkAmbientClip", $"{SfxDirectory}/SFX_04.mp3");

        SerializedProperty playerStepsProp = so.FindProperty("playerFootstepClips");
        if (playerStepsProp != null)
        {
            playerStepsProp.arraySize = 3;
            playerStepsProp.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxDirectory}/SFX_05_1.mp3");
            playerStepsProp.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxDirectory}/SFX_05_2.mp3");
            playerStepsProp.GetArrayElementAtIndex(2).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxDirectory}/SFX_05_3.mp3");
        }

        AssignClip(so, "panicHeartbeatClip", $"{SfxDirectory}/SFX_06.mp3");
        AssignClip(so, "jumpscareClip", $"{SfxDirectory}/SFX_07.mp3");
        AssignClip(so, "exitDoorClip", $"{SfxDirectory}/SFX_08.mp3");
        AssignClip(so, "uiClickClip", $"{SfxDirectory}/SFX_09.mp3");
        AssignClip(so, "victoryClip", $"{SfxDirectory}/SFX_10.mp3");

        SerializedProperty spookyProp = so.FindProperty("spookyClips");
        if (spookyProp != null)
        {
            spookyProp.arraySize = 2;
            spookyProp.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxDirectory}/SFX_11.mp3");
            spookyProp.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxDirectory}/SFX_12.mp3");
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempGO, PrefabPath);
        Object.DestroyImmediate(tempGO);

        // Also configure Monster.prefab with 3D AudioSource and footstepsClip
        string monsterPrefabPath = "Assets/04 PREFABS/Monster/Monster.prefab";
        GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(monsterPrefabPath);
        if (monsterPrefab != null)
        {
            AudioSource src = monsterPrefab.GetComponent<AudioSource>();
            if (src == null)
                src = monsterPrefab.AddComponent<AudioSource>();

            src.playOnAwake = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 2f;
            src.maxDistance = 30f;
            src.dopplerLevel = 0f;

            MonsterBrain brain = monsterPrefab.GetComponent<MonsterBrain>();
            if (brain != null)
            {
                SerializedObject brainSO = new SerializedObject(brain);
                SerializedProperty audioSrcProp = brainSO.FindProperty("monsterAudioSource");
                if (audioSrcProp != null) audioSrcProp.objectReferenceValue = src;

                SerializedProperty clipProp = brainSO.FindProperty("footstepsClip");
                if (clipProp != null) clipProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxDirectory}/SFX_03.mp3");

                brainSO.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(monsterPrefab);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SetupAudioManager] AudioManager and Monster Audio successfully configured! Prefab at: {PrefabPath}");
        Selection.activeObject = savedPrefab;
    }

    private static void AssignClip(SerializedObject so, string propertyName, string assetPath)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            prop.objectReferenceValue = clip;
        }
    }
}
#endif
