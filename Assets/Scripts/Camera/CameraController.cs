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

    private ICameraStrategy followStrategy;
    private ICameraStrategy shakeStrategy;

    public Transform Target => target;
    public float SmoothTime => smoothTime;
    public float FollowSharpness => followSharpness;
    public float DeadzoneRadius => deadzoneRadius;

    private void Start()
    {
        ResolveTarget();

        SetFollowStrategy(new MidpointAimStrategy());

        shakeStrategy = new CameraShakeStrategy();
        shakeStrategy.OnEnter(this);
    }

    private void LateUpdate()
    {
        if (target == null)
            ResolveTarget();

        float dt = Time.deltaTime;

        followStrategy?.TickLate(dt);
        shakeStrategy?.TickLate(dt);
    }

    private void OnDestroy()
    {
        followStrategy?.OnExit();
        shakeStrategy?.OnExit();
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

    public void SetFollowStrategy(ICameraStrategy strategy)
    {
        followStrategy?.OnExit();

        followStrategy = strategy;

        followStrategy?.OnEnter(this);
    }
}