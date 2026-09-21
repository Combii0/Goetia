using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    private float healthPoints;
    public float maxHealthPoints;

    public float invulnarabilityTime;

    public PlayerController playerController;

    void Awake()
    {
        healthPoints = maxHealthPoints;
    }

    public void TakeDamage(int damage)
    {
        healthPoints -= damage;

        if(healthPoints <= 0)
        {
            healthPoints = 0;
            PlayerDeath();
        }

        Debug.Log("Vida: " + healthPoints);
        StartCoroutine(HitEffect());
    }

    IEnumerator HitEffect()
    {
        playerController.SetHitColor(true);
        yield return new WaitForSeconds(invulnarabilityTime);

        playerController.SetHitColor(false);
    }

    void PlayerDeath()
    {
        playerController.enabled = false;
    }
}
