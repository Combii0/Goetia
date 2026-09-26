using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds an animated square rain background entirely from Unity UI primitives.
/// It needs no texture or external art asset.
/// </summary>
public sealed class MainMenuProceduralBackground : MonoBehaviour
{
    [Header("Composition")]
    [SerializeField] private int squareCount = 70;
    [SerializeField] private Vector2 squareSizeRange = new Vector2(10f, 64f);
    [SerializeField] private Vector2 fallSpeedRange = new Vector2(35f, 150f);
    [SerializeField] private Vector2 rotationSpeedRange = new Vector2(-95f, 95f);
    [SerializeField] private float glowChance = 0.28f;

    [Header("Palette")]
    [SerializeField] private Color backgroundColor = new Color(0.025f, 0.008f, 0.075f, 1f);
    [SerializeField] private Color squareColor = new Color(0.45f, 0.15f, 0.85f, 0.65f);
    [SerializeField] private Color brightSquareColor = new Color(0.95f, 0.34f, 0.95f, 0.95f);

    private readonly List<SquareParticle> particles = new List<SquareParticle>();
    private RectTransform canvasRect;

    private sealed class SquareParticle
    {
        public RectTransform transform;
        public Image image;
        public float speed;
        public float rotationSpeed;
        public float baseAlpha;
        public float pulseOffset;
        public bool hasGlow;
    }

    private void Awake()
    {
        BuildCanvas();
        BuildParticles();
    }

    private void Update()
    {
        float height = canvasRect.rect.height;
        float width = canvasRect.rect.width;

        for(int i = 0; i < particles.Count; i++)
        {
            SquareParticle particle = particles[i];
            Vector2 position = particle.transform.anchoredPosition;
            position.y -= particle.speed * Time.unscaledDeltaTime;
            position.x += Mathf.Sin(Time.unscaledTime * 0.8f + particle.pulseOffset) * 5f * Time.unscaledDeltaTime;

            if(position.y < -height * 0.5f - 100f)
            {
                position.y = height * 0.5f + Random.Range(20f, 160f);
                position.x = Random.Range(-width * 0.55f, width * 0.55f);
            }

            particle.transform.anchoredPosition = position;
            particle.transform.Rotate(0f, 0f, particle.rotationSpeed * Time.unscaledDeltaTime);

            float pulse = particle.hasGlow ? 0.82f + Mathf.Sin(Time.unscaledTime * 2.2f + particle.pulseOffset) * 0.18f : 1f;
            Color color = particle.image.color;
            color.a = particle.baseAlpha * pulse;
            particle.image.color = color;
        }
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("ProceduralPurpleSky");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasRect = canvasObject.GetComponent<RectTransform>();

        GameObject backgroundObject = new GameObject("PurpleSky");
        backgroundObject.transform.SetParent(canvasObject.transform, false);
        RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
        Stretch(backgroundRect);
        Image background = backgroundObject.AddComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = false;
    }

    private void BuildParticles()
    {
        float width = Mathf.Max(canvasRect.rect.width, Screen.width);
        float height = Mathf.Max(canvasRect.rect.height, Screen.height);

        for(int i = 0; i < squareCount; i++)
        {
            GameObject squareObject = new GameObject("FallingSquare_" + i);
            squareObject.transform.SetParent(canvasRect, false);

            RectTransform squareRect = squareObject.AddComponent<RectTransform>();
            float size = Random.Range(squareSizeRange.x, squareSizeRange.y);
            squareRect.sizeDelta = new Vector2(size, size);
            squareRect.anchoredPosition = new Vector2(
                Random.Range(-width * 0.55f, width * 0.55f),
                Random.Range(-height * 0.55f, height * 0.65f));
            squareRect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            Image squareImage = squareObject.AddComponent<Image>();
            bool hasGlow = Random.value < glowChance;
            squareImage.color = hasGlow ? brightSquareColor : squareColor;
            squareImage.raycastTarget = false;

            if(hasGlow)
            {
                Outline outline = squareObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.85f, 0.2f, 1f, 0.35f);
                outline.effectDistance = new Vector2(5f, 5f);
            }

            particles.Add(new SquareParticle
            {
                transform = squareRect,
                image = squareImage,
                speed = Random.Range(fallSpeedRange.x, fallSpeedRange.y),
                rotationSpeed = Random.Range(rotationSpeedRange.x, rotationSpeedRange.y),
                baseAlpha = squareImage.color.a,
                pulseOffset = Random.Range(0f, 20f),
                hasGlow = hasGlow
            });
        }
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
