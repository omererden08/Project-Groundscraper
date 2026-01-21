using UnityEngine;

public static class PlayerAimState
{
    private static Vector3 _worldPosition;

    public static Vector3 WorldPosition
    {
        get => _worldPosition;
        set
        {
            _worldPosition = value;
            Debug.Log($"[Aim WRITE] {_worldPosition} by {Time.frameCount}");
        }
    }
}
