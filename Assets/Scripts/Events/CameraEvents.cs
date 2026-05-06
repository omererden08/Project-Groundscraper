using System;
using UnityEngine;

public static class CameraEvents
{
    // (playerPos, aimWorldPos)
    public static Action<Vector3, Vector3> OnAimDirectionChanged;

    // (midpoint)
    public static Action<Vector3> OnCameraFocusPointChanged;

    // (duration, intensity)
    public static Action<float, float> OnCameraShakeRequested;
}