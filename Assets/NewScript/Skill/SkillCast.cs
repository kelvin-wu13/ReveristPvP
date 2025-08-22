using SkillSystem;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public interface ISkillOwnerReceiver { void SetOwner(GameObject owner); }
public interface ISkillTargetReceiver { void SetTarget(Vector3 worldPos); }
public interface ISkillDirectionReceiver { void SetDirection(Vector2 dir); }


[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Animator))]
public class SkillCast : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CharacterStats characterData;

    [Header("Spawn/Target")]
    [SerializeField] private Transform skillSpawnPoint;
    [SerializeField] private Transform crosshair;  

    [Header("Lock durasi (selama cast, gerak & basic attack diblok)")]
    [SerializeField] private float skill1Lock = 0.35f;
    [SerializeField] private float skill2Lock = 0.50f;
    [SerializeField] private float skill3Lock = 0.70f;
    [SerializeField] private float ultimateLock = 1.00f;

    [Header("Animator Triggers")]
    [SerializeField] private string skill1Trigger = "Skill1";
    [SerializeField] private string skill2Trigger = "Skill2";
    [SerializeField] private string skill3Trigger = "Skill3";
    [SerializeField] private string ultimateTrigger = "Ultimate";

    // refs lokal (per pemain)
    private PlayerInput pi;
    private Animator anim;
    private PlayerMovement movement;
    private PlayerShoot shooter;
    private PlayerStats stats;

    // actions
    private InputAction aSkill1, aSkill2, aSkill3, aUltimate;

    private void Awake()
    {
        pi = GetComponent<PlayerInput>();
        anim = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        shooter = GetComponent<PlayerShoot>();
        stats = GetComponent<PlayerStats>();

        if (skillSpawnPoint == null)
        {
            if (shooter != null && shooter.GetBulletSpawnPoint() != null)
                skillSpawnPoint = shooter.GetBulletSpawnPoint();
            else
                skillSpawnPoint = transform;
        }

        if (pi != null)
        {
            aSkill1 = pi.actions.FindAction("Skill1", false);
            aSkill2 = pi.actions.FindAction("Skill2", false);
            aSkill3 = pi.actions.FindAction("Skill3", false);
            aUltimate = pi.actions.FindAction("Ultimate", false);
        }
    }

    private void OnEnable()
    {
        aSkill1?.Enable(); aSkill2?.Enable(); aSkill3?.Enable(); aUltimate?.Enable();
    }

    private void OnDisable()
    {
        aSkill1?.Disable(); aSkill2?.Disable(); aSkill3?.Disable(); aUltimate?.Disable();
    }

    private void Update()
    {
        if (aSkill1 != null && aSkill1.WasPressedThisFrame())
        {
            Debug.Log($"[Skill] P{pi.playerIndex + 1} Skill1");
            CastSkillSlot(characterData?.skill1, skill1Trigger, skill1Lock);
        }

        if (aSkill2 != null && aSkill2.WasPressedThisFrame())
        {
            Debug.Log($"[Skill] P{pi.playerIndex + 1} Skill2");
            CastSkillSlot(characterData?.skill2, skill2Trigger, skill2Lock);
        }

        if (aSkill3 != null && aSkill3.WasPressedThisFrame())
        {
            Debug.Log($"[Skill] P{pi.playerIndex + 1} Skill3");
            CastSkillSlot(characterData?.skill3, skill3Trigger, skill3Lock);
        }

        if (aUltimate != null && aUltimate.WasPressedThisFrame())
        {
            Debug.Log($"[Skill] P{pi.playerIndex + 1} Ultimate");
            CastUltimate();
        }
    }


    private void CastSkillSlot(SkillSlot slot, string trigger, float lockSeconds)
    {
        if (characterData == null) { Debug.LogWarning("[SkillCast] characterData NULL"); return; }
        if (slot == null) { Debug.LogWarning("[SkillCast] Slot NULL (Skill belum diisi di CharacterStats)"); return; }
        if (slot.prefab == null) { Debug.LogWarning($"[SkillCast] Prefab untuk '{slot.displayName}' belum di-assign"); return; }

        float cost = Mathf.Max(0f, slot.manaCost);
        float manaNow = stats != null ? stats.CurrentMana : -1f;
        Debug.Log($"[SkillCast] Cast '{slot.displayName}' cost={cost} manaNow={manaNow}");

        if (stats != null && cost > 0f && !stats.TryUseMana(cost))
        {
            Debug.LogWarning("[SkillCast] Mana tidak cukup");
            return;
        }

        PlayCast(trigger, lockSeconds);

        var go = Instantiate(slot.prefab, skillSpawnPoint.position, skillSpawnPoint.rotation);
        Debug.Log($"[SkillCast] Spawned {go.name} @ {skillSpawnPoint.position}");

        var dir = (shooter != null && shooter.IsShootingRight()) ? Vector2.right : Vector2.left;
        foreach (var r in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (r is ISkillOwnerReceiver o) o.SetOwner(gameObject);
            if (r is ISkillDirectionReceiver d) d.SetDirection(dir);
            if (slot.useCrosshairTarget && crosshair != null && r is ISkillTargetReceiver t) t.SetTarget(crosshair.position);
        }

        var skill = go.GetComponent<Skill>(); 
        if (skill != null)
        {
            var targetGrid = (movement != null)
                ? movement.GetCurrentGridPosition()
                : FindObjectOfType<TileGrid>()?.GetGridPosition(transform.position) ?? Vector2Int.zero;

            skill.ExecuteSkillEffect(targetGrid, transform);
            Debug.Log("[SkillCast] ExecuteSkillEffect() called");
        }
        else
        {
            Debug.LogWarning("[SkillCast] Prefab tidak punya komponen Skill, efek tidak dijalankan");
        }


    }


    private void CastUltimate()
    {
        if (characterData == null || characterData.ultimatePrefab == null) return;

        float cost = Mathf.Max(0f, characterData.ultimateManaCost);
        if (stats != null && cost > 0f && !stats.TryUseMana(cost)) return;

        PlayCast(ultimateTrigger, ultimateLock);

        var go = Instantiate(characterData.ultimatePrefab, skillSpawnPoint.position, skillSpawnPoint.rotation); // :contentReference[oaicite:3]{index=3}
        foreach (var r in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (r is ISkillOwnerReceiver o) o.SetOwner(gameObject);
            if (characterData.ultimateUsesCrosshairTarget && crosshair != null && r is ISkillTargetReceiver t) // :contentReference[oaicite:4]{index=4}
                t.SetTarget(crosshair.position);
        }
    }

    private void PlayCast(string trigger, float lockSeconds)
    {
        if (!string.IsNullOrEmpty(trigger)) anim?.SetTrigger(trigger);

        movement?.SetCanMove(false);
        if (shooter != null) shooter.TriggerSkillAnimation(lockSeconds);

        if (lockSeconds > 0f) StartCoroutine(UnlockAfter(lockSeconds));
    }

    private IEnumerator UnlockAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        movement?.SetCanMove(true);
    }
}
