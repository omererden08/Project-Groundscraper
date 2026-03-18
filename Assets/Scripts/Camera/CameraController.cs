using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;

    [Header("Camera Follow")]
    [SerializeField] private float smoothTime = 0.08f;
    [SerializeField] private float followSharpness = 1f;

    [Header("Player Deadzone")]
    [SerializeField] private float deadzoneRadius = 0.5f;

    private ICameraStrategy currentStrategy;

    public Transform Target => target;
    public float SmoothTime => smoothTime;
    public float FollowSharpness => followSharpness;
    public float DeadzoneRadius => deadzoneRadius;

    private void Start()
    {
        ResolveTarget();
        SetStrategy(new MidpointAimStrategy());
    }

    private void LateUpdate()
    {
        if (target == null)
            ResolveTarget();

        currentStrategy?.TickLate(Time.deltaTime);
    }

    private void ResolveTarget()
    {
        if (target != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
        else
        {
            Debug.LogWarning("[CameraController] Player henüz bulunamadı.");
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetStrategy(ICameraStrategy strategy)
    {
        currentStrategy?.OnExit();
        currentStrategy = strategy;
        currentStrategy?.OnEnter(this);
    }
}