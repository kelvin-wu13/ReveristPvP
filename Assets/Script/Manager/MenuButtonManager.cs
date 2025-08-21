using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButtonManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject confirmationPanel;


    private void Start()
    {
        if (confirmationPanel != null)
                confirmationPanel.SetActive(false); // hide on start
    }

    // Called when "Main Menu" button is clicked
    public void OnMainMenuButtonClicked()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(true);
    }

    // Called when "Confirm" button is clicked in the confirmation panel
    public void OnConfirmMainMenu()
    {
        SceneManager.LoadSceneAsync(0);
    }

    // Called when "Back" button is clicked in the confirmation panel
    public void OnCancel()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }
}
