using UnityEngine;
using System.Collections;


public class DeathSceneManager : MonoBehaviour
{
    public float delayBeforeDeathScene = 2f;
    public FadeController fadeController;

    private bool isDead = false;

    public void HandlePlayerDeath()
    {
        if (isDead) return;
        isDead = true;
        StartCoroutine(LoadDeathSceneAfterDelay());
    }

    private IEnumerator LoadDeathSceneAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeDeathScene);
        fadeController.FadeOutAndLoadScene("DeathScene");
    }
}
