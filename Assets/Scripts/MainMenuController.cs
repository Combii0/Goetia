using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    public TMP_Text pressAnyButtonText;
    public MainMenuSaveSlotsUI saveSlotsUI;
    public CanvasGroup fadeCanvasGroup;
    public float fadeInDuration = 0.8f;

    [Header("Gobbo")]
    public GobboController gobboController;

    private bool menuStarted;
    public Color initialColor;

    public bool gobbo;

    void Awake()
    {
        menuStarted = false;
        pressAnyButtonText.color = initialColor;

        if (gobboController == null)
        {
            gobboController = FindAnyObjectByType<GobboController>(FindObjectsInactive.Include);
        }
    }

    void Start()
    {
        if(fadeCanvasGroup != null)
        {
            StartCoroutine(FadeIn());
        }
    }

    IEnumerator FadeIn()
    {
        float elapsed = 0f;
        float startingAlpha = fadeCanvasGroup.alpha;

        fadeCanvasGroup.blocksRaycasts = true;

        while(elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startingAlpha, 0f, Mathf.Clamp01(elapsed / fadeInDuration));
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    void Update()
    {
        if(!menuStarted)
        {
            if(Keyboard.current.anyKey.wasPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame)
            {
                menuStarted = true;
                SceneMusicManager.PlayUiSelectionSfx();
                StartCoroutine(PressAnimation());
            }
        }
    }

    IEnumerator PressAnimation()
    {
        pressAnyButtonText.color = Color.yellow;
        yield return new WaitForSeconds(0.1f);

        pressAnyButtonText.color = new Color(1f, 0.3f, 0.7f);
        yield return new WaitForSeconds(0.1f);

        pressAnyButtonText.color = Color.white;
        yield return new WaitForSeconds(0.1f);

        pressAnyButtonText.gameObject.SetActive(false);

        gobbo = true;
        if (gobboController == null)
        {
            gobboController = FindAnyObjectByType<GobboController>(FindObjectsInactive.Include);
        }

        if (gobboController != null)
        {
            gobboController.ShowGobbo();
        }
        else
        {
            Debug.LogWarning("GobboController no está en la escena Main Menu.");
        }

        saveSlotsUI.Open();
    }
}
