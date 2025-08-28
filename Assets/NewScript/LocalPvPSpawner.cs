using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class LocalPvPSpawner : MonoBehaviour
{
    [SerializeField] private string schemeP1 = "Player1";
    [SerializeField] private string schemeP2 = "Player2";

    [Header("Scene Refs")]
    [SerializeField] private TileGrid grid;
    [SerializeField] private MatchConfig cfg;

    [Header("Default Start (dipakai jika tidak ada anchor)")]
    [SerializeField] private Vector2Int p1Start = new Vector2Int(1, 1);
    [SerializeField] private Vector2Int p2Start = new Vector2Int(7, 1);

    [Header("Optional Anchors (Transform world pos -> grid)")]
    public Transform p1Anchor;
    public Transform p2Anchor;

    private void Awake()
    {
        if (!grid) grid = FindObjectOfType<TileGrid>();
    }

    private void Start()
    {
        if (!grid || !cfg || !cfg.p1Prefab || !cfg.p2Prefab) return;

        var p1 = Instantiate(cfg.p1Prefab);
        var p2 = Instantiate(cfg.p2Prefab);

        var start1 = p1Anchor ? grid.GetGridPosition(p1Anchor.position) : p1Start;
        var start2 = p2Anchor ? grid.GetGridPosition(p2Anchor.position) : p2Start;

        SetupPlayer(p1, TileGrid.Side.Left, true, start1);
        SetupPlayer(p2, TileGrid.Side.Right, false, start2);

        LockPlayerInput(p1, true);
        LockPlayerInput(p2, false);
    }
    private void LockPlayerInput(GameObject go, bool isP1)
    {
        var pi = go.GetComponent<PlayerInput>();
        if (pi == null) return;

        pi.neverAutoSwitchControlSchemes = true;

        var pads = Gamepad.all;
        if (isP1 && pads.Count > 0 && pads[0] != null)
        {
            pi.SwitchCurrentControlScheme(schemeP1, pads[0]);
        }
        else if (!isP1 && pads.Count > 1 && pads[1] != null)
        {
            pi.SwitchCurrentControlScheme(schemeP2, pads[1]);
        }
        else if (Keyboard.current != null)
        {
            pi.SwitchCurrentControlScheme(isP1 ? schemeP1 : schemeP2, Keyboard.current);
        }
    }

    void SetupPlayer(GameObject go, TileGrid.Side side, bool faceRight, Vector2Int gridPos)
    {
        var mv = go.GetComponent<PlayerMovement>();
        if (mv)
        {
            mv.SetTileGrid(grid);
            mv.TeleportTo(gridPos);
            var f = typeof(PlayerMovement).GetField("side",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null) f.SetValue(mv, side);
        }

        var sh = go.GetComponent<PlayerShoot>();
        var m = typeof(PlayerShoot).GetMethod("SetShootRight",
                 System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                 System.Reflection.BindingFlags.Instance);
        if (m != null) m.Invoke(sh, new object[] { faceRight });
        else
        {
            var fShoot = typeof(PlayerShoot).GetField("shootRight",
                         System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fShoot?.SetValue(sh, faceRight);
        }
        FlipAllSprites(go, faceRight);
        StartCoroutine(ConfigureCrosshairAfterSpawn(go, faceRight));
    }

    void FlipAllSprites(GameObject root, bool faceRight)
    {
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            sr.flipX = !faceRight;
    }
    IEnumerator ConfigureCrosshairAfterSpawn(GameObject go, bool faceRight)
    {
        yield return null;

        var cross = go.GetComponentInChildren<PlayerCrosshair>(true);
        if (cross == null) yield break;

        var ptField = typeof(PlayerCrosshair).GetField("playerTransform",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        ptField?.SetValue(cross, go.transform);

        var gridField = typeof(PlayerCrosshair).GetField("tileGrid",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if ((TileGrid)gridField?.GetValue(cross) == null && grid != null)
            gridField?.SetValue(cross, grid);

        cross.SetPlayerFacingDirection(faceRight ? Vector2Int.right : Vector2Int.left);
    }
}
