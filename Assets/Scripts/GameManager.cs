using UnityEngine;
using System.Collections;
using System;

public class GameManager : MonoBehaviour
{
    private float healthPoints;
    public float maxHealthPoints;

    public float invulnerabilityTime;

    public PlayerController playerController;
    public bool isHit;
    private int ritualInvulnerabilityCount;
    public event Action PlayerDied;
    public event Action<float, float> DamageTaken;

    public float CurrentHealth => healthPoints;
    public bool IsDead { get; private set; }

    void Awake()
    {
        healthPoints = maxHealthPoints;
        IsDead = false;
    }

    public void TakeDamage(int damage)
    {
        if(isHit || ritualInvulnerabilityCount > 0 || IsDead)
        {
            return;
        }

        healthPoints -= damage;
        healthPoints = Mathf.Max(healthPoints, 0f);
        SceneMusicManager.PlayPlayerHitSfx();
        DamageTaken?.Invoke(healthPoints, maxHealthPoints);

        if(healthPoints <= 0)
        {
            healthPoints = 0;
            PlayerDeath();
        }

        Debug.Log("Vida: " + healthPoints);
        StartCoroutine(HitEffect());
    }

    public void PushRitualInvulnerability()
    {
        ritualInvulnerabilityCount++;
    }

    public void PopRitualInvulnerability()
    {
        ritualInvulnerabilityCount = Mathf.Max(0, ritualInvulnerabilityCount - 1);
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
        if(IsDead)
        {
            return;
        }

        IsDead = true;
        playerController.enabled = false;
        PlayerDied?.Invoke();
    }
}
