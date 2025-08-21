using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class FadeController : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 1f;

    private GameObject fadePanelObject;

    private void Awake()
    {
        if (fadeImage != null)
        {
            fadePanelObject = fadeImage.gameObject;
            fadePanelObject.SetActive(false);
        }
    }

    public void FadeIn()
    {
        StartCoroutine(Fade(Color.black, new Color(0, 0, 0, 0), true));
    }

    public void FadeOutAndLoadScene(string sceneName)
    {
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    private IEnumerator Fade(Color from, Color to, bool deactivateAfter = true)
    {
        if (fadePanelObject != null)
            fadePanelObject.SetActive(true);

        float timer = 0f;
        fadeImage.color = from;

        while (timer < fadeDuration)
        {
            fadeImage.color = Color.Lerp(from, to, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = to;

        if (deactivateAfter && fadePanelObject != null)
            fadePanelObject.SetActive(false);
    }

    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        yield return Fade(new Color(0, 0, 0, 0), Color.black, false);
        SceneManager.LoadScene(sceneName);
    }
}
