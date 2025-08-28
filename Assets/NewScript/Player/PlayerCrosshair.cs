using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCrosshair : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private TileGrid tileGrid;
    [SerializeField] private GameObject crosshairVisual;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Settings")]
    [SerializeField] private int distanceFromPlayer = 3;

    private Vector2Int playerFacingDirection = Vector2Int.right;
    private Vector2Int playerGridPosition;
    private Vector2Int targetGridPosition;
    private SpriteRenderer crosshairRenderer;

    private bool isFrozen = false;
    private Vector2Int frozenPosition;

    private void Start()
    {
        if (playerTransform == null)
            Debug.LogWarning("PlayerCrosshair: Player Transform reference is missing!");

        if (tileGrid == null)
        {
            tileGrid = FindObjectOfType<TileGrid>();
            if (tileGrid == null)
                Debug.LogError("PlayerCrosshair: TileGrid not found in scene!");
        }

        if (playerMovement == null)
        {
            playerMovement = playerTransform.GetComponent<PlayerMovement>();
            if (playerMovement == null)
                Debug.LogError("PlayerCrosshair: PlayerMovement not found on player!");
        }

        crosshairRenderer = crosshairVisual.GetComponent<SpriteRenderer>();
        if (crosshairRenderer == null)
            crosshairRenderer = crosshairVisual.AddComponent<SpriteRenderer>();

        UpdatePositions();
    }

    private void Update()
    {
        UpdatePositions();
    }

    private void UpdatePositions()
    {
        if (isFrozen)
        {
            Vector3 frozenTileWorldPos = tileGrid.GetWorldPosition(frozenPosition);
            transform.position = frozenTileWorldPos + new Vector3(-0.05f, -0.01f, 0);
            return;
        }

        playerGridPosition = playerMovement.GetCurrentGridPosition();

        targetGridPosition = playerGridPosition + (playerFacingDirection * (distanceFromPlayer + 1));

        targetGridPosition.x = Mathf.Clamp(targetGridPosition.x, 0, tileGrid.gridWidth - 1);
        targetGridPosition.y = Mathf.Clamp(targetGridPosition.y, 0, tileGrid.gridHeight - 1);

        Vector3 tileWorldPos = tileGrid.GetWorldPosition(targetGridPosition);
        transform.position = tileWorldPos + new Vector3(-0.05f, -0.01f, 0);
    }

    public void SetPlayerFacingDirection(Vector2Int newDirection)
    {
        if (newDirection != Vector2Int.zero && !isFrozen)
        {
            playerFacingDirection = newDirection;
            UpdatePositions();
        }
    }

    public Vector2Int GetTargetGridPosition() => isFrozen ? frozenPosition : targetGridPosition;

    public Vector3 GetTargetWorldPosition()
    {
        Vector2Int currentTarget = isFrozen ? frozenPosition : targetGridPosition;
        return tileGrid.GetWorldPosition(currentTarget) + new Vector3(-0.05f, -0.01f, 0);
    }

    public bool IsCellTargeted(Vector2Int cellPosition)
    {
        Vector2Int currentTarget = isFrozen ? frozenPosition : targetGridPosition;
        return cellPosition == currentTarget;
    }

    public void FreezeCrosshair()
    {
        isFrozen = true;
        frozenPosition = targetGridPosition;
        Debug.Log($"PlayerCrosshair: Frozen at position {frozenPosition}");
    }

    public void UnfreezeCrosshair()
    {
        isFrozen = false;
        Debug.Log("PlayerCrosshair: Unfrozen");
        UpdatePositions();
    }

    public bool IsFrozen() => isFrozen;

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && tileGrid != null)
        {
            Vector2Int currentTarget = isFrozen ? frozenPosition : targetGridPosition;
            Gizmos.color = isFrozen ? new Color(1f, 0f, 0f, 0.5f) : new Color(1f, 1f, 0f, 0.5f);
            Vector3 targetPos = tileGrid.GetWorldPosition(currentTarget) + new Vector3(0.5f, 0.5f, 0);
            Gizmos.DrawCube(targetPos, new Vector3(1, 1, 0.1f));

            if (playerTransform != null)
            {
                Gizmos.color = isFrozen ? new Color(1f, 0f, 0f, 0.2f) : new Color(1f, 1f, 0f, 0.2f);
                Gizmos.DrawLine(playerTransform.position, targetPos);
            }
        }
    }
}
