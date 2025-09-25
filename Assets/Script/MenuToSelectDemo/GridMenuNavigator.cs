using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;

public class GridMenuNavigator : MonoBehaviour
{
    [Header("Buttons (urut L→R, atas→bawah)")]
    public Button[] buttons;
    [Header("Grid")]
    public int columns = 4;

    [Header("Fallback (kalau tidak bind ke PlayerInput)")]
    public InputActionReference navigate;
    public InputActionReference submit;

    [Header("UI Map names (untuk PlayerInput)")]
    [SerializeField] private string uiMapName = "UI";
    [SerializeField] private string navigateActionName = "Navigate";
    [SerializeField] private string submitActionName = "Submit";

    // event index highlight (dipakai manager untuk preview/panel)
    public event Action<int> OnIndexHighlighted;

    // == binding state ==
    private PlayerInput boundPlayer;
    private InputAction curNavigate;
    private InputAction curSubmit;

    int currentIndex = -1;
    float nextRepeat;
    int heldDir; bool held;

    // --- PUBLIC: dipanggil CharacterSelectManager ---
    public void RebindTo(PlayerInput pi)
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

        // fallback: pakai reference global bila tidak ada
        if (curNavigate == null) curNavigate = navigate ? navigate.action : null;
        if (curSubmit == null) curSubmit = submit ? submit.action : null;

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
        // kalau belum pernah bind, lakukan sekali (pakai fallback)
        if (curNavigate == null && curSubmit == null) RebindTo(boundPlayer);
        else
        {
            curNavigate?.Enable();
            if (curSubmit != null) { curSubmit.Enable(); curSubmit.performed += OnSubmit; }
        }

        // pilih tombol pertama
        if (buttons != null && buttons.Length > 0 && buttons[0])
        {
            currentIndex = 0;
            buttons[0].Select();
            RaiseHighlight();
        }
    }

    void OnDisable()
    {
        if (curSubmit != null) curSubmit.performed -= OnSubmit;
        curNavigate?.Disable();
        curSubmit?.Disable();
    }

    void Update()
    {
        Vector2 v = curNavigate != null ? curNavigate.ReadValue<Vector2>() : Vector2.zero;
        int dir = 0;
        if (v.y > 0.5f) dir = -columns;     // up  (pindah baris)
        if (v.y < -0.5f) dir = +columns;     // down
        if (v.x > 0.5f) dir = +1;           // right
        if (v.x < -0.5f) dir = -1;           // left

        if (dir != 0)
        {
            if (!held || dir != heldDir || Time.unscaledTime >= nextRepeat)
            {
                Move(dir);
                nextRepeat = Time.unscaledTime + (held && dir == heldDir ? 0.12f : 0.35f);
                held = true; heldDir = dir;
            }
        }
        else { held = false; heldDir = 0; }

        // sinkron dengan EventSystem (hover mouse)
        if (EventSystem.current)
        {
            var go = EventSystem.current.currentSelectedGameObject;
            if (go)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] && buttons[i].gameObject == go && i != currentIndex)
                    {
                        currentIndex = i;
                        RaiseHighlight();
                        break;
                    }
                }
            }
        }
    }

    void Move(int delta)
    {
        if (buttons == null || buttons.Length == 0) return;
        int next = Mathf.Clamp(currentIndex < 0 ? 0 : currentIndex + delta, 0, buttons.Length - 1);
        if (next == currentIndex || buttons[next] == null) return;

        currentIndex = next;
        buttons[currentIndex].Select();
        RaiseHighlight();
    }

    void OnSubmit(InputAction.CallbackContext _)
    {
        if (currentIndex < 0 || currentIndex >= buttons.Length) return;
        var b = buttons[currentIndex];
        if (b && b.interactable) b.onClick.Invoke();
    }

    void RaiseHighlight() => OnIndexHighlighted?.Invoke(currentIndex);

    public int GetCurrentIndex() => currentIndex;
    public void SetCurrentIndex(int i)
    {
        if (i < 0 || i >= buttons.Length || buttons[i] == null) return;
        currentIndex = i; buttons[i].Select(); RaiseHighlight();
    }
}
