using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Runtime pause menu for Room. Q and Escape toggle it.</summary>
public sealed class PauseMenuController : MonoBehaviour
{
    private const string VolumeKey = "Goetia_MasterVolume";
    private const int CanvasOrder = 3000;

    // Matches MainMenuSaveSlotsUI, the visual source of truth for Goetia menus.
    private readonly Color overlayColor = new Color(0.025f, 0f, 0.055f, 0.62f);
    private readonly Color panelColor = new Color(0.14f, 0.025f, 0.19f, 0.98f);
    private readonly Color accentColor = new Color(0.86f, 0.28f, 1f, 1f);
    private readonly Color buttonColor = new Color(0.43f, 0.10f, 0.58f, 1f);
    private readonly Color buttonHoverColor = new Color(0.67f, 0.22f, 0.82f, 1f);
    private readonly Color subtitleColor = new Color(0.96f, 0.76f, 1f, 0.92f);

    private GameObject root;
    private GameObject mainPanel;
    private GameObject settingsPanel;
    private CanvasGroup mainPanelGroup;
    private CanvasGroup settingsPanelGroup;
    private Slider volumeSlider;
    private TMP_Text volumeValue;
    private PlayerController player;
    private TMP_FontAsset menuFont;
    private bool playerWasEnabled;
    private bool paused;
    private Coroutine panelAnimation;

    public static void EnsureForRoom()
    {
        if(SceneManager.GetActiveScene().name != "Room" || FindAnyObjectByType<PauseMenuController>() != null) return;
        new GameObject("PauseMenuController").AddComponent<PauseMenuController>();
    }

    private void Awake()
    {
        player = FindAnyObjectByType<PlayerController>();
        RoomRoundManager roomManager = FindAnyObjectByType<RoomRoundManager>();
        menuFont = roomManager != null ? roomManager.pixelFontAsset : TMP_Settings.defaultFontAsset;
        BuildUi();
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
    }

    private void Update()
    {
        if(Keyboard.current == null) return;
        if(Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if(!paused) Pause();
            else if(settingsPanel.activeSelf) ShowMain();
            else Continue();
        }
    }

    private void Pause()
    {
        if(paused || Time.timeScale == 0f) return;

        player = player != null ? player : FindAnyObjectByType<PlayerController>();
        if(player != null)
        {
            playerWasEnabled = player.enabled;
            player.enabled = false;
            if(player.rb != null) player.rb.linearVelocity = Vector2.zero;
        }

        paused = true;
        Time.timeScale = 0f;
        root.SetActive(true);
        ShowMain();
        Cursor.visible = true;
    }

    public void Continue()
    {
        if(!paused) return;

        root.SetActive(false);
        if(player != null) player.enabled = playerWasEnabled;
        Time.timeScale = 1f;
        paused = false;
    }

    private void ShowSettings()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
        AnimatePanel(settingsPanel.GetComponent<RectTransform>(), settingsPanelGroup);
    }

    private void ShowMain()
    {
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        AnimatePanel(mainPanel.GetComponent<RectTransform>(), mainPanelGroup);
    }

    private void AnimatePanel(RectTransform panel, CanvasGroup group)
    {
        if(panelAnimation != null) StopCoroutine(panelAnimation);
        panelAnimation = StartCoroutine(AnimatePanelRoutine(panel, group));
    }

    private System.Collections.IEnumerator AnimatePanelRoutine(RectTransform panel, CanvasGroup group)
    {
        const float duration = 0.18f;
        float elapsed = 0f;
        panel.localScale = Vector3.one * 0.92f;
        group.alpha = 0f;

        while(elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            panel.localScale = Vector3.LerpUnclamped(Vector3.one * 0.92f, Vector3.one, eased);
            group.alpha = eased;
            yield return null;
        }

        panel.localScale = Vector3.one;
        group.alpha = 1f;
        panelAnimation = null;
    }

    private void SetVolume(float volume)
    {
        float clamped = Mathf.Clamp01(volume);
        AudioListener.volume = clamped;
        PlayerPrefs.SetFloat(VolumeKey, clamped);
        PlayerPrefs.Save();
        if(volumeValue != null) volumeValue.text = Mathf.RoundToInt(clamped * 100f) + "%";
    }

    private void ReturnToMainMenu()
    {
        Continue();
        MainMenuSaveSlotsUI.SaveCurrentSlot("Room");
        SceneTransitionFader.LoadScene("Main Menu");
    }

    private void CloseGame()
    {
        Continue();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BuildUi()
    {
        EnsureEventSystem();

        root = new GameObject("PauseMenu");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasOrder;
        root.AddComponent<GraphicRaycaster>();

        Image overlay = root.AddComponent<Image>();
        overlay.color = overlayColor;
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;

        mainPanel = CreatePanel(root.transform, "PauseMainPanel");
        mainPanelGroup = mainPanel.AddComponent<CanvasGroup>();
        AddPanelDecorations(mainPanel.transform);
        CreateTitle(mainPanel.transform, "PAUSED");
        CreateSubtitle(mainPanel.transform, "THE RITUAL WAITS");
        CreateMenuButton(mainPanel.transform, "CONTINUE", 0.56f, Continue, buttonColor);
        CreateMenuButton(mainPanel.transform, "SETTINGS", 0.43f, ShowSettings, buttonColor);
        CreateMenuButton(mainPanel.transform, "MAIN MENU", 0.30f, ReturnToMainMenu, buttonColor);
        CreateMenuButton(mainPanel.transform, "CLOSE GAME", 0.17f, CloseGame, new Color(0.70f, 0.14f, 0.32f, 1f));

        settingsPanel = CreatePanel(root.transform, "PauseSettingsPanel");
        settingsPanelGroup = settingsPanel.AddComponent<CanvasGroup>();
        AddPanelDecorations(settingsPanel.transform);
        CreateTitle(settingsPanel.transform, "SETTINGS");
        CreateSubtitle(settingsPanel.transform, "ADJUST THE RITUAL");
        TMP_Text volumeLabel = CreateText(settingsPanel.transform, "GENERAL VOLUME", 26f, Color.white);
        SetAnchored(volumeLabel.rectTransform, new Vector2(0.5f, 0.54f), new Vector2(460f, 42f));

        volumeSlider = CreateSlider(settingsPanel.transform);
        SetAnchored(volumeSlider.GetComponent<RectTransform>(), new Vector2(0.5f, 0.42f), new Vector2(360f, 14f));
        float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        volumeSlider.SetValueWithoutNotify(savedVolume);
        volumeSlider.onValueChanged.AddListener(SetVolume);

        volumeValue = CreateText(settingsPanel.transform, Mathf.RoundToInt(savedVolume * 100f) + "%", 24f, accentColor);
        SetAnchored(volumeValue.rectTransform, new Vector2(0.5f, 0.32f), new Vector2(160f, 40f));
        CreateMenuButton(settingsPanel.transform, "BACK", 0.17f, ShowMain, buttonColor);

        root.SetActive(false);
    }

    private GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        Image image = panel.GetComponent<Image>();
        image.color = panelColor;
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = accentColor;
        outline.effectDistance = new Vector2(3f, -3f);
        Shadow shadow = panel.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.16f, 0.02f, 0.30f, 0.95f);
        shadow.effectDistance = new Vector2(12f, -12f);
        SetAnchored(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(620f, 430f));
        return panel;
    }

    private void CreateTitle(Transform parent, string title)
    {
        TMP_Text text = CreateText(parent, title, 42f, Color.white);
        text.fontStyle = FontStyles.Bold;
        SetAnchored(text.rectTransform, new Vector2(0.5f, 0.77f), new Vector2(560f, 68f));
    }

    private void CreateSubtitle(Transform parent, string subtitle)
    {
        TMP_Text text = CreateText(parent, subtitle, 14f, subtitleColor);
        SetAnchored(text.rectTransform, new Vector2(0.5f, 0.67f), new Vector2(540f, 28f));
    }

    private void AddPanelDecorations(Transform parent)
    {
        CreateDecorationLine(parent, new Vector2(0.5f, 0.91f), 3f);
        CreateDecorationLine(parent, new Vector2(0.5f, 0.09f), 2f);
    }

    private void CreateDecorationLine(Transform parent, Vector2 anchor, float height)
    {
        GameObject line = new GameObject("DecorationLine", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(parent, false);
        line.GetComponent<Image>().color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.82f);
        SetAnchored(line.GetComponent<RectTransform>(), anchor, new Vector2(510f, height));
    }

    private void CreateMenuButton(Transform parent, string label, float y, UnityEngine.Events.UnityAction action, Color color)
    {
        GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        button.colors = colors;
        button.onClick.AddListener(action);
        buttonObject.AddComponent<PauseMenuButtonHover>().Configure(image, color, label == "CLOSE GAME" ? new Color(0.78f, 0.14f, 0.34f, 1f) : buttonHoverColor);
        SetAnchored(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, y), new Vector2(320f, 48f));

        TMP_Text text = CreateText(buttonObject.transform, label, 28f, Color.white);
        text.raycastTarget = false;
        SetAnchored(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(300f, 42f));
    }

    private Slider CreateSlider(Transform parent)
    {
        GameObject sliderObject = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;

        Image background = sliderObject.AddComponent<Image>();
        background.color = new Color(0.07f, 0.01f, 0.1f, 1f);
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(8f, 0f);
        fillAreaRect.offsetMax = new Vector2(-8f, 0f);
        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = accentColor;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        slider.fillRect = fillRect;

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(sliderObject.transform, false);
        handle.GetComponent<Image>().color = Color.white;
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(14f, 26f);
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private TMP_Text CreateText(Transform parent, string content, float size, Color color)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = menuFont != null ? menuFont : TMP_Settings.defaultFontAsset;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }

    private static void EnsureEventSystem()
    {
        if(EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
        // RoomRoundManager normally creates the Input System module before this
        // menu. This is only a defensive fallback for an otherwise empty scene.
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
        DontDestroyOnLoad(eventSystem);
    }
}

/// <summary>Small unscaled hover lift used by the generated pause-menu buttons.</summary>
public sealed class PauseMenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image image;
    private Color normalColor;
    private Color hoverColor;
    private bool hovered;

    public void Configure(Image targetImage, Color normal, Color hover)
    {
        image = targetImage;
        normalColor = normal;
        hoverColor = hover;
    }

    private void Update()
    {
        float t = 1f - Mathf.Exp(-15f * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, hovered ? Vector3.one * 1.055f : Vector3.one, t);
        if(image != null) image.color = Color.Lerp(image.color, hovered ? hoverColor : normalColor, t);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
    }
}
