using UnityEngine;

public class DeathSceneInput : MonoBehaviour
{
    public FadeController fadeController;
    public float delayBeforeInput = 1.5f;

    private bool canContinue = false;

    void Start()
    {
        // Start with fade-in
        if (fadeController != null)
        {
            fadeController.FadeIn();
        }

        // Allow input after a short delay
        Invoke(nameof(EnableContinue), delayBeforeInput);
    }

    void EnableContinue()
    {
        canContinue = true;
    }

    void Update()
    {
        if (canContinue && Input.anyKeyDown)
        {
            fadeController.FadeOutAndLoadScene("Main Menu Demo");
        }
    }
}
