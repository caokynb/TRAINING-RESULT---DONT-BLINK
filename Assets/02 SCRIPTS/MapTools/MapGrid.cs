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

    public Vector3 CellToWorld(Vector2Int cell)
    {
        Vector3 localPosition = new Vector3(
            cell.x * CellSize,
            0f,
            cell.y * CellSize
        );

        return transform.TransformPoint(localPosition);
    }

    public int GetBFSDistance(Vector2Int start, Vector2Int target)
    {
        if (!IsWalkable(start) || !IsWalkable(target))
            return -1;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>();

        queue.Enqueue(start);
        distances[start] = 0;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current == target)
                return distances[current];

            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;

                if (!IsWalkable(next) || distances.ContainsKey(next))
                    continue;

                distances[next] = distances[current] + 1;
                queue.Enqueue(next);
            }
        }

        return -1;
    }

    public List<Vector2Int> GetBFSPath(Vector2Int start, Vector2Int target)
    {
        if (!IsWalkable(start) || !IsWalkable(target))
            return null;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> previous = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(start);
        previous[start] = start;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current == target)
                break;

            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;

                if (!IsWalkable(next) || previous.ContainsKey(next))
                    continue;

                previous[next] = current;
                queue.Enqueue(next);
            }
        }

        if (!previous.ContainsKey(target))
            return null;

        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int step = target;

        while (step != start)
        {
            path.Add(step);
            step = previous[step];
        }

        path.Add(start);
        path.Reverse();

        return path;
    }
}
