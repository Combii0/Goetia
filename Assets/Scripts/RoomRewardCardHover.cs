using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gives the end-of-round card an inviting floating motion and a responsive
/// hover state without fighting the reward entrance animation.
/// </summary>
[RequireComponent(typeof(RectTransform), typeof(Image))]
public sealed class RoomRewardCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Color accentColor = new Color(0.9f, 0.35f, 1f, 1f);
    public float hoverScale = 1.13f;
    public float animationSpeed = 12f;

    private RectTransform rectTransform;
    private Image image;
    private Outline outline;
    private bool isHovered;
    private bool isPressed;
    private float currentScale = 1f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        outline = GetComponent<Outline>();
    }

    private void OnEnable()
    {
        isHovered = false;
        isPressed = false;
        currentScale = 1f;
    }

    private void Update()
    {
        float idlePulse = 1f + Mathf.Sin(Time.unscaledTime * 3.5f) * 0.035f;
        float targetScale = isPressed ? hoverScale * 0.94f : isHovered ? hoverScale : 1f;
        currentScale = Mathf.Lerp(currentScale, targetScale, 1f - Mathf.Exp(-animationSpeed * Time.unscaledDeltaTime));
        rectTransform.localScale = Vector3.one * (idlePulse * currentScale);

        if(outline != null)
        {
            float glow = isHovered ? 1f : 0.7f;
            outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, glow);
            outline.effectDistance = Vector2.one * (isHovered ? 18f : 12f);
        }

        if(image != null && image.sprite == null)
        {
            image.color = Color.Lerp(accentColor * 0.8f, accentColor, isHovered ? 1f : 0.35f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovered = true;
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData) => isPressed = true;
    public void OnPointerUp(PointerEventData eventData) => isPressed = false;
}
