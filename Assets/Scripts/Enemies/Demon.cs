using UnityEngine;
using System.Collections;

public class Demon : MonoBehaviour
{
    public Transform player;
    public GameManager gameManager;

    private Animator animator;

    public LineRenderer blueLightning;
    public LineRenderer whiteLightning;

    public GameObject attackIndicator;

    public LayerMask playerLayer;

    public float maxTimer;
    private float timer;

    public float warningTime;
    public float lightningTime;

    public float lightningDistance;
    public float lightningWidth;

    public int lightningPoints;
    public float lightningRandomness;

    private bool isPreparing;
    private bool isAttacking;

    private Vector2 targetPosition;
    private Vector2 lightningDirection;
    private Vector2 lightningEndPosition;

    void Awake()
    {
        animator = GetComponent<Animator>();

        timer = maxTimer;

        isPreparing = false;
        isAttacking = false;

        blueLightning.enabled = false;
        whiteLightning.enabled = false;

        attackIndicator.SetActive(false);
    }

    void Update()
    {
        if(!isPreparing && !isAttacking)
        {
            timer -= Time.deltaTime;

            if(timer <= 0)
            {
                StartCoroutine(LightningAttack());
            }
        }

        animator.SetBool("isPreparing", isPreparing);
        animator.SetBool("isAttacking", isAttacking);
    }

    IEnumerator LightningAttack()
    {
        isPreparing = true;

        targetPosition = player.position;
        lightningDirection = (targetPosition - (Vector2)transform.position).normalized;
        lightningEndPosition = (Vector2)transform.position + lightningDirection * lightningDistance;

        attackIndicator.transform.position = targetPosition;
        attackIndicator.SetActive(true);

        yield return new WaitForSeconds(warningTime);

        isPreparing = false;
        isAttacking = true;

        attackIndicator.SetActive(false);

        blueLightning.enabled = true;
        whiteLightning.enabled = true;

        RaycastHit2D hit = Physics2D.BoxCast(transform.position, new Vector2(lightningDistance, lightningWidth), Mathf.Atan2(lightningDirection.y, lightningDirection.x) * Mathf.Rad2Deg, lightningDirection, 0, playerLayer);

        if(hit.collider != null)
        {
            gameManager.TakeDamage(1);
        }

        float lightningTimer = lightningTime;

        while(lightningTimer > 0)
        {
            CreateLightning();

            lightningTimer -= 0.04f;

            yield return new WaitForSeconds(0.04f);
        }

        blueLightning.enabled = false;
        whiteLightning.enabled = false;

        isAttacking = false;
        timer = maxTimer;
    }

    void CreateLightning()
    {
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
                float randomOffset = Random.Range(-lightningRandomness, lightningRandomness);
                point += perpendicular * randomOffset;
            }

            blueLightning.SetPosition(i, point);
            whiteLightning.SetPosition(i, point);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        if(lightningDirection != Vector2.zero)
        {
            Gizmos.DrawLine(transform.position, lightningEndPosition);
        }
    }
}