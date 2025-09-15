using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SkillSystem;


public class PulseFall : Skill, ISkillOwnerReceiver
{
    [Header("Damage")]
    [SerializeField] private int damageAmount = 20;

    [Header("Timing")]
    [Tooltip("Delay sebelum efek mengenai tile/target.")]
    [SerializeField] private float impactDelay = 0.15f;
    [Tooltip("Jika PlayerShoot punya trigger anim, durasi lock anim saat cast.")]
    [SerializeField] private float animDuration = 0.5f;

    private GameObject owner;
    private PlayerMovement ownerMove;
    private PlayerStats ownerStats;
    private PlayerShoot ownerShoot;
    private PlayerCrosshair crosshair;
    private TileGrid grid;

    private Vector2Int ownerGridPos;
    private TileGrid.Side ownerSide;
    private int forwardX = +1;

    public void SetOwner(GameObject ownerGO)
    {
        owner = ownerGO;
        if (!owner) return;

        ownerMove = owner.GetComponent<PlayerMovement>();
        ownerStats = owner.GetComponent<PlayerStats>();
        ownerShoot = owner.GetComponent<PlayerShoot>();
        crosshair = owner.GetComponentInChildren<PlayerCrosshair>(true);

        if (!grid) grid = FindObjectOfType<TileGrid>();

        Debug.Log($"[PulseFall] Owner set: {owner.name}, gridFound={(grid != null)}");
    }

    public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
    {
        base.ExecuteSkillEffect(targetPosition, casterTransform);

        if (owner == null && casterTransform != null)
            SetOwner(casterTransform.gameObject);

        if (grid == null) { Debug.LogError("[PulseFall] Grid == null"); return; }
        if (owner == null) { Debug.LogError("[PulseFall] Owner == null"); return; }

        if (ownerShoot != null && animDuration > 0f)
            ownerShoot.TriggerSkillAnimation(animDuration);

        Vector2Int centerCell =
            (crosshair != null) ? crosshair.GetTargetGridPosition() :
            (grid.IsValidGridPosition(targetPosition) ? targetPosition
                                                      : grid.GetGridPosition(owner.transform.position));
        if (!grid.IsValidGridPosition(centerCell))
        {
            centerCell = grid.GetGridPosition(owner.transform.position);
            Debug.LogWarning($"[PulseFall] Target invalid, fallback ke {centerCell}");
        }

        ownerGridPos = (ownerMove != null)
            ? ownerMove.GetCurrentGridPosition()
            : grid.GetGridPosition(owner.transform.position);

        int mid = Mathf.Max(1, grid.gridWidth / 2);
        ownerSide = (ownerGridPos.x < mid) ? TileGrid.Side.Left : TileGrid.Side.Right;
        forwardX = (ownerSide == TileGrid.Side.Left) ? +1 : -1;

        var cells = new List<Vector2Int>(2) { centerCell };
        Vector2Int ahead = new Vector2Int(centerCell.x + forwardX, centerCell.y);
        if (grid.IsValidGridPosition(ahead)) cells.Add(ahead);

        var root = transform.root ? transform.root.gameObject : gameObject;
        root.transform.position = grid.GetCenteredWorldPosition(centerCell);
        if (LifetimeSeconds > 0f) Destroy(root, LifetimeSeconds);

        Debug.Log($"[PulseFall] READY side={ownerSide} forwardX={forwardX} center={centerCell} " +
                  $"cells=[{string.Join(",", cells)}] lifetime={LifetimeSeconds}");

        StartCoroutine(Co_Impact(cells));
    }

    private IEnumerator Co_Impact(List<Vector2Int> cells)
    {
        if (impactDelay > 0f) yield return new WaitForSeconds(impactDelay);

        foreach (var cell in cells)
        {
            if (!grid.IsValidGridPosition(cell))
            {
                Debug.LogWarning($"[PulseFall] Skip (out of grid): {cell}");
                continue;
            }

            var victim = FindVictimAt(cell);

            if (victim != null)
            {
                var vStat = victim.GetComponent<PlayerStats>();
                if (vStat != null)
                {
                    int finalDamage = damageAmount;
                    try
                    {
                        finalDamage = GridDamageCalculator.Calculate(new GridDamageCalculator.Ctx
                        {
                            grid = grid,
                            attackerSide = ownerSide,
                            attackerPos = ownerGridPos,
                            defenderPos = cell,
                            baseDamage = damageAmount
                        });
                    }
                    catch {}

                    vStat.TakeDamage(finalDamage, owner);
                    HitEvents.NotifyOwnerHit(owner, victim.gameObject, true, "PulseFall");
                    Debug.Log($"[PulseFall] Hit {victim.name} @ {cell} for {finalDamage}");
                }

                var before = grid.grid[cell.x, cell.y];
                grid.CrackTile(cell);
                Debug.Log($"[PulseFall] CrackTile {cell} (prev={before}) karena ADA target");
            }
            else
            {
                var before = grid.grid[cell.x, cell.y];
                grid.BreakTile(cell);
                Debug.Log($"[PulseFall] BreakTile {cell} (prev={before}) karena TIDAK ADA target");
            }
        }
    }

    private PlayerMovement FindVictimAt(Vector2Int cell)
    {
        var movers = FindObjectsOfType<PlayerMovement>();
        foreach (var p in movers)
        {
            if (!p) continue;
            if (owner && p.gameObject == owner) continue;
            if (p.GetCurrentGridPosition() == cell) return p;
        }
        return null;
    }
}
