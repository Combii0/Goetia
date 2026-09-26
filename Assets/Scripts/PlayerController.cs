using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerController : MonoBehaviour
{
    public Rigidbody2D rb;
    public SpriteRenderer sprite;
    public Animator animator;
    private BoxCollider2D boxCollider;

    public float movementVelocity;
    public float jumpingForce;

    private float movementInput;
    private bool isGrounded;
    private bool hasExtraJump;
    private bool extraJumpAvailable;
    public event Action DoubleJumpUsed;

    public float raycastLenght;
    public LayerMask groundLayer;

    public float pointing;

    public float coyoteTime;
    private float coyoteTimeCounter;

    // ANIMATIONS //

    public bool isJumping;
    private bool isRunning;
    private bool isFalling;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        if(Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            movementInput = -1;
            sprite.flipX = true;
            isRunning = true;
            pointing = -1;
        }
        else if(Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            movementInput = 1;
            sprite.flipX = false;
            isRunning = true;
            pointing = 1;
        }
        else{movementInput = 0; isRunning = false;}

        rb.linearVelocity = new Vector2(movementInput * movementVelocity, rb.linearVelocity.y);
        
        //  HOLAAA, SOY SANTIAGO, si lo lees, sabes leer!

        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, raycastLenght, groundLayer);
        isGrounded = hit.collider != null;

        if(isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            extraJumpAvailable = hasExtraJump;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if(Keyboard.current.spaceKey.wasPressedThisFrame && coyoteTimeCounter > 0)
        {
            Jump();
            coyoteTimeCounter = 0;
        }
        else if(Keyboard.current.spaceKey.wasPressedThisFrame && hasExtraJump && extraJumpAvailable)
        {
            Jump();
            extraJumpAvailable = false;
            DoubleJumpUsed?.Invoke();
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

    public void SetHitColor(bool damaged)
    {
        if(damaged)
        {
            sprite.color = Color.red;
        }
        else{sprite.color = Color.white;}
    }

    /// <summary>Grants one additional mid-air jump until this controller is disabled.</summary>
    public void EnableDoubleJump()
    {
        hasExtraJump = true;
        extraJumpAvailable = true;
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpingForce, ForceMode2D.Impulse);
        isJumping = true;
        isFalling = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * raycastLenght);
    }
}
