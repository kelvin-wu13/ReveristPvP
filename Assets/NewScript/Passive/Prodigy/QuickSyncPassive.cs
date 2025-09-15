using UnityEngine;

[DisallowMultipleComponent]
public class QuickSyncPassive : MonoBehaviour, IPassiveAbility, IOnHitReceiver
{
    [Header("Quick Sync")]
    [SerializeField] private float boostMultiplier = 1.25f;
    [SerializeField] private float duration = 1.0f;

    private PlayerMovement move;
    private GameObject owner;
    private float activeUntil = 0f;
    private bool boostApplied = false;

    public void OnEquip(GameObject o)
    {
        owner = o;
        move = o.GetComponent<PlayerMovement>();
        enabled = true;
        Debug.Log($"[QuickSyncPassive] Equipped to {owner?.name}");
    }

    public void OnUnequip()
    {
        DeactivateBoost();
        enabled = false;
    }

    private void Update()
    {
        bool shouldBeActive = Time.time < activeUntil;

        if (shouldBeActive && !boostApplied) ActivateBoost();
        else if (!shouldBeActive && boostApplied) DeactivateBoost();
    }

    public void OnHitLanded(GameObject target, bool fromSkill)
    {
        activeUntil = Time.time + duration;
        Debug.Log($"[QuickSyncPassive] Hit landed on {target.name}, perpanjang boost hingga {activeUntil:F2}");
        if (!boostApplied) ActivateBoost();
    }

    private void ActivateBoost()
    {
        if (move != null)
        {
            move.SetExternalSpeedMultiplier(boostMultiplier);
            boostApplied = true;
            Debug.Log("Passive boost on");
        }
    }

    private void DeactivateBoost()
    {
        if (move != null)
        {
            move.SetExternalSpeedMultiplier(1f);
            boostApplied = false;
            Debug.Log($"[QuickSyncPassive] BOOST OFF, speed kembali normal (Owner={owner?.name})");
        }
    }
}
