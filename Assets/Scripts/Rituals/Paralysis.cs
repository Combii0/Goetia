using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Ritual card: freezes every enemy and its abilities temporarily.</summary>
public sealed class Paralysis : MonoBehaviour
{
    [SerializeField] private Sprite pentaSprite;
    [SerializeField] private int pentaSortingOrder = 50;
    [SerializeField] private float duration = 5f;
    [SerializeField, Min(0f)] private float initialActivationDelay = 4f;
    [SerializeField, Min(1)] private int usesPerRound = 2;
    private bool isActive;
    private int usesThisRound;
    private float activationAvailableAt;

    private void OnEnable()
    {
        activationAvailableAt = Time.time + initialActivationDelay;
    }

    private void Update()
    {
        if(!isActive
            && usesThisRound < usesPerRound
            && Time.time >= activationAvailableAt
            && RitualCardUtility.IsActive("paralysis")
            && RitualCardUtility.ActivationPressed())
        {
            usesThisRound++;
            StartCoroutine(Activate());
        }
    }

    private IEnumerator Activate()
    {
        isActive = true;
        List<Behaviour> disabledAbilities = new List<Behaviour>();
        List<Rigidbody2D> frozenBodies = new List<Rigidbody2D>();
        FreezeEnemies(FindObjectsByType<Demon>(), disabledAbilities, frozenBodies);
        FreezeEnemies(FindObjectsByType<Jelly>(), disabledAbilities, frozenBodies);
        FreezeEnemies(FindObjectsByType<Carnage>(), disabledAbilities, frozenBodies);
        FreezeEnemies(FindObjectsByType<MaskTank>(), disabledAbilities, frozenBodies);

        yield return new WaitForSeconds(duration);

        foreach(Rigidbody2D body in frozenBodies) if(body != null) body.simulated = true;
        foreach(Behaviour ability in disabledAbilities) if(ability != null) ability.enabled = true;
        isActive = false;
    }

    private void FreezeEnemies<T>(T[] enemies, List<Behaviour> disabledAbilities, List<Rigidbody2D> frozenBodies) where T : Behaviour
    {
        foreach(T ability in enemies)
        {
            disabledAbilities.Add(ability);
            ability.enabled = false;
            Rigidbody2D body = ability.GetComponent<Rigidbody2D>();
            if(body != null && !frozenBodies.Contains(body))
            {
                frozenBodies.Add(body);
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
            }

            RitualPentaEffect effect = RitualPentaEffect.CreateAbove(ability.transform, pentaSprite, pentaSortingOrder);
            if(effect != null) effect.FadeInAndOut(this, Mathf.Max(0f, duration - 0.72f), true);
        }
    }
}
