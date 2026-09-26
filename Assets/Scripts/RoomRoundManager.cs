using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Runs the one-minute Room rounds, restores the selected-card history and builds
/// the timer/reward UI. Enemy templates remain in the scene and are cloned only
/// when their card is part of the current run.
/// </summary>
public sealed class RoomRoundManager : MonoBehaviour
{
    private const int CardHistoryFormatVersion = 3;

    [Header("Round")]
    public float roundDuration = 60f;
    [Min(0f)] public float extraSecondsPerRound = 5f;
    public float minimumEnemySpawnDelay = 1f;
    public float maximumEnemySpawnDelay = 20f;

    [Header("Assignable UI assets")]
    public TMP_FontAsset pixelFontAsset;
    public Sprite rewardCardSprite;

    [Header("UI colours")]
    public Color timerColor = Color.white;
    public Color panelColor = new Color(0.08f, 0.12f, 0.20f, 0.985f);
    public Color accentColor = new Color(0.47f, 0.82f, 1f, 1f);

    private readonly List<GameObject> enemyTemplates = new List<GameObject>();
    private readonly List<GameObject> activeEnemies = new List<GameObject>();

    private TMP_Text timerText;
    private CanvasGroup rewardCardGroup;
    private RectTransform rewardCardRect;
    private RoomRewardCardHover rewardCardHover;
    private CanvasGroup winPanelGroup;
    private RectTransform winPanelRect;
    private CanvasGroup gameOverGroup;
    private TMP_Text roundInfoText;
    private TMP_Text roundTitleText;
    private readonly List<Image> damageGlowImages = new List<Image>();
    private readonly List<float> damageGlowLayerAlpha = new List<float>();
    private float damageGlowStrength;
    private bool roundFinished;
    private float remainingTime;
    private Transform player;
    private GameManager gameManager;
    private int currentSlot;
    private int roundNumber;
    private List<string> roundEnemyIds = new List<string>();
    private bool pauseMenuReady;

    private void Awake()
    {
        currentSlot = PlayerPrefs.GetInt("Goetia_CurrentSlot", -1);
        MigrateCardHistory(currentSlot);
        roundNumber = currentSlot >= 0 ? Mathf.Max(1, PlayerPrefs.GetInt("Goetia_Slot_" + currentSlot + "_RoundNumber", 1)) : 1;
        player = FindAnyObjectByType<PlayerController>()?.transform;
        gameManager = FindAnyObjectByType<GameManager>();

        if(gameManager != null)
        {
            gameManager.PlayerDied += HandlePlayerDeath;
            gameManager.DamageTaken += HandleDamageTaken;
        }

        CollectEnemyTemplates();
        BuildUi();
        remainingTime = roundDuration + Mathf.Max(0, roundNumber - 1) * extraSecondsPerRound;
        UpdateTimerText();
    }

    private void Start()
    {
        // Create this after BuildUi has ensured the Input System EventSystem.
        PauseMenuController.EnsureForRoom();
        pauseMenuReady = true;
        StartCoroutine(SpawnEnemyHistory());
    }

    private void OnDestroy()
    {
        if(gameManager != null)
        {
            gameManager.PlayerDied -= HandlePlayerDeath;
            gameManager.DamageTaken -= HandleDamageTaken;
        }
    }

    private void Update()
    {
        if(!pauseMenuReady)
        {
            PauseMenuController.EnsureForRoom();
            pauseMenuReady = true;
        }

        if(roundFinished)
        {
            return;
        }

        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        UpdateTimerText();
        UpdateDamageGlow();

        if(remainingTime <= 0f)
        {
            FinishRound();
        }
    }

    private void CollectEnemyTemplates()
    {
        AddTemplates(FindObjectsByType<Demon>(FindObjectsInactive.Include));
        AddTemplates(FindObjectsByType<Jelly>(FindObjectsInactive.Include));
        AddTemplates(FindObjectsByType<Carnage>(FindObjectsInactive.Include));
        AddTemplates(FindObjectsByType<MaskTank>(FindObjectsInactive.Include));

        for(int i = 0; i < enemyTemplates.Count; i++)
        {
            enemyTemplates[i].SetActive(false);
        }
    }

    private void AddTemplates<T>(T[] components) where T : Component
    {
        for(int i = 0; i < components.Length; i++)
        {
            GameObject template = components[i].gameObject;

            if(!enemyTemplates.Contains(template))
            {
                enemyTemplates.Add(template);
            }
        }
    }

    private IEnumerator SpawnEnemyHistory()
    {
        List<string> history = ReadCardHistory();
        roundEnemyIds = history;

        for(int i = 0; i < history.Count; i++)
        {
            float delay = Random.Range(minimumEnemySpawnDelay, maximumEnemySpawnDelay);
            yield return new WaitForSeconds(delay);

            if(!roundFinished)
            {
                SpawnEnemy(history[i]);
            }
        }
    }

    private void SpawnEnemy(string cardId)
    {
        GameObject template = FindTemplate(cardId);

        if(template == null)
        {
            Debug.LogWarning("No Room enemy template found for card: " + cardId);
            return;
        }

        GameObject enemy;

        // Preserve the scene-authored first instance, including its exact spawn
        // point, animator state and renderer/line-renderer setup. Repeats are clones.
        if(!activeEnemies.Contains(template))
        {
            enemy = template;
        }
        else
        {
            // The Demon component lives on an animated child. Clone its root
            // so every copy retains the non-animated transform used for its
            // teleport anchor.
            Transform spawnRoot = template.GetComponent<Demon>() != null && template.transform.parent != null
                ? template.transform.parent
                : template.transform;

            enemy = Instantiate(spawnRoot.gameObject, spawnRoot.position, spawnRoot.rotation);
            enemy.name = cardId + "_RoundEnemy_" + activeEnemies.Count;
        }

        enemy.SetActive(true);
        activeEnemies.Add(enemy);

        ConfigureEnemyReferences(enemy);

        Jelly jelly = enemy.GetComponent<Jelly>();
        if(jelly != null)
        {
            jelly.RandomizeSpawnPoint();
        }
    }

    private GameObject FindTemplate(string cardId)
    {
        string normalizedId = cardId.ToLowerInvariant();

        for(int i = 0; i < enemyTemplates.Count; i++)
        {
            GameObject template = enemyTemplates[i];

            if(normalizedId == "demon" && template.GetComponent<Demon>() != null)
            {
                return template;
            }

            if(normalizedId == "jelly" && template.GetComponent<Jelly>() != null)
            {
                return template;
            }

            if((normalizedId == "carnage" || normalizedId == "blood") && template.GetComponent<Carnage>() != null)
            {
                return template;
            }

            if((normalizedId == "totem" || normalizedId == "masktank" || normalizedId == "taskmask") && template.GetComponent<MaskTank>() != null)
            {
                return template;
            }
        }

        return null;
    }

    private void ConfigureEnemyReferences(GameObject enemy)
    {
        Demon demon = enemy.GetComponent<Demon>();
        if(demon == null) demon = enemy.GetComponentInChildren<Demon>(true);
        if(demon != null)
        {
            if(demon.player == null) demon.player = player;
            if(demon.gameManager == null) demon.gameManager = gameManager;
        }

        Jelly jelly = enemy.GetComponent<Jelly>();
        if(jelly != null)
        {
            if(jelly.player == null) jelly.player = player;
            if(jelly.playerController == null) jelly.playerController = player != null ? player.GetComponent<PlayerController>() : null;
        }

        Carnage carnage = enemy.GetComponent<Carnage>();
        if(carnage != null)
        {
            if(carnage.player == null) carnage.player = player;
            if(carnage.playerController == null) carnage.playerController = player != null ? player.GetComponent<PlayerController>() : null;

            int bloodSelections = currentSlot >= 0
                ? PlayerPrefs.GetInt("Goetia_Slot_" + currentSlot + "_BloodSelections", 0) : 0;
            carnage.followSpeed += bloodSelections * 0.2f;
        }

        MaskTank maskTank = enemy.GetComponent<MaskTank>();
        if(maskTank != null)
        {
            if(maskTank.player == null) maskTank.player = player;
        }
    }

    private void FinishRound()
    {
        roundFinished = true;

        for(int i = 0; i < activeEnemies.Count; i++)
        {
            if(activeEnemies[i] != null)
            {
                activeEnemies[i].SetActive(false);
            }
        }

        rewardCardGroup.gameObject.SetActive(true);
        StartCoroutine(ShowRewardCard());
    }

    private IEnumerator ShowRewardCard()
    {
        float elapsed = 0f;
        rewardCardHover.enabled = false;
        rewardCardGroup.alpha = 0f;
        rewardCardRect.localScale = Vector3.one * 0.35f;

        while(elapsed < 0.45f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.45f);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            rewardCardGroup.alpha = eased;
            rewardCardRect.localScale = Vector3.LerpUnclamped(Vector3.one * 0.35f, Vector3.one, eased);
            yield return null;
        }

        rewardCardGroup.alpha = 1f;
        rewardCardRect.localScale = Vector3.one;
        rewardCardHover.enabled = true;
    }

    private void ShowWinPanel()
    {
        rewardCardHover.enabled = false;
        rewardCardGroup.gameObject.SetActive(false);
        roundTitleText.text = "Room " + roundNumber + " cleared!";
        roundInfoText.text = BuildRoundSummary();
        winPanelGroup.gameObject.SetActive(true);
        winPanelGroup.alpha = 0f;
        StartCoroutine(ShowWinPanelAnimation());
    }

    private void ContinueToShuffle()
    {
        if(currentSlot >= 0)
        {
            PlayerPrefs.SetInt("Goetia_Slot_" + currentSlot + "_SelectionPending", 0);
            PlayerPrefs.SetInt("Goetia_Slot_" + currentSlot + "_RoundNumber", roundNumber + 1);
            PlayerPrefs.Save();
        }

        MainMenuSaveSlotsUI.SaveCurrentSlot("Shuffle");
        SceneTransitionFader.LoadScene("Shuffle");
    }

    private void BuildUi()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("RoomRoundUI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1500;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        timerText = CreateText(canvasObject.transform, "RoundTimer", "01:00", 54f, timerColor);
        RectTransform timerRect = timerText.rectTransform;
        timerRect.anchorMin = new Vector2(0.5f, 1f);
        timerRect.anchorMax = new Vector2(0.5f, 1f);
        timerRect.anchoredPosition = new Vector2(0f, -55f);
        timerRect.sizeDelta = new Vector2(260f, 80f);
        timerText.alignment = TextAlignmentOptions.Center;

        BuildDamageGlow(canvasObject.transform);

        GameObject cardObject = CreatePanel(canvasObject.transform, "RoundRewardCard", new Vector2(260f, 360f));
        rewardCardGroup = cardObject.GetComponent<CanvasGroup>();
        rewardCardRect = cardObject.GetComponent<RectTransform>();
        rewardCardGroup.gameObject.SetActive(false);
        Image cardImage = cardObject.AddComponent<Image>();
        cardImage.sprite = rewardCardSprite;
        cardImage.color = rewardCardSprite == null ? accentColor : Color.white;
        Button cardButton = cardObject.AddComponent<Button>();
        cardButton.transition = Selectable.Transition.None;
        cardButton.onClick.AddListener(ShowWinPanel);
        AddGlow(cardObject);
        rewardCardHover = cardObject.AddComponent<RoomRewardCardHover>();
        rewardCardHover.accentColor = accentColor;
        rewardCardHover.enabled = false;

        GameObject winObject = CreatePanel(canvasObject.transform, "WinPanel", new Vector2(900f, 560f));
        winPanelGroup = winObject.GetComponent<CanvasGroup>();
        winPanelRect = winObject.GetComponent<RectTransform>();
        winPanelGroup.gameObject.SetActive(false);
        Image winBackground = winObject.AddComponent<Image>();
        winBackground.color = panelColor;
        Outline panelOutline = winObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.72f);
        panelOutline.effectDistance = new Vector2(5f, -5f);
        AddWinPanelDecorations(winObject.transform);

        roundTitleText = CreateText(winObject.transform, "WinText", "Room 1 cleared!", 62f, accentColor);
        roundTitleText.alignment = TextAlignmentOptions.Center;
        roundTitleText.textWrappingMode = TextWrappingModes.NoWrap;
        roundTitleText.enableAutoSizing = true;
        roundTitleText.fontSizeMin = 38f;
        roundTitleText.fontSizeMax = 62f;
        roundTitleText.rectTransform.anchorMin = new Vector2(0.5f, 0.72f);
        roundTitleText.rectTransform.anchorMax = new Vector2(0.5f, 0.72f);
        roundTitleText.rectTransform.sizeDelta = new Vector2(820f, 92f);

        roundInfoText = CreateText(winObject.transform, "RoundInfo", string.Empty, 26f, Color.white);
        roundInfoText.alignment = TextAlignmentOptions.Center;
        roundInfoText.enableAutoSizing = true;
        roundInfoText.fontSizeMin = 18f;
        roundInfoText.fontSizeMax = 26f;
        roundInfoText.lineSpacing = 8f;
        roundInfoText.rectTransform.anchorMin = new Vector2(0.5f, 0.47f);
        roundInfoText.rectTransform.anchorMax = new Vector2(0.5f, 0.47f);
        roundInfoText.rectTransform.sizeDelta = new Vector2(790f, 170f);

        Button continueButton = CreateButton(winObject.transform, "ContinueButton", "CONTINUE", 260f);
        continueButton.onClick.AddListener(ContinueToShuffle);
        RectTransform continueRect = continueButton.GetComponent<RectTransform>();
        continueRect.anchorMin = new Vector2(0.5f, 0.13f);
        continueRect.anchorMax = new Vector2(0.5f, 0.13f);
        continueRect.anchoredPosition = Vector2.zero;

        GameObject gameOverObject = CreatePanel(canvasObject.transform, "GameOverPanel", new Vector2(1920f, 1080f));
        gameOverGroup = gameOverObject.GetComponent<CanvasGroup>();
        gameOverGroup.gameObject.SetActive(false);
        Image gameOverBackground = gameOverObject.AddComponent<Image>();
        gameOverBackground.color = Color.black;

        TMP_Text gameOverText = CreateText(gameOverObject.transform, "GameOverText", "GAME OVER", 80f, accentColor);
        gameOverText.alignment = TextAlignmentOptions.Center;
        gameOverText.rectTransform.anchorMin = new Vector2(0.5f, 0.62f);
        gameOverText.rectTransform.anchorMax = new Vector2(0.5f, 0.62f);
        gameOverText.rectTransform.sizeDelta = new Vector2(700f, 120f);

        Button newGameButton = CreateButton(gameOverObject.transform, "NewGameButton", "NEW GAME", 260f);
        newGameButton.onClick.AddListener(NewGame);
        newGameButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-155f, -70f);

        Button mainMenuButton = CreateButton(gameOverObject.transform, "MainMenuButton", "MAIN MENU", 260f);
        mainMenuButton.onClick.AddListener(GoToMainMenu);
        mainMenuButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(155f, -70f);
    }

    private void EnsureEventSystem()
    {
        if(EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("RoomEventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private TMP_Text CreateText(Transform parent, string name, string value, float size, Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.font = pixelFontAsset;
        text.raycastTarget = false;
        return text;
    }

    private GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        CanvasGroup group = panel.AddComponent<CanvasGroup>();
        group.blocksRaycasts = true;
        return panel;
    }

    private Button CreateButton(Transform parent, string name, string label, float width)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.28f);
        rect.anchorMax = new Vector2(0.5f, 0.28f);
        rect.sizeDelta = new Vector2(width, 86f);
        Image image = buttonObject.AddComponent<Image>();
        image.color = accentColor;
        Button button = buttonObject.AddComponent<Button>();
        TMP_Text text = CreateText(buttonObject.transform, "Label", label, 30f, Color.white);
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private void AddGlow(GameObject cardObject)
    {
        Outline outline = cardObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.25f, 1f, 0.85f);
        outline.effectDistance = new Vector2(12f, 12f);
    }

    private void AddWinPanelDecorations(Transform parent)
    {
        CreateWinDecoration(parent, "TopLine", new Vector2(600f, 4f), new Vector2(0.5f, 0.93f));
        CreateWinDecoration(parent, "BottomLine", new Vector2(600f, 4f), new Vector2(0.5f, 0.25f));

        GameObject jewel = new GameObject("CenterJewel");
        jewel.transform.SetParent(parent, false);
        RectTransform jewelRect = jewel.AddComponent<RectTransform>();
        jewelRect.anchorMin = new Vector2(0.5f, 0.93f);
        jewelRect.anchorMax = new Vector2(0.5f, 0.93f);
        jewelRect.sizeDelta = new Vector2(16f, 16f);
        jewelRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image jewelImage = jewel.AddComponent<Image>();
        jewelImage.color = Color.white;
        jewelImage.raycastTarget = false;
    }

    private void CreateWinDecoration(Transform parent, string name, Vector2 size, Vector2 anchor)
    {
        GameObject line = new GameObject(name);
        line.transform.SetParent(parent, false);
        RectTransform rect = line.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        Image image = line.AddComponent<Image>();
        image.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.48f);
        image.raycastTarget = false;
    }

    private void UpdateTimerText()
    {
        if(timerText == null)
        {
            return;
        }

        int seconds = Mathf.CeilToInt(remainingTime);
        timerText.text = string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);
    }

    private void BuildDamageGlow(Transform parent)
    {
        GameObject glowCanvasObject = new GameObject("DamageEdgeGlow");
        glowCanvasObject.transform.SetParent(parent, false);

        Canvas glowCanvas = glowCanvasObject.AddComponent<Canvas>();
        glowCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        glowCanvas.sortingOrder = 1400;

        RectTransform glowRect = glowCanvasObject.GetComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = Vector2.zero;
        glowRect.offsetMax = Vector2.zero;

        CanvasScaler scaler = glowCanvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        for(int layer = 0; layer < 3; layer++)
        {
            float thickness = 24f + layer * 30f;
            float alpha = layer == 0 ? 1f : layer == 1 ? 0.48f : 0.18f;
            CreateGlowEdge(glowCanvasObject.transform, "GlowTop_" + layer, thickness, alpha, true, true);
            CreateGlowEdge(glowCanvasObject.transform, "GlowBottom_" + layer, thickness, alpha, true, false);
            CreateGlowEdge(glowCanvasObject.transform, "GlowLeft_" + layer, thickness, alpha, false, true);
            CreateGlowEdge(glowCanvasObject.transform, "GlowRight_" + layer, thickness, alpha, false, false);
        }
    }

    private void CreateGlowEdge(Transform parent, string name, float thickness, float layerAlpha, bool horizontal, bool firstSide)
    {
        GameObject edgeObject = new GameObject(name);
        edgeObject.transform.SetParent(parent, false);
        RectTransform rect = edgeObject.AddComponent<RectTransform>();

        if(horizontal)
        {
            rect.anchorMin = firstSide ? new Vector2(0f, 1f) : Vector2.zero;
            rect.anchorMax = firstSide ? new Vector2(1f, 1f) : new Vector2(1f, 0f);
            rect.offsetMin = firstSide ? new Vector2(0f, -thickness) : new Vector2(0f, 0f);
            rect.offsetMax = firstSide ? new Vector2(0f, 0f) : new Vector2(0f, thickness);
        }
        else
        {
            rect.anchorMin = firstSide ? Vector2.zero : new Vector2(1f, 0f);
            rect.anchorMax = firstSide ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            rect.offsetMin = firstSide ? new Vector2(0f, 0f) : new Vector2(-thickness, 0f);
            rect.offsetMax = firstSide ? new Vector2(thickness, 0f) : new Vector2(0f, 0f);
        }

        Image image = edgeObject.AddComponent<Image>();
        // Crimson (#DC143C), kept transparent until the player takes damage.
        image.color = new Color(0.86f, 0.08f, 0.24f, 0f);
        image.raycastTarget = false;
        damageGlowImages.Add(image);
        damageGlowLayerAlpha.Add(layerAlpha);
    }

    private void HandleDamageTaken(float currentHealth, float maxHealth)
    {
        if(maxHealth <= 0f)
        {
            return;
        }

        // Three hits are the default Room setup: the first one should be
        // noticeable, while the last remaining life should make the crimson
        // border feel urgent. Keep the value proportional for other max-health
        // configurations too.
        float healthLost = Mathf.Clamp01(1f - currentHealth / maxHealth);
        damageGlowStrength = Mathf.Clamp01(healthLost * 1.25f);
    }

    private void UpdateDamageGlow()
    {
        if(damageGlowImages.Count == 0)
        {
            return;
        }

        float pulse = 0.88f + Mathf.Sin(Time.unscaledTime * 4f) * 0.12f;
        float baseAlpha = damageGlowStrength * pulse;

        for(int i = 0; i < damageGlowImages.Count; i++)
        {
            Image image = damageGlowImages[i];
            Color color = image.color;
            color.a = baseAlpha * damageGlowLayerAlpha[i];
            image.color = color;
        }
    }

    private string BuildRoundSummary()
    {
        List<string> names = new List<string>();
        for(int i = 0; i < roundEnemyIds.Count; i++)
        {
            names.Add(DisplayEnemyName(roundEnemyIds[i]));
        }

        string enemyNames = names.Count == 0 ? "None" : string.Join(", ", names.ToArray());
        int lives = gameManager == null ? 0 : Mathf.Max(0, Mathf.CeilToInt(gameManager.CurrentHealth));
        return string.Format("Amount of enemies: {0}\nEnemies in the room: {1}\nPlayer Lives: {2}", roundEnemyIds.Count, enemyNames, lives);
    }

    private string DisplayEnemyName(string id)
    {
        string normalized = id.ToLowerInvariant();
        if(normalized == "totem" || normalized == "masktank" || normalized == "taskmask") return "MaskTank";
        if(normalized == "jelly") return "Jelly";
        if(normalized == "carnage" || normalized == "blood") return "Carnage";
        if(normalized == "demon") return "Demon";
        return id;
    }

    private void HandlePlayerDeath()
    {
        if(roundFinished)
        {
            return;
        }

        roundFinished = true;
        StopAllCoroutines();

        // Death ends the run. Remove its save immediately so returning to the
        // menu cannot resume this Room or retain buffs from it.
        MainMenuSaveSlotsUI.EraseSaveSlot(currentSlot);
        currentSlot = -1;

        for(int i = 0; i < activeEnemies.Count; i++)
        {
            if(activeEnemies[i] != null)
            {
                activeEnemies[i].SetActive(false);
            }
        }

        gameOverGroup.gameObject.SetActive(true);
        gameOverGroup.alpha = 0f;
        StartCoroutine(FadeCanvasGroup(gameOverGroup, 1f, 0.8f));
    }

    private void NewGame()
    {
        // The previous slot was erased on death. Starting anew goes through the
        // empty-slot flow, which creates a completely clean run.
        SceneTransitionFader.LoadScene("Main Menu");
    }

    private void GoToMainMenu()
    {
        SceneTransitionFader.LoadScene("Main Menu");
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float target, float duration)
    {
        float start = group.alpha;
        float elapsed = 0f;
        while(elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = target;
    }

    private IEnumerator ShowWinPanelAnimation()
    {
        Vector2 finalPosition = winPanelRect.anchoredPosition;
        Vector2 startPosition = finalPosition + new Vector2(0f, -34f);
        winPanelRect.anchoredPosition = startPosition;
        winPanelRect.localScale = Vector3.one * 0.78f;

        const float duration = 0.42f;
        float elapsed = 0f;
        while(elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 4f);
            float overshoot = Mathf.LerpUnclamped(0.78f, 1.04f, eased);
            winPanelGroup.alpha = eased;
            winPanelRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, finalPosition, eased);
            winPanelRect.localScale = Vector3.one * overshoot;
            yield return null;
        }

        elapsed = 0f;
        while(elapsed < 0.12f)
        {
            elapsed += Time.unscaledDeltaTime;
            winPanelRect.localScale = Vector3.Lerp(Vector3.one * 1.04f, Vector3.one, elapsed / 0.12f);
            yield return null;
        }

        winPanelGroup.alpha = 1f;
        winPanelRect.anchoredPosition = finalPosition;
        winPanelRect.localScale = Vector3.one;
    }

    private List<string> ReadCardHistory()
    {
        List<string> history = new List<string>();
        if(currentSlot < 0)
        {
            return history;
        }

        string value = PlayerPrefs.GetString("Goetia_Slot_" + currentSlot + "_SelectedCards", string.Empty);
        if(string.IsNullOrEmpty(value))
        {
            string legacy = PlayerPrefs.GetString("Goetia_Slot_" + currentSlot + "_SelectedCard", string.Empty);
            if(!string.IsNullOrEmpty(legacy) && !RitualCardUtility.IsRitual(legacy))
            {
                history.Add(legacy);
            }
            return history;
        }

        string[] ids = value.Split(',');
        for(int i = 0; i < ids.Length; i++)
        {
            if(!string.IsNullOrWhiteSpace(ids[i]) && !RitualCardUtility.IsRitual(ids[i]))
            {
                history.Add(ids[i]);

                if(history.Count >= roundNumber)
                {
                    break;
                }
            }
        }

        // A previously saved Shuffle could contain more entries than the round
        // number. The selected card is authoritative for the current pending
        // Room, so replace only that round's entry before spawning enemies.
        string pendingKey = "Goetia_Slot_" + currentSlot + "_SelectionPending";
        string selectedCard = PlayerPrefs.GetString("Goetia_Slot_" + currentSlot + "_SelectedCard", string.Empty);
        if(PlayerPrefs.GetInt(pendingKey, 0) == 1 && !string.IsNullOrWhiteSpace(selectedCard))
        {
            int priorRoundCount = Mathf.Max(0, roundNumber - 1);
            if(history.Count > priorRoundCount)
            {
                history.RemoveRange(priorRoundCount, history.Count - priorRoundCount);
            }

            if(!RitualCardUtility.IsRitual(selectedCard) && !IsDuplicateUniqueEnemy(history, selectedCard))
            {
                history.Add(selectedCard);
            }

            PlayerPrefs.SetString("Goetia_Slot_" + currentSlot + "_SelectedCards", string.Join(",", history.ToArray()));
            PlayerPrefs.SetInt(pendingKey, 0);
            PlayerPrefs.Save();
        }

        return history;
    }

    private bool IsDuplicateUniqueEnemy(List<string> history, string cardId)
    {
        string normalizedCardId = cardId.ToLowerInvariant();
        bool isMaskTank = normalizedCardId == "totem" || normalizedCardId == "masktank" || normalizedCardId == "taskmask";
        bool isBlood = normalizedCardId == "blood";
        if(!isMaskTank && !isBlood)
        {
            return false;
        }

        for(int i = 0; i < history.Count; i++)
        {
            string id = history[i].ToLowerInvariant();
            if(isBlood && id == "blood") return true;
            if(isMaskTank && (id == "totem" || id == "masktank" || id == "taskmask"))
            {
                return true;
            }
        }

        return false;
    }

    private void MigrateCardHistory(int slot)
    {
        if(slot < 0)
        {
            return;
        }

        string versionKey = "Goetia_Slot_" + slot + "_CardHistoryFormat";
        if(PlayerPrefs.GetInt(versionKey, 0) >= CardHistoryFormatVersion)
        {
            return;
        }

        string historyKey = "Goetia_Slot_" + slot + "_SelectedCards";
        string[] entries = PlayerPrefs.GetString(historyKey, string.Empty).Split(',');
        List<string> cleanedEntries = new List<string>();

        for(int i = 0; i < entries.Length; i++)
        {
            if(!string.IsNullOrWhiteSpace(entries[i]))
            {
                cleanedEntries.Add(entries[i]);
            }
        }

        if(cleanedEntries.Count == 0)
        {
            string legacyCard = PlayerPrefs.GetString("Goetia_Slot_" + slot + "_SelectedCard", string.Empty);
            if(!string.IsNullOrWhiteSpace(legacyCard))
            {
                cleanedEntries.Add(legacyCard);
            }
        }

        for(int i = cleanedEntries.Count - 1; i > 0; i--)
        {
            if(cleanedEntries[i] == cleanedEntries[i - 1]) cleanedEntries.RemoveAt(i);
        }

        if(cleanedEntries.Count > 0)
        {
            PlayerPrefs.SetString(historyKey, string.Join(",", cleanedEntries.ToArray()));
        }

        PlayerPrefs.SetInt(versionKey, CardHistoryFormatVersion);
        PlayerPrefs.Save();
    }
}
