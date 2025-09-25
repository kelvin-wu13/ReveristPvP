using System.Collections;
using UnityEngine;

namespace SkillSystem
{
    public class SwiftStrike : Skill, ISkillOwnerReceiver, ISkillDirectionReceiver
    {
        [Header("Skill Properties")]
        [SerializeField] public float cooldownDuration = 3f;
        [SerializeField] public float manaCost = 15f;

        [Header("Dash Settings")]
        [SerializeField] private float dashDuration = 0.25f;
        [SerializeField] private AnimationCurve dashCurve;
        [SerializeField] private bool lockRotationDuringDash = true;
        [SerializeField] private bool allowCrossMidline = true;     

        [Header("Damage)")]
        [SerializeField] private int damageAmount = 10;

        [Header("Return Settings")]
        [SerializeField] private float returnDelay = 0.35f;
        [SerializeField] private float returnDuration = 0.35f;
        [SerializeField] private AnimationCurve returnCurve;

        [Header("Visual Setting")]
        [SerializeField] private Transform fxRoot;
        [SerializeField] private float spawnAheadX = 2f;
        private Transform _fxParentBackup;
        private bool _dirSet = false;

        private Quaternion _fxBaseLocalRot = Quaternion.identity;
        private Vector3 _fxBaseLocalScale = Vector3.one;

        private GameObject owner;
        private PlayerMovement ownerMove;
        private PlayerStats ownerStats;
        private PlayerCrosshair crosshair;
        private TileGrid grid;
        private Transform playerTf;
        private Animator ownerAnim;

        private Rigidbody2D rb2d; private Vector2 rb2dVel; private float rb2dGrav; private bool rb2dKinematic;
        private Rigidbody rb3d; private Vector3 rb3dVel; private bool rb3dUseGrav; private bool rb3dKinematic;

        private Quaternion initialRotation;
        private Vector2Int originalGridPos;
        private Vector3 originalWorldPos;
        private bool wasPMEnabled = true;
        private bool prevRootMotion = false;
        private int forwardX = +1;

        private void Awake()
        {
            if (fxRoot)
            {
                _fxBaseLocalRot = fxRoot.localRotation;
                _fxBaseLocalScale = fxRoot.localScale;
            }
        }

        public void SetOwner(GameObject ownerGO)
        {
            owner = ownerGO;
            if (!owner) return;

            playerTf = owner.transform;
            ownerMove = owner.GetComponent<PlayerMovement>();
            ownerStats = owner.GetComponent<PlayerStats>();
            crosshair = owner.GetComponentInChildren<PlayerCrosshair>(true);
            ownerAnim = owner.GetComponent<Animator>();
            rb2d = owner.GetComponent<Rigidbody2D>();
            rb3d = owner.GetComponent<Rigidbody>();
        }

        public void SetDirection(Vector2 dir)
        {
            forwardX = (dir.x >= 0f) ? +1 : -1;
            _dirSet = true;
            ApplyFlip();
            Debug.Log($"[SwiftStrike] SetDirection forwardX={forwardX}");
        }

        public override void Initialize(Vector2Int tp, SkillCombination st, Transform caster)
        {
            base.Initialize(tp, st, caster);
            if (!owner && caster) SetOwner(caster.gameObject);
            if (!playerTf && owner) playerTf = owner.transform;
            if (!grid) grid = FindObjectOfType<TileGrid>();

            if (playerTf)
            {
                originalWorldPos = playerTf.position;
                initialRotation = playerTf.rotation;
            }
            if (!_dirSet && ownerMove != null)
            {
                var face = ownerMove.GetFacingDirection();
                if (face.x != 0) forwardX = (int)Mathf.Sign(face.x);
            }
            ApplyFlip();
        }

        private void ApplyFlip()
        {
            if (!fxRoot) return;

            var s = _fxBaseLocalScale;
            s.x = Mathf.Abs(s.x) * (forwardX >= 0 ? 1f : -1f);
            fxRoot.localScale = s;

            fxRoot.localRotation = _fxBaseLocalRot;

            Debug.Log($"[SwiftStrike] ApplyFlip forwardX={forwardX}, fxRoot.localScale={fxRoot.localScale}");
        }


        private void Start()
        {
            if (!_dirSet)
            {
                if (!grid) grid = FindObjectOfType<TileGrid>();
                if (ownerMove != null && grid != null)
                {
                    Vector2Int gp = ownerMove.GetCurrentGridPosition();
                    int mid = Mathf.Max(1, grid.gridWidth / 2);
                    forwardX = (gp.x < mid) ? +1 : -1;
                    Debug.Log($"[SwiftStrike] Fallback direction via GridSide => forwardX={forwardX}");
                }
            }

            ApplyFlip();
            if (fxRoot && playerTf)
            {
                _fxParentBackup = fxRoot.parent;
                fxRoot.SetParent(playerTf, worldPositionStays: true);

                fxRoot.localPosition = new Vector3(spawnAheadX * (forwardX >= 0 ? 1f : -1f), 0f, 0f);
            }


            if (!grid) grid = FindObjectOfType<TileGrid>();
            if (!crosshair && owner) crosshair = owner.GetComponentInChildren<PlayerCrosshair>(true);
            if (!playerTf && owner) playerTf = owner.transform;

            if (!grid || !playerTf || ownerMove == null)
            {
                Debug.LogError("[SwiftStrike] Missing refs (TileGrid/PlayerTransform/PlayerMovement).");
                Destroy(gameObject); return;
            }

            originalGridPos = ownerMove.GetCurrentGridPosition();

            if (ownerAnim != null) { prevRootMotion = ownerAnim.applyRootMotion; ownerAnim.applyRootMotion = false; }
            wasPMEnabled = ownerMove.enabled; ownerMove.enabled = false;

            if (rb2d)
            {
                rb2dVel = rb2d.linearVelocity; rb2dGrav = rb2d.gravityScale; rb2dKinematic = rb2d.isKinematic;
                rb2d.linearVelocity = Vector2.zero; rb2d.gravityScale = 0f; rb2d.isKinematic = true;
            }
            if (rb3d)
            {
                rb3dVel = rb3d.linearVelocity; rb3dUseGrav = rb3d.useGravity; rb3dKinematic = rb3d.isKinematic;
                rb3d.linearVelocity = Vector3.zero; rb3d.useGravity = false; rb3d.isKinematic = true;
            }

            if (ownerStats != null && ownerStats.TryUseMana(manaCost))
            {
                crosshair?.FreezeCrosshair();
                StartCoroutine(Co_SwiftStrike());
            }
            else
            {
                Debug.LogWarning("[SwiftStrike] Not enough mana.");
                RestoreControllers();
                Destroy(gameObject);
            }
        }

        public override void ExecuteSkillEffect(Vector2Int tp, Transform casterTransform) { }

        private IEnumerator Co_SwiftStrike()
        {
            Vector2Int aim = crosshair ? crosshair.GetTargetGridPosition() : originalGridPos;
            Vector2Int from = ownerMove.GetCurrentGridPosition();
            Vector2Int desired = new Vector2Int(aim.x - forwardX, aim.y);

            Vector2Int dashPos = allowCrossMidline
                ? ComputeValidDashPathThrough(from, desired, forwardX) 
                : ComputeValidDashStaySide(from, desired, forwardX); 

            Debug.Log($"[SwiftStrike] from={from} aim={aim} desired={desired} dashPos={dashPos} fx={forwardX}");

            float t = 0f;
            Vector3 start = playerTf.position;
            Vector3 end = grid.GetCenteredWorldPosition(dashPos);
            end.y = start.y; end.z = start.z;

            while (t < dashDuration)
            {
                float r = t / dashDuration;
                float k = (dashCurve != null) ? dashCurve.Evaluate(r) : r; 
                Vector3 pos = Vector3.Lerp(start, end, k);
                pos.y = start.y; pos.z = start.z;  
                playerTf.position = pos;
                if (lockRotationDuringDash) playerTf.rotation = initialRotation;
                t += Time.deltaTime;
                yield return null;
            }

            ownerMove.TeleportTo(dashPos);
            if (lockRotationDuringDash) playerTf.rotation = initialRotation;

            DoVertical3Damage(dashPos, aim);

            yield return new WaitForSeconds(returnDelay);

            t = 0f;
            start = playerTf.position;
            end = originalWorldPos;
            end.y = start.y; end.z = start.z;

            while (t < returnDuration)
            {
                float r = t / returnDuration;
                float k = (returnCurve != null) ? returnCurve.Evaluate(r) : r;
                Vector3 pos = Vector3.Lerp(start, end, k);
                pos.y = start.y; pos.z = start.z;
                playerTf.position = pos;
                if (lockRotationDuringDash) playerTf.rotation = initialRotation;
                t += Time.deltaTime;
                yield return null;
            }
            ownerMove.TeleportTo(originalGridPos);

            if (fxRoot) fxRoot.SetParent(_fxParentBackup, worldPositionStays: true);

            float ttl = (LifetimeSeconds > 0f) ? LifetimeSeconds : 0.01f;
            Destroy(gameObject, ttl);

            crosshair?.UnfreezeCrosshair();
            RestoreControllers();

        }

        private void RestoreControllers()
        {
            if (ownerAnim != null) ownerAnim.applyRootMotion = prevRootMotion;
            if (ownerMove != null) ownerMove.enabled = wasPMEnabled;

            if (rb2d)
            {
                rb2d.isKinematic = rb2dKinematic; rb2d.gravityScale = rb2dGrav; rb2d.linearVelocity = rb2dVel;
            }
            if (rb3d)
            {
                rb3d.isKinematic = rb3dKinematic; rb3d.useGravity = rb3dUseGrav; rb3d.linearVelocity = rb3dVel;
            }
        }

        private Vector2Int ComputeValidDashStaySide(Vector2Int from, Vector2Int desired, int fx)
        {
            int mid = Mathf.Max(1, grid.gridWidth / 2);
            if (from.x < mid) desired.x = Mathf.Clamp(desired.x, 0, mid - 1);
            else desired.x = Mathf.Clamp(desired.x, mid, grid.gridWidth - 1);

            if (IsWalkable(desired) && ((fx > 0 && desired.x > from.x) || (fx < 0 && desired.x < from.x)))
                return desired;

            var fb = new Vector2Int(from.x + fx, desired.y);
            if (IsWalkable(fb)) return fb;
            return from;
        }

        private Vector2Int ComputeValidDashPathThrough(Vector2Int from, Vector2Int desired, int fx)
        {
            int step = Mathf.Clamp(fx, -1, 1);
            if (step == 0) step = (desired.x > from.x) ? +1 : -1;

            Vector2Int best = from;
            int x = from.x + step;
            int last = desired.x;

            while ((step > 0 && x <= last) || (step < 0 && x >= last))
            {
                var p = new Vector2Int(x, desired.y);
                if (!IsWalkable(p)) break;
                best = p;
                x += step;
            }
            return best;
        }

        private bool IsWalkable(Vector2Int cell)
        {
            if (!grid.IsValidGridPosition(cell)) return false;
            var tt = grid.grid[cell.x, cell.y];
            return tt != TileType.Broken && tt != TileType.Player1Broken && tt != TileType.Player2Broken;
        }

        private void DoVertical3Damage(Vector2Int attackerPosAfterDash, Vector2Int aimCell)
        {
            if (!grid) return;

            int col = aimCell.x;
            for (int dy = -1; dy <= 1; dy++)
            {
                var cell = new Vector2Int(col, aimCell.y + dy);
                if (!grid.IsValidGridPosition(cell)) continue;

                var victim = FindVictimAt(cell);
                if (!victim) continue;

                var parry = victim.GetComponent<ParryController>()
                  ?? victim.GetComponentInChildren<ParryController>(true)
                  ?? victim.GetComponentInParent<ParryController>();

                if (parry != null && parry.IsParryActive)
                {
                    parry.TryParryNonProjectileSuccess();         // refund -> net -1
                    Debug.Log($"[SwiftStrike] NEGATED by Parry @ {cell} (no damage).");
                    continue;                                     // <-- penting: jangan lanjut damage
                }

                var vStat = victim.GetComponent<PlayerStats>();
                if (!vStat) continue;


                int mid = Mathf.Max(1, grid.gridWidth / 2);
                var attackerSide = (attackerPosAfterDash.x < mid) ? TileGrid.Side.Left : TileGrid.Side.Right;

                int finalDamage = damageAmount;
                try
                {
                    finalDamage = GridDamageCalculator.Calculate(new GridDamageCalculator.Ctx
                    {
                        grid = grid,
                        attackerSide = attackerSide,
                        attackerPos = attackerPosAfterDash,
                        defenderPos = cell,
                        baseDamage = damageAmount
                    });
                }
                catch { /* gunakan baseDamage jika helper belum ada */ }

                vStat.TakeDamage(finalDamage, owner);
                HitEvents.NotifyOwnerHit(owner, victim.gameObject, true, "SwiftStrike");
                Debug.Log($"[SwiftStrike] HIT {victim.name} @ {cell} for {finalDamage} (base {damageAmount})");
            }
        }

        private PlayerMovement FindVictimAt(Vector2Int cell)
        {
            var movers = FindObjectsOfType<PlayerMovement>();
            foreach (var p in movers)
            {
                if (!p || (owner && p.gameObject == owner)) continue;
                if (p.GetCurrentGridPosition() == cell) return p;
            }
            return null;
        }
    }
}
