using System.Collections;
using UnityEngine;

/// <summary>Shared penta_0 presentation used by ritual cards.</summary>
public sealed class RitualPentaEffect : MonoBehaviour
{
    [SerializeField] private float rotationDegreesPerSecond = 28f;
    private SpriteRenderer spriteRenderer;

    public static RitualPentaEffect Create(Transform owner, Sprite pentaSprite, int sortingOrder)
    {
        if(pentaSprite == null)
        {
            Debug.LogWarning("Assign penta_0 to the ritual's Penta Sprite field.");
            return null;
        }

        GameObject effect = new GameObject("Ritual Penta");
        effect.transform.SetParent(owner, false);
        effect.transform.localPosition = Vector3.zero;

        SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
        renderer.sprite = pentaSprite;
        renderer.sortingLayerID = owner.GetComponentInChildren<SpriteRenderer>() != null
            ? owner.GetComponentInChildren<SpriteRenderer>().sortingLayerID : 0;
        renderer.sortingOrder = sortingOrder;
        renderer.color = new Color(1f, 1f, 1f, 0f);

        RitualPentaEffect ritualEffect = effect.AddComponent<RitualPentaEffect>();
        ritualEffect.spriteRenderer = renderer;
        return ritualEffect;
    }

    public static RitualPentaEffect CreateAbove(Transform owner, Sprite pentaSprite, int sortingOrder, float scale = 1f)
    {
        RitualPentaEffect effect = Create(owner, pentaSprite, sortingOrder);
        if(effect != null)
        {
            // The ritual seal belongs at the target's centre, not above its head.
            effect.transform.localPosition = Vector3.zero;
            effect.transform.localScale = Vector3.one * scale;
        }
        return effect;
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, rotationDegreesPerSecond * Time.deltaTime);
    }

    public Coroutine FadeInAndOut(MonoBehaviour host, float visibleDuration, bool blinkAtEnd)
    {
        return host.StartCoroutine(FadeRoutine(visibleDuration, blinkAtEnd));
    }

    private IEnumerator FadeRoutine(float visibleDuration, bool blinkAtEnd)
    {
        yield return Fade(0f, 1f, 0.12f);
        yield return new WaitForSeconds(Mathf.Max(0f, visibleDuration));

        if(blinkAtEnd)
        {
            for(int i = 0; i < 3; i++)
            {
                yield return Fade(1f, 0.2f, 0.08f);
                yield return Fade(0.2f, 1f, 0.08f);
            }
        }

        yield return Fade(spriteRenderer.color.a, 0f, 0.12f);
        Destroy(gameObject);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }
}
