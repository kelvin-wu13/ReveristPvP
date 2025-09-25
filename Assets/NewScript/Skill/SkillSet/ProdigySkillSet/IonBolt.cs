using UnityEngine;
using System.Collections;
using SkillSystem;

public class IonBolt : Skill, ISkillOwnerReceiver, ISkillDirectionReceiver, IDeflectableProjectile
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float speedMultiplier = 1f;

    [Header("Hit/Explode")]
    [SerializeField] private int damage = 20;
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private float explosionEffectDuration = 1f;

    [Header("AOE")]
    [SerializeField] private int aoeHalfRange = 1;

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

        // Pos grid pemilik
        currentGridPos = (ownerMove != null)
            ? ownerMove.GetCurrentGridPosition()
            : grid.GetGridPosition(owner.transform.position);

        // Tentukan sisi pemilik (fallback arah bila tidak ada input)
        int mid = grid.gridWidth / 2;
        ownerSide = (currentGridPos.x < mid) ? TileGrid.Side.Left : TileGrid.Side.Right;

        if (!dirManuallySet)
        {
            dirX = (ownerSide == TileGrid.Side.Left) ? +1 : -1; // P2 => kiri
        }

        Vector3 spawnPos = (spawnPoint != null) ? spawnPoint.position : owner.transform.position;
        activeProjectile = Object.Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        if (!activeProjectile.CompareTag("Projectile")) activeProjectile.tag = "Projectile";

        var proxy = activeProjectile.GetComponent<IonBoltProjectile>();
        if (proxy == null) proxy = activeProjectile.AddComponent<IonBoltProjectile>();
        proxy.Bind(this);
        AlignParticleVisual(activeProjectile.transform, dirX);

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

        activeProjectile.transform.position += new Vector3(0, 0, -0.2f);

        isFired = true;
        TryHitOpponentOnThisTile(currentGridPos);
    }

    private void Update()
    {
        if (!isFired || activeProjectile == null) return;

        // gerak
        float spd = projectileSpeed * speedMultiplier;
        activeProjectile.transform.Translate(Vector3.right * dirX * spd * Time.deltaTime, Space.World);

        // kunci ke row pemilik (opsional)
        if (lockToOwnerRow && grid != null)
        {
            var pos = activeProjectile.transform.position;
            pos.y = grid.GetWorldPosition(new Vector2Int(0, fixedRowY)).y;
            activeProjectile.transform.position = pos;
        }

        // --- HIT CHECK saat ganti tile (tetap) ---
        if (grid != null)
        {
            Vector2Int gridPosNow = grid.GetGridPosition(activeProjectile.transform.position);
            if (gridPosNow.x != currentGridPos.x)
            {
                currentGridPos = gridPosNow;
                TryHitOpponentOnThisTile(currentGridPos);
            }
        }

        // --- OOB CHECK setiap frame (baru) ---
        CheckOutOfBoundsWorld();
    }

    private void CheckOutOfBoundsWorld()
    {
        if (grid == null || activeProjectile == null || destroyScheduled) return;

        // Tepi kiri/kanan area grid (x saja cukup, y tidak mempengaruhi tepi X)
        float leftEdgeX = grid.GetWorldPosition(new Vector2Int(0, 0)).x;
        float rightEdgeX = grid.GetWorldPosition(new Vector2Int(grid.gridWidth - 1, 0)).x + 1f; // +lebar 1 tile

        float x = activeProjectile.transform.position.x;

        // Hancurkan hanya kalau SUDAH melewati tepi (strict)
        if (dirX > 0 && x > rightEdgeX)
        {
            DestroyProjectileImmediate();
            return;
        }
        if (dirX < 0 && x < leftEdgeX)
        {
            DestroyProjectileImmediate();
            return;
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

        // Cek parry pada TARGET; jika aktif, proyektil dideflect dan TIDAK memicu AOE
        var parry = opponent.GetComponent<ParryController>()
                ?? opponent.GetComponentInChildren<ParryController>(true)
                ?? opponent.GetComponentInParent<ParryController>();

        if (parry != null && parry.IsParryActive)
        {
            if (parry.TryDeflect(activeProjectile))
            {
                Debug.Log("[IonBolt] DEFLECT triggered, cancel AOE");
                return;
            }
        }

        // >>> Proyektil hanya cek "kena" → picu AOE 3x3 di sel benturan
        DealAOEAt(gridPos);

        // FX & hapus peluru
        SpawnHitEffect(activeProjectile.transform.position);
        DestroyProjectileImmediate();
    }
    private void DealAOEAt(Vector2Int centerCell)
    {
        if (grid == null) return;

        var players = Object.FindObjectsOfType<PlayerMovement>();
        if (players == null || players.Length == 0) return;

        for (int dy = -aoeHalfRange; dy <= aoeHalfRange; dy++)
        {
            for (int dx = -aoeHalfRange; dx <= aoeHalfRange; dx++)
            {
                var cell = new Vector2Int(centerCell.x + dx, centerCell.y + dy);
                if (!grid.IsValidGridPosition(cell)) continue;

                for (int i = 0; i < players.Length; i++)
                {
                    var p = players[i];
                    if (p == null || p.gameObject == owner) continue;
                    if (p.GetCurrentGridPosition() != cell) continue;

                    var stat = p.GetComponent<PlayerStats>();
                    if (stat == null) continue;

                    // Hitung damage (boleh pakai base damage langsung; tetap pakai kalkulator biar konsisten)
                    int finalDamage = GridDamageCalculator.Calculate(new GridDamageCalculator.Ctx
                    {
                        grid = grid,
                        attackerSide = ownerSide,
                        attackerPos = centerCell,   // pusat ledakan
                        defenderPos = cell,
                        baseDamage = damage
                    });

                    stat.TakeDamage(finalDamage, owner);
                    HitEvents.NotifyOwnerHit(owner, p.gameObject, true, "IonBolt-AOE");
                    Debug.Log($"[IonBolt] AOE hit {p.name} @ {cell} for {finalDamage} (base {damage})");
                }
            }
        }
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
    // Dipanggil saat parry sukses
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

        Debug.Log($"[IonBolt] DEFLECT by {owner.name}: dmg x{dmgMult}, speed x{newSpeedMult}, count={reflectCount}, dirX={dirX}");
    }
}

public class IonBoltProjectile : MonoBehaviour, IDeflectableProjectile
{
    private IonBolt skill;
    public void Bind(IonBolt owner) => skill = owner;

    // Dipanggil ParryController
    public void DeflectTo(GameObject newOwner, float damageMult, float speedMult, int reflectCount)
    {
        if (skill != null) skill.DeflectTo(newOwner, damageMult, speedMult, reflectCount);
    }
}

