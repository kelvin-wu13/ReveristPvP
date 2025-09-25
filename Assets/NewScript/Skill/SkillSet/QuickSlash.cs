using UnityEngine;
using System.Collections.Generic;

namespace SkillSystem
{
    public class QuickSlash : Skill
    {
        [SerializeField] private int damageAmount = 10;
        [SerializeField] public float manaCost = 2f;
        [SerializeField] public float cooldownDuration = 2f;

        private TileGrid tileGrid;
        private PlayerStats playerStats;
        private PlayerCrosshair playerCrosshair;

        private void Awake()
        {
            tileGrid = FindObjectOfType<TileGrid>();
            playerStats = FindObjectOfType<PlayerStats>();
            playerCrosshair = FindObjectOfType<PlayerCrosshair>();
        }

        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            if (tileGrid == null || playerStats == null) return;
            if (!playerStats.TryUseMana(manaCost)) return;

            //AudioManager.Instance?.PlayQuickSlashSFX();

            // Adjust Y position based on PlayerMovement's Position Offset
            Vector3 adjustedPos = casterTransform.position;
            adjustedPos.y -= 1.6f;
            adjustedPos.y += 0.1f; // slight upward bias to round into correct row
            Vector2Int playerGridPos = tileGrid.GetGridPosition(adjustedPos);

            int frontX = playerGridPos.x + 1;

            Debug.Log($"[QuickSlash] Player World Y: {casterTransform.position.y}");
            Debug.Log($"[QuickSlash] Adjusted World Y: {adjustedPos.y}");
            Debug.Log($"[QuickSlash] Player Grid Pos: {playerGridPos}");

            List<Vector2Int> damageGridPositions = new List<Vector2Int>
            {
                new Vector2Int(frontX, playerGridPos.y + 1), // front-up
                new Vector2Int(frontX, playerGridPos.y),     // front-mid
                new Vector2Int(frontX, playerGridPos.y - 1)  // front-down
            };

            List<Vector2Int> validPositions = new List<Vector2Int>();

            foreach (Vector2Int pos in damageGridPositions)
            {
                bool isValid = tileGrid.IsValidGridPosition(pos);
                Debug.Log($"[QuickSlash] Checking tile: {pos} | Valid: {isValid}");
                if (isValid)
                {
                    validPositions.Add(pos);
                    Vector3 worldPos = tileGrid.GetWorldPosition(pos);
                    Debug.DrawLine(worldPos + Vector3.up * 0.5f, worldPos - Vector3.up * 0.5f, Color.green, 1f);
                    Debug.DrawLine(worldPos + Vector3.left * 0.5f, worldPos + Vector3.right * 0.5f, Color.green, 1f);
                }
            }

            DamageEnemiesOnTiles(validPositions);
            base.ExecuteSkillEffect(targetPosition, casterTransform);
            ResetMeleeAnimation();
        }

        private void DamageEnemiesOnTiles(List<Vector2Int> gridPositions)
        {
            //Enemy[] allEnemies = FindObjectsOfType<Enemy>();

            foreach (Vector2Int gridPos in gridPositions)
            {
                Debug.Log($"[QuickSlash] Target Grid: {gridPos}");

                //foreach (Enemy enemy in allEnemies)
                //{
                //    if (enemy == null) continue;

                //    Vector2Int enemyGridPos = enemy.GetCurrentGridPosition();

                //    Debug.Log($"    Enemy {enemy.name} GridPos: {enemyGridPos}");

                //    if (enemyGridPos == gridPos)
                //    {
                //        enemy.TakeDamage(damageAmount);
                //        Debug.Log($"    >>> HIT {enemy.name} at {enemyGridPos}");
                //        ShowDamageEffect(gridPos);
                //    }
                //}
            }
        }

        private void ShowDamageEffect(Vector2Int gridPos)
        {
            Vector3 worldPos = tileGrid.GetWorldPosition(gridPos);
            Vector3 effectPos = worldPos + new Vector3(tileGrid.GetTileWidth() * 0.5f, tileGrid.GetTileHeight() * 0.5f, -0.5f);

            Debug.DrawLine(effectPos + Vector3.up * 0.5f, effectPos - Vector3.up * 0.5f, Color.red, 1f);
            Debug.DrawLine(effectPos + Vector3.left * 0.5f, effectPos + Vector3.right * 0.5f, Color.red, 1f);
        }

        private void ResetMeleeAnimation()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Animator animator = player.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.ResetTrigger("QuickSlash");
                    animator.Play("PlayerIdle");
                }
            }
        }
    }
}
