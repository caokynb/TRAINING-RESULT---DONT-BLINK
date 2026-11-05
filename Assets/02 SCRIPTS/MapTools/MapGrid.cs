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

    private struct NeighborStep
    {
        public Vector2Int offset;
        public float cost;
        public bool isDiagonal;

        public NeighborStep(int dx, int dy, float cost, bool isDiagonal)
        {
            this.offset = new Vector2Int(dx, dy);
            this.cost = cost;
            this.isDiagonal = isDiagonal;
        }
    }

    private static readonly NeighborStep[] allSteps = new NeighborStep[]
    {
        // 4 Cardinal directions (cost 1.0)
        new NeighborStep(0, 1, 1.0f, false),    // Up
        new NeighborStep(0, -1, 1.0f, false),   // Down
        new NeighborStep(-1, 0, 1.0f, false),   // Left
        new NeighborStep(1, 0, 1.0f, false),    // Right

        // 4 Diagonal directions (cost ~1.414)
        new NeighborStep(1, 1, 1.414f, true),   // Up-Right
        new NeighborStep(-1, 1, 1.414f, true),  // Up-Left
        new NeighborStep(1, -1, 1.414f, true),  // Down-Right
        new NeighborStep(-1, -1, 1.414f, true)  // Down-Left
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

    public bool HasLineOfSight(Vector2Int from, Vector2Int to)
    {
        if (grid == null)
            return false;

        int x0 = from.x;
        int y0 = from.y;
        int x1 = to.x;
        int y1 = to.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            Vector2Int current = new Vector2Int(x0, y0);
            if (!IsWalkable(current))
                return false;

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = 2 * err;
            int prevX = x0;
            int prevY = y0;

            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }

            // Diagonal corner check: don't see through wall corners
            if (x0 != prevX && y0 != prevY)
            {
                if (!IsWalkable(new Vector2Int(x0, prevY)) || !IsWalkable(new Vector2Int(prevX, y0)))
                    return false;
            }
        }

        return true;
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

        start = FindNearestWalkableCell(start);
        if (!IsWalkable(start))
            return null;

        if (start == target)
        {
            return new List<Vector2Int> { start };
        }

        // 8-Directional A* Search with Octile Heuristic
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
        List<KeyValuePair<Vector2Int, float>> openList = new List<KeyValuePair<Vector2Int, float>>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        gScore[start] = 0f;
        float startH = GetOctileDistance(start, target) * 1.001f;
        openList.Add(new KeyValuePair<Vector2Int, float>(start, startH));

        int maxIterations = width * height * 4;
        while (openList.Count > 0 && maxIterations-- > 0)
        {
            // Pick node with lowest fScore
            int bestIndex = 0;
            float bestF = openList[0].Value;
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].Value < bestF)
                {
                    bestF = openList[i].Value;
                    bestIndex = i;
                }
            }

            Vector2Int current = openList[bestIndex].Key;
            openList.RemoveAt(bestIndex);

            if (current == target)
            {
                List<Vector2Int> path = new List<Vector2Int>();
                Vector2Int curr = target;
                while (curr != start)
                {
                    path.Add(curr);
                    curr = cameFrom[curr];
                }
                path.Add(start);
                path.Reverse();
                return path;
            }

            closedSet.Add(current);
            float currentG = gScore[current];

            for (int s = 0; s < allSteps.Length; s++)
            {
                NeighborStep step = allSteps[s];
                Vector2Int neighbor = current + step.offset;

                if (!IsWalkable(neighbor) || closedSet.Contains(neighbor))
                    continue;

                // Prevent diagonal corner cutting
                if (step.isDiagonal)
                {
                    if (!IsWalkable(new Vector2Int(current.x + step.offset.x, current.y)) ||
                        !IsWalkable(new Vector2Int(current.x, current.y + step.offset.y)))
                    {
                        continue;
                    }
                }

                float tentativeG = currentG + step.cost;
                if (!gScore.TryGetValue(neighbor, out float existingG) || tentativeG < existingG)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;

                    float h = GetOctileDistance(neighbor, target) * 1.001f;
                    float f = tentativeG + h;

                    int existingOpenIdx = -1;
                    for (int i = 0; i < openList.Count; i++)
                    {
                        if (openList[i].Key == neighbor)
                        {
                            existingOpenIdx = i;
                            break;
                        }
                    }

                    if (existingOpenIdx >= 0)
                    {
                        openList[existingOpenIdx] = new KeyValuePair<Vector2Int, float>(neighbor, f);
                    }
                    else
                    {
                        openList.Add(new KeyValuePair<Vector2Int, float>(neighbor, f));
                    }
                }
            }
        }

        // If target was unreachable, return path to closest reached cell
        if (cameFrom.Count > 0)
        {
            Vector2Int closest = start;
            float closestDist = float.MaxValue;
            foreach (Vector2Int cell in cameFrom.Keys)
            {
                float d = GetOctileDistance(cell, target);
                if (d < closestDist)
                {
                    closestDist = d;
                    closest = cell;
                }
            }

            if (closest != start)
            {
                List<Vector2Int> partialPath = new List<Vector2Int>();
                Vector2Int curr = closest;
                while (curr != start)
                {
                    partialPath.Add(curr);
                    curr = cameFrom[curr];
                }
                partialPath.Add(start);
                partialPath.Reverse();
                return partialPath;
            }
        }

        return null;
    }

    private static float GetOctileDistance(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return (dx + dy) + (1.41421356f - 2f) * Mathf.Min(dx, dy);
    }
}
