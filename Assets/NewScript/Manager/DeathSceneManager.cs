using UnityEngine;
using System.Collections;


public class DeathSceneManager : MonoBehaviour
{
    public float delayBeforeEndingScene = 2f;
    public FadeController fadeController;

    private bool isDead = false;

    public void HandlePlayerDeath()
    {
        if (isDead) return;
        isDead = true;
        StartCoroutine(LoadEndingSceneAfterDelay());
    }

    private IEnumerator LoadEndingSceneAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeEndingScene);
        fadeController.FadeOutAndLoadScene("Ending");
    }
}
