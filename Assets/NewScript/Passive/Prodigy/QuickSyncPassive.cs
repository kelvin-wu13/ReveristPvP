using UnityEngine;

[DisallowMultipleComponent]
public class QuickSyncPassive : MonoBehaviour, IPassiveAbility, IOnHitReceiver
{
    [Header("Quick Sync (fixed boost, non-stacking)")]
    [SerializeField] private float boostMultiplier = 1.25f; // 1.25 = +25% movespeed
    [SerializeField] private float duration = 1.0f;         // detik

    private PlayerMovement move;
    private GameObject owner;
    private float activeUntil = 0f;      // waktu berakhir boost
    private bool boostApplied = false;

    public void OnEquip(GameObject o)
    {
        owner = o;
        move = o.GetComponent<PlayerMovement>();
        enabled = true;
    }

    public void OnUnequip()
    {
        DeactivateBoost();
        enabled = false;
    }

    private void Update()
    {
        // aktif bila masih dalam durasi
        bool shouldBeActive = Time.time < activeUntil;

        if (shouldBeActive && !boostApplied) ActivateBoost();
        else if (!shouldBeActive && boostApplied) DeactivateBoost();
    }

    // dipanggil oleh bullet/skill owner melalui HitForwarderOnTrigger
    public void OnHitLanded(GameObject target, bool fromSkill)
    {
        // reset timer ke kini + duration (tidak stacking)
        activeUntil = Time.time + duration;
        // kalau lagi tidak aktif, terapkan segera
        if (!boostApplied) ActivateBoost();
    }

    private void ActivateBoost()
    {
        if (move != null)
        {
            move.SetExternalSpeedMultiplier(boostMultiplier);
            boostApplied = true;
        }
    }

    private void DeactivateBoost()
    {
        if (move != null)
        {
            move.SetExternalSpeedMultiplier(1f);
            boostApplied = false;
        }
    }
}
