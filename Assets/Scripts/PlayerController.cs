using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Animator animator;
    private BoxCollider2D boxCollider;

    public float movementVelocity;
    public float jumpingForce;

    private float movementInput;
    private bool isGrounded = true;

    public float raycastLenght;
    public LayerMask groundLayer;

    // ANIMATIONS //

    private bool isJumping;
    private bool isRunning;
    private bool isFalling;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        if(Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            movementInput = -1;
            sprite.flipX = true;
            isRunning = true;
        }
        else if(Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            movementInput = 1;
            sprite.flipX = false;
            isRunning = true;
        }
        else{movementInput = 0; isRunning = false;}

        rb.linearVelocity = new Vector2(movementInput * movementVelocity, rb.linearVelocity.y);
        
        //  HOLAAA, SOY SANTIAGO, si lo lees, sabes leer!

        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, raycastLenght, groundLayer);
        isGrounded = hit.collider != null;

        if(Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded == true)
        {
            rb.AddForce(Vector2.up * jumpingForce, ForceMode2D.Impulse);

            isJumping = true;
            isFalling = false;
        }
        
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        if(isJumping && state.IsName("GoetiaJump") && state.normalizedTime >= 1f)
        {
            isJumping = false;

            if(!isGrounded)
            {
                isFalling = true;
            }
        }

        if(isFalling && isGrounded)
        {
            isFalling = false;
        }

        animator.SetBool("isJumping", isJumping);
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isFalling", isFalling);
        animator.SetBool("isGrounded", isGrounded);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * raycastLenght);
    }
}
