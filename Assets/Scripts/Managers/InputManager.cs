using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private PlayerInputActions inputActions;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public Vector2 LookDirection { get; private set; }

    private bool attackPressed;
    public bool AttackPressed => attackPressed;

    private bool interactPressed;
    public bool InteractPressed => interactPressed;

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

        inputActions.Player.Attack.performed += ctx => attackPressed = true;
        inputActions.Player.Pause.performed += ctx => PausePressed = true;
        inputActions.Player.Interact.performed += ctx => interactPressed = true;

    }

    private void Update()
    {
        MoveInput = inputActions.Player.Move.ReadValue<Vector2>();

        if (Mouse.current != null)
            LookInput = Mouse.current.position.ReadValue();

        LookDirection = Vector2.zero;

    }

    public void ConsumeAttackInput() => attackPressed = false;
    public void ConsumePauseInput() => PausePressed = false;
    public void ConsumeInteractInput() => interactPressed = false;

}
