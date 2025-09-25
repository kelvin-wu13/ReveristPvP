using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    public FadeController fadeController;

    private void Awake()
    {
        if (fadeController == null)
            Debug.LogWarning("FadeController not assigned in MainMenu.");
    }

    public void SettingsMenu()
    {
        if (fadeController) fadeController.FadeOutAndLoadScene("Settings");
        else SceneManager.LoadScene("Settings");
    }

    public void CharacterSelectMenu()
    {
        if (fadeController) fadeController.FadeOutAndLoadScene("CharacterSelect");
        else SceneManager.LoadScene("CharacterSelect");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
