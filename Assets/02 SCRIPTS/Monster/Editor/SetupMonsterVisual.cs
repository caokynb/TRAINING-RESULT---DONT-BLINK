using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SetupMonsterVisual
{
    private const string MonsterPrefabPath = "Assets/04 PREFABS/Monster/Monster.prefab";
    private const string MonsterVisualPrefabPath = "Assets/04 PREFABS/Monster/MonsterVisual.prefab";
    private const string PosesDir = "Assets/04 PREFABS/Monster";
    private const float DefaultMonsterScale = 0.18f;

    static SetupMonsterVisual()
    {
        EditorApplication.delayCall += CheckAndAutoSetup;
    }

    private static void CheckAndAutoSetup()
    {
        if (Application.isPlaying)
            return;

        GameObject monsterAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
        if (monsterAsset == null)
            return;

        Transform visual = monsterAsset.transform.Find("MonsterVisual");
        PoseSwitcher switcher = monsterAsset.GetComponent<PoseSwitcher>();

        if (visual == null || switcher == null || switcher.Poses == null || switcher.Poses.Length == 0)
        {
            Debug.Log("[SetupMonsterVisual] Auto-detected unconfigured Monster.prefab. Running T10 setup...");
            ExecuteSetup();
        }
    }

    [MenuItem("Tools/Backrooms/Setup MonsterVisual (T10)")]
    public static void ExecuteSetup()
    {
        Debug.Log("[SetupMonsterVisual] Starting Setup for T10...");

        // 1. Load Monster.prefab contents
        GameObject monsterRoot = PrefabUtility.LoadPrefabContents(MonsterPrefabPath);
        if (monsterRoot == null)
        {
            Debug.LogError($"[SetupMonsterVisual] Could not load {MonsterPrefabPath}");
            return;
        }

        try
        {
            // 2. Adjust root transform position to ground level (0, 0, 0)
            monsterRoot.transform.localPosition = Vector3.zero;

            // 3. Remove placeholder capsule MeshFilter & MeshRenderer if present
            MeshFilter mf = monsterRoot.GetComponent<MeshFilter>();
            if (mf != null)
            {
                Object.DestroyImmediate(mf);
                Debug.Log("[SetupMonsterVisual] Removed placeholder MeshFilter from Monster.prefab");
            }

            MeshRenderer mr = monsterRoot.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Object.DestroyImmediate(mr);
                Debug.Log("[SetupMonsterVisual] Removed placeholder MeshRenderer from Monster.prefab");
            }

            // 4. Adjust CapsuleCollider to match 2m humanoid character standing on the ground
            CapsuleCollider col = monsterRoot.GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.center = new Vector3(0f, 1f, 0f);
                col.height = 2f;
                col.radius = 0.4f;
            }

            // 5. Find or create MonsterVisual child
            Transform visualTransform = monsterRoot.transform.Find("MonsterVisual");
            GameObject visualObj;
            if (visualTransform == null)
            {
                visualObj = new GameObject("MonsterVisual");
                visualObj.transform.SetParent(monsterRoot.transform, false);
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localRotation = Quaternion.identity;
                visualObj.transform.localScale = Vector3.one;
            }
            else
            {
                visualObj = visualTransform.gameObject;
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localRotation = Quaternion.identity;
                visualObj.transform.localScale = Vector3.one;
            }

            // 6. Clear any existing children of MonsterVisual to rebuild cleanly
            int childCount = visualObj.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(visualObj.transform.GetChild(i).gameObject);
            }

            // 7. Load and instantiate all Pose_01 through Pose_10 prefabs with correct scale
            List<GameObject> instantiatedPoses = new List<GameObject>();
            for (int i = 1; i <= 10; i++)
            {
                string posePrefabPath = $"{PosesDir}/Pose_{i:D2}.prefab";
                GameObject posePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(posePrefabPath);
                if (posePrefab == null)
                {
                    Debug.LogWarning($"[SetupMonsterVisual] Missing {posePrefabPath}");
                    continue;
                }

                GameObject poseInstance = (GameObject)PrefabUtility.InstantiatePrefab(posePrefab, visualObj.transform);
                poseInstance.name = $"Pose_{i:D2}";
                poseInstance.transform.localPosition = Vector3.zero;
                poseInstance.transform.localRotation = Quaternion.identity;
                poseInstance.transform.localScale = new Vector3(DefaultMonsterScale, DefaultMonsterScale, DefaultMonsterScale);

                // Pose_01 is active by default, others deactivated
                poseInstance.SetActive(i == 1);
                instantiatedPoses.Add(poseInstance);
            }

            // 8. Ensure PoseSwitcher is on monsterRoot
            PoseSwitcher switcher = monsterRoot.GetComponent<PoseSwitcher>();
            if (switcher == null)
            {
                switcher = monsterRoot.AddComponent<PoseSwitcher>();
            }

            switcher.Poses = instantiatedPoses.ToArray();
            switcher.VisualRoot = visualObj.transform;
            switcher.PoseScale = DefaultMonsterScale;
            switcher.ModelFaceDirection = new Vector3(0f, 0f, -1f);

            // 9. Save MonsterVisual as standalone prefab asset
            PrefabUtility.SaveAsPrefabAsset(visualObj, MonsterVisualPrefabPath);
            Debug.Log($"[SetupMonsterVisual] Saved standalone prefab: {MonsterVisualPrefabPath}");

            // 10. Save updated Monster.prefab
            PrefabUtility.SaveAsPrefabAsset(monsterRoot, MonsterPrefabPath);
            Debug.Log($"[SetupMonsterVisual] Successfully updated and saved {MonsterPrefabPath} with {instantiatedPoses.Count} poses at scale {DefaultMonsterScale}!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SetupMonsterVisual] Error during setup: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(monsterRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Automatically place or update the Monster in the open scene for instant Edit Mode preview
        PlaceMonsterInScene();

        Debug.Log("[SetupMonsterVisual] T10 Setup Complete! Monster is placed in Scene and visible in Edit Mode.");
    }

    [MenuItem("Tools/Backrooms/Place Monster in Scene (Edit Mode)")]
    public static void PlaceMonsterInScene()
    {
        if (Application.isPlaying)
            return;

        GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
        if (monsterPrefab == null)
        {
            Debug.LogError($"[SetupMonsterVisual] Cannot find {MonsterPrefabPath}");
            return;
        }

        // 1. Locate spawn position
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;
        bool hasMarker = false;

        GameObject marker = GameObject.Find("MonsterSpawn");
        if (marker != null)
        {
            spawnPos = marker.transform.position;
            spawnRot = marker.transform.rotation;
            hasMarker = true;
        }
        else
        {
            MapGrid mapGrid = Object.FindFirstObjectByType<MapGrid>();
            if (mapGrid != null)
            {
                if (!mapGrid.IsLoaded) mapGrid.LoadMap();
                if (mapGrid.IsLoaded && mapGrid.TryFindCell('M', out Vector2Int mCell))
                {
                    spawnPos = mapGrid.CellToWorld(mCell);
                    hasMarker = true;
                }
            }
        }

        if (!hasMarker)
        {
            MonsterSpawner spawner = Object.FindFirstObjectByType<MonsterSpawner>();
            if (spawner != null)
            {
                spawnPos = spawner.transform.position;
            }
        }

        // Snap to floor surface via raycast
        if (Physics.Raycast(new Vector3(spawnPos.x, spawnPos.y + 2f, spawnPos.z), Vector3.down, out RaycastHit hit, 5f))
        {
            spawnPos.y = hit.point.y;
        }

        // Orient monster to face towards Player if Player exists
        GameObject player = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
        if (player != null)
        {
            Vector3 toPlayer = player.transform.position - spawnPos;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.001f)
            {
                // Monster face is -Z, so face -Z towards player
                Quaternion look = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                spawnRot = look * Quaternion.Euler(0f, 180f, 0f);
            }
        }

        // 2. Find or instantiate Monster in scene
        GameObject existingMonster = GameObject.Find("Monster");
        GameObject monsterSceneObj;

        if (existingMonster != null && PrefabUtility.GetCorrespondingObjectFromSource(existingMonster) == monsterPrefab)
        {
            monsterSceneObj = existingMonster;
            Undo.RecordObject(monsterSceneObj.transform, "Position Monster");
            monsterSceneObj.transform.position = spawnPos;
            monsterSceneObj.transform.rotation = spawnRot;
        }
        else
        {
            if (existingMonster != null && existingMonster.GetComponent<MonsterBrain>() != null)
            {
                Object.DestroyImmediate(existingMonster);
            }

            monsterSceneObj = (GameObject)PrefabUtility.InstantiatePrefab(monsterPrefab);
            monsterSceneObj.name = "Monster";
            monsterSceneObj.transform.position = spawnPos;
            monsterSceneObj.transform.rotation = spawnRot;
            Undo.RegisterCreatedObjectUndo(monsterSceneObj, "Place Monster in Scene");
        }

        monsterSceneObj.tag = "Monster";

        // Select the monster in editor hierarchy so the user sees it immediately
        Selection.activeGameObject = monsterSceneObj;
        EditorSceneManager.MarkSceneDirty(monsterSceneObj.scene);

        Debug.Log($"[SetupMonsterVisual] Placed Monster in scene at {spawnPos} (facing Player with face -Z).");
    }
}
