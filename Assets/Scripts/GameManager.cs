using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    private float healthPoints;
    public float maxHealthPoints;

    public float invulnerabilityTime;

    public PlayerController playerController;
    public bool isHit;

    void Awake()
    {
        healthPoints = maxHealthPoints;
    }

    public void TakeDamage(int damage)
    {
        if(isHit)
        {
            return;
        }

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
        isHit = true;

        float elapsedTime = 0f;
        float blinkTime = 0.1f;

        while(elapsedTime < invulnerabilityTime)
        {
            playerController.SetHitColor(true);
            yield return new WaitForSeconds(blinkTime);

            playerController.SetHitColor(false);
            yield return new WaitForSeconds(blinkTime);

            elapsedTime += blinkTime * 2f;
        }

        playerController.SetHitColor(false);
        isHit = false;
    }

    void PlayerDeath()
    {
        playerController.enabled = false;
    }
}
