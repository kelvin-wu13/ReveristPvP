using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkillSystem
{
    public class PlasmaSurge : Skill
    {
        [Header("Plasma Surge Settings")]
        [SerializeField] private int damage = 25;
        [SerializeField] private float animDuration = 1f;


        [Header("Cooldown")]
        [SerializeField] public float cooldownDuration = 2f;
        [SerializeField] public float manaCost = 1.5f;

        private PlayerShoot playerShoot;
        private TileGrid tileGrid;
        private Transform player;
        private PlayerMovement playerMovement;

        public override void Initialize(Vector2Int targetPosition, SkillCombination combo, Transform playerTransform)
        {
            base.Initialize(targetPosition, combo, playerTransform);
            player = playerTransform;
            playerShoot = player.GetComponent<PlayerShoot>();
            playerMovement = player.GetComponent<PlayerMovement>();
            tileGrid = FindObjectOfType<TileGrid>();

            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats != null && !stats.TryUseMana(manaCost))
            {
                Destroy(gameObject);
                return;
            }

            playerShoot?.TriggerSkillAnimation(animDuration);

            transform.position = GetFirepointPosition();

            Animator anim = GetComponent<Animator>();
            if (anim != null)
                anim.Play("LaserAnimation", 0, 0f);

            StartCoroutine(ExecutePlasmaSurge());
            //AudioManager.Instance?.PlayPlasmaSurgeSFX();
        }

        private Vector3 GetFirepointPosition()
        {
            if (playerShoot != null)
            {
                Transform bulletSpawnPoint = playerShoot.GetBulletSpawnPoint();
                if (bulletSpawnPoint != null)
                    return bulletSpawnPoint.position;
            }

            Transform firepoint = player.Find("FirePoint") ??
                                  player.Find("BulletSpawnPoint") ??
                                  player.Find("Firepoint") ??
                                  player.Find("SpawnPoint");

            return firepoint != null ? firepoint.position : player.position + Vector3.right * 0.5f;
        }

        private IEnumerator ExecutePlasmaSurge()
        {
            Vector2Int playerGridPos = playerMovement != null
                ? playerMovement.GetCurrentGridPosition()
                : tileGrid.GetGridPosition(player.position);

            Vector3 laserStartPos = GetTileCenter(playerGridPos);
            Vector3 laserEndPos = CalculateLaserEndPosition(laserStartPos, playerGridPos);

            DamageEnemiesInLaserPath(playerGridPos);
            yield return new WaitForSeconds(animDuration);
            Destroy(gameObject);
        }

        private Vector3 CalculateLaserEndPosition(Vector3 startPos, Vector2Int playerGridPos)
        {
            int rightmostColumn = tileGrid.gridWidth - 1;
            Vector2Int endGridPos = new Vector2Int(rightmostColumn, playerGridPos.y);
            Vector3 endWorldPos = tileGrid.GetWorldPosition(endGridPos);
            return new Vector3(endWorldPos.x + 1f, startPos.y, startPos.z);
        }

        private void DamageEnemiesInLaserPath(Vector2Int playerGridPos)
        {
            for (int x = playerGridPos.x + 1; x < tileGrid.gridWidth; x++)
            {
                Vector2Int checkPos = new Vector2Int(x, playerGridPos.y);
                if (tileGrid.IsValidGridPosition(checkPos) && IsEnemyTilePosition(checkPos))
                {
                    DamageEnemiesOnTile(checkPos);
                }
            }
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

        private Vector3 GetTileCenter(Vector2Int gridPos)
        {
            Vector3 basePos = tileGrid.GetWorldPosition(gridPos);
            return basePos + new Vector3(tileGrid.GetTileWidth(), tileGrid.GetTileHeight()) * 0.5f;
        }

        private bool IsEnemyTilePosition(Vector2Int gridPosition)
        {
            return tileGrid.IsValidGridPosition(gridPosition) && 
                   gridPosition.x >= tileGrid.gridWidth / 2;
        }
    }
}