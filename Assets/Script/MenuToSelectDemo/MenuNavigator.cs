using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[Serializable]
public class MenuButtonEntry
{
    public Button button;
    public Image targetImage;          // opsional (kalau icon bukan targetGraphic button)
    [Header("Sprites")]
    public Sprite normalSprite;
    public Sprite selectedSprite;       // fallback
    public Sprite selectedSpriteP1;     // biru
    public Sprite selectedSpriteP2;     // merah
}

public class MenuNavigator : MonoBehaviour
{
    public enum SelectorOwner { Neutral, P1, P2 }

    [Header("Fallback (kalau tidak bind ke PlayerInput)")]
    public InputActionReference navigateAction;
    public InputActionReference submitAction;
    public InputActionReference cancelAction;

    [Header("Buttons")]
    public MenuButtonEntry[] entries;
    public int initialIndex = 0;
    public bool loop = true;

    [Header("Selected Skin Owner")]
    public SelectorOwner selectorOwner = SelectorOwner.Neutral;

    [Header("UI Map names (untuk PlayerInput)")]
    [SerializeField] private string uiMapName = "UI";
    [SerializeField] private string navigateActionName = "Navigate";
    [SerializeField] private string submitActionName = "Submit";

    [Header("Repeat")]
    [Range(0.1f, 1f)] public float initialRepeatDelay = 0.35f;
    [Range(0.05f, 0.5f)] public float repeatRate = 0.12f;
    [Range(0.1f, 0.9f)] public float deadzone = 0.5f;

    // binding ke PlayerInput
    private PlayerInput boundPlayer;
    private InputAction curNavigate;
    private InputAction curSubmit;

    int index = -1;
    float nextRepeatTime;
    int lastDir;
    bool held;

    // ========== PUBLIC API ==========
    public void SetSelectorOwner(SelectorOwner owner)
    {
        selectorOwner = owner;
        ApplySprites(index < 0 ? Mathf.Clamp(initialIndex, 0, entries.Length - 1) : index, refreshAll: true);
    }
    public void SetSelectorOwnerP1() => SetSelectorOwner(SelectorOwner.P1);
    public void SetSelectorOwnerP2() => SetSelectorOwner(SelectorOwner.P2);
    public void SetSelectorOwnerNeutral() => SetSelectorOwner(SelectorOwner.Neutral);

    public void BindToPlayer(PlayerInput pi)
    {
        // lepas binding lama
        if (curSubmit != null) curSubmit.performed -= OnSubmit;
        curNavigate?.Disable();
        curSubmit?.Disable();

        boundPlayer = pi;
        curNavigate = null;
        curSubmit = null;

        if (boundPlayer && boundPlayer.actions)
        {
            curNavigate = boundPlayer.actions.FindAction($"{uiMapName}/{navigateActionName}", false);
            curSubmit = boundPlayer.actions.FindAction($"{uiMapName}/{submitActionName}", false);
        }
        // fallback ref global
        if (curNavigate == null) curNavigate = navigateAction ? navigateAction.action : null;
        if (curSubmit == null) curSubmit = submitAction ? submitAction.action : null;

        if (isActiveAndEnabled)
        {
            curNavigate?.Enable();
            if (curSubmit != null)
            {
                curSubmit.Enable();
                curSubmit.performed += OnSubmit;
            }
        }
    }

    void OnEnable()
    {
        if (curNavigate == null && curSubmit == null) BindToPlayer(boundPlayer);
        else
        {
            curNavigate?.Enable();
            if (curSubmit != null) { curSubmit.Enable(); curSubmit.performed += OnSubmit; }
        }
        SelectIndex(Mathf.Clamp(initialIndex, 0, entries.Length - 1), true);
    }

    void OnDisable()
    {
        if (curSubmit != null) curSubmit.performed -= OnSubmit;
        curNavigate?.Disable();
        curSubmit?.Disable();
    }

    void Update()
    {
        Vector2 nav = curNavigate != null ? curNavigate.ReadValue<Vector2>() : Vector2.zero;

        int dir = 0;
        if (nav.y > deadzone) dir = -1;   // up
        if (nav.y < -deadzone) dir = +1;   // down

        if (dir != 0)
        {
            if (!held || dir != lastDir || Time.unscaledTime >= nextRepeatTime)
            {
                Move(dir);
                nextRepeatTime = Time.unscaledTime + (held && dir == lastDir ? repeatRate : initialRepeatDelay);
                held = true; lastDir = dir;
            }
        }
        else { held = false; lastDir = 0; }

        // sinkron hover mouse
        if (EventSystem.current)
        {
            var go = EventSystem.current.currentSelectedGameObject;
            if (go)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].button && entries[i].button.gameObject == go && i != index)
                    {
                        SelectIndex(i, true);
                        break;
                    }
                }
            }
        }
    }

    void OnSubmit(InputAction.CallbackContext _)
    {
        if (index < 0 || index >= entries.Length) return;
        var b = entries[index].button;
        if (b && b.interactable) b.onClick.Invoke();
    }

    void Move(int dir)
    {
        if (entries == null || entries.Length == 0) return;

        int next = index;
        if (next < 0) next = Mathf.Clamp(initialIndex, 0, entries.Length - 1);
        else
        {
            next += dir;
            if (loop)
            {
                if (next < 0) next = entries.Length - 1;
                if (next >= entries.Length) next = 0;
            }
            else next = Mathf.Clamp(next, 0, entries.Length - 1);
        }

        SelectIndex(next, false);
    }

    public void SelectIndex(int i, bool force)
    {
        if (entries == null || entries.Length == 0) return;
        i = Mathf.Clamp(i, 0, entries.Length - 1);
        if (!force && i == index) return;

        index = i;

        // fokus EventSystem (aktifkan SpriteSwap bawaan Button)
        if (EventSystem.current && entries[index].button)
            EventSystem.current.SetSelectedGameObject(entries[index].button.gameObject);

        ApplySprites(index, refreshAll: true);
    }

    void ApplySprites(int selectedIndex, bool refreshAll = false)
    {
        for (int k = 0; k < entries.Length; k++)
        {
            var e = entries[k];
            if (e == null || e.button == null) continue;

            var baseImg = e.targetImage ? e.targetImage : e.button.image;
            if (baseImg && e.normalSprite) baseImg.sprite = e.normalSprite;

            var use =
                selectorOwner == SelectorOwner.P1 ? (e.selectedSpriteP1 ?? e.selectedSprite) :
                selectorOwner == SelectorOwner.P2 ? (e.selectedSpriteP2 ?? e.selectedSprite) :
                                                     (e.selectedSprite ?? e.normalSprite);

            var state = e.button.spriteState;
            state.highlightedSprite = use;
            state.selectedSprite = use;
            state.pressedSprite = use;
            e.button.spriteState = state;

            // swap manual agar langsung terlihat
            if (k == selectedIndex && baseImg && use)
                baseImg.sprite = use;
        }
    }
}
