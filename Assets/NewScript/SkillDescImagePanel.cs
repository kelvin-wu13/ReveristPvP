using UnityEngine;
using UnityEngine.UI;

public class SkillDescImagePanel : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Panel GameObject yang akan di-SetActive. Biasanya gameObject ini sendiri.")]
    public GameObject panelRoot;
    [Tooltip("Image tempat menaruh sprite deskripsi.")]
    public Image targetImage;

    [Header("Assets")]
    [Tooltip("Sprite per karakter, urutannya harus sama dengan tombol karakter kamu.")]
    public Sprite[] characterDescSprites;

    bool _visible;

    void Reset()
    {
        if (!panelRoot) panelRoot = gameObject;
        if (!targetImage) targetImage = GetComponentInChildren<Image>(true);
        SetVisible(false);
    }

    public void ShowByIndex(int characterIndex)
    {
        // Selalu munculkan panel, supaya kamu bisa cek on/off-nya dulu
        SetVisible(true);

        if (characterDescSprites == null || characterDescSprites.Length == 0)
        {
            Debug.LogWarning("[SkillDesc] characterDescSprites kosong. Isi array sprite untuk tiap karakter.");
            return;
        }

        characterIndex = Mathf.Clamp(characterIndex, 0, characterDescSprites.Length - 1);

        if (targetImage)
            targetImage.sprite = characterDescSprites[characterIndex];
        else Debug.LogWarning("[SkillDesc] targetImage belum di-assign.");
    }


    public void Hide() => SetVisible(false);
    public bool IsVisible() => _visible;

    void SetVisible(bool on)
    {
        _visible = on;
        if (!panelRoot) panelRoot = gameObject;
        panelRoot.SetActive(on);
    }
}
