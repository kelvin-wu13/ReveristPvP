using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HitForwarderOnTrigger : MonoBehaviour
{
    public bool isSkill = false;
    private OwnerRef ownerRef;

    private void Awake() { ownerRef = GetComponent<OwnerRef>(); }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var owner = ownerRef ? ownerRef.owner : null;
        if (!owner || !other || other.gameObject == owner) return;

        var recv = owner.GetComponentInChildren<IOnHitReceiver>();
        recv?.OnHitLanded(other.gameObject, isSkill);
    }
}
