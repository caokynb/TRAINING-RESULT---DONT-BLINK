using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private string spawnTag = "MonsterSpawn";

    private GameObject spawnedMonster;

    public GameObject SpawnedMonster => spawnedMonster;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureMonsterBootstrap()
    {
        MonsterSpawner spawner = Object.FindFirstObjectByType<MonsterSpawner>();
        if (spawner != null)
            return;

        if (FindExistingSpawnMarker("MonsterSpawn") != null ||
            Object.FindFirstObjectByType<MapGrid>() != null ||
            FindExistingMonsterInScene() != null)
        {
            GameObject spawnerObj = new GameObject("MonsterSpawner");
            spawnerObj.AddComponent<MonsterSpawner>();
        }
    }

    private void Start()
    {
        SpawnMonster();
    }

    public GameObject SpawnMonster()
    {
        if (spawnedMonster != null)
            return spawnedMonster;

        bool hasSpawnTarget = TryGetSpawnTransform(out Vector3 spawnPos, out Quaternion spawnRot);
        MapGrid mapGrid = Object.FindFirstObjectByType<MapGrid>();
        float floorY = mapGrid != null ? mapGrid.transform.position.y : 0f;
        Vector3 finalSpawnPos = new Vector3(spawnPos.x, floorY + 1f, spawnPos.z);

        GameObject existingMonster = FindExistingMonsterInScene();
        if (existingMonster != null)
        {
            spawnedMonster = existingMonster;
            if (hasSpawnTarget)
            {
                TeleportMonster(spawnedMonster, finalSpawnPos, spawnRot);
            }

            if (spawnedMonster.GetComponent<MonsterBrain>() == null)
            {
                spawnedMonster.AddComponent<MonsterBrain>();
            }

            Debug.Log($"[MonsterSpawner] Positioned Monster at {spawnedMonster.transform.position}");
            return spawnedMonster;
        }

        if (monsterPrefab != null)
        {
            spawnedMonster = Instantiate(monsterPrefab, finalSpawnPos, spawnRot);
            spawnedMonster.name = "Monster";
        }
        else
        {
            spawnedMonster = CreateFallbackCapsule(finalSpawnPos, spawnRot);
        }

        TeleportMonster(spawnedMonster, finalSpawnPos, spawnRot);

        if (spawnedMonster.GetComponent<MonsterBrain>() == null)
        {
            spawnedMonster.AddComponent<MonsterBrain>();
        }

        Debug.Log($"[MonsterSpawner] Monster spawned at {finalSpawnPos}");
        return spawnedMonster;
    }

    private bool TryGetSpawnTransform(out Vector3 position, out Quaternion rotation)
    {
        if (spawnPoint != null)
        {
            position = spawnPoint.position;
            rotation = spawnPoint.rotation;
            return true;
        }

        GameObject marker = FindExistingSpawnMarker(spawnTag);
        if (marker != null)
        {
            position = marker.transform.position;
            rotation = marker.transform.rotation;
            return true;
        }

        if (transform.position.sqrMagnitude > 0.001f)
        {
            position = transform.position;
            rotation = transform.rotation;
            return true;
        }

        MapGrid mapGrid = Object.FindFirstObjectByType<MapGrid>();
        if (mapGrid != null)
        {
            if (!mapGrid.IsLoaded)
                mapGrid.LoadMap();

            if (mapGrid.IsLoaded && mapGrid.TryFindCell('M', out Vector2Int mCell))
            {
                position = mapGrid.CellToWorld(mCell);
                rotation = Quaternion.identity;
                return true;
            }
        }

        position = transform.position;
        rotation = transform.rotation;
        return false;
    }

    private static GameObject FindExistingSpawnMarker(string tagOrName)
    {
        if (!string.IsNullOrEmpty(tagOrName))
        {
            try
            {
                GameObject tagged = GameObject.FindWithTag(tagOrName);
                if (tagged != null)
                    return tagged;
            }
            catch
            {
            }

            GameObject exactNamed = GameObject.Find(tagOrName);
            if (exactNamed != null)
                return exactNamed;
        }

        Transform[] allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform t = allTransforms[i];
            if (t == null)
                continue;

            if (t.name == "MonsterSpawn" || t.name.StartsWith("MonsterSpawn "))
                return t.gameObject;

            try
            {
                if (t.CompareTag("MonsterSpawn"))
                    return t.gameObject;
            }
            catch
            {
            }
        }

        return null;
    }

    private static GameObject FindExistingMonsterInScene()
    {
        MonsterBrain brain = Object.FindFirstObjectByType<MonsterBrain>();
        if (brain != null)
            return brain.gameObject;

        try
        {
            GameObject tagged = GameObject.FindWithTag("Monster");
            if (tagged != null)
                return tagged.transform.root.gameObject;
        }
        catch
        {
        }

        GameObject named = GameObject.Find("Monster");
        if (named != null)
            return named;

        return null;
    }

    private static void TeleportMonster(GameObject monster, Vector3 position, Quaternion rotation)
    {
        CharacterController cc = monster.GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
        {
            cc.enabled = false;
            monster.transform.SetPositionAndRotation(position, rotation);
            cc.enabled = true;
        }
        else
        {
            monster.transform.SetPositionAndRotation(position, rotation);
        }

        Rigidbody rb = monster.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.position = position;
            rb.rotation = rotation;
            rb.linearVelocity = Vector3.zero;
        }
    }

    private static GameObject CreateFallbackCapsule(Vector3 position, Quaternion rotation)
    {
        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "Monster";
        capsule.transform.SetPositionAndRotation(position, rotation);
        try
        {
            capsule.tag = "Monster";
        }
        catch
        {
        }

        return capsule;
    }
}
