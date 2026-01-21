using UnityEngine;

public class MidpointAimStrategy : ICameraStrategy
{
    private CameraController c;
    private Vector3 velocity;

    private Vector3 aimWorld;
    private bool hasAim;

    public void OnEnter(CameraController controller)
    {
        c = controller;
        velocity = Vector3.zero;

        CrosshairController.OnAimWorldChanged += OnAimWorldChanged;
    }

    public void OnExit()
    {
        CrosshairController.OnAimWorldChanged -= OnAimWorldChanged;
    }

    private void OnAimWorldChanged(Vector3 worldPos)
    {
        aimWorld = worldPos;
        hasAim = true;
    }

    public void TickLate(float dt)
    {
        if (!hasAim || c == null || c.Target == null)
            return;

        Vector3 playerPos = c.Target.position;
        Vector3 camPos = c.transform.position;

        // 1️⃣ Midpoint
        Vector3 midpoint = (playerPos + aimWorld) * 0.5f;

        // 2️⃣ Player-centered deadzone
        Vector3 offset = midpoint - playerPos;
        offset.z = 0f;

        Vector3 targetPos;

        if (offset.magnitude < c.DeadzoneRadius)
            targetPos = playerPos;
        else
            targetPos = midpoint;

        targetPos.z = camPos.z;

        // 3️⃣ Smooth follow
        float smoothTime = c.SmoothTime / Mathf.Max(0.0001f, c.FollowSharpness);

        c.transform.position = Vector3.SmoothDamp(
            camPos,
            targetPos,
            ref velocity,
            smoothTime,
            Mathf.Infinity,
            dt
        );
    }
}
