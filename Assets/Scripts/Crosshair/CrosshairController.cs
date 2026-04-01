using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System;

public class CrosshairController : MonoBehaviour
{
    [SerializeField] private RectTransform crosshairImage;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string shootTriggerName = "Shoot";

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
        PlayerEvents.OnCrosshairShoot += PlayShootFeedback;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        PlayerEvents.OnCrosshairShoot -= PlayShootFeedback;
    }

    private void Awake()
    {
        ResolveCamera();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (Mouse.current != null)
            currentScreenPos = Mouse.current.position.ReadValue();
        else
            currentScreenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private void HandleActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        ResolveCamera();
    }

    private void ResolveCamera()
    {
        cam = Camera.main;

        if (cam == null)
            Debug.LogWarning("CrosshairController: Camera.main bulunamadı (henüz yüklenmemiş olabilir).");
    }

    private void Update()
    {
        if (cam == null)
        {
            ResolveCamera();
            if (cam == null) return;
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

        currentScreenPos.x = Mathf.Clamp(currentScreenPos.x, screenPadding, Screen.width - screenPadding);
        currentScreenPos.y = Mathf.Clamp(currentScreenPos.y, screenPadding, Screen.height - screenPadding);

        if (crosshairImage != null)
            crosshairImage.position = currentScreenPos;

        float z = Mathf.Abs(cam.transform.position.z);
        Vector3 aimWorld = cam.ScreenToWorldPoint(new Vector3(currentScreenPos.x, currentScreenPos.y, z));
        aimWorld.z = 0f;

        OnAimWorldChanged?.Invoke(aimWorld);
    }

    public void PlayShootFeedback()
    {
        if (animator == null)
            return;

        animator.SetTrigger(shootTriggerName);
        
    }
}