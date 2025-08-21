using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SkillSystem
{
    public class IonBolt : Skill
    {
        [Header("IonBolt Skill Settings")]
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private int explosionTileRadius = 1;
        [SerializeField] private int damage = 20;
        [SerializeField] private int explosionDamage = 15;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject explosionEffectPrefab;
        [SerializeField] private float explosionEffectDuration = 1.0f;
        [SerializeField] public float manaCost = 1.5f;
        [SerializeField] public float cooldownDuration = 1.5f;

        [Header("Manual Speed Adjustment")]
        [SerializeField] private float speedMultiplier = 1.0f;

        [SerializeField] private float skillAnimationDuration = 0.8f;

        private GameObject activeProjectile;
        private bool isProjectileFired = false;
        private bool isOnCooldown = false;
        private Vector2Int currentGridPosition;

        private TileGrid tileGrid;
        private PlayerMovement playerMovement;
        private PlayerStats playerStats;
        private PlayerShoot playerShoot;

        private ComboTracker comboTracker;
        private Animator playerAnimator;

        private bool isSkillAnimationActive = false;

        private void Awake()
        {
            FindReferences();
        }

        private void FindReferences()
        {
            if (tileGrid == null) tileGrid = FindObjectOfType<TileGrid>();
            if (playerMovement == null) playerMovement = FindObjectOfType<PlayerMovement>();
            if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();
            if (playerShoot == null) playerShoot = FindObjectOfType<PlayerShoot>();

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (comboTracker == null) comboTracker = player.GetComponent<ComboTracker>();
                if (playerAnimator == null) playerAnimator = player.GetComponent<Animator>();
            }

        }

        private void Update()
        {
            if (isProjectileFired && activeProjectile != null)
            {
                float adjustedSpeed = projectileSpeed * speedMultiplier;
                activeProjectile.transform.Translate(Vector3.right * adjustedSpeed * Time.deltaTime, Space.World);

                Vector2Int newGridPosition = currentGridPosition;
                newGridPosition.x = Mathf.RoundToInt(tileGrid.GetGridPosition(activeProjectile.transform.position).x);

                if (newGridPosition.x > currentGridPosition.x)
                {
                    currentGridPosition.x = newGridPosition.x;
                    
                    if (CheckForEnemyOnTile(currentGridPosition))
                    {
                        HandleHitAtTile(currentGridPosition);
                    }
                    
                    CheckIfPastRightmostGrid();
                }
            }

            if (isSkillAnimationActive && playerAnimator != null)
            {
                playerAnimator.SetInteger("ComboIndex", comboTracker.GetCurrentComboIndex());
            }
        }

        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            FindReferences();

            if (isOnCooldown)
            {
                return;
            }

            if (playerStats.TryUseMana(Mathf.CeilToInt(manaCost)))
            {
                StartCoroutine(ExecuteSkillFlow());
            }
        }

        private IEnumerator ExecuteSkillFlow()
        {
            if (playerShoot != null)
            {
                playerShoot.TriggerSkillAnimation(skillAnimationDuration);
            }

            yield return new WaitForSeconds(0.1f);

            FireProjectile();
            StartCoroutine(StartCooldown());
        }

        
        private IEnumerator StartCooldown()
        {
            isOnCooldown = true;
            yield return new WaitForSeconds(cooldownDuration);
            isOnCooldown = false;
        }

        private void FireProjectile()
        {
            Vector3 spawnPos = playerShoot != null && playerShoot.GetBulletSpawnPoint() != null 
                ? playerShoot.GetBulletSpawnPoint().position 
                : playerMovement.transform.position;

            Vector2Int playerGridPos = playerMovement.GetCurrentGridPosition();
            currentGridPosition = new Vector2Int(playerGridPos.x, playerGridPos.y);

            activeProjectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            AudioManager.Instance?.PlayIonBoltSFX();

            isProjectileFired = true;
        }

        private bool CheckForEnemyOnTile(Vector2Int gridPos)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

            foreach (GameObject enemy in enemies)
            {
                Vector3 enemyAdjusted = enemy.transform.position - new Vector3(0, tileGrid.GetTileHeight() * 0.5f, 0);
                Vector2Int enemyGridPos = tileGrid.GetGridPosition(enemyAdjusted);

                if (enemyGridPos == gridPos)
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleHitAtTile(Vector2Int gridPosition)
        {
            DamageEnemiesOnTile(gridPosition);
            ExplodeAtGridPosition(gridPosition);

            if (comboTracker != null)
            {
                comboTracker.TriggerCombo();
                if (playerAnimator != null)
                {
                    playerAnimator.SetInteger("ComboIndex", comboTracker.GetCurrentComboIndex());
                }
            }

            Destroy(activeProjectile);
            isProjectileFired = false;
        }

        private void DamageEnemiesOnTile(Vector2Int gridPos)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

            foreach (GameObject enemy in enemies)
            {
                Vector3 enemyAdjusted = enemy.transform.position - new Vector3(0, tileGrid.GetTileHeight() * 0.5f, 0);
                Vector2Int enemyGridPos = tileGrid.GetGridPosition(enemyAdjusted);

                //if (enemyGridPos == gridPos)
                //{
                //    Enemy enemyComponent = enemy.GetComponent<Enemy>();
                //    if (enemyComponent != null)
                //    {
                //        DealDamageToEnemy(enemyComponent, damage);
                //    }
                //}
            }
        }

        private void ExplodeAtGridPosition(Vector2Int centerGridPosition)
        {
            Vector3 worldPosition = tileGrid.GetWorldPosition(centerGridPosition);

            if (explosionEffectPrefab != null)
            {
                GameObject explosionEffect = Instantiate(explosionEffectPrefab, worldPosition, Quaternion.identity);
                Destroy(explosionEffect, explosionEffectDuration);
            }

            List<Vector2Int> affectedTiles = GetAffectedTiles(centerGridPosition, explosionTileRadius);

            foreach (Vector2Int tilePos in affectedTiles)
            {
                if (tilePos == centerGridPosition) continue;

                DamageEnemiesOnExplosionTile(tilePos);
            }
        }

        private void DamageEnemiesOnExplosionTile(Vector2Int gridPos)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

            foreach (GameObject enemy in enemies)
            {
                Vector3 enemyAdjusted = enemy.transform.position - new Vector3(0, tileGrid.GetTileHeight() * 0.5f, 0);
                Vector2Int enemyGridPos = tileGrid.GetGridPosition(enemyAdjusted);

                //if (enemyGridPos == gridPos)
                //{
                //    Enemy enemyComponent = enemy.GetComponent<Enemy>();
                //    if (enemyComponent != null)
                //    {
                //        DealDamageToEnemy(enemyComponent, explosionDamage);
                //    }
                //}
            }
        }

        private void CheckIfPastRightmostGrid()
        {
            if (currentGridPosition.x >= tileGrid.gridWidth || currentGridPosition.x > tileGrid.gridWidth - 1)
            {
                Destroy(activeProjectile);
                isProjectileFired = false;
            }
        }

        private List<Vector2Int> GetAffectedTiles(Vector2Int centerPos, int radius)
        {
            List<Vector2Int> affectedTiles = new List<Vector2Int>();

            for (int xOffset = -radius; xOffset <= radius; xOffset++)
            {
                for (int yOffset = -radius; yOffset <= radius; yOffset++)
                {
                    Vector2Int tilePos = new Vector2Int(centerPos.x + xOffset, centerPos.y + yOffset);

                    if (tileGrid.IsValidGridPosition(tilePos))
                    {
                        affectedTiles.Add(tilePos);
                    }
                }
            }
            return affectedTiles;
        }
    }
}