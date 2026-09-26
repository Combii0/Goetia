using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Creates one persistent full-screen fade layer and applies it to every scene.
/// SceneTransitionFader.LoadScene should be used for transitions initiated by gameplay/UI.
/// </summary>
public sealed class SceneTransitionFader : MonoBehaviour
{
    private static SceneTransitionFader instance;
    private CanvasGroup fadeGroup;
    private Image fadeImage;
    private Coroutine fadeRoutine;

    [SerializeField] private float fadeInDuration = 0.65f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateAtStartup()
    {
        EnsureInstance();
    }

    private static SceneTransitionFader EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject root = new GameObject("SceneTransitionFader");
        instance = root.AddComponent<SceneTransitionFader>();
        DontDestroyOnLoad(root);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildFadeLayer();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        if(SceneManager.GetActiveScene().name == "Main Menu" || FindAnyObjectByType<SceneFadeController>() != null)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
        }
        else
        {
            StartFadeIn();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void BuildFadeLayer()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        gameObject.AddComponent<CanvasScaler>();

        fadeGroup = gameObject.AddComponent<CanvasGroup>();
        fadeGroup.blocksRaycasts = true;
        fadeGroup.interactable = false;

        GameObject imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(transform, false);

        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeImage = imageObject.AddComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = true;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == "Main Menu" || FindAnyObjectByType<SceneFadeController>() != null)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            return;
        }

        StartFadeIn();
    }

    private void StartFadeIn()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeTo(0f, fadeInDuration));
    }

    public static void LoadScene(string sceneName)
    {
        SceneTransitionFader fader = EnsureInstance();

        SceneFadeController localFade = FindAnyObjectByType<SceneFadeController>();

        if(localFade != null)
        {
            if(fader.fadeRoutine != null)
            {
                fader.StopCoroutine(fader.fadeRoutine);
            }

            fader.fadeRoutine = fader.StartCoroutine(fader.FadeOutLocalAndLoad(localFade, sceneName));
            return;
        }

        if (fader.fadeRoutine != null)
        {
            fader.StopCoroutine(fader.fadeRoutine);
        }

        fader.fadeRoutine = fader.StartCoroutine(fader.FadeOutAndLoad(sceneName));
    }

    private IEnumerator FadeOutLocalAndLoad(SceneFadeController localFade, string sceneName)
    {
        yield return localFade.FadeOut();
        SceneManager.LoadScene(sceneName);
        fadeRoutine = null;
    }

    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        yield return FadeTo(1f, fadeOutDuration);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeGroup == null)
        {
            yield break;
        }

        float startAlpha = fadeGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        fadeGroup.alpha = targetAlpha;
        fadeGroup.blocksRaycasts = targetAlpha > 0.001f;
        fadeRoutine = null;
    }
}
