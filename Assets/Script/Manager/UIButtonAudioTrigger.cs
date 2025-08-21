using UnityEngine;

public class UIButtonAudioTrigger : MonoBehaviour
{
    public enum SoundType { Click, Hover, Select }

    [SerializeField] private SoundType soundType = SoundType.Click;

    // Call this from a Button OnClick() or EventTrigger
    public void Play()
    {
        if (AudioManager.Instance == null) return;

        switch (soundType)
        {
            case SoundType.Click:
                AudioManager.Instance.PlayButtonClickSFX();
                break;
            case SoundType.Hover:
                AudioManager.Instance.PlayButtonHoverSFX();
                break;
            case SoundType.Select:
                AudioManager.Instance.PlayButtonSelectSFX();
                break;
        }
    }
}
