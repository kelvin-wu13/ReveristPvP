using System.Collections.Generic;
using UnityEngine;

public enum OverloadElement { None = 0, Fire = 1, Ice = 2 }

public class ElementalStack : MonoBehaviour
{
    [Header("Max Stack Capacity")]
    [SerializeField] private int capacity = 5;

    [SerializeField] private List<OverloadElement> slots = new List<OverloadElement>();

    public int Capacity => capacity;
    public IReadOnlyList<OverloadElement> Slots => slots;

    private void Awake()
    {
        if (slots.Count != capacity)
        {
            slots.Clear();
            for (int i = 0; i < capacity; i++) slots.Add(OverloadElement.None);
        }
    }

    public void ResetAll()
    {
        for (int i = 0; i < slots.Count; i++) slots[i] = OverloadElement.None;
        Debug.Log($"[Overload] {name}: reset stacks");
    }
    public bool Add(OverloadElement type)
    {
        if (type == OverloadElement.None) return false;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == OverloadElement.None)
            {
                slots[i] = type;
                Debug.Log($"[Overload] {name}: +1 {type} (now {Count(type)}/{capacity})");
                CheckAndTrigger();
                return true;
            }
        }
        Debug.Log($"[Overload] {name}: no empty slot for {type}");
        return false;
    }

    public int Count(OverloadElement type)
    {
        int c = 0; for (int i = 0; i < slots.Count; i++) if (slots[i] == type) c++; return c;
    }

    private void CheckAndTrigger()
    {
        if (Count(OverloadElement.Fire) >= 3)
        {
            TriggerIgnite();
            ResetAll();
            return;
        }
        if (Count(OverloadElement.Ice) >= 3)
        {
            TriggerColdsnap();
            ResetAll();
        }
    }

    private void TriggerIgnite()
    {
        var comp = GetComponent("Ignite");
        if (comp != null)
        {
            var m = comp.GetType().GetMethod("Trigger") ?? comp.GetType().GetMethod("Apply") ?? comp.GetType().GetMethod("StartEffect");
            if (m != null) { m.Invoke(comp, null); Debug.Log("[Overload] Ignite triggered"); return; }
        }
        SendMessage("Ignite", SendMessageOptions.DontRequireReceiver);
        SendMessage("OnIgnite", SendMessageOptions.DontRequireReceiver);
        Debug.Log("[Overload] Ignite triggered (SendMessage fallback)");
    }

    private void TriggerColdsnap()
    {
        var comp = GetComponent("Coldsnap");
        if (comp != null)
        {
            var m = comp.GetType().GetMethod("Trigger") ?? comp.GetType().GetMethod("Apply") ?? comp.GetType().GetMethod("StartEffect");
            if (m != null) { m.Invoke(comp, null); Debug.Log("[Overload] Coldsnap triggered"); return; }
        }
        SendMessage("Coldsnap", SendMessageOptions.DontRequireReceiver);
        SendMessage("OnColdsnap", SendMessageOptions.DontRequireReceiver);
        Debug.Log("[Overload] Coldsnap triggered (SendMessage fallback)");
    }
}
