using UnityEngine;
using SkillSystem;
using System.Collections;
using System.Collections.Generic;

public class TakeOver : Skill
{
    [SerializeField] private TileGrid grid;
    [SerializeField] private bool pickCenterThree = true;

    [SerializeField] private float mutualCancelWindow = 0.08f;

    private readonly List<Vector2Int> _takenTiles = new List<Vector2Int>();
    private static System.Collections.Generic.List<Vector2Int> s_activeP1 = new System.Collections.Generic.List<Vector2Int>();
    private static System.Collections.Generic.List<Vector2Int> s_activeP2 = new System.Collections.Generic.List<Vector2Int>();
    private static float s_lastStartP1 = -1000f;
    private static float s_lastStartP2 = -1000f;
    private static TakeOver s_instP1;
    private static TakeOver s_instP2;

    public override void ExecuteSkillEffect(Vector2Int _ignored, Transform caster)
    {
        if (!grid) grid = FindObjectOfType<TileGrid>();
        if (!grid) { Debug.LogError("[TakeOver] TileGrid tidak ditemukan."); return; }

        var pm = caster.GetComponent<PlayerMovement>();
        Vector2Int casterGridPos = pm != null ? pm.GetCurrentGridPosition()
                                              : grid.GetGridPosition(caster.position);
        var under = grid.GetTileType(casterGridPos);
        int mid = Mathf.Max(1, grid.gridWidth / 2);
        bool casterIsP1 = (under == TileType.Player1) || (under != TileType.Player2 && casterGridPos.x <= (grid.gridWidth - 1) / 2);

        Debug.Log($"[TakeOver] mid={mid} gridW={grid.gridWidth} H={grid.gridHeight} | casterPos={casterGridPos} under={under} casterIsP1={casterIsP1}");

        // Kolom depan wilayah lawan
        int enemyFrontX = casterIsP1 ? mid : (mid - 1);
        enemyFrontX = Mathf.Clamp(enemyFrontX, 0, grid.gridWidth - 1);

        TileType casterOwner = casterIsP1 ? TileType.Player1 : TileType.Player2;
        TileType opponentOwner = casterIsP1 ? TileType.Player2 : TileType.Player1;

        // Ambil 3 tile tengah (atau semua kalau tinggi <=3)
        int h = grid.gridHeight;
        int count = (h <= 3) ? h : 3;
        int yStart = (h <= 3) ? 0 : Mathf.Clamp((h - 3) / 2, 0, h - 3);

        _takenTiles.Clear();
        for (int i = 0; i < count; i++)
        {
            var p = new Vector2Int(enemyFrontX, yStart + i);
            if (!grid.IsValidGridPosition(p)) continue;
            _takenTiles.Add(p);
            grid.SetTileType(p, casterOwner);
        }
        // tandai kumpulan tile yang di-takeover (sekali panggil)
        grid.MarkTakenOver(_takenTiles, casterOwner);

        if (casterIsP1)
        {
            s_activeP1 = new System.Collections.Generic.List<Vector2Int>(_takenTiles);
            s_lastStartP1 = Time.time;
            s_instP1 = this;
        }
        else
        {
            s_activeP2 = new System.Collections.Generic.List<Vector2Int>(_takenTiles);
            s_lastStartP2 = Time.time;
            s_instP2 = this;
        }

        bool otherActive = casterIsP1 ? (s_activeP2.Count > 0) : (s_activeP1.Count > 0);
        float otherLastT = casterIsP1 ? s_lastStartP2 : s_lastStartP1;
        float deltaToOther = Time.time - otherLastT;

        if (otherActive && deltaToOther > mutualCancelWindow)
        {
            // BUKAN cast bersamaan → reset ke normal & batalkan instance yang sedang aktif
            MutualCancelReset();

            if (casterIsP1)
            {
                if (s_instP2 != null) s_instP2.CancelImmediateNoRevert();
                s_instP2 = null;
            }
            else
            {
                if (s_instP1 != null) s_instP1.CancelImmediateNoRevert();
                s_instP1 = null;
            }

            Debug.Log("[TakeOver] Late cast triggers mutual-cancel → no new takeover applied.");
            return; // ← PENTING: jangan lanjut apply takeover baru
        }

        PlayerMovement opp = null;
        var players = Object.FindObjectsOfType<PlayerMovement>();
        int midForSide = Mathf.Max(1, grid.gridWidth / 2);
        foreach (var p in players)
        {
            if (p == null) continue;
            var pos = p.GetCurrentGridPosition();
            bool pIsP1 = (pos.x <= (grid.gridWidth - 1) / 2); // konsisten dgn cara kita tentukan sisi
                                                              // pilih yang berlawanan sisi dengan caster
            if (casterIsP1 ? !pIsP1 : pIsP1) { opp = p; break; }
        }
        if (opp != null)
        {
            var oppPos = opp.GetCurrentGridPosition();
            if (_takenTiles.Contains(oppPos))
            {
                bool enemyIsP1 = !casterIsP1;
                PushBackRowLocked(opp, enemyIsP1);
            }
        }

        bool casterOnTaken = _takenTiles.Exists(p => p == casterGridPos);
        Debug.Log($"[TakeOver] enemyFrontX={enemyFrontX} takenCount={_takenTiles.Count} taken=[{string.Join(",", _takenTiles)}] casterOnTaken={casterOnTaken}");

        PlayerCrosshair xhair = null;
        int oldDist = 0;
        if (casterOnTaken)
        {
            xhair = caster.GetComponentInChildren<PlayerCrosshair>(true);
            if (xhair != null)
            {
                oldDist = xhair.GetDistanceFromPlayer();
                xhair.SetDistanceFromPlayer(0, snap: true);
                Debug.Log($"[TakeOver] Set crosshair distance {oldDist} -> 0 ({xhair.name})");
            }
        }
        StartCoroutine(RevertAfterLifetime(opponentOwner, xhair, oldDist, caster, casterIsP1));
    }

    private IEnumerator RevertAfterLifetime( TileType returnOwner, PlayerCrosshair crosshair, int oldDistance, Transform caster, bool casterIsP1)
    {
        yield return new WaitForSeconds(LifetimeSeconds);

        if (!grid) grid = FindObjectOfType<TileGrid>();
        if (grid)
        {
            // kembalikan pemilik tile
            foreach (var pos in _takenTiles)
                if (grid.IsValidGridPosition(pos)) grid.SetTileType(pos, returnOwner);

            // bersihkan flag takeover (sekali panggil)
            grid.UnmarkTakenOver(_takenTiles);
            Debug.Log($"[TakeOver] Revert {_takenTiles.Count} tile → {returnOwner}");
            
            if (casterIsP1) s_activeP1.Clear();
            else s_activeP2.Clear();

        }

        // Dorong caster ke belakang jika masih berdiri di salah satu tile takeover
        if (grid && caster != null)
        {
            var pm = caster != null ? caster.GetComponent<PlayerMovement>() : null;
            Vector2Int casterPos = pm != null ? pm.GetCurrentGridPosition()
                                              : grid.GetGridPosition(caster.position);

            bool stillOnTaken = _takenTiles.Exists(p => p == casterPos);
            if (stillOnTaken)
            {
                Vector2Int dirBack = casterIsP1 ? Vector2Int.left : Vector2Int.right;

                Vector2Int target = casterPos;
                bool found = false;

                for (int step = 1; step <= grid.gridWidth; step++)
                {
                    var cand = new Vector2Int(casterPos.x + dirBack.x * step, casterPos.y);

                    if (!grid.IsValidGridPosition(cand)) break;

                    var mySide = casterIsP1 ? TileGrid.Side.Left : TileGrid.Side.Right;
                    if (grid.IsValidPositionForSide(cand, mySide))
                    {
                        target = cand; found = true; break;
                    }
                }

                if (found)
                {
                    if (pm != null) pm.TeleportTo(target);
                    else caster.position = grid.GetWorldPosition(target) + new Vector3(0.5f, 0.5f, 0f);

                    Debug.Log($"[TakeOver] Push row-locked {casterPos} -> {target} (dirBack={dirBack})");
                }
                else
                {
                    Debug.LogWarning("[TakeOver] No horizontal tile to push!");
                }
            }
        }
        if (crosshair != null)
        {
            crosshair.SetDistanceFromPlayer(oldDistance, snap: true);
            Debug.Log($"[TakeOver] Restore crosshair distance 0 -> {oldDistance} on {crosshair.name}");
        }

        if (this) Destroy(gameObject);
    }

    private void PushBackRowLocked(PlayerMovement pm, bool isP1)
    {
        if (pm == null || grid == null) return;

        Vector2Int from = pm.GetCurrentGridPosition();
        Vector2Int dirBack = isP1 ? Vector2Int.left : Vector2Int.right;
        var side = isP1 ? TileGrid.Side.Left : TileGrid.Side.Right;

        for (int step = 1; step <= grid.gridWidth; step++)
        {
            var cand = new Vector2Int(from.x + dirBack.x * step, from.y);
            if (!grid.IsValidGridPosition(cand)) break;

            if (grid.IsValidPositionForSide(cand, side))
            {
                pm.TeleportTo(cand);
                Debug.Log($"[TakeOver] PushBack {pm.name} {from} -> {cand}");
                break;
            }
        }
    }
    private void MutualCancelReset()
    {
        if (!grid) return;

        // Kembalikan kolom yang sempat diambil P1 ke Player2 (kolom depan P1 adalah milik P2)
        if (s_activeP1 != null && s_activeP1.Count > 0)
        {
            foreach (var p in s_activeP1)
                if (grid.IsValidGridPosition(p)) grid.SetTileType(p, TileType.Player2);
            grid.UnmarkTakenOver(s_activeP1);
            s_activeP1.Clear();
        }

        // Kembalikan kolom yang sempat diambil P2 ke Player1 (kolom depan P2 adalah milik P1)
        if (s_activeP2 != null && s_activeP2.Count > 0)
        {
            foreach (var p in s_activeP2)
                if (grid.IsValidGridPosition(p)) grid.SetTileType(p, TileType.Player1);
            grid.UnmarkTakenOver(s_activeP2);
            s_activeP2.Clear();
        }

        Debug.Log("[TakeOver] Mutual-cancel → arena reset normal.");
    }
    private void CancelImmediateNoRevert()
    {
        // Tidak mengubah tile apa pun (tile sudah dipulihkan oleh MutualCancelReset)
        try { StopAllCoroutines(); } catch { }
        if (this) Destroy(gameObject);
    }

}
