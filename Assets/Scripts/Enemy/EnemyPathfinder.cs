using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPathfinder : MonoBehaviour
{
    [SerializeField] private float pathUpdateInterval = 0.5f;

    private List<Vector2> currentPath = new List<Vector2>();
    public List<Vector2> CurrentPath => currentPath;
    public bool HasPath => currentPath != null && currentPath.Count > 0;

    private Coroutine updateRoutine;

    public void RequestPath(Vector2 from, Vector2 to)
    {
        currentPath.Clear();
        Debug.Log($"📦 Requesting path from {from} to {to}");

        var path = AStarPathfinder.FindPath(from, to);
        if (path != null && path.Count > 0)
        {
            foreach (var node in path)
                currentPath.Add(node.worldPosition);

            Debug.Log("✅ Path generated with " + currentPath.Count + " points.");
        }
        else
        {
            Debug.LogWarning("❌ Path not found!");
        }
    }


    public void StartTrackingPlayer(Transform player)
    {
        if (updateRoutine != null)
            StopCoroutine(updateRoutine);

        updateRoutine = StartCoroutine(UpdatePathToPlayer(player));
    }

    public void StopTracking()
    {
        if (updateRoutine != null)
        {
            StopCoroutine(updateRoutine);
            updateRoutine = null;
        }
    }

    private IEnumerator UpdatePathToPlayer(Transform target)
    {
        while (true)
        {
            if (target != null)
                RequestPath(transform.position, target.position);

            yield return new WaitForSeconds(pathUpdateInterval);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (currentPath == null || currentPath.Count == 0)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < currentPath.Count - 1; i++)
            Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
    }
#endif

}
