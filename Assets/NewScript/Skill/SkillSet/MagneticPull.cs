//using UnityEngine;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;

//namespace SkillSystem
//{
//    public class MagneticPull : Skill
//    {
//        [SerializeField] private float pushDuration = 0.3f;
//        [SerializeField] private bool preserveExactYPosition = true;
//        [SerializeField] public float cooldownDuration = 2.0f;
//        [SerializeField] public float manaCost = 2.0f;
//        [SerializeField] private float maxWaitTime = 1.0f;
//        [SerializeField] private float animDuration = 0.5f;

//        private PlayerStats playerStats;
//        private PlayerShoot playerShoot;
//        private TileGrid tileGrid;

//        private void Awake()
//        {
//            playerStats = FindObjectOfType<PlayerStats>();
//            playerShoot = FindObjectOfType<PlayerShoot>();
//            tileGrid = FindObjectOfType<TileGrid>();

//            if (playerStats == null)
//                Debug.LogError("MagneticPull: Could not find PlayerStats!");
//            if (tileGrid == null)
//                Debug.LogError("MagneticPull: Could not find TileGrid!");
//        }

//        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
//        {
//            if (playerStats != null && !playerStats.TryUseMana(manaCost))
//            {
//                return;
//            }


//            Enemy.ClearAllReservations();
//            playerShoot?.TriggerSkillAnimation(animDuration);
//            AudioManager.Instance?.PlayMagneticPullSFX();

//            StartCoroutine(ExecutePullSequence());
//        }

//        private IEnumerator ExecutePullSequence()
//        {
//            List<Enemy> allEnemies = FindObjectsOfType<Enemy>().ToList();

//            foreach (Enemy enemy in allEnemies)
//                enemy.InterruptMovementForSkill();

//            yield return StartCoroutine(WaitForAllEnemiesToStop(allEnemies));
//            ExecutePullLogic();
//        }

//        private IEnumerator WaitForAllEnemiesToStop(List<Enemy> enemies)
//        {
//            float waitTime = 0f;
//            while (waitTime < maxWaitTime)
//            {
//                if (enemies.All(e => e == null || !e.IsMoving()))
//                    break;

//                waitTime += Time.deltaTime;
//                yield return null;
//            }
//        }

//        private void ExecutePullLogic()
//        {
//            List<PullCandidate> pullCandidates = new List<PullCandidate>();

//            for (int y = 0; y < tileGrid.gridHeight; y++)
//            {
//                for (int x = 0; x < tileGrid.gridWidth; x++)
//                {
//                    Vector2Int gridPos = new Vector2Int(x, y);
//                    if (tileGrid.grid[x, y] == TileType.Enemy)
//                        FindPullCandidates(gridPos, tileGrid, pullCandidates);
//                }
//            }

//            pullCandidates.Sort((a, b) => b.currentPosition.x.CompareTo(a.currentPosition.x));

//            Dictionary<Vector2Int, PullCandidate> map = pullCandidates.ToDictionary(c => c.currentPosition, c => c);
//            HashSet<Vector2Int> reservedTargets = new HashSet<Vector2Int>();
//            List<PullCandidate> validPulls = new List<PullCandidate>();

//            foreach (var candidate in pullCandidates)
//            {
//                List<PullCandidate> chain = new();
//                if (ResolvePullChain(candidate, map, reservedTargets, chain))
//                {
//                    foreach (var resolved in chain)
//                    {
//                        if (reservedTargets.Add(resolved.targetPosition))
//                            validPulls.Add(resolved);
//                    }
//                }
//            }

//            foreach (var pull in validPulls)
//                pull.enemy.PrepareForPull(pull.targetPosition);

//            foreach (var pull in validPulls)
//                StartCoroutine(PullAnimation(pull.enemy, pull.currentPosition, pull.targetPosition));
//        }

//        private bool ResolvePullChain(PullCandidate current, Dictionary<Vector2Int, PullCandidate> map, HashSet<Vector2Int> reserved, List<PullCandidate> chain)
//        {
//            if (reserved.Contains(current.targetPosition)) return false;

//            if (map.TryGetValue(current.targetPosition, out var blocker))
//                if (!ResolvePullChain(blocker, map, reserved, chain))
//                    return false;

//            if (!IsPositionAvailableForPull(current.targetPosition, current.enemy))
//                return false;

//            chain.Add(current);
//            return true;
//        }

//        private void FindPullCandidates(Vector2Int gridPos, TileGrid tileGrid, List<PullCandidate> candidates)
//        {
//            Vector2Int targetPos = gridPos + Vector2Int.left;

//            if (!tileGrid.IsValidGridPosition(targetPos)) return;

//            foreach (Enemy enemy in FindObjectsOfType<Enemy>())
//            {
//                if (enemy == null || enemy.IsBeingPulled()) continue;

//                Vector2Int enemyPos = tileGrid.GetGridPosition(enemy.transform.position);
//                if (enemyPos != gridPos) continue;

//                if (!IsPositionAvailableForPull(targetPos, enemy)) continue;

//                candidates.Add(new PullCandidate
//                {
//                    enemy = enemy,
//                    currentPosition = gridPos,
//                    targetPosition = targetPos
//                });
//            }
//        }

//        private bool IsPositionAvailableForPull(Vector2Int targetPos, Enemy excludeEnemy)
//        {
//            if (!tileGrid.IsValidGridPosition(targetPos)) return false;

//            TileType type = tileGrid.grid[targetPos.x, targetPos.y];
//            if (type == TileType.Broken || type == TileType.EnemyBroken || type == TileType.PlayerBroken) return false;
//            if (type == TileType.Player || type == TileType.PlayerCracked) return false;
//            if (tileGrid.IsTileOccupied(targetPos)) return false;

//            Vector3 worldPos = tileGrid.GetWorldPosition(targetPos);
//            Collider2D[] overlaps = Physics2D.OverlapCircleAll(worldPos, 0.3f);

//            foreach (Collider2D overlap in overlaps)
//            {
//                if (overlap.CompareTag("Enemy") && overlap.gameObject != excludeEnemy?.gameObject)
//                {
//                    Enemy e = overlap.GetComponent<Enemy>();
//                    if (e != null && !e.IsBeingPulled()) return false;
//                }
//            }

//            return true;
//        }

//        private IEnumerator PullAnimation(Enemy enemy, Vector2Int startGridPos, Vector2Int targetGridPos)
//        {
//            if (enemy == null) yield break;

//            Vector3 start = enemy.transform.position;
//            Vector3 target = tileGrid.GetCenteredWorldPosition(targetGridPos);

//            if (preserveExactYPosition)
//                target.y = start.y;

//            float elapsed = 0f;
//            while (elapsed < pushDuration)
//            {
//                if (enemy == null) yield break;

//                float t = Mathf.SmoothStep(0, 1, elapsed / pushDuration);
//                enemy.transform.position = Vector3.Lerp(start, target, t);
//                elapsed += Time.deltaTime;
//                yield return null;
//            }

//            if (enemy != null)
//            {
//                enemy.transform.position = target;
//                enemy.CompletePull(targetGridPos, target);
//            }
//        }

//        private struct PullCandidate
//        {
//            public Enemy enemy;
//            public Vector2Int currentPosition;
//            public Vector2Int targetPosition;
//        }
//    }
//}
