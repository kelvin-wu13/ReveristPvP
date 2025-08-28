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

    public void ReturnToMainMenu()
    {
        Debug.Log("Returning to Main Menu...");
        Time.timeScale = 1f;
        fadeController.FadeOutAndLoadScene("MainMenu");
    }

    public void CharacterSelectMenu()
    {
        fadeController.FadeOutAndLoadScene("CharacterSelect");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
