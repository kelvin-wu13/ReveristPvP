using UnityEngine;

public class ResumeMainMenuBGM : MonoBehaviour
{
    void Start()
    {
        AudioManager.Instance?.ResumeBGM();
    }
}
