using System.Collections;
using UnityEngine;

public class Demon : MonoBehaviour
{
    public Transform player;
    public GameManager gameManager;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    // DemonIdle.anim animates this object's local position.  Moving its parent
    // keeps that idle bob intact instead of having the Animator snap the Demon
    // back to its authored spawn point on the following frame.
    private Transform teleportAnchor;

    public LineRenderer blueLightning;
    public LineRenderer whiteLightning;
    public GameObject attackIndicator;
    public LayerMask playerLayer;

    [Header("Attack")]
    public float maxTimer;
    public float warningTime;
    public float lightningTime;
    public float lightningDistance;
    public float lightningWidth;
    public int lightningPoints;
    public float lightningRandomness;

    [Header("Teleport Area")]
    public float horizontalMargin = 1.5f;
    public float topMargin = 1.5f;
    public float bottomMargin = 2f;
    public float minimumPlayerDistance = 2f;
    public float minimumDemonDistance = 1.5f;
    public int teleportAttempts = 20;

    [Header("Teleport Animation")]
    [SerializeField] private float teleportDelayAfterAttack = 0.5f;
    public float disappearTime = 0.2f;
    public float teleportPause = 0.08f;
    public float appearTime = 0.25f;
    public float teleportMinScale = 0.15f;

    private float timer;
    private bool isPreparing;
    private bool isAttacking;
    private bool isTeleporting;
    private Vector2 lightningDirection;
    private Vector2 lightningEndPosition;
    private Vector3 initialScale;
    private Color initialColor;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        teleportAnchor = transform.parent != null ? transform.parent : transform;
        initialScale = transform.localScale;
        initialColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        timer = maxTimer;

        if(blueLightning != null) blueLightning.enabled = false;
        if(whiteLightning != null) whiteLightning.enabled = false;
        if(attackIndicator != null) attackIndicator.SetActive(false);
    }

    private void OnEnable()
    {
        timer = maxTimer;
        isPreparing = false;
        isAttacking = false;
        isTeleporting = false;

        if(spriteRenderer != null)
        {
            transform.localScale = initialScale;
            spriteRenderer.color = initialColor;
        }

        if(blueLightning != null) blueLightning.enabled = false;
        if(whiteLightning != null) whiteLightning.enabled = false;
        if(attackIndicator != null) attackIndicator.SetActive(false);
    }

    private void Update()
    {
        if(!isPreparing && !isAttacking && !isTeleporting)
        {
            timer -= Time.deltaTime;
            if(timer <= 0f && player != null)
            {
                StartCoroutine(LightningAttack());
            }
        }

        if(animator != null)
        {
            animator.SetBool("isPreparing", isPreparing);
            animator.SetBool("isAttacking", isAttacking);
        }
    }

    private IEnumerator LightningAttack()
    {
        isPreparing = true;
        Vector2 targetPosition = player.position;
        lightningDirection = (targetPosition - (Vector2)transform.position).normalized;
        lightningEndPosition = (Vector2)transform.position + lightningDirection * lightningDistance;

        if(attackIndicator != null)
        {
            attackIndicator.transform.position = targetPosition;
            attackIndicator.SetActive(true);
        }

        yield return new WaitForSeconds(warningTime);

        isPreparing = false;
        isAttacking = true;
        if(attackIndicator != null) attackIndicator.SetActive(false);
        if(blueLightning != null) blueLightning.enabled = true;
        if(whiteLightning != null) whiteLightning.enabled = true;

        CheckLightningDamage();

        float lightningTimer = lightningTime;
        while(lightningTimer > 0f)
        {
            CreateLightning();
            lightningTimer -= 0.04f;
            yield return new WaitForSeconds(0.04f);
        }

        if(blueLightning != null) blueLightning.enabled = false;
        if(whiteLightning != null) whiteLightning.enabled = false;
        isAttacking = false;

        yield return new WaitForSeconds(teleportDelayAfterAttack);
        yield return StartCoroutine(TeleportAnimation());
        timer = maxTimer;
    }

    private void CheckLightningDamage()
    {
        if(gameManager == null)
        {
            return;
        }

        Vector2 startPosition = transform.position;
        Vector2 middlePosition = (startPosition + lightningEndPosition) * 0.5f;
        float angle = Mathf.Atan2(lightningDirection.y, lightningDirection.x) * Mathf.Rad2Deg;
        Collider2D hit = Physics2D.OverlapBox(middlePosition, new Vector2(lightningDistance, lightningWidth), angle, playerLayer);

        if(hit != null)
        {
            gameManager.TakeDamage(1);
        }
    }

    private void CreateLightning()
    {
        if(lightningPoints < 2 || blueLightning == null || whiteLightning == null)
        {
            return;
        }

        blueLightning.positionCount = lightningPoints;
        whiteLightning.positionCount = lightningPoints;

        Vector2 startPosition = transform.position;
        Vector2 perpendicular = new Vector2(-lightningDirection.y, lightningDirection.x).normalized;

        for(int i = 0; i < lightningPoints; i++)
        {
            float percentage = (float)i / (lightningPoints - 1);
            Vector2 point = Vector2.Lerp(startPosition, lightningEndPosition, percentage);

            if(i != 0 && i != lightningPoints - 1)
            {
                point += perpendicular * Random.Range(-lightningRandomness, lightningRandomness);
            }

            blueLightning.SetPosition(i, point);
            whiteLightning.SetPosition(i, point);
        }
    }

    private IEnumerator TeleportAnimation()
    {
        isTeleporting = true;

        if(spriteRenderer == null)
        {
            SetTeleportPosition(GetRandomTeleportPosition());
            isTeleporting = false;
            yield break;
        }

        float elapsed = 0f;
        while(elapsed < disappearTime)
        {
            elapsed += Time.deltaTime;
            float percentage = Mathf.Clamp01(elapsed / disappearTime);
            transform.localScale = initialScale * Mathf.Lerp(1f, teleportMinScale, percentage);
            spriteRenderer.color = new Color(initialColor.r, initialColor.g, initialColor.b, Mathf.Lerp(1f, 0f, percentage));
            yield return null;
        }

        transform.localScale = initialScale * teleportMinScale;
        spriteRenderer.color = new Color(initialColor.r, initialColor.g, initialColor.b, 0f);
        SetTeleportPosition(GetRandomTeleportPosition());
        yield return new WaitForSeconds(teleportPause);

        elapsed = 0f;
        while(elapsed < appearTime)
        {
            elapsed += Time.deltaTime;
            float percentage = Mathf.Clamp01(elapsed / appearTime);
            transform.localScale = initialScale * Mathf.Lerp(teleportMinScale, 1f, percentage);
            spriteRenderer.color = new Color(initialColor.r, initialColor.g, initialColor.b, Mathf.Lerp(0f, 1f, percentage));
            yield return null;
        }

        transform.localScale = initialScale;
        spriteRenderer.color = initialColor;
        isTeleporting = false;
    }

    private Vector2 GetRandomTeleportPosition()
    {
        if(mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if(mainCamera == null)
        {
            return transform.position;
        }

        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));
        float minX = bottomLeft.x + horizontalMargin;
        float maxX = topRight.x - horizontalMargin;
        float minY = bottomLeft.y + bottomMargin;
        float maxY = topRight.y - topMargin;

        if(minX >= maxX || minY >= maxY)
        {
            return transform.position;
        }

        for(int attempt = 0; attempt < teleportAttempts; attempt++)
        {
            Vector2 candidate = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
            if(player != null && Vector2.Distance(candidate, player.position) < minimumPlayerDistance)
            {
                continue;
            }

            if(!IsTooCloseToAnotherDemon(candidate))
            {
                return candidate;
            }
        }

        return transform.position;
    }

    private void SetTeleportPosition(Vector2 destination)
    {
        if(teleportAnchor == null)
        {
            teleportAnchor = transform.parent != null ? transform.parent : transform;
        }

        if(teleportAnchor == transform)
        {
            transform.position = destination;
            return;
        }

        // Preserve the animator's current local offset while putting the
        // visible Demon exactly at the selected world-space destination.
        Vector3 currentOffset = transform.position - teleportAnchor.position;
        teleportAnchor.position = (Vector3)destination - currentOffset;
    }

    private bool IsTooCloseToAnotherDemon(Vector2 position)
    {
        Demon[] demons = FindObjectsByType<Demon>();
        for(int i = 0; i < demons.Length; i++)
        {
            if(demons[i] != this && Vector2.Distance(position, demons[i].transform.position) < minimumDemonDistance)
            {
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if(lightningDirection != Vector2.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, lightningEndPosition);
        }
    }
}
