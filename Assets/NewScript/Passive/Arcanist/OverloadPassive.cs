using UnityEngine;

public class OverloadPassive : MonoBehaviour, IPassiveAbility
{
    private GameObject owner;

    [SerializeField] private int enemySlots = 5;

    public void OnEquip(GameObject ownerGO)
    {
        owner = ownerGO;

        // Cari musuh (pemain lain)
        var myPM = owner.GetComponent<PlayerMovement>();
        if (!myPM) return;

        PlayerMovement enemy = null;
        foreach (var p in FindObjectsOfType<PlayerMovement>())
        {
            if (p != null && p != myPM) { enemy = p; break; }
        }
        if (!enemy) return;

        // Pasang / siapkan OverloadStacks pada musuh
        var stacks = enemy.GetComponent<ElementalStack>() ?? enemy.gameObject.AddComponent<ElementalStack>();
        // (Awake OverloadStacks sudah handle: inisialisasi slots=5 None)
        while (stacks.Capacity < enemySlots) { /* no-op: capacity fixed to 5 by design */ break; }
        stacks.ResetAll();

        Debug.Log($"[OverloadPassive] Equipped by {owner.name}. Enemy stacks ready (5×None).");
    }

    public void OnUnequip() { }
}
