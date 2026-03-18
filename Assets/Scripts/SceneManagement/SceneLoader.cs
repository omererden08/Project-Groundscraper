using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private string gameplaySceneName = "Gameplay";

    private string currentLoadedScene;
    private bool isTransitioning;
    private bool waitForLevelLoadedBeforeFadeOut;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeImage != null)
        {
            fadeImage.color = Color.black;
            fadeImage.gameObject.SetActive(true);
        }
    }

    private void Start()
    {
        if (fadeImage != null)
            StartCoroutine(FadeInAtStartup());
    }

    private IEnumerator FadeInAtStartup()
    {
        yield return fadeImage.DOFade(0f, fadeDuration).WaitForCompletion();
        fadeImage.gameObject.SetActive(false);

        if (SceneManager.sceneCount > 1)
            currentLoadedScene = SceneManager.GetActiveScene().name;
    }

    private void OnEnable()
    {
        GameEvents.OnSceneLoadRequested += HandleSceneLoadRequested;
        GameEvents.OnQuitRequested += HandleQuitRequested;
        GameEvents.OnLevelLoaded += HandleLevelLoaded;
    }

    private void OnDisable()
    {
        GameEvents.OnSceneLoadRequested -= HandleSceneLoadRequested;
        GameEvents.OnQuitRequested -= HandleQuitRequested;
        GameEvents.OnLevelLoaded -= HandleLevelLoaded;
    }

    private void HandleSceneLoadRequested(string sceneName)
    {
        if (isTransitioning)
            return;

        if (sceneName == currentLoadedScene)
            return;

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isTransitioning = true;

        fadeImage.gameObject.SetActive(true);
        yield return fadeImage.DOFade(1f, fadeDuration).WaitForCompletion();

        GameEvents.RaiseSceneLoadStarted(sceneName);

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!loadOp.isDone)
            yield return null;

        Scene newScene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(newScene);

        if (!string.IsNullOrEmpty(currentLoadedScene))
            yield return SceneManager.UnloadSceneAsync(currentLoadedScene);

        currentLoadedScene = sceneName;

        yield return null;

        GameEvents.RaiseSceneLoadCompleted(sceneName);

        // Sadece Gameplay geçişinde fade out'ı level prefab yüklenene kadar beklet
        if (sceneName == gameplaySceneName)
        {
            waitForLevelLoadedBeforeFadeOut = true;
            yield break;
        }

        yield return FadeOutRoutine();
        isTransitioning = false;
    }

    private void HandleLevelLoaded(string levelId)
    {
        if (!waitForLevelLoadedBeforeFadeOut)
            return;

        StartCoroutine(FinishDeferredFadeOutRoutine());
    }

    private IEnumerator FinishDeferredFadeOutRoutine()
    {
        waitForLevelLoadedBeforeFadeOut = false;

        yield return null;

        yield return FadeOutRoutine();
        isTransitioning = false;
    }

    private IEnumerator FadeOutRoutine()
    {
        if (fadeImage == null)
            yield break;

        yield return fadeImage.DOFade(0f, fadeDuration).WaitForCompletion();
        fadeImage.gameObject.SetActive(false);
    }

    private void HandleQuitRequested()
    {
        if (!gameObject.activeInHierarchy)
            return;

        StartCoroutine(QuitRoutine());
    }

    private IEnumerator QuitRoutine()
    {
        isTransitioning = true;

        fadeImage.gameObject.SetActive(true);
        yield return fadeImage.DOFade(1f, fadeDuration).WaitForCompletion();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}