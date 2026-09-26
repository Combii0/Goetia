using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class CardController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public CardData cardData;

    public SpriteRenderer spriteRenderer;
    public Transform cardVisual;

    public Sprite backSprite;

    public ShuffleManager shuffleManager;

    [Header("Flip")]
    public float flipTime = 0.12f;
    public float hoverScale = 1.08f;

    private bool canInteract;
    private bool isFront;
    private bool isSelected;
    private bool pointerInside;

    private Coroutine flipRoutine;

    private Vector3 originalVisualScale;

    void Awake()
    {
        if(cardVisual == null && spriteRenderer != null)
        {
            cardVisual = spriteRenderer.transform;
        }

        if(spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if(cardVisual == null && spriteRenderer != null)
        {
            cardVisual = spriteRenderer.transform;
        }

        originalVisualScale = cardVisual.localScale;

        spriteRenderer.sprite = backSprite;

        canInteract = false;
        isFront = false;
        isSelected = false;
        pointerInside = false;
    }

    public void Setup(CardData newCard, ShuffleManager manager)
    {
        cardData = newCard;
        shuffleManager = manager;

        spriteRenderer.sprite = backSprite;

        cardVisual.localScale = originalVisualScale;

        canInteract = false;
        isFront = false;
        isSelected = false;
        pointerInside = false;
    }

    public void EnableInteraction()
    {
        canInteract = true;
    }

    public void DisableInteraction()
    {
        canInteract = false;
    }

    public void SetSelected()
    {
        isSelected = true;
        canInteract = false;

        if(flipRoutine != null)
        {
            StopCoroutine(flipRoutine);
        }

        flipRoutine = StartCoroutine(FlipCard(true));
    }

    public void ShowBackInstant()
    {
        if(flipRoutine != null)
        {
            StopCoroutine(flipRoutine);
        }

        spriteRenderer.sprite = backSprite;
        cardVisual.localScale = originalVisualScale;

        isFront = false;
    }

    public void ShowFront()
    {
        if(cardData == null || isFront)
        {
            return;
        }

        if(flipRoutine != null)
        {
            StopCoroutine(flipRoutine);
        }

        flipRoutine = StartCoroutine(FlipCard(true));
    }

    public void ShowBack()
    {
        if(isSelected || !isFront)
        {
            return;
        }

        if(flipRoutine != null)
        {
            StopCoroutine(flipRoutine);
        }

        flipRoutine = StartCoroutine(FlipCard(false));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(!canInteract || cardData == null || isSelected)
        {
            return;
        }

        pointerInside = true;
        SceneMusicManager.PlayUiSelectionSfx();

        ShowFront();

        shuffleManager.ShowCardInfo(cardData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(!canInteract || cardData == null || isSelected)
        {
            return;
        }

        pointerInside = false;

        ShowBack();

        shuffleManager.HideCardInfo();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(!canInteract || cardData == null || isSelected)
        {
            return;
        }

        shuffleManager.SelectCard(this);
    }

    IEnumerator FlipCard(bool showFront)
    {
        Vector3 startScale = cardVisual.localScale;

        float targetY = originalVisualScale.y;
        float targetZ = originalVisualScale.z;

        float elapsed = 0;

        while(elapsed < flipTime)
        {
            elapsed += Time.deltaTime;

            float percentage = Mathf.Clamp01(elapsed / flipTime);

            float newScaleX = Mathf.Lerp(startScale.x, 0, percentage);

            cardVisual.localScale = new Vector3(newScaleX, targetY, targetZ);

            yield return null;
        }

        cardVisual.localScale = new Vector3(0, targetY, targetZ);

        spriteRenderer.sprite = showFront ? cardData.frontSprite : backSprite;

        isFront = showFront;

        elapsed = 0;

        float finalScale = pointerInside && showFront ? hoverScale : 1f;

        while(elapsed < flipTime)
        {
            elapsed += Time.deltaTime;

            float percentage = Mathf.Clamp01(elapsed / flipTime);

            float newScaleX = Mathf.Lerp(0, originalVisualScale.x * finalScale, percentage);
            float newScaleY = Mathf.Lerp(targetY, originalVisualScale.y * finalScale, percentage);

            cardVisual.localScale = new Vector3(newScaleX, newScaleY, targetZ);

            yield return null;
        }

        cardVisual.localScale = new Vector3(originalVisualScale.x * finalScale, originalVisualScale.y * finalScale, originalVisualScale.z);

        flipRoutine = null;
    }
}
