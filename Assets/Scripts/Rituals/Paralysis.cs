using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Ritual card: freezes every enemy and its abilities for two seconds.</summary>
public sealed class Paralysis : MonoBehaviour
{
    [SerializeField] private Sprite pentaSprite;
    [SerializeField] private int pentaSortingOrder = 50;
    [SerializeField] private float duration = 2f;
    private bool isActive;
    private bool usedThisRound;

    private void Update()
    {
        if(!isActive && !usedThisRound && RitualCardUtility.IsActive("paralysis") && RitualCardUtility.ActivationPressed())
        {
            usedThisRound = true;
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
