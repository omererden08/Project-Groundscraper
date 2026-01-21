using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class CrosshairController : MonoBehaviour
{
    [SerializeField] private RectTransform crosshairImage;

    [Header("Mouse Smooth")]
    [SerializeField] private float smoothTime = 0.05f;
    [SerializeField] private float maxSpeed = 10000f;

    [Header("Screen Clamp (pixels)")]
    [SerializeField] private float screenPadding = 20f;

    public static event Action<Vector3> OnAimWorldChanged;

    private Camera cam;

    private Vector2 currentScreenPos;
    private Vector2 velocity;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Start()
    {
        currentScreenPos = Mouse.current.position.ReadValue();
    }

    private void Update()
    {
        Vector2 mouseScreen = InputManager.Instance.LookInput;

        currentScreenPos = Vector2.SmoothDamp(
            currentScreenPos,
            mouseScreen,
            ref velocity,
            smoothTime,
            maxSpeed,
            Time.deltaTime
        );

        // 🔒 SCREEN SPACE CLAMP (ASIL ÇÖZÜM)
        currentScreenPos.x = Mathf.Clamp(
            currentScreenPos.x,
            screenPadding,
            Screen.width - screenPadding
        );

        currentScreenPos.y = Mathf.Clamp(
            currentScreenPos.y,
            screenPadding,
            Screen.height - screenPadding
        );

        // UI crosshair
        crosshairImage.position = currentScreenPos;

        // World aim (kamera merkezine göre doğru)
        Vector3 aimWorld = cam.ScreenToWorldPoint(
            new Vector3(
                currentScreenPos.x,
                currentScreenPos.y,
                Mathf.Abs(cam.transform.position.z)
            )
        );

        aimWorld.z = 0f;

        OnAimWorldChanged?.Invoke(aimWorld);
    }
}
