using UnityEngine;

public class Node : IHeapItem<Node>
{
    public Vector2 worldPosition;
    public bool isWalkable;
    public int gridX, gridY;

    public int gCost;
    public int hCost;
    public Node parent;

    public int fCost => gCost + hCost;

    public Node(bool walkable, Vector2 worldPos, int x, int y)
    {
        isWalkable = walkable;
        worldPosition = worldPos;
        gridX = x;
        gridY = y;

        gCost = int.MaxValue;
        hCost = 0;
        parent = null;
    }

    public override bool Equals(object obj)
    {
        return obj is Node other && gridX == other.gridX && gridY == other.gridY;
    }

    public override int GetHashCode()
    {
        return gridX * 73856093 ^ gridY * 19349663;
    }
    public int HeapIndex { get; set; }

    public int CompareTo(Node other)
    {
        int compare = fCost.CompareTo(other.fCost);
        if (compare == 0)
            compare = hCost.CompareTo(other.hCost);
        return compare;
    }
}
