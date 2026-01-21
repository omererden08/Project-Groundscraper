using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;

    // 🔒 Cache edilen aim yönü
    private Vector2 cachedAimDir;
    private bool hasAim;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 🔥 JITTER FIX
    }

    private void Update()
    {
        CacheAimDirection();
        HandleFireInput();
    }

    private void FixedUpdate()
    {
        Move();
        Rotate();
    }

    private void Move()
    {
        Vector2 moveInput = InputManager.Instance.MoveInput;
        rb.linearVelocity = moveInput.normalized * moveSpeed;
    }

    // 🔒 SADECE CACHE
    private void CacheAimDirection()
    {
        Vector3 playerPos = transform.position;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
            new Vector3(
                InputManager.Instance.LookInput.x,
                InputManager.Instance.LookInput.y,
                Mathf.Abs(Camera.main.transform.position.z)
            )
        );

        Vector2 dir = mouseWorld - playerPos;

        if (dir.sqrMagnitude < 0.001f)
            return;

        cachedAimDir = dir.normalized;
        hasAim = true;
    }

    // 🔒 PHYSICS ADIMINDA ROTATE
    private void Rotate()
    {
        if (!hasAim) return;

        float angle = Mathf.Atan2(cachedAimDir.y, cachedAimDir.x) * Mathf.Rad2Deg;
        rb.rotation = angle;
    }

    private void HandleFireInput()
    {
        if (InputManager.Instance.FirePressed)
        {
            Debug.Log("FIRE!");
        }
    }
}
