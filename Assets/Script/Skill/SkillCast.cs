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
    [SerializeField] private InputActionReference skill1;
    [SerializeField] private InputActionReference skill2;
    [SerializeField] private InputActionReference skill3;
    [SerializeField] private InputActionReference ultimate;

    [Header("Passive")]
    [SerializeField] private bool autoSpawnPassive = true;
    private GameObject spawnedPassive;

    private void Awake()
    {
        if (!castOrigin) castOrigin = transform;
        if (!playerStats) playerStats = GetComponent<PlayerStats>();
        if (!crosshair) crosshair = FindObjectOfType<PlayerCrosshair>();
    }

    private void OnEnable()
    {
        skill1?.action.Enable();
        skill2?.action.Enable();
        skill3?.action.Enable();
        ultimate?.action.Enable();

        if (skill1 != null) skill1.action.performed += OnSouth;
        if (skill2 != null) skill2.action.performed += OnWest;
        if (skill3 != null) skill3.action.performed += OnEast;
        if (ultimate != null) ultimate.action.performed += OnUltimate;

        TrySpawnPassiveOnce();
    }

    private void OnDisable()
    {
        if (skill1 != null) skill1.action.performed -= OnSouth;
        if (skill2 != null) skill2.action.performed -= OnWest;
        if (skill3 != null) skill3.action.performed -= OnEast;
        if (ultimate != null) ultimate.action.performed -= OnUltimate;
    }

    private void TrySpawnPassiveOnce()
    {
        if (!autoSpawnPassive) return;
        if (spawnedPassive != null) return;
        if (characterStats == null || characterStats.passivePrefab == null) return;

        spawnedPassive = Instantiate(characterStats.passivePrefab, castOrigin.position, Quaternion.identity, transform);
    }

    private void OnSouth(InputAction.CallbackContext _) => TryCast(characterStats?.skill1?.prefab, characterStats?.skill1.manaCost, characterStats?.skill1.useCrosshairTarget == true);
    private void OnWest(InputAction.CallbackContext _) => TryCast(characterStats?.skill2?.prefab, characterStats?.skill2.manaCost, characterStats?.skill2.useCrosshairTarget == true);
    private void OnEast(InputAction.CallbackContext _) => TryCast(characterStats?.skill3?.prefab, characterStats?.skill3.manaCost, characterStats?.skill3.useCrosshairTarget == true);
    private void OnUltimate(InputAction.CallbackContext _) => TryCast(characterStats?.ultimatePrefab, characterStats?.ultimateManaCost, characterStats?.ultimateUsesCrosshairTarget == true);

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

        GetComponent<PlayerShoot>()?.TriggerSkillAnimation(0.25f);
    }
}
