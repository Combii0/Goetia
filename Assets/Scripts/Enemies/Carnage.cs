using UnityEngine;
using System.Collections;

public class Carnage : MonoBehaviour
{
    private float velocityX;
    private float velocityY;

    public Transform player;
    public PlayerController playerController;
    public LayerMask playerLayer;

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;

    public float followSpeed;
    public float raycastLenght;
    private bool isLooking;

    public float maxTimer;

    public float freezedTime;

    private float playerPosition;

    private bool canFreeze;
    private bool isFreezed;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        
        canFreeze = true;
        isLooking = false;
    }

    void Update()
    {
        if(transform.position.x >= player.position.x)
        {
            playerPosition = 1;
            sprite.flipX = false;
        }
        else
        {
            playerPosition = -1;
            sprite.flipX = true;
        }
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.left * playerPosition, raycastLenght, playerLayer);

        if(playerController.pointing == 1 && playerPosition == 1 || playerController.pointing == -1 && playerPosition == -1)
        {
            isLooking = hit.collider != null;

            if(canFreeze && isLooking)
            {
                StartCoroutine(FreezePlayer());
            }
        }
        else{isLooking = false;}

        float newX = Mathf.SmoothDamp(transform.position.x, player.position.x, ref velocityX, 3/followSpeed);
        float newY = Mathf.SmoothDamp(transform.position.y, player.position.y, ref velocityY, 3/followSpeed);

        transform.position = new Vector2(newX, newY);

        animator.SetBool("isLooking", isLooking);
        playerController.animator.SetBool("isFreezed", isFreezed);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * playerPosition * raycastLenght);
    }

    IEnumerator FreezePlayer()
    {
        canFreeze = false;
        isFreezed = true;

        playerController.rb.linearVelocity = Vector2.zero;
        playerController.enabled = false;

        yield return new WaitForSeconds(freezedTime);

        playerController.enabled = true;
        isFreezed = false;

        yield return new WaitForSeconds(maxTimer);

        canFreeze = true;
}
}
