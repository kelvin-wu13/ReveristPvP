using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public enum SelectTurn { P1, P2, Ready }

public class CharacterSelectManager : MonoBehaviour
{
    [Header("UI Refs")]
    public Button[] characterButtons;
    public Button confirmButton;
    public Text p1Label, p2Label;

    [Header("Navigators")]
    public GridMenuNavigator navigator;   // driver gerak cursor (controller/mouse) + event highlight
    public MenuNavigator menuNavigator;   // pengganti sprite highlight (P1 biru / P2 merah)

    [Header("Preview (Image + Text)")]
    public CharacterPreviewPanel previewP1;     // drag: panel kiri (punya CharacterPreviewPanel)
    public CharacterPreviewPanel previewP2;     // drag: panel kanan
    [Tooltip("Urutannya HARUS sama dengan urutan tombol kiri→kanan, atas→bawah.")]
    public CharacterInfo[] characters;          // data nama + sprite preview per karakter

    [Header("Skill Description")]
    public SkillDescImagePanel p1DescPanel;
    public SkillDescImagePanel p2DescPanel;

    [Header("Config")]
    public MatchConfig config;
    public bool allowMirrorPick = true;

    [Header("Turn State")]
    public SelectTurn turn = SelectTurn.P1;
    [SerializeField] float handoffBlockSeconds = 0.12f; // cegah double click saat ganti turn

    GameObject selP1, selP2;
    int lastHighlight = 0;          // index tombol yg sedang disorot
    int? lockedIndexP1 = null;      // index yg terkunci saat P1 sudah pick
    int? lockedIndexP2 = null;      // index yg terkunci saat P2 sudah pick

    PlayerInput p1UI, p2UI;
    InputAction _cancel;
    InputAction _submit;
    float blockUntil = 0f;

    private UnityEngine.InputSystem.InputAction _openInfo;
    private UnityEngine.InputSystem.InputAction _closeInfo;

    void Awake()
    {
        if (!config)
            config = Resources.Load<MatchConfig>("MatchConfig")
                  ?? Resources.Load<MatchConfig>("MatchConfigPrefabs");

        if (!config)
        {
            Debug.LogError("[CSEL] MatchConfig tidak ditemukan di Resources (MatchConfig / MatchConfigPrefabs).");
            enabled = false; return;
        }

        Debug.Log($"[CSEL] Awake: Loaded MatchConfig. battleSceneName='{config.battleSceneName}'");
    }

    void Start()
    {
        UpdateUI();

        if (confirmButton)
        {
            confirmButton.interactable = false;
            confirmButton.onClick.RemoveListener(StartMatch);
            confirmButton.onClick.AddListener(StartMatch);
            Debug.Log("[CSEL] Start: ConfirmButton hooked to StartMatch()");
        }
        else
        {
            Debug.LogWarning("[CSEL] Start: ConfirmButton reference is NULL.");
        }

        SetActivePicker(SelectTurn.P1);
        SelectFirstGridButton();

        var ui = FindObjectOfType<InputSystemUIInputModule>();
        Debug.Log($"[CSEL] UI Module present={(ui != null)}; Move/Submit/Cancel muted (script handles input).");

        UpdatePanels();
    }

    void OnEnable()
    {
        MuteGlobalUIModuleInputs();

        if (navigator) navigator.OnIndexHighlighted += HandleHighlight;

        var pim = FindObjectOfType<PlayerInputManager>();
        if (pim)
        {
            pim.onPlayerJoined -= OnPlayerJoined;
            pim.onPlayerJoined += OnPlayerJoined;
            Debug.Log("[CSEL] OnEnable: PlayerInputManager subscribed.");
        }
        else Debug.LogWarning("[CSEL] OnEnable: NO PlayerInputManager in scene (OK kalau assign p1UI/p2UI manual).");
    }

    void OnDisable()
    {
        _cancel?.Disable();
        _submit?.Disable();

        if (navigator) navigator.OnIndexHighlighted -= HandleHighlight;

        var pim = FindObjectOfType<PlayerInputManager>();
        if (pim) pim.onPlayerJoined -= OnPlayerJoined;
    }

    void Update()
    {
        bool didCancel = (_cancel != null) && _cancel.WasPerformedThisFrame();
        bool didSubmit = (turn == SelectTurn.Ready) && (_submit != null) && _submit.WasPerformedThisFrame();
        bool wantOpen = (_openInfo != null) && _openInfo.WasPerformedThisFrame();
        bool wantClose = (_closeInfo != null) && _closeInfo.WasPerformedThisFrame();

        // 1) PRIORITAS: Y/B untuk panel skill
        if (IsAnyDescOpen() && (wantClose || didCancel))
        {
            CloseDescPanels();
            return; // jangan teruskan ke cancel/back
        }

        if (wantOpen) { OpenDescForCurrent(); Debug.Log($"[CSEL] OpenInfo pressed by {turn} at index={lastHighlight}"); }
        if (wantClose) { CloseDescPanels(); Debug.Log($"[CSEL] CloseInfo pressed by {turn}"); }

        // 2) Cancel (Back) sesuai turn
        if (didCancel)
        {
            Debug.Log($"[CSEL] Cancel pressed. turn={turn}");
            if (turn == SelectTurn.P1)
            {
                SceneManager.LoadScene("MainMenu");
                return;
            }
            if (turn == SelectTurn.P2)
            {
                // P2 back → balik ke P1
                selP2 = null; lockedIndexP2 = null;
                TrySetCfgPrefab(false, null);
                UpdateUI();
                SetActivePicker(SelectTurn.P1);
                SelectFirstGridButton();
                return;
            }
            if (turn == SelectTurn.Ready)
            {
                // Ready back → kembali ke P2
                SetActivePicker(SelectTurn.P2);
                SelectFirstGridButton();
                return;
            }
        }

        // 3) Submit (Confirm) saat READY
        if (didSubmit)
        {
            Debug.Log($"[CSEL] Submit pressed on READY. Confirm interactable={confirmButton && confirmButton.interactable}");
            if (confirmButton && confirmButton.interactable) StartMatch();
        }
    }

    public void OnPlayerJoined(PlayerInput pi)
    {
        if (pi == null) return;

        if (pi.playerIndex == 0) p1UI = pi;
        else p2UI = pi;

        pi.neverAutoSwitchControlSchemes = true;

        Debug.Log($"[CSEL] OnPlayerJoined: idx={pi.playerIndex} devices=[{string.Join(",", pi.devices.Select(d => d.displayName))}]");

        ToggleExclusive(p1UI, turn == SelectTurn.P1);
        ToggleExclusive(p2UI, turn != SelectTurn.P1);

        if (turn == SelectTurn.P1 && p1UI == pi)
        {
            navigator?.RebindTo(p1UI);
            menuNavigator?.BindToPlayer(p1UI);
            BindUiActionsFrom(p1UI);
        }
        if (turn == SelectTurn.P2 && p2UI == pi)
        {
            navigator?.RebindTo(p2UI);
            menuNavigator?.BindToPlayer(p2UI);
            BindUiActionsFrom(p2UI);
        }
    }

    public void OnCharacterClicked(GameObject prefab)
    {
        if (Time.unscaledTime < blockUntil)
        {
            Debug.Log($"[CSEL] Click ignored due to handoff block. now={Time.unscaledTime:F3} until={blockUntil:F3}");
            return;
        }

        if (turn == SelectTurn.P1)
        {
            if (!selP1)
            {
                selP1 = prefab; TrySetCfgPrefab(true, selP1);
                lockedIndexP1 = Mathf.Clamp(lastHighlight, 0, (characters?.Length ?? 1) - 1);
                Debug.Log($"[CSEL] P1 picked '{selP1.name}' (idx={lockedIndexP1}).");
            }
            SetActivePicker(SelectTurn.P2);
        }
        else if (turn == SelectTurn.P2)
        {
            if (!selP2)
            {
                if (!allowMirrorPick && prefab == selP1) { Debug.Log("[CSEL] P2 pick blocked (mirror not allowed)."); return; }
                selP2 = prefab; TrySetCfgPrefab(false, selP2);
                lockedIndexP2 = Mathf.Clamp(lastHighlight, 0, (characters?.Length ?? 1) - 1);
                Debug.Log($"[CSEL] P2 picked '{selP2.name}' (idx={lockedIndexP2}).");
            }
            SetActivePicker(SelectTurn.Ready);
            FocusConfirm();
        }
        else
        {
            Debug.Log("[CSEL] Click ignored: Already in READY.");
            return;
        }

        UpdateUI();
        UpdatePanels();
    }

    void OpenDescForCurrent()
    {
        int idx = Mathf.Max(0, lastHighlight);
        if (turn == SelectTurn.P1)
        {
            if (p1DescPanel) p1DescPanel.ShowByIndex(idx);
        }
        else
        {
            if (p2DescPanel) p2DescPanel.ShowByIndex(idx);
        }
    }

    void CloseDescPanels()
    {
        if (p1DescPanel) p1DescPanel.Hide();
        if (p2DescPanel) p2DescPanel.Hide();
    }

    bool IsAnyDescOpen()
    {
        return (p1DescPanel && p1DescPanel.IsVisible()) || (p2DescPanel && p2DescPanel.IsVisible());
    }

    void SetActivePicker(SelectTurn who)
    {
        turn = who;
        Debug.Log($"[CSEL] SetActivePicker -> {turn}");

        ToggleExclusive(p1UI, turn == SelectTurn.P1);
        ToggleExclusive(p2UI, (turn == SelectTurn.P2) || (turn == SelectTurn.Ready));

        if (turn == SelectTurn.P1)
        {
            if (previewP2) previewP2.gameObject.SetActive(false);
            if(previewP1) previewP1.gameObject.SetActive(true);
            navigator?.RebindTo(p1UI);
            menuNavigator?.BindToPlayer(p1UI);
            menuNavigator?.SetSelectorOwnerP1();
        }
        else if (turn == SelectTurn.P2)
        {
            if (previewP2) previewP2.gameObject.SetActive(true);
            navigator?.RebindTo(p2UI);
            menuNavigator?.BindToPlayer(p2UI);
            menuNavigator?.SetSelectorOwnerP2();
        }
        else
        {
            if (previewP1) previewP1.gameObject.SetActive(true);
            if (previewP2) previewP2.gameObject.SetActive(true);
            navigator?.RebindTo(p2UI);
            menuNavigator?.BindToPlayer(p2UI);
            menuNavigator?.SetSelectorOwnerNeutral();
        }

        BindUiActionsFrom(turn == SelectTurn.P1 ? p1UI : p2UI);

        if (turn == SelectTurn.P2)
        {
            blockUntil = Time.unscaledTime + handoffBlockSeconds;
            EventSystem.current?.SetSelectedGameObject(null);
            Debug.Log($"[CSEL] Handoff block set for {handoffBlockSeconds * 1000f:F0} ms.");
        }

        bool lockGrid = (turn == SelectTurn.Ready);
        SetCharacterButtonsInteractable(!lockGrid);
        if (navigator) navigator.enabled = !lockGrid;

        if (lockGrid)
        {
            if (confirmButton) confirmButton.interactable = (selP1 && selP2);
            Debug.Log($"[CSEL] READY. Confirm interactable={(confirmButton && confirmButton.interactable)}  selP1={(selP1 ? selP1.name : "null")}  selP2={(selP2 ? selP2.name : "null")}");
            FocusConfirm();
        }

        UpdatePanels();
    }

    public void OnHighlightChanged(int index)
    {
        lastHighlight = index;

        if (p1DescPanel && p1DescPanel.IsVisible())
            p1DescPanel.ShowByIndex(lastHighlight);

        if (p2DescPanel && p2DescPanel.IsVisible())
            p2DescPanel.ShowByIndex(lastHighlight);
    }

    void HandleHighlight(int index)
    {
        lastHighlight = index;
        UpdatePanels();
    }

    void UpdatePanels()
    {
        if (characters == null || characters.Length == 0) return;

        CharacterInfo InfoAt(int idx)
        {
            idx = Mathf.Clamp(idx, 0, characters.Length - 1);
            return characters[idx];
        }

        if (turn == SelectTurn.P1)
        {
            if (previewP1) previewP1.Show(InfoAt(lastHighlight), true);
            if (previewP2)
            {
                if (lockedIndexP2.HasValue) previewP2.Show(InfoAt(lockedIndexP2.Value), false);
                else previewP2.Hide();
            }
        }
        else if (turn == SelectTurn.P2)
        {
            if (previewP2) previewP2.Show(InfoAt(lastHighlight), false);
            if (previewP1)
            {
                if (lockedIndexP1.HasValue) previewP1.Show(InfoAt(lockedIndexP1.Value), true);
                else previewP1.Hide();
            }
        }
        else // Ready
        {
            if (previewP1 && lockedIndexP1.HasValue) previewP1.Show(InfoAt(lockedIndexP1.Value), true);
            if (previewP2 && lockedIndexP2.HasValue) previewP2.Show(InfoAt(lockedIndexP2.Value), false);
        }
    }

    // =============== Utility UI ===============

    void FocusConfirm()
    {
        if (!confirmButton) { Debug.LogWarning("[CSEL] FocusConfirm: confirmButton is NULL."); return; }
        EventSystem.current?.SetSelectedGameObject(confirmButton.gameObject);
        confirmButton.Select();
        Debug.Log("[CSEL] Focused Confirm button.");
    }

    void SelectFirstGridButton()
    {
        if (characterButtons == null || characterButtons.Length == 0) { Debug.Log("[CSEL] No characterButtons assigned."); return; }
        foreach (var b in characterButtons)
            if (b && b.interactable) { b.Select(); Debug.Log($"[CSEL] Selected first grid button: {b.name}"); break; }
    }

    void SetCharacterButtonsInteractable(bool on)
    {
        if ((characterButtons == null || characterButtons.Length == 0))
            characterButtons = FindObjectsOfType<Button>(true)
                               .Where(b => b != confirmButton).ToArray();
        foreach (var b in characterButtons)
            if (b != null && b != confirmButton) b.interactable = on;
        Debug.Log($"[CSEL] Grid buttons interactable set to {on}");
    }

    void UpdateUI()
    {
        if (p1Label) p1Label.text = selP1 ? selP1.name : "P1 ?";
        if (p2Label) p2Label.text = selP2 ? selP2.name : "P2 ?";
        if (confirmButton) confirmButton.interactable = selP1 && selP2;
        Debug.Log($"[CSEL] UpdateUI: selP1={(selP1 ? selP1.name : "null")} selP2={(selP2 ? selP2.name : "null")} confirmInteractable={(confirmButton && confirmButton.interactable)}");
    }

    void StartMatch()
    {
        Debug.Log("[CSEL] StartMatch() called.");

        if (!config) { Debug.LogError("[CSEL] StartMatch: No MatchConfig"); return; }
        if (string.IsNullOrEmpty(config.battleSceneName)) { Debug.LogError("[CSEL] StartMatch: battleSceneName is empty."); return; } // fix typo if needed
        if (!selP1 || !selP2) { Debug.LogWarning("[CSEL] StartMatch: Both players must pick."); return; }

        TrySetCfgPrefab(true, selP1);
        TrySetCfgPrefab(false, selP2);

        if (p1UI) config.p1DeviceIds = p1UI.devices.Select(d => d.deviceId).ToArray();
        if (p2UI) config.p2DeviceIds = p2UI.devices.Select(d => d.deviceId).ToArray();

        Debug.Log($"[CSEL] Loading scene='{config.battleSceneName}'. P1='{selP1.name}' P2='{selP2.name}' " +
                  $"P1Devices=[{string.Join(",", config.p1DeviceIds)}] P2Devices=[{string.Join(",", config.p2DeviceIds)}]");

        SceneManager.LoadScene(config.battleSceneName);
    }

    void TrySetCfgPrefab(bool isP1, GameObject prefab)
    {
        if (!config) return;
        if (isP1) config.p1Prefab = prefab;
        else config.p2Prefab = prefab;
    }

    void BindUiActionsFrom(PlayerInput pi)
    {
        _cancel?.Disable(); _cancel = null;
        _submit?.Disable(); _submit = null;
        _openInfo?.Disable(); _openInfo = null;
        _closeInfo?.Disable(); _closeInfo = null;

        if (pi && pi.actions)
        {
            // UI map
            _cancel = pi.actions.FindAction("UI/Cancel", false) ?? pi.actions.FindAction("Cancel", false);
            _submit = pi.actions.FindAction("UI/Submit", false) ?? pi.actions.FindAction("Submit", false);
            _openInfo = pi.actions.FindAction("UI/OpenInfo", false) ?? pi.actions.FindAction("OpenInfo", false);
            _closeInfo = pi.actions.FindAction("UI/CloseInfo", false) ?? pi.actions.FindAction("CloseInfo", false);

            Debug.Log($"[CSEL] Bind from {(pi == p1UI ? "P1" : "P2")}:" +
                      $" cancel={(_cancel != null)} submit={(_submit != null)}" +
                      $" openInfo={(_openInfo != null)} closeInfo={(_closeInfo != null)}");
        }
        else
        {
            Debug.LogWarning("[CSEL] BindUiActionsFrom: PlayerInput/actions NULL.");
        }

        _cancel?.Enable();
        _submit?.Enable();
        _openInfo?.Enable();
        _closeInfo?.Enable();
    }

    void ToggleExclusive(PlayerInput pi, bool on)
    {
        if (!pi) return;
        if (on) pi.ActivateInput(); else pi.DeactivateInput();
        Debug.Log($"[CSEL] {(pi == p1UI ? "P1" : "P2")} UI {(on ? "ENABLED" : "DISABLED")}");
    }

    void MuteGlobalUIModuleInputs()
    {
        var ui = FindObjectOfType<InputSystemUIInputModule>();
        if (ui == null) { Debug.LogWarning("[CSEL] No InputSystemUIInputModule found."); return; }

        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var t = ui.GetType();
        var fMove = t.GetField("move", flags);
        var fSubmit = t.GetField("submit", flags);
        var fCancel = t.GetField("cancel", flags);

        if (fMove == null || fSubmit == null || fCancel == null)
        {
            Debug.Log("[CSEL] UI Module reflection fields not found (version mismatch). Skipping mute.");
            return;
        }

        if (fMove.FieldType.FullName.Contains("InputActionProperty"))
        {
            var empty = System.Activator.CreateInstance(fMove.FieldType);
            fMove.SetValue(ui, empty);
            fSubmit.SetValue(ui, empty);
            fCancel.SetValue(ui, empty);
        }
        else
        {
            fMove.SetValue(ui, null);
            fSubmit.SetValue(ui, null);
            fCancel.SetValue(ui, null);
        }
        Debug.Log("[CSEL] UI Module actions muted (Move/Submit/Cancel cleared).");
    }
}
