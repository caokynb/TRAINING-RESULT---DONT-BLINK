using UnityEditor;
using UnityEngine;

public static class MapBuilder
{
    private const int FixedSeed = 1337;
    private const string DefaultThemePath = "Assets/07 SETTINGS/BackroomsTheme.asset";
    private const string Level01MapPath = "Assets/03 ASSETS/Maps/level01.txt";
    private const string FallbackMapPath = "Assets/03 ASSETS/Maps/sample_map_30x30.txt";
    private const string WallCleanPath = "Assets/04 PREFABS/Level/Wall_Clean.prefab";
    private const string WallDirtyPath = "Assets/04 PREFABS/Level/Wall_Dirty.prefab";
    private const string FloorCarpetPath = "Assets/04 PREFABS/Level/Floor_Carpet.prefab";
    private const string CeilingTilePath = "Assets/04 PREFABS/Level/Ceiling_Tile.prefab";
    private const string CeilingLightPath = "Assets/04 PREFABS/Level/Ceiling_Light.prefab";
    private const string CeilingLightOffPath = "Assets/04 PREFABS/Level/Ceiling_Light_Turn_Off.prefab";
    private const string DoorExitPath = "Assets/04 PREFABS/Level/DoorFrame_Exit.prefab";
    private const string MonsterPrefabPath = "Assets/04 PREFABS/Monster/Monster.prefab";

    [MenuItem("Tools/Backrooms/Build Map")]
    public static void BuildMap()
    {
        EnsureTagsExist("PlayerSpawn", "MonsterSpawn", "Exit", "Monster");

        MapGrid mapGrid = Object.FindFirstObjectByType<MapGrid>();
        if (mapGrid == null)
        {
            EditorUtility.DisplayDialog("Build Map", "MapGrid not found in current scene!", "OK");
            return;
        }

        if (mapGrid.LevelFile == null)
        {
            TextAsset defaultMap = AssetDatabase.LoadAssetAtPath<TextAsset>(Level01MapPath);
            if (defaultMap == null)
                defaultMap = AssetDatabase.LoadAssetAtPath<TextAsset>(FallbackMapPath);

            if (defaultMap != null)
            {
                mapGrid.LevelFile = defaultMap;
                EditorUtility.SetDirty(mapGrid);
            }
        }

        mapGrid.LoadMap();
        if (!mapGrid.IsLoaded)
        {
            EditorUtility.DisplayDialog("Build Map", "Failed to load map file from MapGrid!", "OK");
            return;
        }

        MapTheme theme = mapGrid.Theme;
        if (theme == null)
        {
            theme = AssetDatabase.LoadAssetAtPath<MapTheme>(DefaultThemePath);
            if (theme != null)
            {
                mapGrid.Theme = theme;
                EditorUtility.SetDirty(mapGrid);
            }
        }

        GameObject wallCleanPrefab = GetRootPrefab(theme != null ? theme.WallPrefab : null, WallCleanPath);
        GameObject wallDirtyPrefab = GetRootPrefab(theme != null ? theme.WallDirtyPrefab : null, WallDirtyPath);
        GameObject floorPrefab = GetRootPrefab(theme != null ? theme.FloorPrefab : null, FloorCarpetPath);
        GameObject ceilingTilePrefab = GetRootPrefab(theme != null ? theme.CeilingPrefab : null, CeilingTilePath);
        GameObject ceilingLightPrefab = GetRootPrefab(theme != null ? theme.LightPrefab : null, CeilingLightPath);
        GameObject ceilingLightOffPrefab = GetRootPrefab(theme != null ? theme.LightOffPrefab : null, CeilingLightOffPath);
        GameObject doorExitPrefab = GetRootPrefab(theme != null ? theme.DoorPrefab : null, DoorExitPath);

        if (theme != null)
        {
            SerializedObject so = new SerializedObject(theme);
            SetObjectProperty(so, "wallPrefab", wallCleanPrefab);
            SetObjectProperty(so, "wallDirtyPrefab", wallDirtyPrefab);
            SetObjectProperty(so, "floorPrefab", floorPrefab);
            SetObjectProperty(so, "ceilingPrefab", ceilingTilePrefab);
            SetObjectProperty(so, "lightPrefab", ceilingLightPrefab);
            SetObjectProperty(so, "lightOffPrefab", ceilingLightOffPrefab);
            SetObjectProperty(so, "doorPrefab", doorExitPrefab);
            SerializedProperty cellSizeProp = so.FindProperty("cellSize");
            if (cellSizeProp != null)
                cellSizeProp.floatValue = 3f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
        }

        float cellSize = 3f;
        float wallHeight = 3f;

        GameObject existingRoot;
        while ((existingRoot = GameObject.Find("Generated")) != null)
        {
            Undo.DestroyObjectImmediate(existingRoot);
        }

        GameObject generatedRoot = new GameObject("Generated");
        Undo.RegisterCreatedObjectUndo(generatedRoot, "Build Map");

        System.Random rng = new System.Random(FixedSeed);
        Vector3? monsterSpawnPos = null;

        for (int y = 0; y < mapGrid.Height; y++)
        {
            for (int x = 0; x < mapGrid.Width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (!mapGrid.IsWalkable(cell))
                    continue;

                char cellChar = mapGrid.GetCell(cell);
                Vector3 center = mapGrid.transform.TransformPoint(new Vector3(x * cellSize, 0f, y * cellSize));

                SpawnFloor(generatedRoot.transform, floorPrefab, center, cellSize, x, y);
                SpawnCeiling(generatedRoot.transform, ceilingTilePrefab, center, cellSize, wallHeight, $"Ceiling_{x}_{y}");

                // Chỉ sinh đèn sáng khi ô là 'L', không sinh ngẫu nhiên đèn tắt nữa
                GameObject lightToUse = SelectLightPrefab(cellChar, ceilingLightPrefab);
                if (lightToUse != null)
                {
                    SpawnCeilingLight(generatedRoot.transform, lightToUse, center, wallHeight, $"CeilingLight_{x}_{y}");
                }

                if (cellChar == 'M')
                {
                    monsterSpawnPos = center;
                }

                SpawnMarkerIfAny(generatedRoot.transform, cellChar, center);

                bool doorPlaced = false;

                CheckAndSpawnWall(mapGrid, generatedRoot.transform, wallCleanPrefab, wallDirtyPrefab, doorExitPrefab,
                    cell, cellChar, ref doorPlaced, rng, Vector2Int.up, center,
                    Quaternion.Euler(0f, 90f, 0f), $"Wall_{x}_{y}_North", cellSize, wallHeight);

                CheckAndSpawnWall(mapGrid, generatedRoot.transform, wallCleanPrefab, wallDirtyPrefab, doorExitPrefab,
                    cell, cellChar, ref doorPlaced, rng, Vector2Int.down, center,
                    Quaternion.Euler(0f, -90f, 0f), $"Wall_{x}_{y}_South", cellSize, wallHeight);

                CheckAndSpawnWall(mapGrid, generatedRoot.transform, wallCleanPrefab, wallDirtyPrefab, doorExitPrefab,
                    cell, cellChar, ref doorPlaced, rng, Vector2Int.right, center,
                    Quaternion.Euler(0f, 180f, 0f), $"Wall_{x}_{y}_East", cellSize, wallHeight);

                CheckAndSpawnWall(mapGrid, generatedRoot.transform, wallCleanPrefab, wallDirtyPrefab, doorExitPrefab,
                    cell, cellChar, ref doorPlaced, rng, Vector2Int.left, center,
                    Quaternion.identity, $"Wall_{x}_{y}_West", cellSize, wallHeight);
            }
        }

        EnsureMonsterSpawner(monsterSpawnPos);

        EditorUtility.SetDirty(generatedRoot);
        Debug.Log($"Build Map completed: {mapGrid.Width}x{mapGrid.Height} under root 'Generated'.");
    }

    private static void EnsureMonsterSpawner(Vector3? monsterSpawnPos)
    {
        MonsterSpawner spawner = Object.FindFirstObjectByType<MonsterSpawner>();
        GameObject spawnerObj;
        if (spawner == null)
        {
            spawnerObj = new GameObject("MonsterSpawner");
            Undo.RegisterCreatedObjectUndo(spawnerObj, "Create MonsterSpawner");
            spawner = spawnerObj.AddComponent<MonsterSpawner>();
        }
        else
        {
            spawnerObj = spawner.gameObject;
        }

        // Tự động gắn tag MonsterSpawn cho MonsterSpawner
        spawnerObj.tag = "MonsterSpawn";

        if (monsterSpawnPos.HasValue)
        {
            spawnerObj.transform.position = monsterSpawnPos.Value;
        }

        GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
        GameObject spawnMarker = GameObject.Find("MonsterSpawn");
        SerializedObject so = new SerializedObject(spawner);
        if (monsterPrefab != null)
            SetObjectProperty(so, "monsterPrefab", monsterPrefab);
        if (spawnMarker != null)
            SetObjectProperty(so, "spawnPoint", spawnMarker.transform);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(spawner);
    }

    private static void EnsureTagsExist(params string[] requiredTags)
    {
        Object[] tagAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagAssets == null || tagAssets.Length == 0)
            return;

        SerializedObject tagManager = new SerializedObject(tagAssets[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        if (tagsProp == null)
            return;

        bool modified = false;
        foreach (string tag in requiredTags)
        {
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                int index = tagsProp.arraySize;
                tagsProp.InsertArrayElementAtIndex(index);
                tagsProp.GetArrayElementAtIndex(index).stringValue = tag;
                modified = true;
            }
        }

        if (modified)
        {
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SpawnMarkerIfAny(Transform parent, char cellChar, Vector3 center)
    {
        string markerName = null;
        string markerTag = null;

        if (cellChar == 'P')
        {
            markerName = "PlayerSpawn";
            markerTag = "PlayerSpawn";
        }
        else if (cellChar == 'M')
        {
            markerName = "MonsterSpawn";
            markerTag = "MonsterSpawn";
        }
        else if (cellChar == 'E')
        {
            markerName = "Exit";
            markerTag = "Exit";
        }

        if (markerName == null)
            return;

        GameObject marker = new GameObject(markerName);
        marker.transform.SetParent(parent);
        marker.transform.position = center;
        marker.transform.rotation = Quaternion.identity;
        marker.tag = markerTag;

        if (cellChar == 'E')
        {
            BoxCollider trigger = marker.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2f, 3f, 2f);
            trigger.center = new Vector3(0f, 1.5f, 0f);
            marker.AddComponent<ExitDoorTrigger>();
        }
    }

    private static void SetObjectProperty(SerializedObject so, string propName, Object value)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null)
            prop.objectReferenceValue = value;
    }

    private static GameObject GetRootPrefab(GameObject assignedPrefab, string fallbackPath)
    {
        if (assignedPrefab != null)
        {
            string assetPath = AssetDatabase.GetAssetPath(assignedPrefab);
            if (!string.IsNullOrEmpty(assetPath))
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (root != null)
                    return root;
            }
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(fallbackPath);
    }

    private static GameObject SelectLightPrefab(char cellChar, GameObject ceilingLightPrefab)
    {
        // Chỉ sinh đèn sáng khi ô bản đồ có ký tự 'L'
        if (cellChar == 'L' && ceilingLightPrefab != null)
            return ceilingLightPrefab;

        return null;
    }

    private static GameObject SelectWallPrefab(char cellChar, ref bool doorPlaced, System.Random rng,
        GameObject wallCleanPrefab, GameObject wallDirtyPrefab, GameObject doorExitPrefab)
    {
        if (cellChar == 'E' && !doorPlaced && doorExitPrefab != null)
        {
            doorPlaced = true;
            return doorExitPrefab;
        }

        if (wallDirtyPrefab != null && rng.NextDouble() < 0.25)
            return wallDirtyPrefab;

        return wallCleanPrefab;
    }

    private static void SpawnFloor(Transform parent, GameObject floorPrefab, Vector3 center, float cellSize, int x, int y)
    {
        if (floorPrefab != null)
        {
            GameObject floor = (GameObject)PrefabUtility.InstantiatePrefab(floorPrefab, parent);
            floor.name = $"Floor_{x}_{y}";
            floor.transform.position = center;
            floor.transform.rotation = Quaternion.identity;
        }
        else
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"Floor_{x}_{y}";
            cube.transform.SetParent(parent);
            cube.transform.position = center + new Vector3(0f, -0.05f, 0f);
            cube.transform.localScale = new Vector3(cellSize, 0.1f, cellSize);
        }
    }

    private static void SpawnCeiling(Transform parent, GameObject ceilingPrefab, Vector3 center, float cellSize, float wallHeight, string ceilingName)
    {
        if (ceilingPrefab != null)
        {
            GameObject ceiling = (GameObject)PrefabUtility.InstantiatePrefab(ceilingPrefab, parent);
            ceiling.name = ceilingName;
            float childLocalY = ceiling.transform.childCount > 0 ? ceiling.transform.GetChild(0).localPosition.y : 0f;
            float offsetY = childLocalY > 0.01f ? (wallHeight - childLocalY) : wallHeight;
            ceiling.transform.position = center + new Vector3(0f, offsetY, 0f);
            ceiling.transform.rotation = Quaternion.identity;
        }
        else
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = ceilingName;
            cube.transform.SetParent(parent);
            cube.transform.position = center + new Vector3(0f, wallHeight + 0.05f, 0f);
            cube.transform.localScale = new Vector3(cellSize, 0.1f, cellSize);
        }
    }

    private static void SpawnCeilingLight(Transform parent, GameObject lightPrefab, Vector3 center, float wallHeight, string lightName)
    {
        GameObject lightObj = (GameObject)PrefabUtility.InstantiatePrefab(lightPrefab, parent);
        lightObj.name = lightName;
        float childLocalY = lightObj.transform.childCount > 0 ? lightObj.transform.GetChild(0).localPosition.y : 0f;
        float offsetY = childLocalY > 0.01f ? (wallHeight - childLocalY - 0.02f) : (wallHeight - 0.02f);
        lightObj.transform.position = center + new Vector3(0f, offsetY, 0f);
        lightObj.transform.rotation = Quaternion.identity;
    }

    private static void CheckAndSpawnWall(MapGrid mapGrid, Transform parent,
        GameObject wallCleanPrefab, GameObject wallDirtyPrefab, GameObject doorExitPrefab,
        Vector2Int cell, char cellChar, ref bool doorPlaced, System.Random rng,
        Vector2Int direction, Vector3 center, Quaternion prefabRotation, string wallName, float cellSize, float wallHeight)
    {
        Vector2Int neighbor = cell + direction;
        if (mapGrid.IsWalkable(neighbor))
            return;

        Vector3 wallPos = center + new Vector3(direction.x * cellSize * 0.5f, 0f, direction.y * cellSize * 0.5f);
        GameObject prefabToUse = SelectWallPrefab(cellChar, ref doorPlaced, rng,
            wallCleanPrefab, wallDirtyPrefab, doorExitPrefab);

        if (prefabToUse != null)
        {
            GameObject wall = (GameObject)PrefabUtility.InstantiatePrefab(prefabToUse, parent);
            wall.name = prefabToUse == doorExitPrefab ? $"DoorExit_{cell.x}_{cell.y}" : wallName;
            wall.transform.position = wallPos;
            wall.transform.rotation = prefabRotation;

            // Gắn tag Exit cho cánh cửa Exit
            if (prefabToUse == doorExitPrefab)
            {
                wall.tag = "Exit";
            }
        }
        else
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = wallName;
            cube.transform.SetParent(parent);
            cube.transform.position = wallPos + new Vector3(0f, wallHeight * 0.5f, 0f);
            Vector3 scale = (direction.x != 0) ? new Vector3(0.2f, wallHeight, cellSize) : new Vector3(cellSize, wallHeight, 0.2f);
            cube.transform.localScale = scale;
        }
    }
}
