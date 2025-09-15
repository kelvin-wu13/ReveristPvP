using UnityEngine;

public static class GridDamageCalculator
{
    public struct Ctx
    {
        public TileGrid grid;
        public TileGrid.Side attackerSide;
        public Vector2Int attackerPos;
        public Vector2Int defenderPos;
        public int baseDamage;
    }

    public static int Calculate(Ctx c)
    {
        if (c.grid == null) return c.baseDamage;

        float dmg = c.baseDamage;

        TileType atkTile = c.grid.grid[c.attackerPos.x, c.attackerPos.y];
        TileType defTile = c.grid.grid[c.defenderPos.x, c.defenderPos.y];

        if (defTile == TileType.Player1Cracked || defTile == TileType.Player2Cracked || defTile == TileType.Cracked)
            dmg *= 1.25f;

        if (atkTile == TileType.Player1Cracked || atkTile == TileType.Player2Cracked || atkTile == TileType.Cracked)
            dmg *= 0.85f;

        int dx = Mathf.Abs(c.attackerPos.x - c.defenderPos.x);
        if (c.grid.gridWidth > 1)
        {
            float t = Mathf.Clamp01(dx / (float)(c.grid.gridWidth - 1));
            dmg *= Mathf.Lerp(1.0f, 1.30f, t);
        }

        int dy = Mathf.Abs(c.attackerPos.y - c.defenderPos.y);
        dmg *= Mathf.Clamp(1f - (dy * 0.05f), 0.85f, 1f);

        return Mathf.Max(1, Mathf.RoundToInt(dmg));
    }
}
