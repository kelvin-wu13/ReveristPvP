using UnityEngine;
using System.Collections;
using SkillSystem;

public class IonBolt : Skill, ISkillOwnerReceiver, ISkillDirectionReceiver
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float speedMultiplier = 1f;

    [Header("Hit/Explode (opsional)")]
    [SerializeField] private int damage = 20;
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private float explosionEffectDuration = 1f;

    [Header("Costs & CD")]
    [SerializeField] public float manaCost = 1.5f;
    [SerializeField] public float cooldownDuration = 1.5f;

    [Header("Anim Lock")]
    [SerializeField] private float skillAnimationDuration = 0.8f;

    [SerializeField] private float outOfBoundsDestroyDelay = 1.5f;

    private GameObject owner;
    private PlayerMovement ownerMove;
    private PlayerShoot ownerShoot;
    private PlayerStats ownerStats;
    private Animator ownerAnim;
    private TileGrid grid;

    private GameObject activeProjectile;
    private bool isFired;
    private bool onCooldown;

    // arah horizontal (+1 kanan / -1 kiri)
    private int dirX = 1;

    // tracking posisi grid & sisi pemilik (Left/Right)
    private Vector2Int currentGridPos;
    private TileGrid.Side ownerSide;

    private Transform spawnPoint;
    private bool destroyScheduled = false;
    private Coroutine destroyRoutine;

    // ===== API dari SkillCast =====
    public void SetOwner(GameObject ownerGO)
    {
        owner = ownerGO;
        ownerMove = owner ? owner.GetComponent<PlayerMovement>() : null;
        ownerShoot = owner ? owner.GetComponent<PlayerShoot>() : null;
        ownerStats = owner ? owner.GetComponent<PlayerStats>() : null;
        ownerAnim = owner ? owner.GetComponent<Animator>() : null;

        if (grid == null) grid = Object.FindObjectOfType<TileGrid>();
        spawnPoint = (ownerShoot != null && ownerShoot.GetBulletSpawnPoint() != null)
            ? ownerShoot.GetBulletSpawnPoint()
            : owner.transform;
    }

    public void SetDirection(Vector2 dir)
    {
        dirX = (dir.x >= 0f) ? 1 : -1;
    }

    public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
    {
        if (owner == null && casterTransform != null) SetOwner(casterTransform.gameObject);
        if (onCooldown || ownerStats == null) return;

        if (!ownerStats.TryUseMana(Mathf.CeilToInt(manaCost))) return;

        if (ownerShoot != null) ownerShoot.TriggerSkillAnimation(skillAnimationDuration);
        StartCoroutine(FireFlow());
    }

    private IEnumerator FireFlow()
    {
        // beri sedikit delay supaya anim/pos tersinkron
        yield return new WaitForSeconds(0.1f);

        FireProjectile();
        StartCoroutine(Cooldown());
    }

    private IEnumerator Cooldown()
    {
        onCooldown = true;
        yield return new WaitForSeconds(cooldownDuration);
        onCooldown = false;
    }

    private void FireProjectile()
    {
        if (grid == null) grid = Object.FindObjectOfType<TileGrid>();

        currentGridPos = (ownerMove != null)
            ? ownerMove.GetCurrentGridPosition()
            : grid.GetGridPosition(owner.transform.position);

        int mid = grid.gridWidth / 2;
        ownerSide = (currentGridPos.x < mid) ? TileGrid.Side.Left : TileGrid.Side.Right;

        Vector3 spawnPos = (spawnPoint != null) ? spawnPoint.position : owner.transform.position;
        activeProjectile = Object.Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        var psr = activeProjectile.GetComponent<ParticleSystemRenderer>();
        if (psr != null) psr.sortingOrder = 100;
        var sr = activeProjectile.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 100;
        activeProjectile.transform.position += new Vector3(0, 0, -0.2f);

        float angle = Mathf.Atan2(0f, dirX) * Mathf.Rad2Deg;
        activeProjectile.transform.rotation = Quaternion.Euler(0, 0, angle);

        isFired = true;

        TryHitOpponentOnThisTile(currentGridPos);
    }

    private void Update()
    {
        if (!isFired || activeProjectile == null) return;

        float spd = projectileSpeed * speedMultiplier;
        activeProjectile.transform.Translate(Vector3.right * dirX * spd * Time.deltaTime, Space.World);

        if (grid != null)
        {
            Vector2Int gridPosNow = grid.GetGridPosition(activeProjectile.transform.position);
            if (gridPosNow.x != currentGridPos.x)
            {
                currentGridPos.x = gridPosNow.x;

                TryHitOpponentOnThisTile(currentGridPos);

                CheckOutOfBounds();
            }
        }
        else
        {
            CheckOutOfBounds();
        }
    }

    private void TryHitOpponentOnThisTile(Vector2Int gridPos)
    {
        if (grid == null || !grid.IsValidGridPosition(gridPos)) return;

        var players = Object.FindObjectsOfType<PlayerMovement>();
        if (players == null || players.Length == 0) return;

        PlayerMovement opponent = null;
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p == null || p.gameObject == owner) continue;
            if (p.GetCurrentGridPosition() == gridPos)
            {
                opponent = p;
                break;
            }
        }

        if (opponent == null) return;

        var stat = opponent.GetComponent<PlayerStats>();
        var oppPos = opponent.GetCurrentGridPosition();

        int finalDamage = GridDamageCalculator.Calculate(new GridDamageCalculator.Ctx
        {
            grid = grid,
            attackerSide = ownerSide,
            attackerPos = currentGridPos,
            defenderPos = oppPos,
            baseDamage = damage
        });

        Debug.Log($"[IonBolt] HIT {opponent.name} @ {oppPos} for {finalDamage} (base {damage})");

        if (stat != null) stat.TakeDamage(finalDamage, owner);
        HitEvents.NotifyOwnerHit(owner, opponent.gameObject, true, "IonBolt");

        SpawnHitEffect(activeProjectile.transform.position);
        DestroyProjectileImmediate();
    }

    private void SpawnHitEffect(Vector3 pos)
    {
        if (explosionEffectPrefab != null)
        {
            var fx = Object.Instantiate(explosionEffectPrefab, pos, Quaternion.identity);
            Object.Destroy(fx, explosionEffectDuration);
        }
    }

    private void CheckOutOfBounds()
    {
        if (grid == null || activeProjectile == null || destroyScheduled) return;

        bool passedRight = dirX > 0 && currentGridPos.x >= grid.gridWidth - 1;
        bool passedLeft = dirX < 0 && currentGridPos.x <= 0;

        if (passedRight || passedLeft)
        {
            destroyScheduled = true;
            destroyRoutine = StartCoroutine(DestroyProjectileAfterDelay(outOfBoundsDestroyDelay));
        }
    }

    private IEnumerator DestroyProjectileAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        DestroyProjectileImmediate();
    }

    private void DestroyProjectileImmediate()
    {
        if (destroyRoutine != null) StopCoroutine(destroyRoutine);
        destroyScheduled = false;
        isFired = false;

        if (activeProjectile != null)
            Object.Destroy(activeProjectile);

        activeProjectile = null;
    }
}
