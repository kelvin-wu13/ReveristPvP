using UnityEngine;
using System.Collections;

namespace SkillSystem
{
    public class EEBullet : Skill
    {
        [Header("Bullet Properties")]
        [SerializeField] private float stunDuration = 3f;
        [SerializeField] private string targetTag = "Enemy";

        [Header("Visual Effects")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float fadeOutTime = 0.2f;

        private Vector2 direction;
        private float speed;
        private int bulletDamage;
        private TileGrid tileGrid;
        private Vector2Int currentGridPosition;
        private bool isDestroying = false;


        public void Initialize(Vector2 dir, float spd, int dmg, float stun, TileGrid grid, Vector2Int spawnGridPos, SkillCombination skillCombo)
        {
            base.Initialize(spawnGridPos, skillCombo, null);

            direction = dir.normalized;
            speed = spd;
            bulletDamage = dmg;
            stunDuration = stun;
            tileGrid = grid;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            currentGridPosition = spawnGridPos;
        }

        private void Update()
        {
            if (isDestroying) return;

            transform.Translate(direction * speed * Time.deltaTime, Space.World);

            Vector2Int newGridPosition = currentGridPosition;
            newGridPosition.x = Mathf.RoundToInt(tileGrid.GetGridPosition(transform.position).x);

            if (newGridPosition.x > currentGridPosition.x)
            {
                currentGridPosition.x = newGridPosition.x;

                //if (CheckForEnemyOnTile(currentGridPosition))
                //{
                //    CheckForEnemyHit(currentGridPosition);
                //}

                CheckIfPastRightmostGrid();
            }
        }

        //private bool CheckForEnemyOnTile(Vector2Int gridPosition)
        //{
        //    if (!IsEnemyTilePosition(gridPosition)) return false;

        //    GameObject[] enemies = GameObject.FindGameObjectsWithTag(targetTag);
        //    foreach (GameObject enemy in enemies)
        //    {
        //        Vector3 enemyAdjusted = enemy.transform.position - new Vector3(0, tileGrid.GetTileHeight() * 0.5f, 0);
        //        Vector2Int enemyGridPos = tileGrid.GetGridPosition(enemyAdjusted);

        //        if (enemyGridPos == gridPosition)
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        //private void CheckForEnemyHit(Vector2Int gridPosition)
        //{
        //    GameObject[] enemies = GameObject.FindGameObjectsWithTag(targetTag);
        //    foreach (GameObject enemy in enemies)
        //    {
        //        Vector3 enemyAdjusted = enemy.transform.position - new Vector3(0, tileGrid.GetTileHeight() * 0.5f, 0);
        //        Vector2Int enemyGridPos = tileGrid.GetGridPosition(enemyAdjusted);

        //        if (enemyGridPos == gridPosition)
        //        {
        //            Enemy enemyComponent = enemy.GetComponent<Enemy>();
        //            if (enemyComponent != null)
        //            {
        //                DealDamageToEnemy(enemyComponent, bulletDamage);
        //                enemyComponent.Stun(stunDuration);

        //                SpawnHitEffect(transform.position);
        //                DestroyBullet();
        //                break;
        //            }
        //        }
        //    }
        //}

        private void SpawnHitEffect(Vector3 pos)
        {
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, pos, Quaternion.identity);
            }
            else if (hitEffect != null)
            {
                Instantiate(hitEffect, pos, Quaternion.identity);
            }
        }

        private void CheckIfPastRightmostGrid()
        {
            if (currentGridPosition.x >= tileGrid.gridWidth || currentGridPosition.x > tileGrid.gridWidth - 1)
            {
                DestroyBullet();
            }
        }

        private bool IsEnemyTilePosition(Vector2Int gridPosition)
        {
            return tileGrid.IsValidGridPosition(gridPosition) && gridPosition.x >= tileGrid.gridWidth / 2;
        }

        private void DestroyBullet()
        {
            if (isDestroying) return;
            isDestroying = true;

            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            StartCoroutine(FadeOutAndDestroy());
        }

        private IEnumerator FadeOutAndDestroy()
        {
            float startAlpha = 1f;
            float elapsedTime = 0;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();

            while (elapsedTime < fadeOutTime)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutTime);
                if (sr != null)
                {
                    Color color = sr.color;
                    color.a = alpha;
                    sr.color = color;
                }
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}