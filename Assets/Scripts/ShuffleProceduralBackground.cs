using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A card-table-like field of squares that orbit, wobble and breathe behind the
/// shuffle UI. Everything is generated from UI primitives at runtime.
/// </summary>
public sealed class ShuffleProceduralBackground : MonoBehaviour
{
    public int squareCount = 52;
    public Vector2 squareSizeRange = new Vector2(18f, 86f);
    public Vector2 orbitRadiusRange = new Vector2(180f, 820f);
    public Vector2 orbitSpeedRange = new Vector2(-0.18f, 0.18f);
    public Color backgroundColor = new Color(0.018f, 0.006f, 0.045f, 1f);
    public Color squareColor = new Color(0.30f, 0.07f, 0.46f, 0.42f);
    public Color highlightColor = new Color(0.88f, 0.18f, 0.72f, 0.68f);

    private readonly List<Square> squares = new List<Square>();
    private RectTransform canvasRect;

    private sealed class Square
    {
        public RectTransform rect;
        public Image image;
        public float radius;
        public float angle;
        public float orbitSpeed;
        public float wobble;
        public float size;
        public float rotationSpeed;
        public bool highlighted;
    }

    private void Awake()
    {
        BuildCanvas();
        BuildSquares();
    }

    private void Update()
    {
        for(int i = 0; i < squares.Count; i++)
        {
            Square square = squares[i];
            square.angle += square.orbitSpeed * Time.unscaledDeltaTime;

            float wobble = Mathf.Sin(Time.unscaledTime * 0.65f + square.wobble) * 22f;
            Vector2 position = new Vector2(
                Mathf.Cos(square.angle) * (square.radius + wobble),
                Mathf.Sin(square.angle) * (square.radius + wobble) * 0.56f);

            square.rect.anchoredPosition = position;
            square.rect.Rotate(0f, 0f, square.rotationSpeed * Time.unscaledDeltaTime);

            float breathe = 1f + Mathf.Sin(Time.unscaledTime * 1.4f + square.wobble) * 0.1f;
            square.rect.localScale = Vector3.one * breathe;

            Color color = square.image.color;
            color.a = (square.highlighted ? 0.55f : 0.3f) + Mathf.Sin(Time.unscaledTime * 1.8f + square.wobble) * 0.12f;
            square.image.color = color;
        }
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("ShuffleViscousSky");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 50f;
        canvas.sortingOrder = -100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasRect = canvasObject.GetComponent<RectTransform>();

        GameObject backgroundObject = new GameObject("ShufflePurpleBackground");
        backgroundObject.transform.SetParent(canvasObject.transform, false);
        RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        Image background = backgroundObject.AddComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = false;
    }

    private void BuildSquares()
    {
        for(int i = 0; i < squareCount; i++)
        {
            GameObject squareObject = new GameObject("SwirlingSquare_" + i);
            squareObject.transform.SetParent(canvasRect, false);

            RectTransform rect = squareObject.AddComponent<RectTransform>();
            float size = Random.Range(squareSizeRange.x, squareSizeRange.y);
            rect.sizeDelta = new Vector2(size, size);
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            Image image = squareObject.AddComponent<Image>();
            bool highlighted = Random.value < 0.2f;
            image.color = highlighted ? highlightColor : squareColor;
            image.raycastTarget = false;

            Outline outline = squareObject.AddComponent<Outline>();
            outline.effectColor = highlighted ? new Color(1f, 0.2f, 0.8f, 0.4f) : new Color(0.55f, 0.15f, 0.8f, 0.18f);
            outline.effectDistance = highlighted ? new Vector2(8f, 8f) : new Vector2(3f, 3f);

            squares.Add(new Square
            {
                rect = rect,
                image = image,
                radius = Random.Range(orbitRadiusRange.x, orbitRadiusRange.y),
                angle = Random.Range(0f, Mathf.PI * 2f),
                orbitSpeed = Random.Range(orbitSpeedRange.x, orbitSpeedRange.y),
                wobble = Random.Range(0f, 30f),
                size = size,
                rotationSpeed = Random.Range(-35f, 35f),
                highlighted = highlighted
            });
        }
    }
}
