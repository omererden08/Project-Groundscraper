using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;

    private void Awake()
    {
        if (fadeImage != null)
        {
            fadeImage.color = Color.black;
            fadeImage.gameObject.SetActive(true);
        }

    }

    private void Start()
    {
        if (fadeImage != null)
            StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        yield return fadeImage.DOFade(0f, fadeDuration).WaitForCompletion();
        fadeImage.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.OnSceneLoadRequested += HandleSceneLoadRequested;
        GameEvents.OnQuitRequested += HandleQuitRequested;
    }

    private void OnDisable()
    {
        GameEvents.OnSceneLoadRequested -= HandleSceneLoadRequested;
        GameEvents.OnQuitRequested -= HandleQuitRequested;
    }

    private void HandleSceneLoadRequested(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        fadeImage.gameObject.SetActive(true);
        yield return fadeImage.DOFade(1f, fadeDuration).WaitForCompletion();

        GameEvents.RaiseSceneLoadStarted(sceneName);

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName);
        yield return loadOp;

        yield return new WaitForSeconds(0.1f);

        yield return fadeImage.DOFade(0f, fadeDuration).WaitForCompletion();
        fadeImage.gameObject.SetActive(false);

        GameEvents.RaiseSceneLoadCompleted(sceneName);
    }

    private void HandleQuitRequested()
    {
        StartCoroutine(QuitRoutine());
    }

    private IEnumerator QuitRoutine()
    {
        fadeImage.gameObject.SetActive(true);
        yield return fadeImage.DOFade(1f, fadeDuration).WaitForCompletion();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
