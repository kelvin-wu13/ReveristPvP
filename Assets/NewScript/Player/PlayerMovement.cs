using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float moveDuration = 0.2f;

    [Header("Grid")]
    [SerializeField] private TileGrid tileGrid;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    private readonly int isMovingParam = Animator.StringToHash("IsMoving");
    private readonly int directionXParam = Animator.StringToHash("DirectionX");
    private readonly int directionYParam = Animator.StringToHash("DirectionY");

    [Header("Position Offset")]
    [SerializeField] private Vector2 positionOffset = new Vector2(0f, 1.6f);
    [SerializeField] private float yOffsetFalloffPerRow = 0.1f;
    [SerializeField] private float xOffsetFalloffPerRow = 0.05f;

    [Header("Animation Settings")]
    [SerializeField] private bool smoothDirectionTransition = true;
    [SerializeField] private float directionSmoothTime = 0.1f;

    [Header("Input System")]
    [SerializeField] private InputActionReference moveAction;   // Vector2
    [SerializeField] private float analogThreshold = 0.5f;
    [SerializeField] private float initialRepeatDelay = 0.25f;
    [SerializeField] private float repeatRate = 0.12f;

    [SerializeField] private float externalSpeedMultiplier = 1f;

    private Vector2 currentAnimDirection = Vector2.zero;
    private Vector2 targetAnimDirection = Vector2.down;
    private Vector2 directionVelocity = Vector2.zero;

    private bool isMoving = false;
    private bool canMove = true;

    private Vector2Int currentGridPosition = new Vector2Int(0, 0);
    private Vector2Int lastDirection = Vector2Int.down;

    // repeat state
    private Vector2Int heldDir = Vector2Int.zero;
    private float nextRepeatTime = 0f;

    public Vector2Int GetCurrentGridPosition() => currentGridPosition;
    public Vector2 GetPositionOffset() => positionOffset;
    public Vector2Int GetFacingDirection() => lastDirection;
    public bool IsMoving() => isMoving;

    private void OnEnable()
    {
        moveAction?.action.Enable();
    }

    private void OnDisable()
    {
        moveAction?.action.Disable();
    }

    private void Start()
    {
        transform.position = GetAdjustedWorldPosition(currentGridPosition);
        if (animator == null) animator = GetComponent<Animator>();

        targetAnimDirection = Vector2.down;
        currentAnimDirection = Vector2.down;
        UpdateAnimationParameters(false);
    }

    private void Update()
    {
        if (!canMove) return;

        HandleControllerMove();
        UpdateAnimationDirection();
        UpdateAnimationParameters(isMoving);
    }
    public void SetExternalSpeedMultiplier(float mult)
    {
        externalSpeedMultiplier = Mathf.Max(0.25f, mult);
    }

    private void HandleControllerMove()
    {
        if (moveAction == null) return;

        Vector2 raw = moveAction.action.ReadValue<Vector2>();

        Vector2Int dir = Vector2Int.zero;
        if (Mathf.Abs(raw.x) > Mathf.Abs(raw.y))
        {
            if (Mathf.Abs(raw.x) >= analogThreshold) dir = new Vector2Int((int)Mathf.Sign(raw.x), 0);
        }
        else
        {
            if (Mathf.Abs(raw.y) >= analogThreshold) dir = new Vector2Int(0, (int)Mathf.Sign(raw.y));
        }

        if (dir != Vector2Int.zero)
        {
            lastDirection = dir;
            targetAnimDirection = new Vector2(dir.x, dir.y);
        }

        if (isMoving) return;

        if (dir == Vector2Int.zero)
        {
            heldDir = Vector2Int.zero;
        }
        else
        {
            if (heldDir != dir)
            {
                heldDir = dir;
                TryMoveOnce(heldDir);
                nextRepeatTime = Time.time + (initialRepeatDelay / externalSpeedMultiplier);
            }
            else if (Time.time >= nextRepeatTime)
            {
                TryMoveOnce(heldDir);
                nextRepeatTime = Time.time + (repeatRate / externalSpeedMultiplier);
            }
        }
    }

    private void TryMoveOnce(Vector2Int dir)
    {
        if (!CanMove(dir)) return;
        lastDirection = dir;
        targetAnimDirection = new Vector2(dir.x, dir.y);
        TryMove(dir);
    }

    private void UpdateAnimationDirection()
    {
        currentAnimDirection = smoothDirectionTransition
            ? Vector2.SmoothDamp(currentAnimDirection, targetAnimDirection, ref directionVelocity, directionSmoothTime)
            : targetAnimDirection;
    }

    private bool CanMove(Vector2Int direction)
    {
        Vector2Int targetGridPosition = currentGridPosition + direction;
        return tileGrid.IsValidPlayerPosition(targetGridPosition);
    }

    private void TryMove(Vector2Int direction)
    {
        Vector2Int targetGridPosition = currentGridPosition + direction;
        if (tileGrid.IsValidPlayerPosition(targetGridPosition))
            StartCoroutine(Move(targetGridPosition));
    }

    private IEnumerator Move(Vector2Int targetGridPosition)
    {
        isMoving = true;
        animator.SetBool(isMovingParam, true);

        Vector3 startPos = transform.position;
        Vector3 endPos = GetAdjustedWorldPosition(targetGridPosition);

        float elapsedTime = 0f;
        float duration = moveDuration / externalSpeedMultiplier;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float percent = elapsedTime / duration;
            transform.position = Vector3.Lerp(startPos, endPos, percent);
            yield return null;
        }

        transform.position = endPos;
        currentGridPosition = targetGridPosition;
        isMoving = false;
        animator.SetBool(isMovingParam, false);
        targetAnimDirection = Vector2.zero;
    }

    private void UpdateAnimationParameters(bool moving)
    {
        if (animator == null) return;

        if (moving)
        {
            animator.SetFloat(directionXParam, currentAnimDirection.x);
            animator.SetFloat(directionYParam, currentAnimDirection.y);
        }
        else
        {
            animator.SetFloat(directionXParam, 0f);
            animator.SetFloat(directionYParam, 0f);
        }
    }

    public void ForceIdle()
    {
        isMoving = false;
        animator.SetBool(isMovingParam, false);
        currentAnimDirection = Vector2.zero;
        targetAnimDirection = Vector2.zero;
        animator.SetFloat(directionXParam, 0f);
        animator.SetFloat(directionYParam, 0f);
    }

    public void SetFacingDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero) return;
        lastDirection = direction;
        targetAnimDirection = new Vector2(direction.x, direction.y);
    }

    private Vector3 GetAdjustedWorldPosition(Vector2Int gridPosition)
    {
        Vector3 basePos = tileGrid.GetCenteredWorldPosition(gridPosition);

        float dynamicYOffset = positionOffset.y - (gridPosition.y * yOffsetFalloffPerRow);
        float dynamicXOffset = positionOffset.x - (gridPosition.y * xOffsetFalloffPerRow);

        return basePos + new Vector3(dynamicXOffset, dynamicYOffset, 0f);
    }

    public void SetCanMove(bool state) { canMove = state; }
}
