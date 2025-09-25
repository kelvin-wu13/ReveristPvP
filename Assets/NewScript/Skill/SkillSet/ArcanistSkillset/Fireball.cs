using UnityEngine;
using System.Collections;
using SkillSystem;

public class Fireball : Skill, ISkillOwnerReceiver, ISkillDirectionReceiver, IDeflectableProjectile
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private int damage = 20;

    [Header("Costs & CD")]
    [SerializeField] public float manaCost = 1.5f;
    [SerializeField] public float cooldownDuration = 1.5f;

    [Header("Lock")]
    [SerializeField] private float skillAnimationDuration = 0.8f;
    [SerializeField] private bool lockToOwnerRow = true;
    [SerializeField] private float outOfBoundsDestroyDelay = 1.5f;

    private GameObject owner;
    private PlayerMovement ownerMove;
    private PlayerShoot ownerShoot;
    private PlayerStats ownerStats;
    private TileGrid grid;
    private int fixedRowY = -1;
    private GameObject activeProjectile;
    private bool isFired;
    private bool onCooldown;
    private int dirX = 1;
    private bool dirManuallySet = false;

    private Vector2Int currentGridPos;
    private TileGrid.Side ownerSide;

    private Transform spawnPoint;
    private bool destroyScheduled = false;
    private Coroutine destroyRoutine;

    public void SetOwner(GameObject ownerGO)
    {
        owner = ownerGO;
        ownerMove = owner ? owner.GetComponent<PlayerMovement>() : null;
        ownerShoot = owner ? owner.GetComponent<PlayerShoot>() : null;
        ownerStats = owner ? owner.GetComponent<PlayerStats>() : null;

        if (grid == null) grid = Object.FindObjectOfType<TileGrid>();
        spawnPoint = (ownerShoot != null && ownerShoot.GetBulletSpawnPoint() != null)
            ? ownerShoot.GetBulletSpawnPoint()
            : owner.transform;
    }

    public void SetDirection(Vector2 dir)
    {
        dirX = (dir.x >= 0f) ? 1 : -1;
        dirManuallySet = true;
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

        // Pos grid pemilik
        currentGridPos = (ownerMove != null)
            ? ownerMove.GetCurrentGridPosition()
            : grid.GetGridPosition(owner.transform.position);

        // Tentukan sisi pemilik (fallback arah bila tidak ada input)
        int mid = grid.gridWidth / 2;
        ownerSide = (currentGridPos.x < mid) ? TileGrid.Side.Left : TileGrid.Side.Right;

        if (!dirManuallySet)
            dirX = (ownerSide == TileGrid.Side.Left) ? +1 : -1; // P2 => kiri

        // Spawn peluru
        Vector3 spawnPos = (spawnPoint != null) ? spawnPoint.position : owner.transform.position;
        activeProjectile = Object.Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        // Tag & proxy interface agar Parry bisa menemukan handler di GO peluru
        if (!activeProjectile.CompareTag("Projectile")) activeProjectile.tag = "Projectile";
        var proxy = activeProjectile.GetComponent<FireballProjectile>();
        if (proxy == null) proxy = activeProjectile.AddComponent<FireballProjectile>();
        proxy.Bind(this);

        // Orientasi visual (Particle System / sprite flip)
        AlignParticleVisual(activeProjectile.transform, dirX);

        // Kunci ke row caster
        fixedRowY = currentGridPos.y;
        if (lockToOwnerRow && grid != null)
        {
            float rowY = grid.GetWorldPosition(new Vector2Int(0, fixedRowY)).y;
            var p = activeProjectile.transform.position;
            p.y = rowY;
            activeProjectile.transform.position = p;
        }

        currentGridPos = (grid != null) ? grid.GetGridPosition(activeProjectile.transform.position) : currentGridPos;
        if (lockToOwnerRow) currentGridPos = new Vector2Int(currentGridPos.x, fixedRowY);

        // Sedikit offset Z agar tidak ketimpa sprites lain (opsional)
        activeProjectile.transform.position += new Vector3(0, 0, -0.2f);

        isFired = true;
        TryHitOpponentOnThisTile(currentGridPos);
    }

    private void Update()
    {
        if (!isFired || activeProjectile == null) return;

        float spd = projectileSpeed * speedMultiplier;
        activeProjectile.transform.Translate(Vector3.right * dirX * spd * Time.deltaTime, Space.World);

        if (lockToOwnerRow && grid != null)
        {
            var pos = activeProjectile.transform.position;
            pos.y = grid.GetWorldPosition(new Vector2Int(0, fixedRowY)).y; // Y tetap untuk semua X
            activeProjectile.transform.position = pos;
        }

        if (grid != null)
        {
            Vector2Int gridPosNow = grid.GetGridPosition(activeProjectile.transform.position);

            if (gridPosNow.x != currentGridPos.x)
            {
                currentGridPos = gridPosNow;

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
            if (p.GetCurrentGridPosition() == gridPos) { opponent = p; break; }
        }
        if (opponent == null) return;

        var parry = opponent.GetComponent<ParryController>()
                ?? opponent.GetComponentInChildren<ParryController>(true)
                ?? opponent.GetComponentInParent<ParryController>();

        // Parry universal → proyektil dideflect (tidak ada damage/stack di sini)
        if (parry != null && parry.IsParryActive)
        {
            if (parry.TryDeflect(activeProjectile))
            {
                Debug.Log("[Fireball] DEFLECT triggered, cancel damage & stack");
                return;
            }
        }

        // Damage single-target (seperti Bullet)
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

        Debug.Log($"[Fireball] HIT {opponent.name} @ {oppPos} for {finalDamage} (base {damage})");

        if (stat != null) stat.TakeDamage(finalDamage, owner);
        HitEvents.NotifyOwnerHit(owner, opponent.gameObject, true, "Fireball");

        // +1 Fire stack pada target (akan trigger Ignite saat >=3 & reset)
        var stacks = opponent.GetComponent<ElementalStack>();
        if (stacks != null) stacks.Add(OverloadElement.Fire);

        DestroyProjectileImmediate(); // tidak meledak—langsung hilangkan peluru
    }

    private void CheckOutOfBounds()
    {
        if (grid == null || activeProjectile == null || destroyScheduled) return;

        bool passedRight = dirX > 0 && currentGridPos.x > grid.gridWidth - 1;
        bool passedLeft = dirX < 0 && currentGridPos.x < 0;

        if (passedRight || passedLeft)
        {
            destroyScheduled = true;
            destroyRoutine = StartCoroutine(DestroyProjectileAfterDelay(outOfBoundsDestroyDelay));
        }
    }

    private void AlignParticleVisual(Transform t, int dir)
    {
        if (t == null) return;

        var s = t.localScale;
        s.x = Mathf.Abs(s.x) * (dir >= 0 ? 1f : -1f);
        t.localScale = s;
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

    public void DeflectTo(GameObject newOwner, float dmgMult, float newSpeedMult, int reflectCount)
    {
        if (grid == null || activeProjectile == null || newOwner == null) return;

        // ganti "owner" ke defender (yang parry), agar kredit damage balik ke dia
        owner = newOwner;
        ownerMove = owner.GetComponent<PlayerMovement>();
        ownerShoot = owner.GetComponent<PlayerShoot>();
        ownerStats = owner.GetComponent<PlayerStats>();

        // hitung sisi & baris defender saat ini
        Vector2Int newOwnerPos = (ownerMove != null) ? ownerMove.GetCurrentGridPosition()
                                                     : grid.GetGridPosition(owner.transform.position);
        int mid = grid.gridWidth / 2;
        ownerSide = (newOwnerPos.x < mid) ? TileGrid.Side.Left : TileGrid.Side.Right;

        // balik arah & naikkan damage dan kecepatan
        dirX *= -1;
        damage = Mathf.CeilToInt(damage * dmgMult);
        speedMultiplier = newSpeedMult;

        var worldRowY = grid.GetWorldPosition(new Vector2Int(0, newOwnerPos.y)).y;
        var p = activeProjectile.transform.position;
        p.y = worldRowY;
        activeProjectile.transform.position = p;

        // samakan visualnya
        AlignParticleVisual(activeProjectile.transform, dirX);

        Debug.Log($"[Fireball] DEFLECT by {owner.name}: dmg x{dmgMult}, speed x{newSpeedMult}, count={reflectCount}, dirX={dirX}");
    }
}

// Proxy di GO peluru agar ParryController menemukan IDeflectableProjectile pada objek proyektil
public class FireballProjectile : MonoBehaviour, IDeflectableProjectile
{
    private Fireball skill;
    public void Bind(Fireball owner) => skill = owner;

    public void DeflectTo(GameObject newOwner, float damageMult, float speedMult, int reflectCount)
    {
        if (skill != null) skill.DeflectTo(newOwner, damageMult, speedMult, reflectCount);
    }
}
