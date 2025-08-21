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
        fadeController.FadeOutAndLoadScene("Main Menu Demo");
    }

    public void CharacterSelectDemo()
    {
        fadeController.FadeOutAndLoadScene("Character Select Demo");
    }

    public void SkillArsenalDemo()
    {
        fadeController.FadeOutAndLoadScene("ArsenalDemo");
    }

    public void SkillSaberDemo()
    {
        fadeController.FadeOutAndLoadScene("SaberDemo");
    }

    public void SkillInterfereDemo()
    {
        fadeController.FadeOutAndLoadScene("InterfereDemo");
    }

    public void TrainingSceneDemo()
    {
        fadeController.FadeOutAndLoadScene("TrainingScene");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
