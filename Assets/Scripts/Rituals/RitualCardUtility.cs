using UnityEngine;
using UnityEngine.InputSystem;

public static class RitualCardUtility
{
    public static bool IsRitual(string cardId)
    {
        return cardId == "doublejump" || cardId == "invulnerability" || cardId == "paralysis";
    }

    public static bool IsRitual(CardData card)
    {
        return card != null && string.Equals(card.category, "Ritual", System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsActive(string cardId)
    {
        int slot = PlayerPrefs.GetInt("Goetia_CurrentSlot", -1);
        return slot >= 0 && PlayerPrefs.GetString("Goetia_Slot_" + slot + "_ActiveRitual", string.Empty) == cardId;
    }

    public static void SetActive(string cardId)
    {
        int slot = PlayerPrefs.GetInt("Goetia_CurrentSlot", -1);
        if(slot >= 0) PlayerPrefs.SetString("Goetia_Slot_" + slot + "_ActiveRitual", cardId);
    }

    public static bool ActivationPressed()
    {
        return (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Keyboard.current != null && (Keyboard.current.leftShiftKey.wasPressedThisFrame
                || Keyboard.current.rightShiftKey.wasPressedThisFrame
                || Keyboard.current.eKey.wasPressedThisFrame));
    }
}
