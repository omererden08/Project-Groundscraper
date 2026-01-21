using UnityEngine;

public class PlayerAimPublisher : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private Transform player;
    [SerializeField] private float aimDistance = 2.5f;

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;

        if (player == null)
            player = transform;
    }

    private void Update()
    {
        // 🔒 Player'ın bulunduğu düzleme dik plane (XZ için UP)
        Plane aimPlane = new Plane(Vector3.up, player.position);

        Ray ray = cam.ScreenPointToRay(InputManager.Instance.LookInput);

        if (aimPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);

            Vector3 dir = hitPoint - player.position;
            dir.y = 0f;                // Y eksenini kilitle
            dir.Normalize();

            PlayerAimState.WorldPosition =
                player.position + dir * aimDistance;
        }
    }
}
