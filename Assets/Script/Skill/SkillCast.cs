using UnityEngine;
using UnityEngine.InputSystem;
using SkillSystem;

public class SkillCast : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CharacterStats characterStats;
    [SerializeField] private PlayerStats playerStats;

    [Header("World/Targeting")]
    [SerializeField] private PlayerCrosshair crosshair;
    [SerializeField] private Transform castOrigin;

    [Header("Input System (Gameplay Map)")]
    [SerializeField] private InputActionReference skillSouth;
    [SerializeField] private InputActionReference skillWest;
    [SerializeField] private InputActionReference skillEast;
    [SerializeField] private InputActionReference ultimate;
    [SerializeField] private InputActionReference passive;

    private void Awake()
    {
        if (!castOrigin) castOrigin = transform;
        if (!playerStats) playerStats = GetComponent<PlayerStats>();
        if (!crosshair) crosshair = FindObjectOfType<PlayerCrosshair>();
    }

    private void OnEnable()
    {
        skillSouth?.action.Enable();
        skillWest?.action.Enable();
        skillEast?.action.Enable();
        ultimate?.action.Enable();
        passive?.action.Enable();

        if (skillSouth != null) skillSouth.action.performed += OnSouth;
        if (skillWest != null) skillWest.action.performed += OnWest;
        if (skillEast != null) skillEast.action.performed += OnEast;
        if (ultimate != null) ultimate.action.performed += OnUltimate;
        if (passive != null) passive.action.performed += OnPassive;
    }

    private void OnDisable()
    {
        if (skillSouth != null) skillSouth.action.performed -= OnSouth;
        if (skillWest != null) skillWest.action.performed -= OnWest;
        if (skillEast != null) skillEast.action.performed -= OnEast;
        if (ultimate != null) ultimate.action.performed -= OnUltimate;
        if (passive != null) passive.action.performed -= OnPassive;
    }

    private void OnSouth(InputAction.CallbackContext _) => TryCast(characterStats?.skill1?.prefab, characterStats?.skill1.manaCost, characterStats?.skill1.useCrosshairTarget == true);
    private void OnWest(InputAction.CallbackContext _) => TryCast(characterStats?.skill2?.prefab, characterStats?.skill2.manaCost, characterStats?.skill2.useCrosshairTarget == true);
    private void OnEast(InputAction.CallbackContext _) => TryCast(characterStats?.skill3?.prefab, characterStats?.skill3.manaCost, characterStats?.skill3.useCrosshairTarget == true);
    private void OnUltimate(InputAction.CallbackContext _) => TryCast(characterStats?.ultimatePrefab, characterStats?.ultimateManaCost, characterStats?.ultimateUsesCrosshairTarget == true);

    private void OnPassive(InputAction.CallbackContext _)
    {
        if (characterStats?.passivePrefab == null) return;
        Instantiate(characterStats.passivePrefab, castOrigin.position, Quaternion.identity);
    }

    private void TryCast(GameObject prefab, float? manaCost, bool useCrosshair)
    {
        if (prefab == null || playerStats == null) return;
        float cost = Mathf.Max(0f, manaCost ?? 0f);
        if (!playerStats.TryUseMana(cost)) return;

        Vector3 spawnPos = useCrosshair && crosshair != null
            ? crosshair.GetTargetWorldPosition()
            : castOrigin.position;

        var go = Instantiate(prefab, spawnPos, Quaternion.identity);

        var maybeSkill = go.GetComponent<Skill>();
        if (maybeSkill != null && crosshair != null)
        {
            var gridPos = crosshair.GetTargetGridPosition();
            var owner = transform;
            maybeSkill.Initialize(gridPos, default, owner);
        }

        var shoot = GetComponent<PlayerShoot>();
        shoot?.TriggerSkillAnimation(0.25f);
    }
}
