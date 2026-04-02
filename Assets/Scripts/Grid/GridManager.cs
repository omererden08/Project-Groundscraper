using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Settings")]
    [SerializeField] private Vector2 gridWorldSize = new Vector2(20, 20);
    [SerializeField] private float nodeRadius = 0.5f;
    [SerializeField] private LayerMask unwalkableMask;
    [SerializeField] private float obstacleBuffer = 0.15f;

    private Node[,] grid;
    private float nodeDiameter;
    private int gridSizeX;
    private int gridSizeY;

    private static readonly Vector2Int[] directions =
    {
        new Vector2Int(-1, 0), new Vector2Int(1, 0),
        new Vector2Int(0, -1), new Vector2Int(0, 1),
        new Vector2Int(-1, -1), new Vector2Int(1, -1),
        new Vector2Int(-1, 1), new Vector2Int(1, 1)
    };

    public bool IsBuilt => grid != null;
    public int MaxSize => gridSizeX * gridSizeY;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RecalculateGridSize();
    }

    private void OnEnable()
    {
        GameEvents.OnLevelLoaded += HandleLevelLoaded;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelLoaded -= HandleLevelLoaded;
    }

    private void HandleLevelLoaded(string levelId)
    {
        RebuildGrid();
    }

    public void RebuildGrid()
    {
        RecalculateGridSize();
        CreateGrid();
        Debug.Log("[GridManager] Grid rebuilt.");
    }

    private void RecalculateGridSize()
    {
        nodeDiameter = nodeRadius * 2f;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
    }

    private void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];

        Vector2 bottomLeft =
            (Vector2)transform.position
            - Vector2.right * gridWorldSize.x / 2f
            - Vector2.up * gridWorldSize.y / 2f;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector2 worldPoint =
                    bottomLeft
                    + Vector2.right * (x * nodeDiameter + nodeRadius)
                    + Vector2.up * (y * nodeDiameter + nodeRadius);

                bool walkable = !Physics2D.OverlapCircle(
                    worldPoint,
                    nodeRadius + obstacleBuffer,
                    unwalkableMask
                );

                grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }

    public Node NodeFromWorldPoint(Vector2 worldPos)
    {
        if (grid == null)
        {
            Debug.LogWarning("GridManager: Grid henüz build edilmedi.");
            return null;
        }

        float percentX = Mathf.Clamp01((worldPos.x - transform.position.x + gridWorldSize.x / 2f) / gridWorldSize.x);
        float percentY = Mathf.Clamp01((worldPos.y - transform.position.y + gridWorldSize.y / 2f) / gridWorldSize.y);

        int x = Mathf.FloorToInt(gridSizeX * percentX);
        int y = Mathf.FloorToInt(gridSizeY * percentY);

        x = Mathf.Clamp(x, 0, gridSizeX - 1);
        y = Mathf.Clamp(y, 0, gridSizeY - 1);

        return grid[x, y];
    }

    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();

        if (grid == null || node == null)
            return neighbours;

        foreach (Vector2Int dir in directions)
        {
            int checkX = node.gridX + dir.x;
            int checkY = node.gridY + dir.y;

            if (checkX < 0 || checkX >= gridSizeX || checkY < 0 || checkY >= gridSizeY)
                continue;

            if (Mathf.Abs(dir.x) == 1 && Mathf.Abs(dir.y) == 1)
            {
                if (!grid[node.gridX + dir.x, node.gridY].isWalkable ||
                    !grid[node.gridX, node.gridY + dir.y].isWalkable)
                {
                    continue;
                }
            }

            neighbours.Add(grid[checkX, checkY]);
        }

        return neighbours;
    }

    public IEnumerable<Node> GetAllNodes()
    {
        if (grid == null)
            yield break;

        foreach (Node n in grid)
            yield return n;
    }
    /*
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, gridWorldSize.y, 1f));

        if (grid == null)
            return;

        foreach (Node n in grid)
        {
            Gizmos.color = n.isWalkable ? Color.white : Color.red;
            Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - 0.1f));
        }
    }
#endif*/
}