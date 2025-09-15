using UnityEngine;

public static class HitEvents
{
    public static void NotifyOwnerHit(GameObject owner, GameObject target, bool fromSkill, string sourceTag = "")
    {
        if (!owner) return;
        var recv = owner.GetComponentInChildren<IOnHitReceiver>();
        if (recv != null)
        {
            Debug.Log($"[HitEvents] Notify owner={owner.name} target={target?.name} fromSkill={fromSkill} source={sourceTag}");
            recv.OnHitLanded(target, fromSkill);
        }
        else
        {
            Debug.LogWarning($"[HitEvents] No IOnHitReceiver found under {owner?.name}");
        }
    }
}
