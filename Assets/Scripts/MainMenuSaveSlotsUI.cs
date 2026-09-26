using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuSaveSlotsUI : MonoBehaviour
{
    private const int SlotCount = 3;
    private const float OpenAnimationDuration = 0.34f;
    private const float CardAnimationDuration = 0.22f;
    private const float CardStaggerDelay = 0.06f;
    private const float PopupAnimationDuration = 0.18f;

    [Header("Colors")]
    public Color OverlayColor = new Color(0.01f, 0.02f, 0.05f, 0.93f);
    public Color BackdropGlowColor = new Color(0.07f, 0.19f, 0.31f, 0.42f);
    public Color PanelColor = new Color(0.08f, 0.12f, 0.20f, 0.985f);
    public Color PanelAccentColor = new Color(0.47f, 0.82f, 1f, 1f);
    public Color PanelAccentPulseColor = new Color(0.75f, 0.94f, 1f, 1f);

    public Color SlotCardColor = new Color(0.16f, 0.21f, 0.33f, 0.98f);
    public Color SlotInfoColor = new Color(0.06f, 0.09f, 0.16f, 0.96f);

    public Color SlotButtonColor = new Color(0.44f, 0.78f, 0.98f, 1f);
    public Color SlotButtonHoverColor = new Color(0.21f, 0.56f, 0.83f, 1f);

    public Color DeleteButtonColor = new Color(0.94f, 0.60f, 0.39f, 1f);
    public Color DeleteButtonHoverColor = new Color(0.84f, 0.32f, 0.22f, 1f);

    public Color EmptySlotColor = new Color(0.38f, 0.46f, 0.58f, 1f);
    public Color EmptySlotTextColor = new Color(0.85f, 0.91f, 0.98f, 0.92f);

    public Color ActiveStatusColor = new Color(0.54f, 0.97f, 0.78f, 1f);

    public Color TextColor = Color.white;
    public Color SubtitleColor = new Color(0.84f, 0.93f, 1f, 0.94f);

    public Color PopupColor = new Color(0.10f, 0.14f, 0.23f, 0.99f);
    public Color ModalBackgroundColor = new Color(0f, 0f, 0f, 0.56f);

    public Color FadeColor = Color.black;

    [Header("Style")]
    [SerializeField] private TMP_FontAsset pixelFontAsset;

    [Header("Scenes")]
    [SerializeField] private string transitionScene = "Shufle";

    [Header("Transition")]
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("SFX")]
    [SerializeField] private AudioClip openSfx;
    [SerializeField] private AudioClip buttonSfx;
    [SerializeField] private AudioClip slotConfirmSfx;

    [SerializeField, Range(0f, 1f)] private float openSfxVolume = 0.85f;
    [SerializeField, Range(0f, 1f)] private float buttonSfxVolume = 0.78f;
    [SerializeField, Range(0f, 1f)] private float slotConfirmSfxVolume = 0.92f;

    private Canvas canvas;
    private CanvasGroup rootCanvasGroup;

    private Image overlayImage;

    private RectTransform ambientGlowRect;
    private Image ambientGlowImage;

    private GameObject root;

    private RectTransform panelRect;
    private CanvasGroup panelCanvasGroup;
    private Image panelAccentImage;

    private GameObject modalBlocker;
    private CanvasGroup modalBlockerCanvasGroup;

    private GameObject deletePopup;
    private RectTransform deletePopupRect;
    private CanvasGroup deletePopupCanvasGroup;
    private TextMeshProUGUI deletePopupText;
    private Button confirmDeleteButton;

    private TextMeshProUGUI footerHintText;

    private GameObject fadeObject;
    private CanvasGroup fadeCanvasGroup;

    private bool isOpen;
    private bool isLoading;

    private int pendingDeleteSlotIndex = -1;

    private Coroutine introRoutine;
    private Coroutine popupRoutine;

    private readonly SlotWidgets[] slotWidgets = new SlotWidgets[SlotCount];

    public static int CurrentSlot
    {
        get
        {
            return PlayerPrefs.GetInt("Goetia_CurrentSlot", -1);
        }
    }

    private struct SlotWidgets
    {
        public Button primaryButton;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI infoText;
        public TextMeshProUGUI actionText;
        public Button deleteButton;
        public Image deleteButtonImage;
        public RectTransform cardRect;
        public CanvasGroup cardCanvasGroup;
    }

    void Awake()
    {
        LoadFont();
        EnsureEventSystemExists();
        EnsureUi();
        SetVisible(false);
    }

    void Update()
    {
        if(!isOpen || panelAccentImage == null)
        {
            return;
        }

        float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 2.1f) * 0.5f;
        panelAccentImage.color = Color.Lerp(PanelAccentColor, PanelAccentPulseColor, pulse * 0.35f);

        if(ambientGlowRect != null)
        {
            float glowPulse = 1f + Mathf.Sin(Time.unscaledTime * 1.35f) * 0.025f;
            ambientGlowRect.localScale = new Vector3(glowPulse, glowPulse, 1f);
        }

        if(ambientGlowImage != null)
        {
            float alphaPulse = 0.32f + Mathf.Sin(Time.unscaledTime * 1.8f) * 0.05f;
            ambientGlowImage.color = new Color(BackdropGlowColor.r, BackdropGlowColor.g, BackdropGlowColor.b, alphaPulse);
        }
    }

    public void Open()
    {
        LoadFont();
        EnsureEventSystemExists();
        EnsureUi();

        RefreshSlots();
        SetVisible(true);
    }

    private void LoadFont()
    {
        if(pixelFontAsset == null)
        {
            pixelFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/BoldPixels SDF");
        }

        if(pixelFontAsset == null)
        {
            pixelFontAsset = TMP_Settings.defaultFontAsset;
        }
    }

    private void EnsureUi()
    {
        if(root != null)
        {
            return;
        }

        root = CreateUiObject("SaveSlotsCanvas", transform);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3500;
        canvas.pixelPerfect = true;

        root.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;
        scaler.dynamicPixelsPerUnit = 10f;

        rootCanvasGroup = root.AddComponent<CanvasGroup>();

        overlayImage = root.AddComponent<Image>();
        overlayImage.color = OverlayColor;

        GameObject ambientGlow = CreateUiObject("AmbientGlow", root.transform);

        ambientGlowRect = ambientGlow.GetComponent<RectTransform>();
        ambientGlowRect.anchorMin = new Vector2(0.5f, 0.5f);
        ambientGlowRect.anchorMax = new Vector2(0.5f, 0.5f);
        ambientGlowRect.pivot = new Vector2(0.5f, 0.5f);
        ambientGlowRect.sizeDelta = new Vector2(1780f, 920f);

        ambientGlowImage = ambientGlow.AddComponent<Image>();
        ambientGlowImage.color = BackdropGlowColor;

        GameObject panel = CreateUiObject("Panel", root.transform);

        panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1660f, 860f);

        panelCanvasGroup = panel.AddComponent<CanvasGroup>();

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = PanelColor;

        Outline panelOutline = panel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.7f, 0.93f, 1f, 0.18f);
        panelOutline.effectDistance = new Vector2(7f, -7f);

        Shadow panelShadow = panel.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.38f);
        panelShadow.effectDistance = new Vector2(0f, -14f);

        GameObject accent = CreateUiObject("Accent", panel.transform);

        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.sizeDelta = new Vector2(0f, 20f);

        panelAccentImage = accent.AddComponent<Image>();
        panelAccentImage.color = PanelAccentColor;

        GameObject content = CreateUiObject("Content", panel.transform);

        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(60f, 54f);
        contentRect.offsetMax = new Vector2(-60f, -54f);

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 22f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        CreateTitle(content.transform, "CHOOSE A FILE", 58f);
        CreateSubtitle(content.transform, "Select a save slot.");

        GameObject slotsRow = CreateUiObject("SlotsRow", content.transform);

        LayoutElement slotsLayout = slotsRow.AddComponent<LayoutElement>();
        slotsLayout.preferredHeight = 560f;

        HorizontalLayoutGroup slotsGroup = slotsRow.AddComponent<HorizontalLayoutGroup>();
        slotsGroup.spacing = 22f;
        slotsGroup.childAlignment = TextAnchor.MiddleCenter;
        slotsGroup.childControlWidth = true;
        slotsGroup.childControlHeight = true;
        slotsGroup.childForceExpandWidth = true;
        slotsGroup.childForceExpandHeight = false;

        for(int i = 0; i < slotWidgets.Length; i++)
        {
            slotWidgets[i] = BuildSlotCard(slotsRow.transform, i);
        }

        footerHintText = CreateSubtitle(content.transform, "Select a file to begin.");
        footerHintText.fontSize = 22f;
        footerHintText.alignment = TextAlignmentOptions.Center;

        BuildDeletePopup();
        BuildFade();
    }

    private SlotWidgets BuildSlotCard(Transform parent, int slotIndex)
    {
        GameObject card = CreateUiObject("Slot" + (slotIndex + 1), parent);

        RectTransform cardRect = card.GetComponent<RectTransform>();

        LayoutElement cardLayout = card.AddComponent<LayoutElement>();
        cardLayout.preferredWidth = 0f;
        cardLayout.flexibleWidth = 1f;
        cardLayout.preferredHeight = 540f;

        CanvasGroup cardCanvasGroup = card.AddComponent<CanvasGroup>();

        Image cardImage = card.AddComponent<Image>();
        cardImage.color = SlotCardColor;

        Outline cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0f, 0f, 0f, 0.34f);
        cardOutline.effectDistance = new Vector2(5f, -5f);

        Shadow cardShadow = card.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
        cardShadow.effectDistance = new Vector2(0f, -10f);

        VerticalLayoutGroup cardLayoutGroup = card.AddComponent<VerticalLayoutGroup>();
        cardLayoutGroup.padding = new RectOffset(26, 26, 26, 24);
        cardLayoutGroup.spacing = 18f;
        cardLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        cardLayoutGroup.childControlWidth = true;
        cardLayoutGroup.childControlHeight = true;
        cardLayoutGroup.childForceExpandWidth = true;
        cardLayoutGroup.childForceExpandHeight = false;

        GameObject headerRow = CreateUiObject("HeaderRow", card.transform);

        HorizontalLayoutGroup headerLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 12f;
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = true;
        headerLayout.childForceExpandHeight = false;

        TextMeshProUGUI titleText = CreateText(headerRow.transform, "Title", $"FILE {slotIndex + 1}", 34f, TextColor, TextAlignmentOptions.Left);
        titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 3f;

        TextMeshProUGUI statusText = CreateText(headerRow.transform, "Status", "EMPTY", 22f, EmptySlotColor, TextAlignmentOptions.Right);
        statusText.fontStyle = FontStyles.Bold;
        statusText.characterSpacing = 3f;

        GameObject infoPanel = CreateUiObject("InfoPanel", card.transform);

        LayoutElement infoLayout = infoPanel.AddComponent<LayoutElement>();
        infoLayout.preferredHeight = 294f;

        Image infoImage = infoPanel.AddComponent<Image>();
        infoImage.color = SlotInfoColor;

        Outline infoOutline = infoPanel.AddComponent<Outline>();
        infoOutline.effectColor = new Color(0.33f, 0.48f, 0.67f, 0.18f);
        infoOutline.effectDistance = new Vector2(2f, -2f);

        TextMeshProUGUI infoText = CreateText(infoPanel.transform, "Info", "", 23f, EmptySlotTextColor, TextAlignmentOptions.Center);

        RectTransform infoRect = infoText.rectTransform;
        infoRect.anchorMin = Vector2.zero;
        infoRect.anchorMax = Vector2.one;
        infoRect.offsetMin = new Vector2(26f, 24f);
        infoRect.offsetMax = new Vector2(-26f, -24f);

        infoText.lineSpacing = 12f;
        infoText.characterSpacing = 1.3f;
        infoText.textWrappingMode = TextWrappingModes.Normal;

        Button primaryButton = CreateButton(card.transform, "ContinueButton", "START", SlotButtonColor, SlotButtonHoverColor, 76f, 26f, () => HandleSlotSelected(slotIndex));
        TextMeshProUGUI actionText = primaryButton.GetComponentInChildren<TextMeshProUGUI>();

        Button deleteButton = CreateButton(card.transform, "DeleteButton", "DELETE", DeleteButtonColor, DeleteButtonHoverColor, 62f, 22f, () => BeginDelete(slotIndex));
        Image deleteButtonImage = deleteButton.GetComponent<Image>();

        return new SlotWidgets
        {
            primaryButton = primaryButton,
            titleText = titleText,
            statusText = statusText,
            infoText = infoText,
            actionText = actionText,
            deleteButton = deleteButton,
            deleteButtonImage = deleteButtonImage,
            cardRect = cardRect,
            cardCanvasGroup = cardCanvasGroup
        };
    }

    private void RefreshSlots()
    {
        for(int i = 0; i < slotWidgets.Length; i++)
        {
            bool hasData = SlotExists(i);
            SlotWidgets widgets = slotWidgets[i];

            widgets.titleText.text = $"FILE {i + 1}";
            widgets.primaryButton.interactable = true;
            widgets.deleteButton.interactable = hasData;
            widgets.actionText.text = hasData ? "CONTINUE" : "START";

            if(hasData)
            {
                widgets.statusText.text = "ACTIVE";
                widgets.statusText.color = ActiveStatusColor;

                string lastScene = PlayerPrefs.GetString(GetKey(i, "Scene"), "ROOM");
                string lastSave = PlayerPrefs.GetString(GetKey(i, "LastSave"), "UNKNOWN");
                int roundNumber = Mathf.Max(1, PlayerPrefs.GetInt(GetKey(i, "RoundNumber"), 1));

                widgets.infoText.color = TextColor;
                widgets.infoText.text = $"AREA\n{lastScene.ToUpperInvariant()}\n\nROUND {roundNumber}\n\nLAST SAVE\n{lastSave.ToUpperInvariant()}";
            }
            else
            {
                widgets.statusText.text = "EMPTY";
                widgets.statusText.color = EmptySlotColor;

                widgets.infoText.color = EmptySlotTextColor;
                widgets.infoText.text = "EMPTY FILE\n\nStart a new journey.";
            }

            if(widgets.deleteButtonImage != null)
            {
                widgets.deleteButtonImage.color = hasData ? DeleteButtonColor : new Color(EmptySlotColor.r, EmptySlotColor.g, EmptySlotColor.b, 0.7f);
            }
        }

        if(footerHintText != null)
        {
            footerHintText.text = "Select a file to begin.";
        }
    }

    private void HandleSlotSelected(int slotIndex)
    {
        if(isLoading)
        {
            return;
        }

        if(!SlotExists(slotIndex))
        {
            CreateSlot(slotIndex);
        }

        PlayerPrefs.SetInt("Goetia_CurrentSlot", slotIndex);
        PlayerPrefs.Save();

        PlayUiSfx(slotConfirmSfx != null ? slotConfirmSfx : buttonSfx, slotConfirmSfx != null ? slotConfirmSfxVolume : buttonSfxVolume);

        StartCoroutine(FadeAndLoadScene());
    }

    private void CreateSlot(int slotIndex)
    {
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "SelectedCard"));
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "SelectedCards"));
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "SelectionPending"));
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "RoundNumber"));
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "CardHistoryFormat"));
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "BloodSelections"));
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "ActiveRitual"));
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "LastSelectionWasRitual"));
        PlayerPrefs.SetInt(GetKey(slotIndex, "Exists"), 1);
        PlayerPrefs.SetString(GetKey(slotIndex, "Scene"), "Room");
        PlayerPrefs.SetString(GetKey(slotIndex, "LastSave"), DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        PlayerPrefs.Save();
    }

    public static void SaveCurrentSlot(string currentScene)
    {
        int slotIndex = CurrentSlot;

        if(slotIndex < 0)
        {
            return;
        }

        PlayerPrefs.SetInt(GetKey(slotIndex, "Exists"), 1);
        PlayerPrefs.SetString(GetKey(slotIndex, "Scene"), currentScene);
        PlayerPrefs.SetString(GetKey(slotIndex, "LastSave"), DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        PlayerPrefs.Save();
    }

    private bool SlotExists(int slotIndex)
    {
        return PlayerPrefs.GetInt(GetKey(slotIndex, "Exists"), 0) == 1;
    }

    private static string GetKey(int slotIndex, string value)
    {
        return "Goetia_Slot_" + slotIndex + "_" + value;
    }

    private void BeginDelete(int slotIndex)
    {
        pendingDeleteSlotIndex = slotIndex;

        if(deletePopupText != null)
        {
            deletePopupText.text = $"Delete FILE {slotIndex + 1}?\nThis save slot will be erased.";
        }

        PlayUiSfx(buttonSfx, buttonSfxVolume);

        ShowDeletePopup();
    }

    private void ConfirmDelete()
    {
        if(pendingDeleteSlotIndex >= 0)
        {
            DeleteSlot(pendingDeleteSlotIndex);
        }

        pendingDeleteSlotIndex = -1;

        PlayUiSfx(buttonSfx, buttonSfxVolume);

        HideDeletePopup();
        RefreshSlots();
    }

    private void DeleteSlot(int slotIndex)
    {
        EraseSaveSlot(slotIndex);
    }

    /// <summary>Erases every persisted value belonging to a run, including its active ritual.</summary>
    public static void EraseSaveSlot(int slotIndex)
    {
        if(slotIndex < 0)
        {
            return;
        }

        PlayerPrefs.DeleteKey(GetKey(slotIndex, "Exists"));
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "Scene"));
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "LastSave"));
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "SelectedCard"));
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "SelectedCards"));
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "SelectionPending"));
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "RoundNumber"));
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "CardHistoryFormat"));
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "BloodSelections"));
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "ActiveRitual"));
        PlayerPrefs.DeleteKey(GetKey(slotIndex, "LastSelectionWasRitual"));

        if(CurrentSlot == slotIndex)
        {
            PlayerPrefs.DeleteKey("Goetia_CurrentSlot");
        }

        PlayerPrefs.Save();
    }

    private IEnumerator FadeAndLoadScene()
    {
        isLoading = true;
        rootCanvasGroup.interactable = false;

        fadeObject.SetActive(true);
        fadeCanvasGroup.alpha = 0f;

        float elapsed = 0f;

        while(elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;

        SceneTransitionFader.LoadScene(transitionScene);
    }

    private void BuildFade()
    {
        fadeObject = CreateUiObject("Fade", root.transform);

        RectTransform fadeRect = fadeObject.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        Image fadeImage = fadeObject.AddComponent<Image>();
        fadeImage.color = FadeColor;

        fadeCanvasGroup = fadeObject.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = true;

        fadeObject.SetActive(false);
    }

    private void BuildDeletePopup()
    {
        modalBlocker = CreateUiObject("ModalBlocker", root.transform);

        RectTransform blockerRect = modalBlocker.GetComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;

        modalBlockerCanvasGroup = modalBlocker.AddComponent<CanvasGroup>();

        Image blockerImage = modalBlocker.AddComponent<Image>();
        blockerImage.color = ModalBackgroundColor;

        Button blockerButton = modalBlocker.AddComponent<Button>();
        blockerButton.onClick.AddListener(CancelDelete);

        deletePopup = CreateUiObject("DeletePopup", root.transform);

        deletePopupRect = deletePopup.GetComponent<RectTransform>();
        deletePopupRect.anchorMin = new Vector2(0.5f, 0.5f);
        deletePopupRect.anchorMax = new Vector2(0.5f, 0.5f);
        deletePopupRect.pivot = new Vector2(0.5f, 0.5f);
        deletePopupRect.sizeDelta = new Vector2(700f, 330f);

        deletePopupCanvasGroup = deletePopup.AddComponent<CanvasGroup>();

        Image popupImage = deletePopup.AddComponent<Image>();
        popupImage.color = PopupColor;

        Outline popupOutline = deletePopup.AddComponent<Outline>();
        popupOutline.effectColor = new Color(0.72f, 0.92f, 1f, 0.18f);
        popupOutline.effectDistance = new Vector2(5f, -5f);

        Shadow popupShadow = deletePopup.AddComponent<Shadow>();
        popupShadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
        popupShadow.effectDistance = new Vector2(0f, -12f);

        VerticalLayoutGroup popupLayout = deletePopup.AddComponent<VerticalLayoutGroup>();
        popupLayout.padding = new RectOffset(34, 34, 32, 32);
        popupLayout.spacing = 18f;
        popupLayout.childAlignment = TextAnchor.UpperCenter;
        popupLayout.childControlWidth = true;
        popupLayout.childControlHeight = true;
        popupLayout.childForceExpandWidth = true;
        popupLayout.childForceExpandHeight = false;

        CreateTitle(deletePopup.transform, "DELETE FILE?", 38f);

        deletePopupText = CreateSubtitle(deletePopup.transform, "This action cannot be undone.");
        deletePopupText.alignment = TextAlignmentOptions.Center;
        deletePopupText.fontSize = 24f;
        deletePopupText.textWrappingMode = TextWrappingModes.Normal;

        LayoutElement popupTextLayout = deletePopupText.gameObject.AddComponent<LayoutElement>();
        popupTextLayout.preferredHeight = 92f;

        GameObject buttonsRow = CreateUiObject("ButtonsRow", deletePopup.transform);

        HorizontalLayoutGroup rowLayout = buttonsRow.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 16f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        confirmDeleteButton = CreateButton(buttonsRow.transform, "ConfirmDelete", "DELETE", DeleteButtonColor, DeleteButtonHoverColor, 68f, 24f, ConfirmDelete);

        CreateButton(buttonsRow.transform, "CancelDelete", "CANCEL", EmptySlotColor, SlotButtonHoverColor, 68f, 24f, CancelDelete);

        modalBlocker.SetActive(false);
        deletePopup.SetActive(false);
    }

    private void ShowDeletePopup()
    {
        if(modalBlocker == null || deletePopup == null)
        {
            return;
        }

        modalBlocker.SetActive(true);
        deletePopup.SetActive(true);

        if(popupRoutine != null)
        {
            StopCoroutine(popupRoutine);
        }

        popupRoutine = StartCoroutine(AnimateDeletePopupRoutine(true));
    }

    private void HideDeletePopup()
    {
        if(modalBlocker == null || deletePopup == null)
        {
            return;
        }

        if(!modalBlocker.activeSelf && !deletePopup.activeSelf)
        {
            return;
        }

        if(popupRoutine != null)
        {
            StopCoroutine(popupRoutine);
        }

        popupRoutine = StartCoroutine(AnimateDeletePopupRoutine(false));
    }

    private void CancelDelete()
    {
        pendingDeleteSlotIndex = -1;
        HideDeletePopup();
    }

    private void SetVisible(bool visible)
    {
        isOpen = visible;

        if(root == null)
        {
            return;
        }

        if(!visible)
        {
            if(introRoutine != null)
            {
                StopCoroutine(introRoutine);
                introRoutine = null;
            }

            if(popupRoutine != null)
            {
                StopCoroutine(popupRoutine);
                popupRoutine = null;
            }

            pendingDeleteSlotIndex = -1;

            if(modalBlocker != null)
            {
                modalBlocker.SetActive(false);
            }

            if(deletePopup != null)
            {
                deletePopup.SetActive(false);
            }

            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;

            root.SetActive(false);

            return;
        }

        root.SetActive(true);

        rootCanvasGroup.alpha = 1f;
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = true;

        PlayUiSfx(openSfx, openSfxVolume);

        if(introRoutine != null)
        {
            StopCoroutine(introRoutine);
        }

        introRoutine = StartCoroutine(AnimateOpenRoutine());
    }

    private IEnumerator AnimateOpenRoutine()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        Canvas.ForceUpdateCanvases();

        overlayImage.color = new Color(OverlayColor.r, OverlayColor.g, OverlayColor.b, 0f);

        panelCanvasGroup.alpha = 0f;
        panelRect.anchoredPosition = new Vector2(0f, -38f);
        panelRect.localScale = Vector3.one * 0.96f;

        for(int i = 0; i < slotWidgets.Length; i++)
        {
            if(slotWidgets[i].cardCanvasGroup == null || slotWidgets[i].cardRect == null)
            {
                continue;
            }

            slotWidgets[i].cardCanvasGroup.alpha = 0f;
            slotWidgets[i].cardRect.localScale = Vector3.one * 0.93f;
        }

        float elapsed = 0f;

        while(elapsed < OpenAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / OpenAnimationDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            overlayImage.color = new Color(OverlayColor.r, OverlayColor.g, OverlayColor.b, OverlayColor.a * eased);

            panelCanvasGroup.alpha = eased;
            panelRect.anchoredPosition = Vector2.Lerp(new Vector2(0f, -38f), Vector2.zero, eased);
            panelRect.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, eased);

            for(int i = 0; i < slotWidgets.Length; i++)
            {
                if(slotWidgets[i].cardCanvasGroup == null || slotWidgets[i].cardRect == null)
                {
                    continue;
                }

                float localDelay = 0.08f + i * CardStaggerDelay;
                float localT = Mathf.Clamp01((elapsed - localDelay) / CardAnimationDuration);
                float localEase = Mathf.SmoothStep(0f, 1f, localT);

                slotWidgets[i].cardCanvasGroup.alpha = localEase;
                slotWidgets[i].cardRect.localScale = Vector3.one * Mathf.Lerp(0.93f, 1f, localEase);
            }

            yield return null;
        }

        overlayImage.color = OverlayColor;

        panelCanvasGroup.alpha = 1f;
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.localScale = Vector3.one;

        for(int i = 0; i < slotWidgets.Length; i++)
        {
            if(slotWidgets[i].cardCanvasGroup == null || slotWidgets[i].cardRect == null)
            {
                continue;
            }

            slotWidgets[i].cardCanvasGroup.alpha = 1f;
            slotWidgets[i].cardRect.localScale = Vector3.one;
        }

        rootCanvasGroup.interactable = true;

        if(EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        introRoutine = null;
    }

    private IEnumerator AnimateDeletePopupRoutine(bool showing)
    {
        if(modalBlockerCanvasGroup == null || deletePopupCanvasGroup == null || deletePopupRect == null)
        {
            yield break;
        }

        float startBlockerAlpha = modalBlockerCanvasGroup.alpha;
        float startPopupAlpha = deletePopupCanvasGroup.alpha;
        Vector3 startScale = deletePopupRect.localScale;

        float targetBlockerAlpha = showing ? 1f : 0f;
        float targetPopupAlpha = showing ? 1f : 0f;
        Vector3 targetScale = showing ? Vector3.one : Vector3.one * 0.94f;

        if(showing)
        {
            modalBlockerCanvasGroup.alpha = 0f;
            deletePopupCanvasGroup.alpha = 0f;
            deletePopupRect.localScale = Vector3.one * 0.94f;

            startBlockerAlpha = 0f;
            startPopupAlpha = 0f;
            startScale = deletePopupRect.localScale;
        }

        float elapsed = 0f;

        while(elapsed < PopupAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / PopupAnimationDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            modalBlockerCanvasGroup.alpha = Mathf.Lerp(startBlockerAlpha, targetBlockerAlpha, eased);
            deletePopupCanvasGroup.alpha = Mathf.Lerp(startPopupAlpha, targetPopupAlpha, eased);
            deletePopupRect.localScale = Vector3.Lerp(startScale, targetScale, eased);

            yield return null;
        }

        modalBlockerCanvasGroup.alpha = targetBlockerAlpha;
        deletePopupCanvasGroup.alpha = targetPopupAlpha;
        deletePopupRect.localScale = targetScale;

        if(!showing)
        {
            modalBlocker.SetActive(false);
            deletePopup.SetActive(false);
        }
        else if(EventSystem.current != null && confirmDeleteButton != null)
        {
            EventSystem.current.SetSelectedGameObject(confirmDeleteButton.gameObject);
        }

        popupRoutine = null;
    }

    private void PlayUiSfx(AudioClip clip, float volume)
    {
        if(clip == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(clip, Vector3.zero, volume);
    }

    private TextMeshProUGUI CreateTitle(Transform parent, string value, float fontSize)
    {
        TextMeshProUGUI text = CreateText(parent, "Title", value, fontSize, TextColor, TextAlignmentOptions.Center);

        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 3.8f;

        text.gameObject.AddComponent<LayoutElement>().preferredHeight = fontSize + 22f;

        return text;
    }

    private TextMeshProUGUI CreateSubtitle(Transform parent, string value)
    {
        TextMeshProUGUI text = CreateText(parent, "Subtitle", value, 24f, SubtitleColor, TextAlignmentOptions.Center);

        text.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
        text.characterSpacing = 1.2f;

        return text;
    }

    private TextMeshProUGUI CreateText(Transform parent, string objectName, string value, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUiObject(objectName, parent);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();

        text.text = value;
        text.font = pixelFontAsset;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.extraPadding = true;

        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.01f, 0.02f, 0.04f, 0.82f);
        shadow.effectDistance = new Vector2(2f, -2f);

        return text;
    }

    private Button CreateButton(Transform parent, string objectName, string label, Color normalColor, Color hoverColor, float preferredHeight, float fontSize, UnityEngine.Events.UnityAction callback)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);

        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 0f;
        layout.flexibleWidth = 1f;
        layout.preferredHeight = preferredHeight;

        Image image = buttonObject.AddComponent<Image>();
        image.color = normalColor;

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.30f);
        outline.effectDistance = new Vector2(3f, -3f);

        GameObject topHighlight = CreateUiObject("TopHighlight", buttonObject.transform);

        RectTransform highlightRect = topHighlight.GetComponent<RectTransform>();
        highlightRect.anchorMin = new Vector2(0f, 1f);
        highlightRect.anchorMax = new Vector2(1f, 1f);
        highlightRect.pivot = new Vector2(0.5f, 1f);
        highlightRect.sizeDelta = new Vector2(0f, 8f);

        Image highlightImage = topHighlight.AddComponent<Image>();
        highlightImage.color = new Color(1f, 1f, 1f, 0.18f);
        highlightImage.raycastTarget = false;

        Button button = buttonObject.AddComponent<Button>();

        ColorBlock colors = button.colors;

        colors.normalColor = normalColor;
        colors.highlightedColor = hoverColor;
        colors.selectedColor = normalColor;
        colors.pressedColor = hoverColor;
        colors.disabledColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.45f);
        colors.fadeDuration = 0.06f;

        button.colors = colors;
        button.targetGraphic = image;
        button.onClick.AddListener(callback);

        TextMeshProUGUI labelText = CreateText(buttonObject.transform, "Label", label, fontSize, TextColor, TextAlignmentOptions.Center);

        RectTransform labelRect = labelText.rectTransform;

        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(14f, 10f);
        labelRect.offsetMax = new Vector2(-14f, -10f);

        labelText.fontStyle = FontStyles.Bold;
        labelText.characterSpacing = 2.2f;

        return button;
    }

    private static void EnsureEventSystemExists()
    {
        if(EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));

        uiObject.transform.SetParent(parent, false);

        return uiObject;
    }
}
