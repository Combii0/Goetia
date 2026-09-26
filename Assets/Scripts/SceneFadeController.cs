using System.Collections;
using UnityEngine;

/// <summary>
/// Scene-local fade controlled by a CanvasGroup assigned in the Inspector.
/// </summary>
public sealed class SceneFadeController : MonoBehaviour
{
    public CanvasGroup fadeCanvasGroup;
    public float fadeInDuration = 0.65f;
    public float fadeOutDuration = 0.5f;

    private void Start()
    {
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        if(fadeCanvasGroup == null)
        {
            yield break;
        }

        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.blocksRaycasts = true;

        yield return FadeTo(0f, fadeInDuration);
    }

    public IEnumerator FadeOut()
    {
        if(fadeCanvasGroup == null)
        {
            yield break;
        }

        fadeCanvasGroup.blocksRaycasts = true;
        yield return FadeTo(1f, fadeOutDuration);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;

        while(elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
        fadeCanvasGroup.blocksRaycasts = targetAlpha > 0.001f;
    }
}
