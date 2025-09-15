using UnityEngine;
using SkillSystem;
using System.Collections;
using System.Collections.Generic;
using SkillSystem;

public class TakeOver : Skill
{
    [SerializeField] private TileGrid grid;
    [SerializeField] private bool pickCenterThree = true;

    private readonly List<Vector2Int> _takenTiles = new List<Vector2Int>();

    public override void ExecuteSkillEffect(Vector2Int _ignored, Transform caster)
    {
        if (!grid) grid = FindObjectOfType<TileGrid>();
        if (!grid) { Debug.LogError("[TakeOver] TileGrid tidak ditemukan."); return; }

        Vector2Int casterGridPos = grid.GetGridPosition(caster.position);
        int mid = Mathf.Max(1, grid.gridWidth / 2);
        bool casterIsP1 = casterGridPos.x < mid;

        int enemyFrontX = casterIsP1 ? mid : (mid - 1);
        TileType casterOwner = casterIsP1 ? TileType.Player1 : TileType.Player2;
        TileType opponentOwner = casterIsP1 ? TileType.Player2 : TileType.Player1;

        int h = grid.gridHeight;
        int count, yStart;
        if (!pickCenterThree || h <= 3)
        {
            yStart = 0;
            count = h;
        }
        else
        {
            yStart = Mathf.Clamp((h - 3) / 2, 0, h - 3);
            count = 3;
        }

        _takenTiles.Clear();

        // Ambil kepemilikan
        for (int i = 0; i < count; i++)
        {
            var p = new Vector2Int(enemyFrontX, yStart + i);
            if (!grid.IsValidGridPosition(p)) continue;

            _takenTiles.Add(p);
            grid.SetTileType(p, casterOwner);
        }

        Debug.Log($"[TakeOver] {(casterIsP1 ? "P1" : "P2")} mengambil kolom X={enemyFrontX}, {_takenTiles.Count} tile.");

        StartCoroutine(RevertAfterLifetime(opponentOwner));
    }

    private IEnumerator RevertAfterLifetime(TileType returnOwner)
    {
        yield return new WaitForSeconds(LifetimeSeconds);

        if (!grid) grid = FindObjectOfType<TileGrid>();

        if (grid)
        {
            foreach (var pos in _takenTiles)
            {
                if (!grid.IsValidGridPosition(pos)) continue;
                grid.SetTileType(pos, returnOwner);
            }
            Debug.Log($"[TakeOver] Revert {_takenTiles.Count} tile → {returnOwner}");
        }
        if (this) Destroy(gameObject);
    }
}
