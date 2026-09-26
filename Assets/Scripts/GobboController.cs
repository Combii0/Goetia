using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GobboController : MonoBehaviour
{
    [Header("References")]
    public GameObject gobbo;

    [Header("Audio")]
    public AudioClip appearanceSfx;
    [Range(0f, 1f)] public float appearanceSfxVolume = 1f;

    [Header("Timing")]
    [Min(0f)] public float visibleDuration = 1f;

    private Coroutine hideRoutine;
    private GameObject overlayRoot;

    private void Awake()
    {
        if (gobbo == null)
        {
            gobbo = gameObject;
        }

        CreateTopmostOverlay();
        gobbo.SetActive(false);
    }

    private void CreateTopmostOverlay()
    {
        if (overlayRoot != null)
        {
            return;
        }

        overlayRoot = new GameObject("GobboTopmostOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));

        Canvas overlayCanvas = overlayRoot.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = overlayRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform gobboRect = gobbo.GetComponent<RectTransform>();
        if (gobboRect != null)
        {
            gobboRect.SetParent(overlayRoot.transform, false);
            gobboRect.SetAsLastSibling();
        }

        Graphic graphic = gobbo.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.raycastTarget = false;
        }
    }

    /// <summary>Called by MainMenuManager as soon as the player presses any button.</summary>
    public void ShowGobbo()
    {
        if (gobbo == null)
        {
            gobbo = gameObject;
        }

        gobbo.SetActive(true);

        if (appearanceSfx != null)
        {
            SceneMusicManager.PlayOneShotSfx(appearanceSfx, appearanceSfxVolume);
        }

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        hideRoutine = StartCoroutine(HideGobbo());
    }

    private IEnumerator HideGobbo()
    {
        yield return new WaitForSeconds(visibleDuration);
        gobbo.SetActive(false);
        hideRoutine = null;
    }
}
