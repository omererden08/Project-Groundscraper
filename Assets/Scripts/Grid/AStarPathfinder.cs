using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder
{
    public static List<Node> FindPath(Vector2 startPos, Vector2 targetPos)
    {
        GridManager grid = GridManager.Instance;

        Node startNode = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        if (!startNode.isWalkable || !targetNode.isWalkable)
        {
            Debug.LogWarning("🚫 Start or Target not walkable.");
            return null;
        }

        foreach (var node in grid.GetAllNodes())
        {
            node.gCost = int.MaxValue;
            node.hCost = 0;
            node.parent = null;
        }

        startNode.gCost = 0;

        var openSet = new MinHeap<Node>(GridManager.Instance.MaxSize);
        var closedSet = new HashSet<Node>();

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet.RemoveFirst();

            if (currentNode == targetNode)
                return RetracePath(startNode, targetNode);

            closedSet.Add(currentNode);

            foreach (Node neighbour in grid.GetNeighbours(currentNode))
            {
                if (!neighbour.isWalkable || closedSet.Contains(neighbour))
                    continue;

                int newCost = currentNode.gCost + GetDistance(currentNode, neighbour);

                if (newCost < neighbour.gCost)
                {
                    neighbour.gCost = newCost;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                    else
                        openSet.UpdateItem(neighbour);
                }
            }
        }

        Debug.LogWarning("❌ No path found.");
        return null;
    }

    static List<Node> RetracePath(Node start, Node end)
    {
        var path = new List<Node>();
        Node current = end;

        while (current != start)
        {
            path.Add(current);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }

    static int GetDistance(Node a, Node b)
    {
        return Mathf.Abs(a.gridX - b.gridX) + Mathf.Abs(a.gridY - b.gridY); // Manhattan
    }
}
