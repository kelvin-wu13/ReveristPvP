using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class UISfxBinder : MonoBehaviour,
    IPointerEnterHandler, IPointerClickHandler, ISelectHandler, ISubmitHandler
{
    [Header("Override Clips (opsional)")]
    public AudioClip hoverClip;
    public AudioClip selectClip;
    public AudioClip clickClip;

    [Header("Pengaturan")]
    public bool playOnHover = true;
    public bool playOnSelect = true;  // fokus via controller/keyboard
    public bool playOnClick = true;  // mouse click & submit (A/Enter)

    Button _btn;

    void Awake()
    {
        _btn = GetComponent<Button>();
        if (_btn && playOnClick) _btn.onClick.AddListener(PlayClick);
    }
    void OnDestroy()
    {
        if (_btn) _btn.onClick.RemoveListener(PlayClick);
    }

    public void OnPointerEnter(PointerEventData e) { if (playOnHover) PlayHover(); }
    public void OnSelect(BaseEventData e) { if (playOnSelect) PlaySelect(); }
    public void OnPointerClick(PointerEventData e) { if (playOnClick) PlayClick(); }
    public void OnSubmit(BaseEventData e) { if (playOnClick) PlayClick(); }

    void PlayHover()
    {
        var am = AudioManager.Instance; if (!am) return;
        if (hoverClip) am.PlaySFX(hoverClip); else am.PlayButtonHover();
    }
    void PlaySelect()
    {
        var am = AudioManager.Instance; if (!am) return;
        if (selectClip) am.PlaySFX(selectClip); else am.PlayButtonSelect();
    }
    void PlayClick()
    {
        var am = AudioManager.Instance; if (!am) return;
        if (clickClip) am.PlaySFX(clickClip); else am.PlayButtonClick();
    }
}
