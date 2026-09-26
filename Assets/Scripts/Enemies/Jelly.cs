using System.Collections;
using UnityEngine;

public class Jelly : MonoBehaviour
{
    private SpriteRenderer sprite;
    public Transform player;
    public PlayerController playerController;
    public Carnage carnage;

    private Rigidbody2D rb;
    private Animator animator;
    private Camera roomCamera;
    private bool canJump;
    private float movementDirection;
    private float nextDirectionChange;

    [Header("Jump")]
    public float jumpingForce;
    public float newPlayerJumpingForce;
    public float jumpCooldown;
    public bool canDoubleJump;
    public Color jumpColor;

    [Header("Movement")]
    public float movingVelocity;
    [Range(0f, 1f)] public float playerPursuitChance = 0.45f;
    public Vector2 directionChangeInterval = new Vector2(1.4f, 3.2f);

    [Header("Random Room Spawn")]
    public LayerMask groundLayer = 1 << 6;
    public float spawnHorizontalPadding = 0.7f;
    public float minimumPlayerSpawnDistance = 2.2f;
    public float minimumJellySpawnDistance = 1.2f;
    public int spawnAttempts = 24;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        roomCamera = Camera.main;
        canJump = true;
        canDoubleJump = true;
        ChooseDirection();
    }

    private void OnEnable()
    {
        canJump = true;
        canDoubleJump = true;
        ChooseDirection();
    }

    private void Update()
    {
        if(Time.time >= nextDirectionChange)
        {
            ChooseDirection();
        }

        if(IsGrounded() && canJump)
        {
            Jump();
            canJump = false;
            StartCoroutine(JellyJumpAvailableAgain());
        }

        if(animator != null && rb != null)
        {
            animator.SetBool("isJumping", rb.linearVelocity.y > 0.05f);
        }
    }

    private void FixedUpdate()
    {
        if(rb != null)
        {
            rb.linearVelocity = new Vector2(movementDirection * movingVelocity, rb.linearVelocity.y);
        }
    }

    public void RandomizeSpawnPoint()
    {
        if(roomCamera == null) roomCamera = Camera.main;
        if(roomCamera == null) return;

        float cameraDepth = Mathf.Abs(roomCamera.transform.position.z);
        Vector3 bottomLeft = roomCamera.ViewportToWorldPoint(new Vector3(0f, 0f, cameraDepth));
        Vector3 topRight = roomCamera.ViewportToWorldPoint(new Vector3(1f, 1f, cameraDepth));
        float minX = bottomLeft.x + spawnHorizontalPadding;
        float maxX = topRight.x - spawnHorizontalPadding;
        float rayStartY = topRight.y + 1f;

        for(int attempt = 0; attempt < spawnAttempts; attempt++)
        {
            RaycastHit2D ground = Physics2D.Raycast(new Vector2(Random.Range(minX, maxX), rayStartY), Vector2.down, 30f, groundLayer);
            if(ground.collider == null) continue;

            Vector2 candidate = ground.point + Vector2.up * GetSpawnHeight();
            if(player != null && Vector2.Distance(candidate, player.position) < minimumPlayerSpawnDistance) continue;
            if(IsTooCloseToAnotherJelly(candidate)) continue;

            if(rb != null)
            {
                rb.position = candidate;
                rb.linearVelocity = Vector2.zero;
            }
            else transform.position = candidate;

            ChooseDirection();
            return;
        }
    }

    private void ChooseDirection()
    {
        bool pursuePlayer = player != null && Random.value < playerPursuitChance;
        movementDirection = pursuePlayer
            ? (player.position.x >= transform.position.x ? 1f : -1f)
            : (Random.value < 0.5f ? -1f : 1f);
        nextDirectionChange = Time.time + Random.Range(directionChangeInterval.x, directionChangeInterval.y);
    }

    private bool IsGrounded()
    {
        if(rb == null || rb.linearVelocity.y > 0.02f)
        {
            return false;
        }

        Collider2D collider = GetComponent<Collider2D>();
        float halfHeight = collider == null ? 0.4f : collider.bounds.extents.y;
        Vector2 rayOrigin = rb.position + Vector2.down * Mathf.Max(0f, halfHeight - 0.04f);
        return Physics2D.Raycast(rayOrigin, Vector2.down, 0.14f, groundLayer).collider != null;
    }

    private float GetSpawnHeight()
    {
        Collider2D collider = GetComponent<Collider2D>();
        return collider == null ? 0.6f : collider.bounds.extents.y + 0.04f;
    }

    private bool IsTooCloseToAnotherJelly(Vector2 candidate)
    {
        Jelly[] jellies = FindObjectsByType<Jelly>();
        for(int i = 0; i < jellies.Length; i++)
        {
            if(jellies[i] != this && Vector2.Distance(candidate, jellies[i].transform.position) < minimumJellySpawnDistance)
            {
                return true;
            }
        }
        return false;
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingForce);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(!other.CompareTag("Player") || !canDoubleJump || (carnage != null && carnage.isFreezed)) return;

        if(sprite != null) sprite.color = jumpColor;
        if(playerController != null && playerController.rb != null)
        {
            playerController.rb.linearVelocity = new Vector2(playerController.rb.linearVelocity.x, newPlayerJumpingForce);
        }

        canDoubleJump = false;
        StartCoroutine(JellyJumpAvailable());
    }

    private IEnumerator JellyJumpAvailableAgain()
    {
        yield return new WaitForSeconds(jumpCooldown);
        canJump = true;
        if(sprite != null) sprite.color = Color.white;
    }

    private IEnumerator JellyJumpAvailable()
    {
        yield return new WaitForSeconds(0.5f);
        canDoubleJump = true;
    }
}
