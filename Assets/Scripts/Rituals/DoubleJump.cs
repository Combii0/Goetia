using UnityEngine;

/// <summary>Ritual card: grants the player a permanent extra air jump for this Room.</summary>
public sealed class DoubleJump : MonoBehaviour
{
    [SerializeField] private Sprite pentaSprite;
    [SerializeField] private int pentaSortingOrder = 7;
    private PlayerController player;

    private void Start()
    {
        if(!RitualCardUtility.IsActive("doublejump")) return;

        player = GetComponent<PlayerController>();
        if(player == null) return;

        player.EnableDoubleJump();
        player.DoubleJumpUsed += ShowDoubleJumpRitual;
    }

    private void OnDisable()
    {
        if(player != null) player.DoubleJumpUsed -= ShowDoubleJumpRitual;
    }

    private void ShowDoubleJumpRitual()
    {
        RitualPentaEffect effect = RitualPentaEffect.CreateAbove(player.transform, pentaSprite, pentaSortingOrder);
        if(effect != null) effect.FadeInAndOut(this, 0.18f, false);
    }
}
