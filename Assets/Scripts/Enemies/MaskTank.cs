using UnityEngine;

public class MaskTank : MonoBehaviour
{
    public Transform player;
    private Animator animator;
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;

    public float smoothTime;
    private float velocityX;
    private float velocityY;

    public float maxTimer;
    private float timer;

    public float attackTimer;
    private float enemyTimer;

    public float sleepyPublicTimer;
    private float sleepyTimer;

    private bool isReturning;
    private bool isAttacking;

    private float oldY;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        timer = maxTimer;
        sleepyTimer = sleepyPublicTimer;

        isAttacking = false;
        enemyTimer = attackTimer;

    }

    void Update()
    {
        if(player != null && !isAttacking)
        {
            float newX = Mathf.SmoothDamp(transform.position.x, player.position.x, ref velocityX, smoothTime);
            transform.position = new Vector2(newX, transform.position.y);
        }

        if(timer <= 0)
        {
            animator.SetBool("isAttacking", true);
            enemyTimer -= Time.deltaTime;

            if(enemyTimer <= 0)
            {
                if(!isAttacking)
                {
                    oldY = transform.position.y;

                    isAttacking = true;
                }
                else
                {
                    Boing();
                }
            }
        }
        else{animator.SetBool("isAttacking", false);}

        if(isReturning)
        {
            boxCollider.enabled = false;

            sleepyTimer -= 1 * Time.deltaTime;

            if(sleepyTimer <= 0)
            {
                float returnY = Mathf.SmoothDamp(transform.position.y, oldY, ref velocityY, smoothTime);
                transform.position = new Vector2(transform.position.x, returnY);
        
                if(Mathf.Abs(transform.position.y - oldY) <= 0.01f)
                {
                    transform.position = new Vector2(transform.position.x, oldY);
                    isReturning = false;
                    sleepyTimer = sleepyPublicTimer;
                    velocityY = 0f;
                    boxCollider.enabled = true;
                }
            }
        }

        timer -= Time.deltaTime;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleFloorContact(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleFloorContact(collision);
    }

    private void HandleFloorContact(Collision2D collision)
    {
        if(!isAttacking || !collision.gameObject.CompareTag("Floor"))
        {
            return;
        }

        rb.linearVelocity = Vector2.zero;
        timer = maxTimer;
        enemyTimer = attackTimer;

        isAttacking = false;
        isReturning = true;

        animator.SetBool("isAttacking", false);
        SceneMusicManager.PlayMaskTankImpactSfx();
    }

    private void Boing()
    {
        rb.linearVelocity = Vector2.down * 10;
    }
}
