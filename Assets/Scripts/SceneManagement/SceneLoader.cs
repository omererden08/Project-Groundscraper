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

    [Header("Special Scenes")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    private bool isTransitioning;
    private bool waitForLevelLoadedBeforeFadeOut;
    private string currentLoadedScene;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentLoadedScene = SceneManager.GetActiveScene().name;

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
        yield return FadeTo(0f);
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
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneLoader: Scene name boş!");
            return;
        }

        if (isTransitioning)
            return;

        if (sceneName == currentLoadedScene)
            return;

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isTransitioning = true;
        waitForLevelLoadedBeforeFadeOut = false;

        yield return FadeTo(1f);

        GameEvents.RaiseSceneLoadStarted(sceneName);

        // ÖNEMLİ:
        // Additive yerine Single kullanıyoruz.
        // Böylece MainMenu sahnesi Cutscene üstünde kalmaz.
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        if (loadOp == null)
        {
            Debug.LogError($"SceneLoader: Scene yüklenemedi. Build Settings içinde var mı? Scene: {sceneName}");
            isTransitioning = false;
            yield break;
        }

        while (!loadOp.isDone)
            yield return null;

        currentLoadedScene = sceneName;

        yield return null;

        bool isGameplayScene = sceneName == gameplaySceneName;

        if (isGameplayScene)
            waitForLevelLoadedBeforeFadeOut = true;

        GameEvents.RaiseSceneLoadCompleted(sceneName);

        if (isGameplayScene)
            yield break;

        yield return FadeTo(0f);

        isTransitioning = false;
    }

    private void HandleLevelLoaded(string levelId)
    {
        if (!waitForLevelLoadedBeforeFadeOut)
            return;

        StartCoroutine(FinishGameplayTransitionRoutine());
    }

    private IEnumerator FinishGameplayTransitionRoutine()
    {
        waitForLevelLoadedBeforeFadeOut = false;

        yield return null;

        yield return FadeTo(0f);

        isTransitioning = false;
    }

    private IEnumerator FadeTo(float alpha)
    {
        if (fadeImage == null)
            yield break;

        fadeImage.DOKill();
        fadeImage.gameObject.SetActive(true);

        yield return fadeImage
            .DOFade(alpha, fadeDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .WaitForCompletion();

        if (alpha <= 0f)
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

        yield return FadeTo(1f);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}