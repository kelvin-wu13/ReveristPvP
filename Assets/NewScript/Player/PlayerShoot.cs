using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShoot : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterStats characterData;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private TileGrid tileGrid;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private bool shootRight = true;

    private readonly int isShootingParam = Animator.StringToHash("IsShooting");
    private readonly int comboIndexParam = Animator.StringToHash("ComboIndex");
    [SerializeField] private float WaitTime = 1f;

    private PlayerInput playerInput;
    private InputAction fire;

    private float Time_elapsed;
    private float lastShootTime;
    private bool isSkillAnimating = false;
    private float skillAnimationEndTime = 0f;

    private ComboTracker comboTracker;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        fire = playerInput != null ? playerInput.actions.FindAction("Fire", false) : null;

        Debug.Log($"[Shoot] {name} P{playerInput?.playerIndex + 1} fireFound={(fire != null)} scheme={playerInput?.currentControlScheme}");
        if (fire != null)
        {
            Debug.Log($"[Shoot] actionMap={fire.actionMap?.name} enabled={fire.enabled}");
            for (int i = 0; i < fire.bindings.Count; i++)
            {
                var b = fire.bindings[i];
                Debug.Log($"[Shoot] binding[{i}] path={b.path} groups={b.groups}");
            }
        }

        if (bulletSpawnPoint == null)
        {
            bulletSpawnPoint = transform.Find("FirePoint");
            if (bulletSpawnPoint == null) bulletSpawnPoint = transform;
        }
        if (tileGrid == null) tileGrid = FindObjectOfType<TileGrid>();
        if (animator == null) animator = GetComponent<Animator>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (comboTracker == null) comboTracker = GetComponent<ComboTracker>();
    }

    private void OnEnable() { fire?.Enable(); }
    private void OnDisable() { fire?.Disable(); }

    private void Update()
    {
        if (isSkillAnimating && Time.time > skillAnimationEndTime) isSkillAnimating = false;

        if (fire != null)
        {
            float val = fire.ReadValue<float>();     // trigger = 0..1
            if (val > 0.01f) Debug.Log($"[Shoot] P{playerInput.playerIndex + 1} value={val} scheme={playerInput.currentControlScheme}");
        }
        bool tapped = fire != null && fire.WasPressedThisFrame();
        if (tapped) Debug.Log($"[Shoot] TAPPED by P{playerInput.playerIndex + 1}");

        if (tapped) TryShoot();

        animator.SetBool(isShootingParam, tapped || isSkillAnimating);
        if (comboTracker != null) animator.SetInteger(comboIndexParam, comboTracker.GetCurrentComboIndex());
        Time_elapsed += Time.deltaTime;
    }

    public void TriggerSkillAnimation(float duration)
    {
        isSkillAnimating = true;
        skillAnimationEndTime = Time.time + duration;
        comboTracker?.TriggerCombo();
    }

    public Transform GetBulletSpawnPoint() => bulletSpawnPoint;

    private void TryShoot()
    {
        if (isSkillAnimating) return;
        if (characterData == null) return;
        if (Time.time - lastShootTime < characterData.shootCooldown) return;

        ShootBulletFromCurrentTile();
        lastShootTime = Time.time;
        comboTracker?.TriggerCombo();
    }

    private void ShootBulletFromCurrentTile()
    {
        Vector2Int playerGridPos = playerMovement != null
            ? playerMovement.GetCurrentGridPosition()
            : tileGrid.GetGridPosition(transform.position);

        Vector2Int spawnGridPos = new Vector2Int(playerGridPos.x, playerGridPos.y);
        Vector3 spawnPosition = bulletSpawnPoint.position;

        GameObject bulletObject = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        Time_elapsed = 0;

        Bullet bullet = bulletObject.GetComponent<Bullet>();
        if (bullet != null)
        {
            Vector2Int face = shootRight ? Vector2Int.right : Vector2Int.left;
            Vector2 dir = new Vector2(face.x, face.y);
            bullet.Initialize(dir, characterData.bulletSpeed, characterData.bulletDamage, tileGrid, spawnGridPos);
            AudioManager.Instance?.PlayBasicShootSFX();
        }
    }

    public void SetShootRight(bool right) { shootRight = right; }
    public bool IsShootingRight() => shootRight;

}
