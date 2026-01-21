using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;   // PLAYER

    [Header("Camera Follow")]
    [SerializeField] private float smoothTime = 0.08f;
    [SerializeField] private float followSharpness = 1f;

    [Header("Player Deadzone")]
    [SerializeField] private float deadzoneRadius = 0.5f;

    public Transform Target => target;
    public float SmoothTime => smoothTime;
    public float FollowSharpness => followSharpness;
    public float DeadzoneRadius => deadzoneRadius;

    private ICameraStrategy currentStrategy;

    private void Awake()
    {
        // Güvenlik: target boşsa Player tag'inden bul
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
            else
                Debug.LogError("[CameraController] Player bulunamadı!");
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

    public void SetStrategy(ICameraStrategy strategy)
    {
        currentStrategy?.OnExit();
        currentStrategy = strategy;
        currentStrategy.OnEnter(this);
    }
}
