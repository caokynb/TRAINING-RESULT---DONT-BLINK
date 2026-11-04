using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private string spawnTag = "MonsterSpawn";

    private GameObject spawnedMonster;

    public GameObject SpawnedMonster => spawnedMonster;

    private void Start()
    {
        SpawnMonster();
    }

    public GameObject SpawnMonster()
    {
        if (spawnedMonster != null)
            return spawnedMonster;

        GameObject spawnMarker = FindSpawnMarker();
        Vector3 spawnPos = spawnMarker != null ? spawnMarker.transform.position : transform.position;
        Quaternion spawnRot = spawnMarker != null ? spawnMarker.transform.rotation : transform.rotation;

        if (monsterPrefab != null)
        {
            spawnedMonster = Instantiate(monsterPrefab, spawnPos, spawnRot);
            spawnedMonster.name = "Monster";
        }
        else
        {
            spawnedMonster = CreateFallbackCapsule(spawnPos, spawnRot);
        }

        if (spawnedMonster.GetComponent<MonsterBrain>() == null)
        {
            spawnedMonster.AddComponent<MonsterBrain>();
        }

        Debug.Log($"[MonsterSpawner] Monster spawned at {spawnPos}");
        return spawnedMonster;
    }

    private GameObject FindSpawnMarker()
    {
        try
        {
            GameObject tagged = GameObject.FindWithTag(spawnTag);
            if (tagged != null)
                return tagged;
        }
        catch
        {
        }

        return GameObject.Find(spawnTag);
    }

    private static GameObject CreateFallbackCapsule(Vector3 position, Quaternion rotation)
    {
        GameObject root = new GameObject("Monster");
        root.transform.position = position;
        root.transform.rotation = rotation;
        try
        {
            root.tag = "Monster";
        }
        catch
        {
        }

        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "Visual";
        capsule.transform.SetParent(root.transform, false);
        capsule.transform.localPosition = new Vector3(0f, 1f, 0f);
        return root;
    }
}
