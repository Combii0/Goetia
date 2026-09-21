using UnityEngine;

public class DamageCollider : MonoBehaviour
{
    public GameManager gameManager;

    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            gameManager.TakeDamage(1);
        }
    }
}
