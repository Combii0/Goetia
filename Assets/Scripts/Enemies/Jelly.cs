using UnityEngine;
using System.Collections;

public class Jelly : MonoBehaviour
{
    private SpriteRenderer sprite;
    public Transform player;
    public PlayerController playerController;
    public Carnage carnage;
    
    private Rigidbody2D rb;
    private Animator animator;

    private bool isJumping;
    private bool canJump;

    public float jumpingForce;
    public float newPlayerJumpingForce;
    public float movingVelocity;

    public float jumpCooldown;

    private float playerPosition;
    private float playerInitialPositionY;

    public bool canDoubleJump;

    public Color jumpColor;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();

        isJumping = false;
        canJump = true;
        canDoubleJump = true;

        playerInitialPositionY = player.transform.position.y -1;
    }

    void Update()
    {

        if(transform.position.x <= player.position.x)
        {
            playerPosition = 1;
        }
        else
        {
            playerPosition = -1;
        }

        transform.position = new Vector3(transform.position.x + playerPosition * movingVelocity * Time.deltaTime, transform.position.y, transform.position.z);

        if(transform.position.y <= playerInitialPositionY && rb.linearVelocity.y <= 0 && canJump)
        {
            Jump();
            canJump = false;
            StartCoroutine(JellyJumpAvailableAgain());
        }

        if(rb.linearVelocity.y > 0.05f)
        {
            isJumping = true;
        }
        else
        {
            isJumping = false;
        }

        animator.SetBool("isJumping", isJumping);
    }


    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingForce);
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player") && canDoubleJump && carnage.isFreezed == false)
        {
            sprite.color = jumpColor;

            playerController.rb.linearVelocity = new Vector2(playerController.rb.linearVelocity.x, newPlayerJumpingForce);
            canDoubleJump = false;

            StartCoroutine(JellyJumpAvailable());
        }
    }


    IEnumerator JellyJumpAvailableAgain()
    {
        yield return new WaitForSeconds(jumpCooldown);

        canJump = true;

        sprite.color = Color.white;
    }


    IEnumerator JellyJumpAvailable()
    {
        yield return new WaitForSeconds(0.5f);

        canDoubleJump = true;
    }
}

//ADMITO QUE ESTE CODIGO ME DIO DURO EH