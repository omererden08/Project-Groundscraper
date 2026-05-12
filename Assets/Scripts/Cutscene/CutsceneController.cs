using UnityEngine;
using DG.Tweening;
using System.Collections;

public class CutsceneController : MonoBehaviour
{
    [Header("Move Target")]
    [SerializeField] private Transform objectToMove;

    [Header("Start Position")]
    [SerializeField] private float startDelay = 1f;
    [SerializeField] private float startXOffset = -12f;
    [SerializeField] private bool animateStartOffset = true;
    [SerializeField] private float startMoveSpeed = 4f;
    [SerializeField] private Ease startMoveEase = Ease.OutCubic;

    [Header("Click Position Points")]
    [SerializeField] private Transform[] clickPositionPoints;
    [SerializeField] private float targetWorldX = 0f;

    [Header("Click Count")]
    [SerializeField] private int requiredClickCount = 5;
    [SerializeField] private int startPointIndex = 0;

    [Header("Input Settings")]
    [SerializeField] private bool useAttackInput = true;
    [SerializeField] private bool useInteractInput = false;

    [Header("Click Settings")]
    [SerializeField] private bool ignoreClicksWhileMoving = true;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;

    [Header("Scene Transition")]
    [SerializeField] private bool useCutsceneFinishEvent = true;
    [SerializeField] private string gameplaySceneName = "Gameplay";

    private int currentPointIndex;
    private int currentClickCount;

    private bool isMoving;
    private bool inputEnabled;
    private bool transitionStarted;
    private bool inputManagerWarningShown;

    private Tween moveTween;
    private Coroutine startRoutine;

    private void Start()
    {
        if (objectToMove == null)
            objectToMove = transform;

        currentPointIndex = Mathf.Max(0, startPointIndex);
        currentClickCount = 0;

        inputEnabled = false;

        startRoutine = StartCoroutine(StartIntroMoveRoutine());
    }

    private IEnumerator StartIntroMoveRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        ClearBufferedCutsceneInput();

        ApplyStartOffset();
    }

    private void Update()
    {
        if (!inputEnabled)
            return;

        if (transitionStarted)
            return;

        if (WasAdvanceInputPressed())
        {
            HandleClick();
        }
    }

    private void ApplyStartOffset()
    {
        Vector3 startPosition = objectToMove.position;
        Vector3 targetPosition = startPosition;
        targetPosition.x += startXOffset;

        if (!animateStartOffset)
        {
            objectToMove.position = targetPosition;
            EnableInputAfterStartMove();
            return;
        }

        isMoving = true;

        float distance = Mathf.Abs(targetPosition.x - startPosition.x);
        float duration = distance / startMoveSpeed;

        moveTween?.Kill();

        moveTween = objectToMove
            .DOMoveX(targetPosition.x, duration)
            .SetEase(startMoveEase)
            .OnComplete(() =>
            {
                isMoving = false;
                EnableInputAfterStartMove();
            });
    }

    private void EnableInputAfterStartMove()
    {
        ClearBufferedCutsceneInput();
        inputEnabled = true;
    }

    private void ClearBufferedCutsceneInput()
    {
        if (InputManager.Instance == null)
            return;

        InputManager.Instance.TryConsumeCutsceneAdvanceInput(
            useAttackInput,
            useInteractInput
        );
    }

    private bool WasAdvanceInputPressed()
    {
        if (InputManager.Instance == null)
        {
            if (!inputManagerWarningShown)
            {
                inputManagerWarningShown = true;
                Debug.LogError("CutsceneController: InputManager.Instance bulunamadı!");
            }

            return false;
        }

        return InputManager.Instance.TryConsumeCutsceneAdvanceInput(
            useAttackInput,
            useInteractInput
        );
    }

    private void HandleClick()
    {
        if (ignoreClicksWhileMoving && isMoving)
            return;

        currentClickCount++;

        bool hasPoint =
            clickPositionPoints != null &&
            currentPointIndex >= 0 &&
            currentPointIndex < clickPositionPoints.Length &&
            clickPositionPoints[currentPointIndex] != null;

        if (hasPoint)
        {
            Transform currentPoint = clickPositionPoints[currentPointIndex];
            currentPointIndex++;

            MoveObjectUntilPointXBecomesZero(currentPoint);
        }
        else
        {
            Debug.LogWarning(
                $"CutsceneController: {currentPointIndex} indexinde point yok. Click sayıldı ama hareket yapılmadı."
            );

            CheckTransitionAfterClick();
        }
    }

    private void MoveObjectUntilPointXBecomesZero(Transform point)
    {
        if (objectToMove == null)
        {
            CheckTransitionAfterClick();
            return;
        }

        isMoving = true;

        moveTween?.Kill();

        float xDifference = targetWorldX - point.position.x;

        Vector3 targetPosition = objectToMove.position;
        targetPosition.x += xDifference;

        float distance = Mathf.Abs(objectToMove.position.x - targetPosition.x);
        float duration = distance / moveSpeed;

        moveTween = objectToMove
            .DOMoveX(targetPosition.x, duration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                isMoving = false;
                CheckTransitionAfterClick();
            });
    }

    private void CheckTransitionAfterClick()
    {
        if (transitionStarted)
            return;

        if (currentClickCount >= requiredClickCount)
        {
            transitionStarted = true;
            GoToGameplay();
        }
    }

    private void GoToGameplay()
    {
        if (useCutsceneFinishEvent)
        {
            GameEvents.RaiseCutsceneFinished();
        }
        else
        {
            GameEvents.RaiseSceneLoadRequested(gameplaySceneName);
        }
    }

    private void OnDestroy()
    {
        if (startRoutine != null)
            StopCoroutine(startRoutine);

        moveTween?.Kill();
    }
}