using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputManager))]
public class LocalPvPInputManager : MonoBehaviour
{
    private const string P1_SCHEME = "Player1";
    private const string P2_SCHEME = "Player2";

    [SerializeField] private Vector2Int p1StartGrid = new Vector2Int(-1, -1);
    [SerializeField] private Vector2Int p2StartGrid = new Vector2Int(-1, -1);

    [Header("Options")]
    [SerializeField] private bool neverAutoSwitchSchemes = true;

    private PlayerInputManager pim;
    private TileGrid grid; // akan dicari otomatis

    private void Awake()
    {
        pim = GetComponent<PlayerInputManager>();
        pim.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
        pim.notificationBehavior = PlayerNotifications.InvokeUnityEvents;
        pim.playerJoinedEvent.AddListener(OnPlayerJoined);
        pim.playerLeftEvent.AddListener(OnPlayerLeft);
    }

    private void Start()
    {
        pim.EnableJoining();
    }

    private void OnPlayerJoined(PlayerInput pi)
    {
        var scheme = (pi.playerIndex == 0) ? P1_SCHEME : P2_SCHEME;
        pi.SwitchCurrentControlScheme(scheme, pi.devices.ToArray());
        Debug.Log($"[Join] P{pi.playerIndex + 1} devices= {string.Join(", ", pi.devices)}  scheme= {pi.currentControlScheme}");

        pi.neverAutoSwitchControlSchemes = neverAutoSwitchSchemes;
        StartCoroutine(PlaceWhenGridReady(pi));
    }

    private IEnumerator PlaceWhenGridReady(PlayerInput pi)
    {
        while (grid == null)
        {
            grid = FindObjectOfType<TileGrid>();
            if (grid == null) yield return null;
        }

        while (grid.grid == null)
            yield return null;

        var go = pi.gameObject;
        var mv = go.GetComponent<PlayerMovement>();
        var shoot = go.GetComponent<PlayerShoot>();

        if (mv != null) mv.SetTileGrid(grid);

        Vector2Int start;
        int cx = Mathf.Clamp(grid.gridHeight / 2, 0, grid.gridHeight - 1);

        if (pi.playerIndex == 0)
        {
            int x = (p1StartGrid.x < 0) ? Mathf.Clamp(1, 0, grid.gridWidth - 1) : p1StartGrid.x;
            int y = (p1StartGrid.y < 0) ? cx : Mathf.Clamp(p1StartGrid.y, 0, grid.gridHeight - 1);
            start = new Vector2Int(x, y);
            SetSideIfExists(mv, TileGrid.Side.Left);
            if (shoot != null) shoot.SetShootRight(true);
        }
        else
        {
            int x = (p2StartGrid.x < 0) ? Mathf.Clamp(grid.gridWidth - 2, 0, grid.gridWidth - 1) : p2StartGrid.x;
            int y = (p2StartGrid.y < 0) ? cx : Mathf.Clamp(p2StartGrid.y, 0, grid.gridHeight - 1);
            start = new Vector2Int(x, y);
            SetSideIfExists(mv, TileGrid.Side.Right);
            if (shoot != null) shoot.SetShootRight(false);
        }

        if (mv != null) mv.TeleportTo(start);

        Debug.Log($"[LocalPvP] Player {pi.playerIndex + 1} spawn @ grid {start} (grid size {grid.gridWidth}x{grid.gridHeight})");
    }

    private void OnPlayerLeft(PlayerInput pi)
    {
        Debug.Log($"[LocalPvP] Player {pi.playerIndex + 1} left.");
    }

    private void SetSideIfExists(PlayerMovement mv, TileGrid.Side side)
    {
        if (mv == null) return;
        var f = typeof(PlayerMovement).GetField("side",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (f != null) f.SetValue(mv, side);

        // kalau kamu punya method public SetSide(), boleh ganti ke:
        // mv.SetSide(side);
    }
}
