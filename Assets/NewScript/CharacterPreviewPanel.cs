using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class CharacterInfo
{
    public string displayName;
    public Sprite preview;    // cukup isi ini; previewP1/P2 opsional
    public Sprite previewP1;
    public Sprite previewP2;
}

public class CharacterPreviewPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image portrait;   // drag: child CharacterImage
    [SerializeField] private TMP_Text nameText;// drag: child Text (TMP)

    [Header("Name Color (opsional)")]
    public Color p1NameColor = new Color(0.35f, 0.7f, 1f);
    public Color p2NameColor = new Color(1f, 0.45f, 0.45f);

    public void Show(CharacterInfo info, bool isP1)
    {
        if (portrait)
        {
            var s = isP1
                ? (info.previewP1 ? info.previewP1 : info.preview)
                : (info.previewP2 ? info.previewP2 : info.preview);
            portrait.sprite = s;
        }
        if (nameText)
        {
            nameText.text = info.displayName ?? "";
            nameText.color = isP1 ? p1NameColor : p2NameColor;
        }
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (portrait) portrait.sprite = null;
        if (nameText) nameText.text = "";
        // optional: gameObject.SetActive(false);
    }
}
