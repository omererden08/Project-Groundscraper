using UnityEngine;

public class CameraShakeStrategy : ICameraStrategy
{
    private CameraController c;

    private float timer;
    private float duration;
    private float intensity;

    private Vector3 shakeOffset;

    private float seedX;
    private float seedY;

    public void OnEnter(CameraController controller)
    {
        c = controller;

        timer = 0f;
        duration = 0f;
        intensity = 0f;
        shakeOffset = Vector3.zero;

        seedX = Random.Range(0f, 1000f);
        seedY = Random.Range(0f, 1000f);

        CameraEvents.OnCameraShakeRequested += OnCameraShakeRequested;
    }

    public void OnExit()
    {
        CameraEvents.OnCameraShakeRequested -= OnCameraShakeRequested;

        if (c != null)
        {
            c.transform.position -= shakeOffset;
        }

        shakeOffset = Vector3.zero;
        c = null;
    }

    private void OnCameraShakeRequested(float shakeDuration, float shakeIntensity)
    {
        duration = shakeDuration;
        timer = shakeDuration;

        intensity = Mathf.Max(intensity, shakeIntensity);
    }

    public void TickLate(float dt)
    {
        if (c == null)
            return;

        // Önce bir önceki frame'deki shake offsetini temizle
        Vector3 basePosition = c.transform.position - shakeOffset;
        shakeOffset = Vector3.zero;

        if (timer <= 0f)
        {
            c.transform.position = basePosition;
            return;
        }

        timer -= dt;

        float normalizedTime = duration > 0f
            ? Mathf.Clamp01(timer / duration)
            : 0f;

        float currentIntensity = intensity * normalizedTime;

        float noiseX = Mathf.PerlinNoise(seedX, Time.time * 35f) * 2f - 1f;
        float noiseY = Mathf.PerlinNoise(seedY, Time.time * 35f) * 2f - 1f;

        shakeOffset = new Vector3(noiseX, noiseY, 0f) * currentIntensity;

        c.transform.position = basePosition + shakeOffset;

        if (timer <= 0f)
        {
            intensity = 0f;
            shakeOffset = Vector3.zero;
            c.transform.position = basePosition;
        }
    }
}