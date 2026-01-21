using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private PlayerInputActions inputActions;

    public Vector2 MoveInput { get; private set; }

    // 🔒 LOOKINPUT = SCREEN POSITION (PIXEL)
    public Vector2 LookInput { get; private set; }

    // 🔒 ROTATE / DIRECTION İÇİN AYRI (ileride kullanılır)
    public Vector2 LookDirection { get; private set; }

    public bool FirePressed { get; private set; }
    public bool PausePressed { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        inputActions = new PlayerInputActions();
        inputActions.Enable();
    }

    private void Update()
    {
        // Hareket
        MoveInput = inputActions.Player.Move.ReadValue<Vector2>();

        // 🖱️ AIM İÇİN — SCREEN POSITION
        if (Mouse.current != null)
            LookInput = Mouse.current.position.ReadValue();

        // 🎮 ROTATE / STICK İÇİN (şimdilik sıfır, gamepad eklenirse burası dolar)
        LookDirection = Vector2.zero;

        FirePressed = false;
        PausePressed = false;
    }
}
