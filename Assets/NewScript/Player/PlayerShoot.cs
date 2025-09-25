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

    [Header("SFX")]
    [SerializeField] private AudioClip sfxShoot;

    [SerializeField] private float extraHoldDelay = 0.0f;
    [SerializeField] private float overrideInterval = 0.0f;

    private readonly int isShootingParam = Animator.StringToHash("IsShooting");
    private readonly int comboIndexParam = Animator.StringToHash("ComboIndex");

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
        if (isSkillAnimating && Time.time > skillAnimationEndTime)
            isSkillAnimating = false;

        bool holding = fire != null && fire.IsPressed();
        if (holding) TryShoot();

        animator.SetBool(isShootingParam, holding || isSkillAnimating);
        if (comboTracker != null)
            animator.SetInteger(comboIndexParam, comboTracker.GetCurrentComboIndex());

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

        float interval = characterData.shootCooldown + extraHoldDelay;
        if (overrideInterval > 0f) interval = overrideInterval;

        if (Time.time - lastShootTime < interval) return;

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
        AudioManager.Instance?.PlaySFX(sfxShoot);
        Time_elapsed = 0;

        Bullet bullet = bulletObject.GetComponent<Bullet>();
        if (bullet != null)
        {
            Vector2Int face = shootRight ? Vector2Int.right : Vector2Int.left;
            Vector2 dir = new Vector2(face.x, face.y);
            bullet.Initialize(dir, characterData.bulletSpeed, characterData.bulletDamage,tileGrid, spawnGridPos, this.gameObject);
        }
    }

    public void SetShootRight(bool right) { shootRight = right; }
    public bool IsShootingRight() => shootRight;
}
