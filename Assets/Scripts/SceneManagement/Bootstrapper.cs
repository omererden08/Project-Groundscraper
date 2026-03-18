using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrapper : MonoBehaviour
{
    [Header("First Scene To Load (Additive)")]
    [SerializeField] private string startSceneName = "MainMenu";

    private static bool _initialized;

    private void Awake()
    {
        // Çift bootstrap oluþmasýný engelle
        if (_initialized)
        {
            Destroy(gameObject);
            return;
        }

        _initialized = true;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Eðer sadece Bootstrap sahnesi açýksa baþlangýç sahnesini yükle
        if (SceneManager.sceneCount == 1)
        {
            StartCoroutine(LoadStartScene());
        }
    }

    private IEnumerator LoadStartScene()
    {
        AsyncOperation loadOp =
            SceneManager.LoadSceneAsync(startSceneName, LoadSceneMode.Additive);

        while (!loadOp.isDone)
            yield return null;

        Scene loadedScene = SceneManager.GetSceneByName(startSceneName);
        SceneManager.SetActiveScene(loadedScene);
    }
}