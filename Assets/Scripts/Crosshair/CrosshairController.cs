using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System;

public class CrosshairController : MonoBehaviour
{
    [SerializeField] private RectTransform crosshairImage;

    [Header("Mouse Smooth")]
    [SerializeField] private float smoothTime = 0.05f;
    [SerializeField] private float maxSpeed = 10000f;

    [SerializeField] private float screenPadding = 20f;

    public static event Action<Vector3> OnAimWorldChanged;

    private Camera cam;

    private Vector2 currentScreenPos;
    private Vector2 velocity;

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
    }

    private void Awake()
    {
        ResolveCamera();
    }

    private void Start()
    {
        // Mouse.current null olabilir (gamepad vs). Güvenli al:
        if (Mouse.current != null)
            currentScreenPos = Mouse.current.position.ReadValue();
        else
            currentScreenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private void HandleActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        // Aktif sahne değişince (MainMenu -> Gameplay) kamera yenilenir
        ResolveCamera();
    }

    private void ResolveCamera()
    {
        cam = Camera.main;

        // Eğer main camera henüz oluşmadıysa, bir süre sonra tekrar deneyeceğiz (Update’te)
        if (cam == null)
            Debug.LogWarning("CrosshairController: Camera.main bulunamadı (henüz yüklenmemiş olabilir).");
    }

    private void Update()
    {
        // Kamera destroy edilmişse / yoksa tekrar bul
        if (cam == null)
        {
            ResolveCamera();
            if (cam == null) return; // kamera gelene kadar aim hesaplama yapma
        }

        Vector2 mouseScreen = InputManager.Instance.LookInput;

        currentScreenPos = Vector2.SmoothDamp(
            currentScreenPos,
            mouseScreen,
            ref velocity,
            smoothTime,
            maxSpeed,
            Time.deltaTime
        );

        // Screen clamp
        currentScreenPos.x = Mathf.Clamp(currentScreenPos.x, screenPadding, Screen.width - screenPadding);
        currentScreenPos.y = Mathf.Clamp(currentScreenPos.y, screenPadding, Screen.height - screenPadding);

        // UI crosshair
        if (crosshairImage != null)
            crosshairImage.position = currentScreenPos;

        // World aim
        float z = Mathf.Abs(cam.transform.position.z);
        Vector3 aimWorld = cam.ScreenToWorldPoint(new Vector3(currentScreenPos.x, currentScreenPos.y, z));
        aimWorld.z = 0f;

        OnAimWorldChanged?.Invoke(aimWorld);
    }
}