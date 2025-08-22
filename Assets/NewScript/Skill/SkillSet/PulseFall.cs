using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SkillSystem;

public class PulseFall : Skill
{
    [Header("Skill Properties")]
    [SerializeField] public float cooldownDuration = 3.0f;
    [SerializeField] public float manaCost = 15.0f;

    [Header("QE Skill Settings")]
    [SerializeField] private int damageAmount = 20;
    [SerializeField] private int horizontalRange = 1;
    [SerializeField] private float impactDelay = 0.5f;
    [SerializeField] private float  animDuration = 0.5f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject skillProjectilePrefab;
    [SerializeField] private float projectileDropHeight = 5f;
    [SerializeField] private float projectileDropSpeed = 10f;
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private AudioClip impactSound;

    [Header("Tile Effects")]
    [SerializeField] private GameObject crackedTileEffectPrefab;
    [SerializeField] private GameObject brokenTileEffectPrefab;

    private TileGrid tileGrid;
    private PlayerCrosshair playerCrosshair;
    private List<Vector2Int> crackedTilePositions = new List<Vector2Int>();
    private AudioSource audioSource;
    private PlayerStats playerStats;
    private PlayerShoot playerShoot;

    private void Awake()
    {
        tileGrid = FindObjectOfType<TileGrid>();
        playerStats = FindObjectOfType<PlayerStats>();
        playerCrosshair = FindObjectOfType<PlayerCrosshair>();
        playerShoot = FindObjectOfType<PlayerShoot>();
        
    }

    public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
    {
        base.ExecuteSkillEffect(targetPosition, casterTransform);

        if (tileGrid == null) return;

        if (playerStats != null && !playerStats.TryUseMana(manaCost))
        {
            return;
        }

        Vector3 targetWorldPos;
        Vector2Int crosshairTargetPosition;

        if (playerCrosshair != null)
        {
            crosshairTargetPosition = playerCrosshair.GetTargetGridPosition();
            targetWorldPos = playerCrosshair.GetTargetWorldPosition();
        }
        else
        {
            crosshairTargetPosition = targetPosition;
            targetWorldPos = tileGrid.GetWorldPosition(targetPosition) + new Vector3(0.5f, 0.5f, 0);
        }

        playerShoot?.TriggerSkillAnimation(animDuration);
        StartCoroutine(DropProjectileAndCrackTiles(crosshairTargetPosition, targetWorldPos));
    }

    private IEnumerator DropProjectileAndCrackTiles(Vector2Int targetPosition, Vector3 targetWorldPos)
    {
        Vector3 projectileStartPos = targetWorldPos + new Vector3(0, projectileDropHeight, 0);
        GameObject projectile = null;

        if (skillProjectilePrefab != null)
        {
            projectile = Instantiate(skillProjectilePrefab, projectileStartPos, Quaternion.identity);

            float startTime = Time.time;
            float journeyLength = projectileDropHeight;

            while (projectile != null && Time.time - startTime < journeyLength / projectileDropSpeed)
            {
                float distanceCovered = (Time.time - startTime) * projectileDropSpeed;
                float fractionOfJourney = distanceCovered / journeyLength;

                Vector3 newPosition = Vector3.Lerp(projectileStartPos, targetWorldPos, fractionOfJourney);
                if (projectile != null)
                    projectile.transform.position = newPosition;

                yield return null;
            }

            if (projectile != null)
                projectile.transform.position = targetWorldPos;

            AudioManager.Instance?.PlayPulseFallSFX();

            if (impactEffectPrefab != null)
            {
                GameObject impactEffect = Instantiate(impactEffectPrefab, targetWorldPos, Quaternion.identity);
                Destroy(impactEffect, 2f);
            }

            if (projectile != null)
                Destroy(projectile, 0.1f);
        }

        yield return new WaitForSeconds(impactDelay);

        for (int x = targetPosition.x - horizontalRange; x <= targetPosition.x + horizontalRange; x++)
        {
            Vector2Int currentPos = new Vector2Int(x, targetPosition.y);

            if (tileGrid.IsValidGridPosition(currentPos))
            {
                StartCoroutine(DelayedTileEffect(currentPos, Mathf.Abs(x - targetPosition.x) * 0.1f));
            }
        }
    }

    private IEnumerator DelayedTileEffect(Vector2Int position, float delay)
    {
        yield return new WaitForSeconds(delay);

        //DealDamageAtPosition(position);
        CrackTile(position);
    }

    //private void DealDamageAtPosition(Vector2Int position)
    //{
    //    Enemy[] enemies = FindObjectsOfType<Enemy>();

    //    foreach (Enemy enemy in enemies)
    //    {
    //        Vector3 enemyAdjusted = enemy.transform.position - new Vector3(0, tileGrid.GetTileHeight() * 0.5f, 0);
    //        Vector2Int enemyGridPos = tileGrid.GetGridPosition(enemyAdjusted);

    //        if (enemyGridPos == position)
    //        {
    //            DealDamageToEnemy(enemy, damageAmount);
    //        }
    //    }
    //}

    private void CrackTile(Vector2Int position)
    {
        if (tileGrid == null) return;

        tileGrid.CrackTile(position);

        Vector3 worldPos = tileGrid.GetWorldPosition(position) + new Vector3(0.5f, 0.5f, 0);
        if (crackedTileEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(crackedTileEffectPrefab, worldPos, Quaternion.identity);
            effectInstance.transform.SetParent(transform);
        }

        if (!crackedTilePositions.Contains(position))
            crackedTilePositions.Add(position);
    }
}
