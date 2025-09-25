using System.Collections;
using UnityEngine;
using SkillSystem;

public class WilloWisp : Skill
{
    [Header("Skill Parameters")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private int damageAmount = 20;
    [SerializeField] private int maxShots = 3;
    [SerializeField] private float chargeTime = 1.0f;
    [SerializeField] public float cooldownDuration = 2.0f;
    [SerializeField] public float manaCost = 2.0f;
    [SerializeField] public float animDuration = 0.5f;

    [Header("Visual Effects")]
    [SerializeField] private Color attackFlashColor = Color.red;
    [SerializeField] private Color chargingColor = Color.yellow;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float spawnYOffset = 1.0f;

    private TileGrid tileGrid;
    private PlayerStats playerStats;
    private PlayerShoot playerShoot;
    private Transform targetEnemy;
    private Vector2Int currentGridPosition;
    private Vector2Int targetPositionAtChargeStart;
    private int shotsFired = 0;
    private bool isMoving = false;
    private bool isSkillActive = true;
    private bool isCharging = false;

    public override void Initialize(Vector2Int gridPos, SkillCombination type, Transform caster)
    {
        base.Initialize(gridPos, type, caster);

        tileGrid = FindObjectOfType<TileGrid>();
        if (tileGrid == null) { Debug.LogError("WilloWisp: TileGrid not found!"); return; }

        playerStats = FindObjectOfType<PlayerStats>();
        if (playerStats == null) { Debug.LogError("WilloWisp: PlayerStats not found!"); return; }

        shotsFired = 0;
        isMoving = false;
        isCharging = false;
        currentGridPosition = gridPos;

        targetEnemy = FindNearestEnemy();

        if (targetEnemy != null)
        {
            transform.position = targetEnemy.position + Vector3.up * spawnYOffset;
        }
        else
        {
            transform.position = tileGrid.GetWorldPosition(gridPos);
        }

        playerShoot?.TriggerSkillAnimation(animDuration);
        //AudioManager.Instance?.PlayWilloWispSFX();
        StartCoroutine(FindAndEngageEnemies());
    }

    public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform) { }

    private IEnumerator FindAndEngageEnemies()
    {
        if (playerStats != null && !playerStats.TryUseMana(manaCost))
        {
            Debug.Log("Not enough mana to cast WilloWisp!");
            Destroy(gameObject);
            yield break;
        }

        while (isSkillActive && shotsFired < maxShots)
        {
            if (targetEnemy == null || !targetEnemy.gameObject.activeInHierarchy)
            {
                targetEnemy = FindNearestEnemy();
                if (targetEnemy == null)
                {
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }
                transform.position = targetEnemy.position + Vector3.up * spawnYOffset;
            }

            Vector2Int enemyGridPos = tileGrid.GetGridPosition(targetEnemy.position);

            if (currentGridPosition != enemyGridPos)
            {
                yield return StartCoroutine(MoveTowardsEnemy());
                if (targetEnemy == null || !targetEnemy.gameObject.activeInHierarchy) continue;
            }

            enemyGridPos = tileGrid.GetGridPosition(targetEnemy.position);

            if (currentGridPosition == enemyGridPos)
            {
                targetPositionAtChargeStart = enemyGridPos;
                yield return StartCoroutine(ChargeAttack());
                yield return StartCoroutine(PerformAttack());

                if (shotsFired >= maxShots) break;
            }

            yield return new WaitForSeconds(0.2f);
        }

        Destroy(gameObject);
    }

    private Transform FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return null;

        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (GameObject enemy in enemies)
        {
            if (!enemy.activeInHierarchy) continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = enemy.transform;
            }
        }

        return closest;
    }

    private IEnumerator MoveTowardsEnemy()
    {
        if (targetEnemy == null) yield break;

        isMoving = true;
        Vector2Int targetGridPos = tileGrid.GetGridPosition(targetEnemy.position);

        while (currentGridPosition != targetGridPos && targetEnemy != null && targetEnemy.gameObject.activeInHierarchy)
        {
            Vector2Int moveDir = GetMoveDirection(currentGridPosition, targetGridPos);
            Vector2Int newPos = currentGridPosition + moveDir;

            transform.position = tileGrid.GetWorldPosition(newPos);
            currentGridPosition = newPos;

            yield return new WaitForSeconds(1f / moveSpeed);

            if (targetEnemy != null && targetEnemy.gameObject.activeInHierarchy)
            {
                targetGridPos = tileGrid.GetGridPosition(targetEnemy.position);
            }
            else
            {
                break;
            }
        }
        isMoving = false;

        // ✅ Reposition above enemy head after move completes
        if (targetEnemy != null && targetEnemy.gameObject.activeInHierarchy)
        {
            transform.position = targetEnemy.position + Vector3.up * spawnYOffset;
        }
    }

    private Vector2Int GetMoveDirection(Vector2Int current, Vector2Int target)
    {
        Vector2Int dir = Vector2Int.zero;
        if (current.x < target.x) dir.x = 1;
        else if (current.x > target.x) dir.x = -1;
        if (current.y < target.y) dir.y = 1;
        else if (current.y > target.y) dir.y = -1;
        return dir;
    }

    private IEnumerator ChargeAttack()
    {
        if (targetEnemy == null || !targetEnemy.gameObject.activeInHierarchy) yield break;

        isCharging = true;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        Color originalColor = Color.white;
        if (renderer != null)
        {
            originalColor = renderer.color;
            renderer.color = chargingColor;
        }

        yield return new WaitForSeconds(chargeTime);

        if (renderer != null) renderer.color = originalColor;

        isCharging = false;
    }

    private IEnumerator PerformAttack()
    {
        if (targetEnemy == null || !targetEnemy.gameObject.activeInHierarchy) yield break;

        yield return StartCoroutine(VisualAttackFeedback());

        Vector2Int currentEnemyPos = tileGrid.GetGridPosition(targetEnemy.position);
        shotsFired++;

        bool dodged = currentEnemyPos != targetPositionAtChargeStart;

        //if (!dodged)
        //{
        //    Enemy enemyComponent = targetEnemy.GetComponent<Enemy>();
        //    if (enemyComponent != null)
        //    {
        //        enemyComponent.TakeDamage(damageAmount);
        //    }
        //}
    }

    private IEnumerator VisualAttackFeedback()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.color;
            renderer.color = attackFlashColor;
            yield return new WaitForSeconds(flashDuration);
            renderer.color = originalColor;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && tileGrid != null)
        {
            Vector3 centerPos = tileGrid.GetWorldPosition(currentGridPosition);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(centerPos, Vector3.one);

            if (isCharging && targetEnemy != null)
            {
                Vector3 targetPos = tileGrid.GetWorldPosition(targetPositionAtChargeStart);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(targetPos, 0.3f);
            }
        }
    }
}
