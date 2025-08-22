using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private int explosionTileRadius = 1;

    [Header("Costs & CD")]
    [SerializeField] public float manaCost = 1.5f;
    [SerializeField] public float cooldownDuration = 1.5f;

    [Header("Anim Lock")]
    [SerializeField] private float skillAnimationDuration = 0.8f;

    [SerializeField] private float outOfBoundsDestroyDelay = 1.5f;

    private bool destroyScheduled = false;
    private Coroutine destroyRoutine;
    private GameObject owner;
    private PlayerMovement ownerMove;
    private PlayerShoot ownerShoot;
    private PlayerStats ownerStats;
    private Animator ownerAnim;
    private TileGrid grid;

    private GameObject activeProjectile;
    private bool isFired;
    private bool onCooldown;
    private Vector2Int currentGridPos;
    private int dirX = 1;
    private Transform spawnPoint;

    public void SetOwner(GameObject ownerGO)
    {
        owner = ownerGO;
        ownerMove = owner ? owner.GetComponent<PlayerMovement>() : null;
        ownerShoot = owner ? owner.GetComponent<PlayerShoot>() : null;
        ownerStats = owner ? owner.GetComponent<PlayerStats>() : null;
        ownerAnim = owner ? owner.GetComponent<Animator>() : null;

        if (grid == null) grid = FindObjectOfType<TileGrid>();

        spawnPoint = (ownerShoot != null && ownerShoot.GetBulletSpawnPoint() != null)
            ? ownerShoot.GetBulletSpawnPoint()
            : owner.transform;
    }

    public void SetDirection(Vector2 dir)
    {
        dirX = dir.x >= 0 ? 1 : -1;
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
        if (grid == null) grid = FindObjectOfType<TileGrid>();
        if (ownerMove != null) currentGridPos = ownerMove.GetCurrentGridPosition();
        else if (grid != null) currentGridPos = grid.GetGridPosition(owner.transform.position);

        Vector3 spawnPos = (spawnPoint != null) ? spawnPoint.position : owner.transform.position;

        activeProjectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        var psr = activeProjectile.GetComponent<ParticleSystemRenderer>();
        if (psr != null) psr.sortingOrder = 100;
        var sr = activeProjectile.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 100;

        var p = activeProjectile.transform.position;
        activeProjectile.transform.position = new Vector3(p.x, p.y, p.z - 0.2f);

        isFired = true;


        AudioManager.Instance?.PlayIonBoltSFX();
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
                CheckOutOfBounds();
            }
        }
        else
        {
            CheckOutOfBounds();
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

        if (activeProjectile != null)
            Destroy(activeProjectile);

        activeProjectile = null;
        isFired = false;
    }
}
