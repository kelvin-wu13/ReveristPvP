using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
    Player,
    Enemy,
    Empty,
    PlayerCracked,
    PlayerBroken,
    EnemyCracked,
    EnemyBroken,
    Cracked,
    Broken
}

[System.Serializable]
public class TileSet
{
    public Sprite playerTileSprite;
    public Sprite enemyTileSprite;
    public Sprite emptyTileSprite;
    public Sprite playerCrackedTileSprite;
    public Sprite playerBrokenTileSprite;
    public Sprite enemyCrackedTileSprite;
    public Sprite enemyBrokenTileSprite;
}

public class TileGrid : MonoBehaviour
{
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

    // Dictionary to store objects currently in each grid position
    private Dictionary<Vector2Int, List<GameObject>> objectsInTiles = new Dictionary<Vector2Int, List<GameObject>>();

    private Dictionary<Vector2Int, bool> tileOccupationStatus = new Dictionary<Vector2Int, bool>();

    public void SetTileOccupied(Vector2Int pos, bool occupied)
    {
        if (IsValidGridPosition(pos))
            tileOccupationStatus[pos] = occupied;
    }

    public bool IsTileOccupied(Vector2Int pos)
    {
        return tileOccupationStatus.ContainsKey(pos) && tileOccupationStatus[pos];
    }

    // Calculated total tile size including spacing
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
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                objectsInTiles[pos] = new List<GameObject>();
                tileOccupationStatus[pos] = false;
            }
        }
    }

    // Add these public getter methods for tile size
    public float GetTileWidth()
    {
        return tileWidth;
    }

    public float GetTileHeight()
    {
        return tileHeight;
    }

    public Vector3 GetCenteredWorldPosition(Vector2Int gridPosition, float fixedZ = -1f)
    {
        Vector3 pos = GetWorldPosition(gridPosition);
        return new Vector3(pos.x, pos.y, fixedZ);
    }

    private void OnValidate()
    {
        // Ensure grid dimensions are always positive
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);

        // Ensure tile dimensions are always positive
        tileWidth = Mathf.Max(0.1f, tileWidth);
        tileHeight = Mathf.Max(0.1f, tileHeight);

        // Spacing can be zero but not negative
        horizontalSpacing = Mathf.Max(0f, horizontalSpacing);
        verticalSpacing = Mathf.Max(0f, verticalSpacing);

        //Apply rotation change
        if (Application.isPlaying)
        {
            transform.rotation = Quaternion.Euler(gridXRotation, gridYRotation, gridZRotation);
        }

        // If the grid is already initialized in play mode, update it
        if (Application.isPlaying && grid != null)
        {
            UpdateGridLayout();
        }
    }

    private void UpdateGridLayout()
    {
        // Store the current grid state
        TileType[,] oldGrid = grid;
        GameObject[,] oldTileObjects = tileObjects;
        TileType[,] oldOriginalTypes = originalTileTypes;

        int oldWidth = oldGrid.GetLength(0);
        int oldHeight = oldGrid.GetLength(1);

        // Initialize with new dimensions
        grid = new TileType[gridWidth, gridHeight];
        tileObjects = new GameObject[gridWidth, gridHeight];
        originalTileTypes = new TileType[gridWidth, gridHeight];

        // Update the objectsInTiles dictionary for the new dimensions
        InitializeObjectsInTilesDict();

        // Copy over the old data where possible
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (x < oldWidth && y < oldHeight)
                {
                    grid[x, y] = oldGrid[x, y];
                    originalTileTypes[x, y] = oldOriginalTypes[x, y];

                    if (oldTileObjects[x, y] != null)
                    {
                        // Update position and scale of existing tiles
                        tileObjects[x, y] = oldTileObjects[x, y];
                        UpdateTileTransform(new Vector2Int(x, y));
                    }
                    else
                    {
                        // Create tile if it doesn't exist
                        CreateTile(new Vector2Int(x, y));
                    }
                }
                else
                {
                    // Create new tiles for expanded grid
                    CreateTile(new Vector2Int(x, y));
                }
            }
        }

        // Clean up any tiles that are now out of bounds
        for (int x = 0; x < oldWidth; x++)
        {
            for (int y = 0; y < oldHeight; y++)
            {
                if (x >= gridWidth || y >= gridHeight)
                {
                    if (oldTileObjects[x, y] != null)
                    {
                        Destroy(oldTileObjects[x, y]);
                    }
                }
            }
        }
    }

    private void InitializeGrid()
    {
        // Initialize the grid
        grid = new TileType[gridWidth, gridHeight];
        tileObjects = new GameObject[gridWidth, gridHeight];
        originalTileTypes = new TileType[gridWidth, gridHeight];

        // Create the grid
        CreateGrid();

        // Setup initial player and enemy positions
        SetupInitialPositions();
    }

    private void CreateGrid()
    {
        //Apply Rotation
        transform.rotation = Quaternion.Euler(gridXRotation, gridYRotation, gridZRotation);
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                CreateTile(new Vector2Int(x, y));
            }
        }
    }

    public Vector3 GetWorldPositionWith3DEffect(Vector2Int gridPosition)
    {
        // Base position
        float x = gridPosition.x * totalTileWidth + gridOffset.x;
        float y = gridPosition.y * totalTileHeight + gridOffset.y;

        // Add 3D perspective effect
        float depthFactor = (float)gridPosition.y / gridHeight; // 0 to 1
        float perspectiveOffset = depthFactor * 0.5f; // Adjust this value

        // Scale tiles based on depth (further tiles smaller)
        float scaleReduction = 1f - (depthFactor * 0.2f);

        return new Vector3(x, y + perspectiveOffset, -depthFactor);
    }

    public Vector3 GetWorldPositionWithSimpleArena(Vector2Int gridPosition)
    {
        // Standard grid positioning (keep this structured)
        float x = gridPosition.x * totalTileWidth + gridOffset.x;
        float y = gridPosition.y * totalTileHeight + gridOffset.y;

        // Add VERY subtle depth effect only
        // Back rows (higher Y) pushed slightly back
        float depth = -gridPosition.y * 0.2f;

        // Optional: Very subtle Y offset for back rows (makes them appear slightly higher)
        // float heightOffset = gridPosition.y * 0.05f;

        return new Vector3(x, y, depth);
    }

    private void CreateTile(Vector2Int position)
    {
        // Use simple positioning that maintains grid structure
        Vector3 worldPosition = GetWorldPositionWithSimpleArena(position);
        GameObject tile = Instantiate(tilePrefab, worldPosition, Quaternion.identity, transform);
        tile.name = $"Tile_{position.x}_{position.y}";

        // Keep uniform scaling - don't vary tile sizes
        tile.transform.localScale = new Vector3(tileWidth, tileHeight, 1f);

        // NO rotation - keep tiles aligned
        // tile.transform.rotation = Quaternion.identity; (default)

        // Set up sprite renderer
        SpriteRenderer spriteRenderer = tile.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = tile.AddComponent<SpriteRenderer>();
        }
        spriteRenderer.sortingOrder = tileRenderingLayer;

        // Uniform color - no lighting variations
        spriteRenderer.color = Color.white;

        // Initialize tile type
        grid[position.x, position.y] = TileType.Empty;
        originalTileTypes[position.x, position.y] = TileType.Empty;
        spriteRenderer.sprite = tileSet.emptyTileSprite;

        tileObjects[position.x, position.y] = tile;
    }

    private void UpdateTileTransform(Vector2Int position)
    {
        GameObject tile = tileObjects[position.x, position.y];
        if (tile != null)
        {
            // Update position with simple arena effect
            tile.transform.position = GetWorldPositionWithSimpleArena(position);

            // Keep uniform scale
            tile.transform.localScale = new Vector3(tileWidth, tileHeight, 1f);

            // No rotation
            tile.transform.rotation = Quaternion.identity;

            // Update sorting order
            SpriteRenderer spriteRenderer = tile.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = tileRenderingLayer;
                spriteRenderer.color = Color.white;
            }
        }
    }

    public void SetupInitialPositions()
    {
        // Set left half for player (first half of columns)
        for (int x = 0; x < gridWidth / 2; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                SetTileType(new Vector2Int(x, y), TileType.Player);
            }
        }

        // Set right half for enemy (second half of columns)
        for (int x = gridWidth / 2; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                SetTileType(new Vector2Int(x, y), TileType.Enemy);
            }
        }
    }

    public void SetTileType(Vector2Int gridPosition, TileType type)
    {
        if (IsValidGridPosition(gridPosition))
        {
            grid[gridPosition.x, gridPosition.y] = type;

            SpriteRenderer spriteRenderer = tileObjects[gridPosition.x, gridPosition.y].GetComponent<SpriteRenderer>();

            switch (type)
            {
                case TileType.Player:
                    spriteRenderer.sprite = tileSet.playerTileSprite;
                    // Update originalTileType only for base types (not damage states)
                    originalTileTypes[gridPosition.x, gridPosition.y] = TileType.Player;
                    break;
                case TileType.Enemy:
                    spriteRenderer.sprite = tileSet.enemyTileSprite;
                    // Update originalTileType only for base types (not damage states)
                    originalTileTypes[gridPosition.x, gridPosition.y] = TileType.Enemy;
                    break;
                case TileType.Empty:
                    spriteRenderer.sprite = tileSet.emptyTileSprite;
                    // Update originalTileType only for base types (not damage states)
                    originalTileTypes[gridPosition.x, gridPosition.y] = TileType.Empty;
                    break;
                case TileType.PlayerCracked:
                    spriteRenderer.sprite = tileSet.playerCrackedTileSprite;
                    // Don't update originalTileType for damage states
                    break;
                case TileType.PlayerBroken:
                    spriteRenderer.sprite = tileSet.playerBrokenTileSprite;
                    // Don't update originalTileType for damage states
                    break;
                case TileType.EnemyCracked:
                    spriteRenderer.sprite = tileSet.enemyCrackedTileSprite;
                    // Don't update originalTileType for damage states
                    break;
                case TileType.EnemyBroken:
                    spriteRenderer.sprite = tileSet.enemyBrokenTileSprite;
                    // Don't update originalTileType for damage states
                    break;
            }
        }
    }

    public bool IsValidGridPosition(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < gridWidth &&
               gridPosition.y >= 0 && gridPosition.y < gridHeight;
    }

    public bool IsValidPlayerPosition(Vector2Int gridPosition)
    {
        // Check if position is valid and not an enemy tile or any broken tile
        return IsValidGridPosition(gridPosition) &&
               grid[gridPosition.x, gridPosition.y] != TileType.Enemy &&
               grid[gridPosition.x, gridPosition.y] != TileType.EnemyCracked &&
               grid[gridPosition.x, gridPosition.y] != TileType.EnemyBroken &&
               grid[gridPosition.x, gridPosition.y] != TileType.Broken &&
               grid[gridPosition.x, gridPosition.y] != TileType.PlayerBroken;
    }

    public void CrackTile(Vector2Int gridPosition)
    {
        if (IsValidGridPosition(gridPosition) &&
            grid[gridPosition.x, gridPosition.y] != TileType.Broken &&
            grid[gridPosition.x, gridPosition.y] != TileType.PlayerBroken &&
            grid[gridPosition.x, gridPosition.y] != TileType.EnemyBroken)
        {
            // Determine the appropriate cracked tile type based on the original tile
            TileType crackedType;
            switch (originalTileTypes[gridPosition.x, gridPosition.y])
            {
                case TileType.Player:
                    crackedType = TileType.PlayerCracked;
                    break;
                case TileType.Enemy:
                    crackedType = TileType.EnemyCracked;
                    break;
                default:
                    crackedType = TileType.Cracked;
                    break;
            }

            grid[gridPosition.x, gridPosition.y] = crackedType;

            // Update tile sprite to appropriate cracked type
            SpriteRenderer spriteRenderer = tileObjects[gridPosition.x, gridPosition.y].GetComponent<SpriteRenderer>();
            switch (crackedType)
            {
                case TileType.PlayerCracked:
                    spriteRenderer.sprite = tileSet.playerCrackedTileSprite;
                    break;
                case TileType.EnemyCracked:
                    spriteRenderer.sprite = tileSet.enemyCrackedTileSprite;
                    break;
            }

            // Optional: Add some visual effect to indicate the crack
            StartCoroutine(TileCrackEffect(gridPosition));

            // Start timer to auto-repair the cracked tile
            StartCoroutine(AutoRepairCrackedTile(gridPosition, crackedTileDuration));
        }
    }

    public void BreakTile(Vector2Int gridPosition)
    {
        if (IsValidGridPosition(gridPosition))
        {
            // Determine the appropriate broken tile type based on the original tile
            TileType brokenType;
            switch (originalTileTypes[gridPosition.x, gridPosition.y])
            {
                case TileType.Player:
                    brokenType = TileType.PlayerBroken;
                    break;
                case TileType.Enemy:
                    brokenType = TileType.EnemyBroken;
                    break;
                default:
                    brokenType = TileType.Broken;
                    break;
            }

            grid[gridPosition.x, gridPosition.y] = brokenType;

            // Update tile sprite to appropriate broken type
            SpriteRenderer spriteRenderer = tileObjects[gridPosition.x, gridPosition.y].GetComponent<SpriteRenderer>();
            switch (brokenType)
            {
                case TileType.PlayerBroken:
                    spriteRenderer.sprite = tileSet.playerBrokenTileSprite;
                    break;
                case TileType.EnemyBroken:
                    spriteRenderer.sprite = tileSet.enemyBrokenTileSprite;
                    break;
            }

            // Optional: Add some visual effect to indicate the breaking
            StartCoroutine(TileBreakEffect(gridPosition));

            // Start timer to auto-repair the broken tile
            StartCoroutine(AutoRepairBrokenTile(gridPosition, brokenTileDuration));
        }
    }

    private IEnumerator TileCrackEffect(Vector2Int gridPosition)
    {
        // Get the tile game object
        GameObject tile = tileObjects[gridPosition.x, gridPosition.y];
        if (tile == null) yield break;

        // Flash the tile
        SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
        Color originalColor = renderer.color;

        // Quick flash effect
        renderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        renderer.color = originalColor;

        // Optional: Add a slight shake effect
        Vector3 originalPosition = tile.transform.position;

        for (int i = 0; i < 3; i++)
        {
            // Small random movement
            tile.transform.position = originalPosition + new Vector3(
                Random.Range(-0.05f, 0.05f),
                Random.Range(-0.05f, 0.05f),
                0
            );
            yield return new WaitForSeconds(0.05f);
        }

        // Return to original position
        tile.transform.position = originalPosition;
    }

    private IEnumerator TileBreakEffect(Vector2Int gridPosition)
    {
        // Get the tile game object
        GameObject tile = tileObjects[gridPosition.x, gridPosition.y];
        if (tile == null) yield break;

        // Flash the tile
        SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
        Color originalColor = renderer.color;

        // Quick flash effect
        renderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        renderer.color = originalColor;

        // More dramatic shake effect
        Vector3 originalPosition = tile.transform.position;

        for (int i = 0; i < 5; i++)
        {
            // Larger random movement
            tile.transform.position = originalPosition + new Vector3(
                Random.Range(-0.1f, 0.1f),
                Random.Range(-0.1f, 0.1f),
                0
            );
            yield return new WaitForSeconds(0.05f);
        }

        // Return to original position
        tile.transform.position = originalPosition;
    }

    // Auto-repair methods for timed duration
    private IEnumerator AutoRepairCrackedTile(Vector2Int gridPosition, float duration)
    {
        yield return new WaitForSeconds(duration);

        // Only repair if the tile is still cracked (any cracked type)
        if (IsValidGridPosition(gridPosition) &&
            (grid[gridPosition.x, gridPosition.y] == TileType.Cracked ||
             grid[gridPosition.x, gridPosition.y] == TileType.PlayerCracked ||
             grid[gridPosition.x, gridPosition.y] == TileType.EnemyCracked))
        {
            // Reset to the original tile type
            TileType originalType = originalTileTypes[gridPosition.x, gridPosition.y];
            SetTileType(gridPosition, originalType);
        }
    }

    private IEnumerator AutoRepairBrokenTile(Vector2Int gridPosition, float duration)
    {
        yield return new WaitForSeconds(duration);

        // Only repair if the tile is still broken (any broken type)
        if (IsValidGridPosition(gridPosition) &&
            (grid[gridPosition.x, gridPosition.y] == TileType.Broken ||
             grid[gridPosition.x, gridPosition.y] == TileType.PlayerBroken ||
             grid[gridPosition.x, gridPosition.y] == TileType.EnemyBroken))
        {
            // Reset to the original tile type
            TileType originalType = originalTileTypes[gridPosition.x, gridPosition.y];
            SetTileType(gridPosition, originalType);
        }
    }

    public Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        return GetWorldPositionWithSimpleArena(gridPosition);
    }

    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        // Calculate approximate visual Y correction using vertical spacing (0.28f from inspector)
        float estimatedGridY = (worldPosition.y - gridOffset.y) / totalTileHeight;
        float visualYOffset = -estimatedGridY * 0.28f; // Visual depth correction
        float correctedY = worldPosition.y - visualYOffset;

        float adjustedX = (worldPosition.x - gridOffset.x - (tileWidth * 0.5f)) / totalTileWidth;
        float adjustedY = (correctedY - gridOffset.y - (tileHeight * 0.5f)) / totalTileHeight;

        int x = Mathf.RoundToInt(adjustedX);
        int y = Mathf.RoundToInt(adjustedY);

        x = Mathf.Clamp(x, 0, gridWidth - 1);
        y = Mathf.Clamp(y, 0, gridHeight - 1);

        return new Vector2Int(x, y);
    }
}