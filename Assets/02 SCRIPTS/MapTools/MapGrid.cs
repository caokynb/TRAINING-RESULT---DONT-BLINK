using System;
using System.Collections.Generic;
using UnityEngine;

public class MapGrid : MonoBehaviour
{
    [SerializeField] private TextAsset levelFile;
    [SerializeField] private MapTheme mapTheme;

    private char[,] grid;
    private int width;
    private int height;

    public int Width => width;
    public int Height => height;

    public TextAsset LevelFile
    {
        get => levelFile;
        set => levelFile = value;
    }

    public MapTheme Theme
    {
        get => mapTheme;
        set => mapTheme = value;
    }

    public bool IsLoaded => grid != null;
    public float CellSize => mapTheme != null ? mapTheme.CellSize : 3f;

    private readonly Vector2Int[] directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private void Awake()
    {
        LoadMap();
    }

    public void LoadMap()
    {
        if (levelFile == null)
        {
            Debug.LogError("MapGrid: Level file is missing!");
            grid = null;
            return;
        }

        string[] lines = levelFile.text.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries
        );

        if (lines.Length == 0)
        {
            Debug.LogError("MapGrid: Level file is empty!");
            grid = null;
            return;
        }

        height = lines.Length;
        width = lines[0].Length;

        if (width == 0)
        {
            Debug.LogError("MapGrid: Map width cannot be zero!");
            grid = null;
            return;
        }

        grid = new char[height, width];

        for (int z = 0; z < height; z++)
        {
            if (lines[z].Length != width)
            {
                Debug.LogError($"MapGrid: Invalid row length at row {z}!");
                grid = null;
                return;
            }

            for (int x = 0; x < width; x++)
            {
                char cell = lines[z][x];
                if (cell != '#' && cell != '.' &&
                    cell != 'L' && cell != 'P' &&
                    cell != 'M' && cell != 'E')
                {
                    Debug.LogError($"MapGrid: Invalid character '{cell}' at ({x}, {z})!");
                    grid = null;
                    return;
                }

                grid[z, x] = cell;
            }
        }

        Debug.Log($"Map loaded successfully: {width} x {height}");
    }

    public bool IsWalkable(Vector2Int cell)
    {
        if (grid == null)
            return false;

        if (cell.x < 0 || cell.x >= width ||
            cell.y < 0 || cell.y >= height)
            return false;

        return grid[cell.y, cell.x] != '#';
    }

    public char GetCell(Vector2Int cell)
    {
        if (grid == null ||
            cell.x < 0 || cell.x >= width ||
            cell.y < 0 || cell.y >= height)
            return '\0';

        return grid[cell.y, cell.x];
    }

    public bool TryFindCell(char targetChar, out Vector2Int foundCell)
    {
        foundCell = Vector2Int.zero;
        if (grid == null)
            return false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[y, x] == targetChar)
                {
                    foundCell = new Vector2Int(x, y);
                    return true;
                }
            }
        }

        return false;
    }

    public Vector2Int FindNearestWalkableCell(Vector2Int cell)
    {
        if (IsWalkable(cell))
            return cell;

        if (grid == null)
            return cell;

        int maxRadius = Mathf.Max(width, height);
        float bestDistSq = float.MaxValue;
        Vector2Int bestCell = cell;
        bool found = false;

        for (int r = 1; r <= maxRadius; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r)
                        continue;

                    Vector2Int candidate = new Vector2Int(cell.x + dx, cell.y + dy);
                    if (!IsWalkable(candidate))
                        continue;

                    float distSq = dx * dx + dy * dy;
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestCell = candidate;
                        found = true;
                    }
                }
            }

            if (found)
                return bestCell;
        }

        return cell;
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        Vector3 localPosition = new Vector3(
            cell.x * CellSize,
            0f,
            cell.y * CellSize
        );

        return transform.TransformPoint(localPosition);
    }

    public Vector2Int WorldToCell(Vector3 worldPosition)
    {
        float size = CellSize > 0.001f ? CellSize : 3f;
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        int x = Mathf.RoundToInt(localPosition.x / size);
        int y = Mathf.RoundToInt(localPosition.z / size);
        return new Vector2Int(x, y);
    }

    public int GetBFSDistance(Vector2Int start, Vector2Int target)
    {
        List<Vector2Int> path = GetBFSPath(start, target);
        if (path == null || path.Count == 0)
            return -1;

        return path.Count - 1;
    }

    public List<Vector2Int> GetBFSPath(Vector2Int start, Vector2Int target)
    {
        if (grid == null)
            return null;

        target = FindNearestWalkableCell(target);
        if (!IsWalkable(target))
            return null;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> nextTowardsTarget = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(target);
        nextTowardsTarget[target] = target;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (Vector2Int direction in directions)
            {
                Vector2Int neighbor = current + direction;

                if (!IsWalkable(neighbor) || nextTowardsTarget.ContainsKey(neighbor))
                    continue;

                nextTowardsTarget[neighbor] = current;
                queue.Enqueue(neighbor);
            }
        }

        Vector2Int actualStart = start;
        if (!nextTowardsTarget.ContainsKey(actualStart))
        {
            float bestDistSq = float.MaxValue;
            bool foundReachable = false;

            foreach (Vector2Int reachableCell in nextTowardsTarget.Keys)
            {
                float dx = reachableCell.x - start.x;
                float dy = reachableCell.y - start.y;
                float distSq = dx * dx + dy * dy;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    actualStart = reachableCell;
                    foundReachable = true;
                }
            }

            if (!foundReachable)
                return null;
        }

        List<Vector2Int> path = new List<Vector2Int>();
        if (actualStart != start)
        {
            path.Add(start);
        }

        Vector2Int step = actualStart;
        int safetyLimit = width * height + 4;
        while (step != target && safetyLimit-- > 0)
        {
            path.Add(step);
            step = nextTowardsTarget[step];
        }

        path.Add(target);
        return path;
    }
}
