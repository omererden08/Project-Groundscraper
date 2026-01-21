using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;   // Player
    [SerializeField] private Camera cam;

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.10f;

    public Transform Target => target;
    public Camera Cam => cam;
    public float SmoothTime => smoothTime;

    private ICameraStrategy currentStrategy;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;

        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
            else Debug.LogError("[CameraController] Player (tag=Player) bulunamadı!");
        }
    }

    private void Start()
    {
        SetStrategy(new MidpointAimStrategy());
    }

    private void LateUpdate()
    {
        currentStrategy?.TickLate(Time.deltaTime);
    }

    public void SetStrategy(ICameraStrategy next)
    {
        currentStrategy?.OnExit();
        currentStrategy = next;
        currentStrategy?.OnEnter(this);
    }
}
