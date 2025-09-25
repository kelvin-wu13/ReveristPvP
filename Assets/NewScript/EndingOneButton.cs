using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class EndingOneButton : MonoBehaviour
{
    public Button button;
    public string mainMenuSceneName = "MainMenu";

    void Awake()
    {
        if (!button) button = GetComponent<Button>();
        if (button) button.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(mainMenuSceneName);
        });
    }

    void OnEnable()
    {
        if (button)
        {
            EventSystem.current?.SetSelectedGameObject(null);
            EventSystem.current?.SetSelectedGameObject(button.gameObject);
            button.Select();
        }
    }

    void Update()
    {
        // A / Enter / B / Start
        bool pressed =
            (Gamepad.current != null && (
                Gamepad.current.buttonSouth.wasPressedThisFrame || // A
                Gamepad.current.buttonEast.wasPressedThisFrame || // B
                Gamepad.current.startButton.wasPressedThisFrame))  // Start
            || (Keyboard.current != null && (
                Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame ||
                Keyboard.current.escapeKey.wasPressedThisFrame));

        if (pressed && button && button.interactable)
            button.onClick.Invoke();
    }
}
