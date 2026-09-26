using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class ShuffleManager : MonoBehaviour
{
    // Version 1 accidentally duplicated the first selected card in a run.
    // This marker repairs existing slots once, without affecting real repeats.
    private const int CardHistoryFormatVersion = 3;

    [Header("Cards")]
    public CardController cardPrefab;
    public Transform cardsParent;
    public CardData[] availableCards;
    public Sprite cardBackSprite;

    [Header("Final Cards")]
    public float cardSpacing = 3f;
    public float cardsY = -0.5f;
    public float outsideX = 11f;

    public int finalCardsIntroSortingOrder = 5;
    public int finalCardsSortingOrder = 100;

    [Header("Intro Rain")]
    public int rainCardCount = 20;

    public float rainDuration = 3f;
    public float rainSpawnDelay = 0.08f;

    public float rainStartY = 7f;
    public float rainEndY = -7f;

    public float rainMinX = -8f;
    public float rainMaxX = 8f;

    public float rainMinSpeed = 4.5f;
    public float rainMaxSpeed = 8f;

    public float rainHorizontalDrift = 1.2f;

    public float rainMinRotationSpeed = -160f;
    public float rainMaxRotationSpeed = 160f;

    public float rainMinScale = 0.55f;
    public float rainMaxScale = 0.9f;

    public int rainSortingMin = 20;
    public int rainSortingMax = 60;

    [Header("Skip Intro")]
    public float skipSpeedMultiplier = 4f;

    [Header("Final Deal")]
    public float dealStartDelay = 0.5f;
    public float dealDuration = 2.2f;
    public float dealDelay = 0.18f;

    [Header("Card Info")]
    public GameObject cardInfo;
    public TMP_Text cardNameText;
    public TMP_Text categoryText;
    public TMP_Text descriptionText;

    [Header("Round UI")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private Color roundTextColor = Color.white;
    [SerializeField, Min(12f)] private float roundTextSize = 24f;

    [Header("Text Animation")]
    public float letterDelay = 0.025f;

    [Header("Selected Shuffle")]
    public float selectedShuffleDuration = 1f;
    public float selectedShuffleStrength = 0.25f;

    [Header("Transition")]
    public CanvasGroup fadeCanvas;
    public float fadeDuration = 1.5f;
    public string nextScene = "Room";

    private bool cardSelected;
    private bool introPlaying;
    private bool skipIntro;

    private bool rainFinished;
    private bool dealFinished;

    private Coroutine descriptionRoutine;

    private readonly List<CardController> finalCards = new List<CardController>();
    private readonly List<GameObject> rainCards = new List<GameObject>();

    void Awake()
    {
        cardSelected = false;

        int currentSlot = PlayerPrefs.GetInt("Goetia_CurrentSlot", -1);
        if(currentSlot >= 0)
        {
            MigrateCardHistory(currentSlot);
            PlayerPrefs.SetInt("Goetia_Slot_" + currentSlot + "_SelectionPending", 0);
        }

        introPlaying = false;
        skipIntro = false;

        rainFinished = false;
        dealFinished = false;

        if(cardInfo != null)
        {
            cardInfo.SetActive(false);
        }

        if(fadeCanvas != null)
        {
            fadeCanvas.alpha = 0;
            fadeCanvas.blocksRaycasts = false;
        }

        SetupRoundLabel(currentSlot);
    }

    private void SetupRoundLabel(int currentSlot)
    {
        int roundNumber = currentSlot >= 0
            ? Mathf.Max(1, PlayerPrefs.GetInt("Goetia_Slot_" + currentSlot + "_RoundNumber", 1))
            : 1;

        if(roundText == null)
        {
            Canvas sceneCanvas = cardInfo != null ? cardInfo.GetComponentInParent<Canvas>() : FindAnyObjectByType<Canvas>();
            if(sceneCanvas == null)
            {
                return;
            }

            GameObject labelObject = new GameObject("RoundLabel");
            labelObject.transform.SetParent(sceneCanvas.transform, false);

            RectTransform rect = labelObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 42f);
            rect.sizeDelta = new Vector2(420f, 54f);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.font = cardNameText != null ? cardNameText.font : TMP_Settings.defaultFontAsset;
            label.fontSize = roundTextSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            Outline outline = labelObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.04f, 0f, 0.08f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);

            roundText = label;
        }

        roundText.fontSize = roundTextSize;
        roundText.color = roundTextColor;
        roundText.text = "ROUND " + roundNumber;
    }

    void Start()
    {
        if(cardPrefab == null)
        {
            Debug.LogError("Card Prefab is not assigned.");
            return;
        }

        if(cardsParent == null)
        {
            Debug.LogError("Cards Parent is not assigned.");
            return;
        }

        if(cardBackSprite == null)
        {
            Debug.LogError("Card Back Sprite is not assigned.");
            return;
        }

        if(availableCards == null || availableCards.Length < 3)
        {
            Debug.LogError("At least 3 CardData assets are required.");
            return;
        }

        StartCoroutine(StartShuffleSequence());
    }

    void Update()
    {
        if(!introPlaying || skipIntro)
        {
            return;
        }

        if(Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            skipIntro = true;
        }
    }

    float AnimationSpeed
    {
        get
        {
            return skipIntro ? skipSpeedMultiplier : 1f;
        }
    }

    IEnumerator StartShuffleSequence()
    {
        introPlaying = true;

        GenerateFinalCards();

        StartCoroutine(CardRain());

        yield return WaitScaled(dealStartDelay);

        StartCoroutine(DealFinalCards());

        while(!rainFinished || !dealFinished)
        {
            yield return null;
        }

        ClearRainCards();

        BringFinalCardsToFront();

        for(int i = 0; i < finalCards.Count; i++)
        {
            finalCards[i].EnableInteraction();
        }

        introPlaying = false;
    }

    IEnumerator CardRain()
    {
        float spawnTimer = 0f;
        float elapsed = 0f;

        int spawnedCards = 0;

        while(elapsed < rainDuration)
        {
            float speed = AnimationSpeed;

            elapsed += Time.deltaTime * speed;
            spawnTimer += Time.deltaTime * speed;

            if(spawnedCards < rainCardCount && spawnTimer >= rainSpawnDelay)
            {
                spawnTimer = 0f;

                SpawnRainCard(spawnedCards);

                spawnedCards++;
            }

            yield return null;
        }

        rainFinished = true;
    }

    void SpawnRainCard(int index)
    {
        GameObject rainCard = new GameObject("RainCard_" + index);

        rainCard.transform.SetParent(cardsParent, false);

        float randomX = Random.Range(rainMinX, rainMaxX);

        rainCard.transform.position = new Vector3(randomX, rainStartY, 0);

        float randomScale = Random.Range(rainMinScale, rainMaxScale);

        rainCard.transform.localScale = Vector3.one * randomScale;

        rainCard.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-180f, 180f));

        SpriteRenderer renderer = rainCard.AddComponent<SpriteRenderer>();

        renderer.sprite = cardBackSprite;

        renderer.sortingOrder = Random.Range(rainSortingMin, rainSortingMax + 1);

        rainCards.Add(rainCard);

        StartCoroutine(AnimateRainCard(rainCard.transform));
    }

    IEnumerator AnimateRainCard(Transform card)
    {
        float speed = Random.Range(rainMinSpeed, rainMaxSpeed);
        float rotationSpeed = Random.Range(rainMinRotationSpeed, rainMaxRotationSpeed);
        float horizontalSpeed = Random.Range(-rainHorizontalDrift, rainHorizontalDrift);

        while(card != null && card.position.y > rainEndY)
        {
            float multiplier = AnimationSpeed;

            card.position += new Vector3(horizontalSpeed, -speed, 0) * Time.deltaTime * multiplier;

            card.Rotate(0, 0, rotationSpeed * Time.deltaTime * multiplier);

            yield return null;
        }

        if(card != null)
        {
            rainCards.Remove(card.gameObject);

            Destroy(card.gameObject);
        }
    }

    void ClearRainCards()
    {
        for(int i = rainCards.Count - 1; i >= 0; i--)
        {
            if(rainCards[i] != null)
            {
                Destroy(rainCards[i]);
            }
        }

        rainCards.Clear();
    }

    void GenerateFinalCards()
    {
        finalCards.Clear();

        List<CardData> cardPool = new List<CardData>();

        int currentSlot = PlayerPrefs.GetInt("Goetia_CurrentSlot", -1);
        int roundNumber = currentSlot >= 0
            ? Mathf.Max(1, PlayerPrefs.GetInt("Goetia_Slot_" + currentSlot + "_RoundNumber", 1)) : 1;
        bool ritualsAvailableThisRound = roundNumber % 3 != 1;
        bool ritualChosenLastRound = currentSlot >= 0
            && PlayerPrefs.GetInt("Goetia_Slot_" + currentSlot + "_LastSelectionWasRitual", 0) == 1;
        string activeRitualId = currentSlot >= 0
            ? PlayerPrefs.GetString("Goetia_Slot_" + currentSlot + "_ActiveRitual", string.Empty)
            : string.Empty;

        for(int i = 0; i < availableCards.Length; i++)
        {
            CardData candidate = availableCards[i];
            if(candidate == null)
            {
                continue;
            }

            bool isRitual = RitualCardUtility.IsRitual(candidate);
            bool isCurrentRitual = isRitual
                && !string.IsNullOrEmpty(activeRitualId)
                && string.Equals(candidate.cardID, activeRitualId, System.StringComparison.OrdinalIgnoreCase);
            bool isAllowedThisRound = !isRitual || (ritualsAvailableThisRound && !ritualChosenLastRound);

            if(isAllowedThisRound && !isCurrentRitual)
            {
                cardPool.Add(candidate);
            }
        }

        if(cardPool.Count < 3)
        {
            Debug.LogError("There are fewer than 3 valid cards in Available Cards.");
            return;
        }

        float startX = -cardSpacing;

        for(int i = 0; i < 3; i++)
        {
            int randomIndex = Random.Range(0, cardPool.Count);

            CardData selectedData = cardPool[randomIndex];

            cardPool.RemoveAt(randomIndex);

            float spawnX;

            if(i == 0)
            {
                spawnX = -outsideX;
            }
            else if(i == 1)
            {
                spawnX = Random.value < 0.5f ? -outsideX : outsideX;
            }
            else
            {
                spawnX = outsideX;
            }

            Vector3 startPosition = new Vector3(spawnX, Random.Range(cardsY - 1.2f, cardsY + 1.2f), 0);

            CardController card = Instantiate(cardPrefab, startPosition, Quaternion.identity, cardsParent);

            card.Setup(selectedData, this);

            card.DisableInteraction();

            card.ShowBackInstant();

            card.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-65f, 65f));

            card.gameObject.name = selectedData.cardName;

            if(card.spriteRenderer != null)
            {
                card.spriteRenderer.sortingOrder = finalCardsIntroSortingOrder + i;
            }

            finalCards.Add(card);
        }
    }

    IEnumerator DealFinalCards()
    {
        for(int i = 0; i < finalCards.Count; i++)
        {
            float targetX = -cardSpacing + i * cardSpacing;

            Vector3 targetPosition = new Vector3(targetX, cardsY, 0);

            StartCoroutine(AnimateCardIntoPosition(finalCards[i].transform, targetPosition, i * dealDelay));
        }

        float totalTime = dealDuration + dealDelay * 2f;

        float elapsed = 0f;

        while(elapsed < totalTime)
        {
            elapsed += Time.deltaTime * AnimationSpeed;

            yield return null;
        }

        dealFinished = true;
    }

    IEnumerator AnimateCardIntoPosition(Transform card, Vector3 targetPosition, float delay)
    {
        yield return WaitScaled(delay);

        Vector3 startPosition = card.position;

        Quaternion startRotation = card.rotation;

        float elapsed = 0f;

        while(elapsed < dealDuration)
        {
            elapsed += Time.deltaTime * AnimationSpeed;

            float percentage = Mathf.Clamp01(elapsed / dealDuration);

            float eased = EaseOutCubic(percentage);

            card.position = Vector3.Lerp(startPosition, targetPosition, eased);

            card.rotation = Quaternion.Lerp(startRotation, Quaternion.identity, eased);

            yield return null;
        }

        card.position = targetPosition;

        card.rotation = Quaternion.identity;
    }

    float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    void BringFinalCardsToFront()
    {
        for(int i = 0; i < finalCards.Count; i++)
        {
            if(finalCards[i] != null && finalCards[i].spriteRenderer != null)
            {
                finalCards[i].spriteRenderer.sortingOrder = finalCardsSortingOrder + i;
            }
        }
    }

    public void ShowCardInfo(CardData card)
    {
        if(cardSelected || card == null)
        {
            return;
        }

        if(descriptionRoutine != null)
        {
            StopCoroutine(descriptionRoutine);
        }

        cardInfo.SetActive(true);

        cardNameText.text = card.cardName;

        categoryText.text = card.category;

        descriptionRoutine = StartCoroutine(TypeDescription(card.description));
    }

    IEnumerator TypeDescription(string text)
    {
        descriptionText.text = "";

        for(int i = 0; i < text.Length; i++)
        {
            descriptionText.text += text[i];

            yield return new WaitForSeconds(letterDelay);
        }

        descriptionRoutine = null;
    }

    public void HideCardInfo()
    {
        if(cardSelected)
        {
            return;
        }

        if(descriptionRoutine != null)
        {
            StopCoroutine(descriptionRoutine);

            descriptionRoutine = null;
        }

        descriptionText.text = "";

        cardInfo.SetActive(false);
    }

    public void SelectCard(CardController selectedCard)
    {
        if(cardSelected || selectedCard == null)
        {
            return;
        }

        cardSelected = true;
        SceneMusicManager.PlayCardSelectionSfx();

        for(int i = 0; i < finalCards.Count; i++)
        {
            finalCards[i].DisableInteraction();
        }

        selectedCard.SetSelected();

        cardInfo.SetActive(true);

        cardNameText.text = selectedCard.cardData.cardName;

        categoryText.text = selectedCard.cardData.category;

        if(descriptionRoutine != null)
        {
            StopCoroutine(descriptionRoutine);
        }

        descriptionRoutine = StartCoroutine(TypeDescription(selectedCard.cardData.description));

        SaveSelectedCard(selectedCard.cardData);

        StartCoroutine(FinishSelection());
    }

    void SaveSelectedCard(CardData card)
    {
        int currentSlot = PlayerPrefs.GetInt("Goetia_CurrentSlot", -1);

        if(currentSlot < 0 || card == null)
        {
            return;
        }

        string historyKey = "Goetia_Slot_" + currentSlot + "_SelectedCards";
        List<string> previousCards = ReadHistoryEntries(PlayerPrefs.GetString(historyKey, string.Empty));
        previousCards.RemoveAll(RitualCardUtility.IsRitual);
        int roundNumber = Mathf.Max(1, PlayerPrefs.GetInt("Goetia_Slot_" + currentSlot + "_RoundNumber", 1));
        int previousRoundCount = roundNumber - 1;

        // A Shuffle choice belongs to one specific Room. If the player returns
        // to the same Shuffle, replace that pending choice instead of appending
        // a second enemy and shifting every subsequent round.
        if(previousCards.Count > previousRoundCount)
        {
            previousCards.RemoveRange(previousRoundCount, previousCards.Count - previousRoundCount);
        }

        bool isRitual = RitualCardUtility.IsRitual(card);
        bool isMaskTank = card.cardID == "totem" || card.cardID == "masktank" || card.cardID == "taskmask";
        bool isBlood = card.cardID == "blood";
        bool alreadyHasMaskTank = false;
        bool alreadyHasBlood = false;

        for(int i = 0; i < previousCards.Count; i++)
        {
            if(previousCards[i] == "totem" || previousCards[i] == "masktank" || previousCards[i] == "taskmask")
            {
                alreadyHasMaskTank = true;
            }

            if(previousCards[i] == "blood") alreadyHasBlood = true;
        }

        if(!isRitual && (!isMaskTank || !alreadyHasMaskTank) && (!isBlood || !alreadyHasBlood))
        {
            previousCards.Add(card.cardID);
        }

        // Blood never creates another Carnage, but each selection empowers the
        // one that is already stalking the Room.
        if(isBlood)
        {
            string bloodSelectionsKey = "Goetia_Slot_" + currentSlot + "_BloodSelections";
            PlayerPrefs.SetInt(bloodSelectionsKey, PlayerPrefs.GetInt(bloodSelectionsKey, 0) + 1);
        }

        // A ritual is a player ability, never an enemy. Selecting a new one
        // replaces the previous active ritual for this save slot.
        if(isRitual)
        {
            RitualCardUtility.SetActive(card.cardID);
        }

        PlayerPrefs.SetInt("Goetia_Slot_" + currentSlot + "_LastSelectionWasRitual", isRitual ? 1 : 0);

        PlayerPrefs.SetString(historyKey, string.Join(",", previousCards.ToArray()));
        PlayerPrefs.SetString("Goetia_Slot_" + currentSlot + "_SelectedCard", card.cardID);
        PlayerPrefs.SetInt("Goetia_Slot_" + currentSlot + "_CardHistoryFormat", CardHistoryFormatVersion);
        PlayerPrefs.SetInt("Goetia_Slot_" + currentSlot + "_SelectionPending", 1);

        PlayerPrefs.Save();
    }

    private List<string> ReadHistoryEntries(string value)
    {
        string[] entries = value.Split(',');
        List<string> history = new List<string>();

        for(int i = 0; i < entries.Length; i++)
        {
            if(!string.IsNullOrWhiteSpace(entries[i]))
            {
                history.Add(entries[i]);
            }
        }

        return history;
    }

    private void MigrateCardHistory(int slot)
    {
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

        // A pending selection used to be written twice in some saves. Collapse
        // only adjacent equal entries; repetitions separated by other cards
        // remain valid choices.
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

    IEnumerator FinishSelection()
    {
        yield return new WaitForSeconds(0.45f);

        cardInfo.SetActive(false);

        yield return StartCoroutine(SelectedShuffleAnimation());

        SceneTransitionFader.LoadScene(nextScene);
    }

    IEnumerator SelectedShuffleAnimation()
    {
        Vector3[] initialPositions = new Vector3[finalCards.Count];

        Quaternion[] initialRotations = new Quaternion[finalCards.Count];

        for(int i = 0; i < finalCards.Count; i++)
        {
            initialPositions[i] = finalCards[i].transform.position;

            initialRotations[i] = finalCards[i].transform.rotation;

            finalCards[i].ShowBackInstant();
        }

        float elapsed = 0f;

        Vector3 center = new Vector3(0, cardsY, 0);

        while(elapsed < selectedShuffleDuration)
        {
            elapsed += Time.deltaTime;

            float percentage = Mathf.Clamp01(elapsed / selectedShuffleDuration);

            float eased = Mathf.SmoothStep(0, 1, percentage);

            float strength = Mathf.Lerp(selectedShuffleStrength, 0, percentage);

            for(int i = 0; i < finalCards.Count; i++)
            {
                Vector3 target = Vector3.Lerp(initialPositions[i], center, eased);

                target.x += Random.Range(-strength, strength);

                target.y += Random.Range(-strength, strength);

                finalCards[i].transform.position = target;

                float targetRotation = (i - 1) * 10f;

                float rotation = Mathf.LerpAngle(initialRotations[i].eulerAngles.z, targetRotation, eased);

                finalCards[i].transform.rotation = Quaternion.Euler(0, 0, rotation);
            }

            yield return null;
        }

        for(int i = 0; i < finalCards.Count; i++)
        {
            finalCards[i].transform.position = center;

            finalCards[i].transform.rotation = Quaternion.Euler(0, 0, (i - 1) * 10f);
        }
    }

    IEnumerator FadeToBlack()
    {
        if(fadeCanvas == null)
        {
            yield break;
        }

        fadeCanvas.blocksRaycasts = true;

        float elapsed = 0f;

        while(elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            fadeCanvas.alpha = Mathf.Clamp01(elapsed / fadeDuration);

            yield return null;
        }

        fadeCanvas.alpha = 1f;
    }

    IEnumerator WaitScaled(float duration)
    {
        float elapsed = 0f;

        while(elapsed < duration)
        {
            elapsed += Time.deltaTime * AnimationSpeed;

            yield return null;
        }
    }
}
