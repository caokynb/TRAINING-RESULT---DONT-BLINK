using UnityEngine;

[CreateAssetMenu(fileName = "NewMapTheme", menuName = "Don't Blink/Map Theme")]
public class MapTheme : ScriptableObject
{
    [Header("Map Prefabs")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject wallDirtyPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject ceilingPrefab;
    [SerializeField] private GameObject lightPrefab;
    [SerializeField] private GameObject lightOffPrefab;
    [SerializeField] private GameObject doorPrefab;

    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 3f;

    public GameObject WallPrefab => wallPrefab;
    public GameObject WallDirtyPrefab => wallDirtyPrefab;
    public GameObject FloorPrefab => floorPrefab;
    public GameObject CeilingPrefab => ceilingPrefab;
    public GameObject LightPrefab => lightPrefab;
    public GameObject LightOffPrefab => lightOffPrefab;
    public GameObject DoorPrefab => doorPrefab;

    public float CellSize => cellSize;
}
