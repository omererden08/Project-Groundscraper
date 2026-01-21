using UnityEngine;

public class MidpointAimStrategy : ICameraStrategy
{
    private CameraController c;
    private Vector3 velocity;

    private Vector3 aimWorld;
    private bool hasAim;

    // 🔧 Smooth ayarları
    [Header("Camera Smooth")]
    [SerializeField] private float followSharpness = 1f;
    // 1   → normal SmoothDamp
    // <1  → daha ağır / yumuşak
    // >1  → daha hızlı

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

        // 1️⃣ Midpoint (kamera hedefi)
        Vector3 targetPos = (playerPos + aimWorld) * 0.5f;

        // Kamera Z sabit
        Vector3 camPos = c.transform.position;
        targetPos.z = camPos.z;

        // 2️⃣ Daha yumuşak SmoothDamp
        float smoothTime = c.SmoothTime / Mathf.Max(0.0001f, followSharpness);

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
