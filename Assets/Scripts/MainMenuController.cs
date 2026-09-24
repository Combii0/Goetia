using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    public TMP_Text pressAnyButtonText;
    public MainMenuSaveSlotsUI saveSlotsUI;

    private bool menuStarted;
    public Color initialColor;

    void Awake()
    {
        menuStarted = false;
        pressAnyButtonText.color = initialColor;
    }

    void Update()
    {
        if(!menuStarted)
        {
            if(Keyboard.current.anyKey.wasPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame)
            {
                menuStarted = true;
                StartCoroutine(PressAnimation());
            }
        }
    }

    IEnumerator PressAnimation()
    {
        pressAnyButtonText.color = Color.yellow;
        yield return new WaitForSeconds(0.1f);

        pressAnyButtonText.color = new Color(1f, 0.3f, 0.7f);
        yield return new WaitForSeconds(0.1f);

        pressAnyButtonText.color = Color.white;
        yield return new WaitForSeconds(0.1f);

        pressAnyButtonText.gameObject.SetActive(false);

        saveSlotsUI.Open();
    }
}