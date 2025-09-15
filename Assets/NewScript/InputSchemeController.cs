using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

[AddComponentMenu("Input/Scheme Controller (Local PvP)")]
[DefaultExecutionOrder(-100)]
public class InputSchemeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputManager playerInputManager;

    [Header("Scheme Names")]
    [SerializeField] private string schemeP1 = "Player1";
    [SerializeField] private string schemeP2 = "Player2";

    [Header("Prefab P1 dan P2")]
    [SerializeField] private bool useTwoPrefabs = false;
    [SerializeField] private GameObject player1Prefab;
    [SerializeField] private GameObject player2Prefab;

    [Header("Options")]
    [SerializeField] private bool lockAutoSwitch = true;
    [SerializeField] private bool applyBindingMask = true;
    [SerializeField] private bool logDebug = true;


    private void OnEnable()
    {
        if (!playerInputManager)
            playerInputManager = GetComponent<PlayerInputManager>();

        if (playerInputManager)
        {
            playerInputManager.onPlayerJoined += OnPlayerJoined;
            playerInputManager.onPlayerLeft += OnPlayerLeft;
            UpdateNextPrefab();
        }
        else
        {
            if (logDebug) Debug.LogWarning("[SchemeController] PlayerInputManager tidak ditemukan di GameObject ini.");
            ReapplyToAllExistingPlayers();
        }
    }

    private void OnDisable()
    {
        if (playerInputManager)
        {
            playerInputManager.onPlayerJoined -= OnPlayerJoined;
            playerInputManager.onPlayerLeft -= OnPlayerLeft;
        }
    }


    private void OnPlayerJoined(PlayerInput pi)
    {
        bool isP1 = (pi.playerIndex == 0);

        if (lockAutoSwitch)
            pi.neverAutoSwitchControlSchemes = true;

        string targetScheme = isP1 ? schemeP1 : schemeP2;

        var devices = pi.devices.Count > 0  ? pi.devices.ToArray() : InputSystem.devices.ToArray();

        pi.SwitchCurrentControlScheme(targetScheme, devices);

        if (applyBindingMask && pi.actions != null)
            pi.actions.bindingMask = InputBinding.MaskByGroup(targetScheme);

        if (pi.currentActionMap == null && pi.actions != null)
        {
            var moveMap = pi.actions.FindActionMap("Move", true);
            if (moveMap != null) pi.SwitchCurrentActionMap(moveMap.name);
        }

        if (logDebug)
        {
            string devs = string.Join(",", pi.devices.Select(d => d.displayName));
            Debug.Log($"[SchemeController] Joined P{pi.playerIndex + 1}  scheme={targetScheme}  mask={pi.actions?.bindingMask}  devices=[{devs}]");
        }

        UpdateNextPrefab();
    }

    private void OnPlayerLeft(PlayerInput pi)
    {
        if (logDebug) Debug.Log($"[SchemeController] Player left: P{pi.playerIndex + 1}");
        UpdateNextPrefab();
    }

    private void UpdateNextPrefab()
    {
        if (!playerInputManager || !useTwoPrefabs) return;

        if (playerInputManager.playerCount <= 0 && player1Prefab)
            playerInputManager.playerPrefab = player1Prefab;
        else if (playerInputManager.playerCount == 1 && player2Prefab)
            playerInputManager.playerPrefab = player2Prefab;
    }

    
    public void ReapplyToAllExistingPlayers()
    {
        var all = FindObjectsOfType<PlayerInput>();
        foreach (var pi in all.OrderBy(p => p.playerIndex))
        {
            bool isP1 = (pi.playerIndex == 0);
            string targetScheme = isP1 ? schemeP1 : schemeP2;

            if (lockAutoSwitch) pi.neverAutoSwitchControlSchemes = true;

            var devices = pi.devices.Count > 0 ? pi.devices.ToArray() : InputSystem.devices.ToArray();
            pi.SwitchCurrentControlScheme(targetScheme, devices);

            if (applyBindingMask && pi.actions != null)
                pi.actions.bindingMask = InputBinding.MaskByGroup(targetScheme);

            if (logDebug)
            {
                string devs = string.Join(",", pi.devices.Select(d => d.displayName));
                Debug.Log($"[SchemeController] Reapply -> P{pi.playerIndex + 1} scheme={targetScheme} devices=[{devs}] mask={pi.actions?.bindingMask}");
            }
        }
    }
}
