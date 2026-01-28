using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Settings")]
    public Vector2 gridWorldSize = new Vector2(20, 20);
    public float nodeRadius = 0.5f;
    public LayerMask unwalkableMask;
    [SerializeField] private float obstacleBuffer = 0.15f; // 0.1 - 0.2 arası idealdir

    private Node[,] grid;
    private float nodeDiameter;
    private int gridSizeX, gridSizeY;

    private readonly static Vector2Int[] directions = {
        new Vector2Int(-1, 0), new Vector2Int(1, 0),
        new Vector2Int(0, -1), new Vector2Int(0, 1),
        new Vector2Int(-1, -1), new Vector2Int(1, -1),
        new Vector2Int(-1, 1), new Vector2Int(1, 1)
    };

    private void Awake()
    {
        Instance = this;

        nodeDiameter = nodeRadius * 2f;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        CreateGrid();
    }

    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector2 bottomLeft = (Vector2)transform.position - Vector2.right * gridWorldSize.x / 2 - Vector2.up * gridWorldSize.y / 2;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector2 worldPoint = bottomLeft + Vector2.right * (x * nodeDiameter + nodeRadius)
                                                  + Vector2.up * (y * nodeDiameter + nodeRadius);

                bool walkable = !Physics2D.OverlapCircle(worldPoint, nodeRadius + obstacleBuffer, unwalkableMask);
                grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }

    public Node NodeFromWorldPoint(Vector2 worldPos)
    {
        float percentX = Mathf.Clamp01((worldPos.x - transform.position.x + gridWorldSize.x / 2) / gridWorldSize.x);
        float percentY = Mathf.Clamp01((worldPos.y - transform.position.y + gridWorldSize.y / 2) / gridWorldSize.y);

        int x = Mathf.FloorToInt(gridSizeX * percentX);
        int y = Mathf.FloorToInt(gridSizeY * percentY);

        return grid[Mathf.Clamp(x, 0, gridSizeX - 1), Mathf.Clamp(y, 0, gridSizeY - 1)];
    }

    public List<Node> GetNeighbours(Node node)
    {
        var neighbours = new List<Node>();

        foreach (var dir in directions)
        {
            int checkX = node.gridX + dir.x;
            int checkY = node.gridY + dir.y;

            if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
            {
                if (Mathf.Abs(dir.x) == 1 && Mathf.Abs(dir.y) == 1)
                {
                    if (!grid[node.gridX + dir.x, node.gridY].isWalkable || !grid[node.gridX, node.gridY + dir.y].isWalkable)
                        continue;
                }

                neighbours.Add(grid[checkX, checkY]);
            }
        }

        return neighbours;
    }

    public IEnumerable<Node> GetAllNodes()
    {
        foreach (var n in grid)
            yield return n;
    }

    public int MaxSize => gridSizeX * gridSizeY; 

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, gridWorldSize.y, 1));

        if (grid == null) return;

        foreach (var n in grid)
        {
            Gizmos.color = n.isWalkable ? Color.white : Color.red;
            Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - 0.1f));
        }
    }
#endif

}
