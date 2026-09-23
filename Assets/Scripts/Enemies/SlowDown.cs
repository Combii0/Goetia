using UnityEngine;

public class SlowDown : MonoBehaviour
{
    public PlayerController playerController;
    public Jelly jelly;

    public float newVelocity;

    private float initialVelocity;

    void Awake()
    {
        initialVelocity = playerController.movementVelocity;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            playerController.movementVelocity = newVelocity;
            jelly.canDoubleJump = false;
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            jelly.canDoubleJump = true;
            playerController.movementVelocity = initialVelocity;
        }    
    }
}
