using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum TileType
{
    Player1,
    Player2,
    Empty,
    Player1Cracked,
    Player1Broken,
    Player2Cracked,
    Player2Broken,
    Cracked,
    Broken
}

[System.Serializable]
public class TileSet
{
    [FormerlySerializedAs("playerTileSprite")]
    public Sprite player1TileSprite;
    [FormerlySerializedAs("enemyTileSprite")]
    public Sprite player2TileSprite;
    [FormerlySerializedAs("emptyTileSprite")]
    public Sprite emptyTileSprite;

    [FormerlySerializedAs("playerCrackedTileSprite")]
    public Sprite player1CrackedTileSprite;
    [FormerlySerializedAs("playerBrokenTileSprite")]
    public Sprite player1BrokenTileSprite;

    [FormerlySerializedAs("enemyCrackedTileSprite")]
    public Sprite player2CrackedTileSprite;
    [FormerlySerializedAs("enemyBrokenTileSprite")]
    public Sprite player2BrokenTileSprite;
}

public class TileGrid : MonoBehaviour
{
    public enum Side { Left, Right }

    [Header("Grid Setting")]
    [SerializeField] private float gridXRotation = 120f;
    [SerializeField] private float gridYRotation = 15f;
    [SerializeField] private float gridZRotation = 15f;
    [SerializeField] public int gridWidth = 8;
    [SerializeField] public int gridHeight = 4;

    [Header("Tile Size and Spacing")]
    [SerializeField] private float tileWidth = 1f;
    [SerializeField] private float tileHeight = 1f;
    [SerializeField] private float horizontalSpacing = 0.1f;
    [SerializeField] private float verticalSpacing = 0.1f;
    private float HalfCols => (gridWidth - 1) * 0.5f;
    private float HalfRows => (gridHeight - 1) * 0.5f;



    [Header("Tile Effect Durations")]
    [SerializeField] private float crackedTileDuration = 1.5f;
    [SerializeField] private float brokenTileDuration = 2.0f;

    [Header("Tile Reference")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private TileSet tileSet;
    [SerializeField] private int tileRenderingLayer = 0;

    [SerializeField] private Vector2 gridOffset = Vector2.zero;

    public TileType[,] grid;
    private GameObject[,] tileObjects;
    private TileType[,] originalTileTypes;

    private Dictionary<Vector2Int, List<GameObject>> objectsInTiles = new Dictionary<Vector2Int, List<GameObject>>();
    private Dictionary<Vector2Int, bool> tileOccupationStatus = new Dictionary<Vector2Int, bool>();
    private readonly Dictionary<Vector2Int, TileType> takeoverOwnerByPos = new Dictionary<Vector2Int, TileType>();

    public void SetTileOccupied(Vector2Int pos, bool occupied)
    {
        if (IsValidGridPosition(pos)) tileOccupationStatus[pos] = occupied;
    }
    public bool IsTileOccupied(Vector2Int pos) => tileOccupationStatus.ContainsKey(pos) && tileOccupationStatus[pos];

    private float totalTileWidth => tileWidth + horizontalSpacing;
    private float totalTileHeight => tileHeight + verticalSpacing;

    private void Awake()
    {
        transform.rotation = Quaternion.Euler(gridXRotation, gridYRotation, gridZRotation);
        InitializeGrid();
        InitializeObjectsInTilesDict();
    }

    private void InitializeObjectsInTilesDict()
    {
        objectsInTiles.Clear();
        tileOccupationStatus.Clear();
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                var pos = new Vector2Int(x, y);
                objectsInTiles[pos] = new List<GameObject>();
                tileOccupationStatus[pos] = false;
            }
    }

    public float GetTileWidth() => tileWidth;
    public float GetTileHeight() => tileHeight;

    public Vector3 GetCenteredWorldPosition(Vector2Int gridPosition, float fixedZ = -1f)
    {
        Vector3 pos = GetWorldPosition(gridPosition);
        return new Vector3(pos.x, pos.y, fixedZ);
    }

    private void OnValidate()
    {
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);
        tileWidth = Mathf.Max(0.1f, tileWidth);
        tileHeight = Mathf.Max(0.1f, tileHeight);
        horizontalSpacing = Mathf.Max(0f, horizontalSpacing);
        verticalSpacing = Mathf.Max(0f, verticalSpacing);

        if (Application.isPlaying)
            transform.rotation = Quaternion.Euler(gridXRotation, gridYRotation, gridZRotation);

        if (Application.isPlaying && grid != null)
            UpdateGridLayout();
    }

    private void UpdateGridLayout()
    {
        var oldGrid = grid;
        var oldTileObjects = tileObjects;
        var oldOriginalTypes = originalTileTypes;

        int oldWidth = oldGrid.GetLength(0);
        int oldHeight = oldGrid.GetLength(1);

        grid = new TileType[gridWidth, gridHeight];
        tileObjects = new GameObject[gridWidth, gridHeight];
        originalTileTypes = new TileType[gridWidth, gridHeight];

        InitializeObjectsInTilesDict();

        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                if (x < oldWidth && y < oldHeight)
                {
                    grid[x, y] = oldGrid[x, y];
                    originalTileTypes[x, y] = oldOriginalTypes[x, y];

                    if (oldTileObjects[x, y] != null)
                    {
                        tileObjects[x, y] = oldTileObjects[x, y];
                        UpdateTileTransform(new Vector2Int(x, y));
                    }
                    else
                    {
                        CreateTile(new Vector2Int(x, y));
                    }
                }
                else
                {
                    CreateTile(new Vector2Int(x, y));
                }
            }

        for (int x = 0; x < oldWidth; x++)
            for (int y = 0; y < oldHeight; y++)
            {
                if (x >= gridWidth || y >= gridHeight)
                    if (oldTileObjects[x, y] != null) Destroy(oldTileObjects[x, y]);
            }
    }

    private void InitializeGrid()
    {
        grid = new TileType[gridWidth, gridHeight];
        tileObjects = new GameObject[gridWidth, gridHeight];
        originalTileTypes = new TileType[gridWidth, gridHeight];

        CreateGrid();
        SetupInitialPositions();
    }

    private void CreateGrid()
    {
        transform.rotation = Quaternion.Euler(gridXRotation, gridYRotation, gridZRotation);

        foreach (var pos in EnumerateFromCenter())
            CreateTile(pos);
    }


    IEnumerable<Vector2Int> EnumerateFromCenter()
    {
        float cx = (gridWidth - 1) * 0.5f;
        float cy = (gridHeight - 1) * 0.5f;

        var list = new List<Vector2Int>(gridWidth * gridHeight);
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                list.Add(new Vector2Int(x, y));

        list.Sort((a, b) => {
            float ra = Mathf.Max(Mathf.Abs(a.x - cx), Mathf.Abs(a.y - cy));
            float rb = Mathf.Max(Mathf.Abs(b.x - cx), Mathf.Abs(b.y - cy));
            int c = ra.CompareTo(rb);
            if (c != 0) return c;

            float aa = Mathf.Atan2(a.y - cy, a.x - cx);
            float ab = Mathf.Atan2(b.y - cy, b.x - cx);
            return aa.CompareTo(ab);
        });

        return list;
    }

    public Vector3 GetWorldPositionWithSimpleArena(Vector2Int gridPosition)
    {
        float x = (gridPosition.x - HalfCols) * totalTileWidth + gridOffset.x;
        float y = (gridPosition.y - HalfRows) * totalTileHeight + gridOffset.y;

        float depth = -(gridPosition.y) * 0.2f;

        return new Vector3(x, y, depth);
    }


    private void CreateTile(Vector2Int position)
    {
        Vector3 worldPosition = GetWorldPositionWithSimpleArena(position);
        GameObject tile = Instantiate(tilePrefab, worldPosition, Quaternion.identity, transform);
        tile.name = $"Tile_{position.x}_{position.y}";

        tile.transform.localScale = new Vector3(tileWidth, tileHeight, 1f);

        var sr = tile.GetComponent<SpriteRenderer>();
        if (sr == null) sr = tile.AddComponent<SpriteRenderer>();
        sr.sortingOrder = tileRenderingLayer;
        sr.color = Color.white;

        grid[position.x, position.y] = TileType.Empty;
        originalTileTypes[position.x, position.y] = TileType.Empty;
        sr.sprite = tileSet.emptyTileSprite;

        tileObjects[position.x, position.y] = tile;
    }

    private void UpdateTileTransform(Vector2Int position)
    {
        GameObject tile = tileObjects[position.x, position.y];
        if (tile == null) return;

        tile.transform.position = GetWorldPositionWithSimpleArena(position);
        tile.transform.localScale = new Vector3(tileWidth, tileHeight, 1f);
        tile.transform.rotation = Quaternion.identity;

        var sr = tile.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = tileRenderingLayer;
            sr.color = Color.white;
        }
    }

    public void SetupInitialPositions()
    {
        for (int x = 0; x < gridWidth / 2; x++)
            for (int y = 0; y < gridHeight; y++)
                SetTileType(new Vector2Int(x, y), TileType.Player1);

        for (int x = gridWidth / 2; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                SetTileType(new Vector2Int(x, y), TileType.Player2);
    }

    public void SetTileType(Vector2Int gridPosition, TileType type)
    {
        if (!IsValidGridPosition(gridPosition)) return;

        grid[gridPosition.x, gridPosition.y] = type;

        var sr = tileObjects[gridPosition.x, gridPosition.y].GetComponent<SpriteRenderer>();
        switch (type)
        {
            case TileType.Player1:
                sr.sprite = tileSet.player1TileSprite;
                originalTileTypes[gridPosition.x, gridPosition.y] = TileType.Player1;
                break;
            case TileType.Player2:
                sr.sprite = tileSet.player2TileSprite;
                originalTileTypes[gridPosition.x, gridPosition.y] = TileType.Player2;
                break;
            case TileType.Empty:
                sr.sprite = tileSet.emptyTileSprite;
                originalTileTypes[gridPosition.x, gridPosition.y] = TileType.Empty;
                break;

            case TileType.Player1Cracked: sr.sprite = tileSet.player1CrackedTileSprite; break;
            case TileType.Player1Broken: sr.sprite = tileSet.player1BrokenTileSprite; break;
            case TileType.Player2Cracked: sr.sprite = tileSet.player2CrackedTileSprite; break;
            case TileType.Player2Broken: sr.sprite = tileSet.player2BrokenTileSprite; break;

            case TileType.Cracked:
            case TileType.Broken:
                break;
        }
    }

    public void MarkTakenOver(IEnumerable<Vector2Int> positions, TileType newOwner)
    {
        foreach (var p in positions)
            if (IsValidGridPosition(p)) takeoverOwnerByPos[p] = newOwner;
    }

    public void UnmarkTakenOver(IEnumerable<Vector2Int> positions)
    {
        foreach (var p in positions)
            takeoverOwnerByPos.Remove(p);
    }

    public bool IsTakenOverBy(Vector2Int p, TileType owner)
    {
        return takeoverOwnerByPos.TryGetValue(p, out var o) && o == owner;
    }
    public TileType GetTileType(Vector2Int p)
    {
        if (!IsValidGridPosition(p)) return TileType.Empty;
        return grid[p.x, p.y];
    }

    public bool IsValidGridPosition(Vector2Int p)
    {
        return p.x >= 0 && p.x < gridWidth && p.y >= 0 && p.y < gridHeight;
    }

    public bool IsValidPositionForSide(Vector2Int pos, Side side)
    {
        if (!IsValidGridPosition(pos)) return false;

        if (grid[pos.x, pos.y] == TileType.Broken ||
            grid[pos.x, pos.y] == TileType.Player1Broken ||
            grid[pos.x, pos.y] == TileType.Player2Broken)
            return false;

        if (side == Side.Left)
        {
            return grid[pos.x, pos.y] != TileType.Player2 &&
                   grid[pos.x, pos.y] != TileType.Player2Cracked &&
                   grid[pos.x, pos.y] != TileType.Player2Broken;
        }
        else
        {
            return grid[pos.x, pos.y] != TileType.Player1 &&
                   grid[pos.x, pos.y] != TileType.Player1Cracked &&
                   grid[pos.x, pos.y] != TileType.Player1Broken;
        }
    }

    public void CrackTile(Vector2Int p)
    {
        if (!IsValidGridPosition(p)) return;
        if (grid[p.x, p.y] == TileType.Broken ||
            grid[p.x, p.y] == TileType.Player1Broken ||
            grid[p.x, p.y] == TileType.Player2Broken) return;

        TileType crackedType;
        switch (originalTileTypes[p.x, p.y])
        {
            case TileType.Player1: crackedType = TileType.Player1Cracked; break;
            case TileType.Player2: crackedType = TileType.Player2Cracked; break;
            default: crackedType = TileType.Cracked; break;
        }

        grid[p.x, p.y] = crackedType;

        var sr = tileObjects[p.x, p.y].GetComponent<SpriteRenderer>();
        switch (crackedType)
        {
            case TileType.Player1Cracked: sr.sprite = tileSet.player1CrackedTileSprite; break;
            case TileType.Player2Cracked: sr.sprite = tileSet.player2CrackedTileSprite; break;
        }

        StartCoroutine(TileCrackEffect(p));
        StartCoroutine(AutoRepairCrackedTile(p, crackedTileDuration));
    }

    public void BreakTile(Vector2Int p)
    {
        if (!IsValidGridPosition(p)) return;

        TileType brokenType;
        switch (originalTileTypes[p.x, p.y])
        {
            case TileType.Player1: brokenType = TileType.Player1Broken; break;
            case TileType.Player2: brokenType = TileType.Player2Broken; break;
            default: brokenType = TileType.Broken; break;
        }

        grid[p.x, p.y] = brokenType;

        var sr = tileObjects[p.x, p.y].GetComponent<SpriteRenderer>();
        switch (brokenType)
        {
            case TileType.Player1Broken: sr.sprite = tileSet.player1BrokenTileSprite; break;
            case TileType.Player2Broken: sr.sprite = tileSet.player2BrokenTileSprite; break;
        }

        StartCoroutine(TileBreakEffect(p));
        StartCoroutine(AutoRepairBrokenTile(p, brokenTileDuration));
    }

    private IEnumerator TileCrackEffect(Vector2Int p)
    {
        var tile = tileObjects[p.x, p.y];
        if (tile == null) yield break;

        var renderer = tile.GetComponent<SpriteRenderer>();
        var originalColor = renderer.color;

        renderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        renderer.color = originalColor;

        var originalPos = tile.transform.position;
        for (int i = 0; i < 3; i++)
        {
            tile.transform.position = originalPos + new Vector3(
                Random.Range(-0.05f, 0.05f),
                Random.Range(-0.05f, 0.05f), 0);
            yield return new WaitForSeconds(0.05f);
        }
        tile.transform.position = originalPos;
    }

    private IEnumerator TileBreakEffect(Vector2Int p)
    {
        var tile = tileObjects[p.x, p.y];
        if (tile == null) yield break;

        var renderer = tile.GetComponent<SpriteRenderer>();
        var originalColor = renderer.color;

        renderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        renderer.color = originalColor;

        var originalPos = tile.transform.position;
        for (int i = 0; i < 5; i++)
        {
            tile.transform.position = originalPos + new Vector3(
                Random.Range(-0.1f, 0.1f),
                Random.Range(-0.1f, 0.1f), 0);
            yield return new WaitForSeconds(0.05f);
        }
        tile.transform.position = originalPos;
    }

    private IEnumerator AutoRepairCrackedTile(Vector2Int p, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (IsValidGridPosition(p) &&
            (grid[p.x, p.y] == TileType.Cracked ||
             grid[p.x, p.y] == TileType.Player1Cracked ||
             grid[p.x, p.y] == TileType.Player2Cracked))
        {
            var originalType = originalTileTypes[p.x, p.y];
            SetTileType(p, originalType);
        }
    }

    private IEnumerator AutoRepairBrokenTile(Vector2Int p, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (IsValidGridPosition(p) &&
            (grid[p.x, p.y] == TileType.Broken ||
             grid[p.x, p.y] == TileType.Player1Broken ||
             grid[p.x, p.y] == TileType.Player2Broken))
        {
            var originalType = originalTileTypes[p.x, p.y];
            SetTileType(p, originalType);
        }
    }

    public Vector3 GetWorldPosition(Vector2Int p) => GetWorldPositionWithSimpleArena(p);

    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        float estimatedGridY = (worldPosition.y - gridOffset.y) / totalTileHeight;
        float visualYOffset = -estimatedGridY * 0.28f;
        float correctedY = worldPosition.y - visualYOffset;

        float adjustedX = (worldPosition.x - gridOffset.x) / totalTileWidth + HalfCols;
        float adjustedY = (correctedY - gridOffset.y) / totalTileHeight + HalfRows;

        int x = Mathf.RoundToInt(adjustedX);
        int y = Mathf.RoundToInt(adjustedY);

        x = Mathf.Clamp(x, 0, gridWidth - 1);
        y = Mathf.Clamp(y, 0, gridHeight - 1);

        return new Vector2Int(x, y);
    }

}
