using UnityEngine;

public interface IPassiveAbility { void OnEquip(GameObject owner); void OnUnequip(); }

[DisallowMultipleComponent]
public class PassiveController : MonoBehaviour
{
    [SerializeField] private CharacterStats characterStats;
    private GameObject instance;
    private IPassiveAbility iPassive;
    private void Start()
    {
        if (characterStats != null)
        {
            Equip();
            Debug.Log($"[PassiveController] Auto-Equip passive for {gameObject.name}");
        }
    }

    public void SetCharacter(CharacterStats cs)
    {
        characterStats = cs;
        Equip();
    }

    private void Equip()
    {
        if (instance != null) { iPassive?.OnUnequip(); Destroy(instance); instance = null; iPassive = null; }

        if (characterStats == null || characterStats.passivePrefab == null)
        {
            Debug.LogWarning($"[PassiveController] No passivePrefab for {gameObject.name}");
            return;
        }

        instance = Instantiate(characterStats.passivePrefab, transform);
        var ownerRef = instance.GetComponent<OwnerRef>() ?? instance.AddComponent<OwnerRef>();
        ownerRef.owner = gameObject;

        iPassive = instance.GetComponent<IPassiveAbility>();
        iPassive?.OnEquip(gameObject);

        Debug.Log($"[PassiveController] Equipped {instance.name} under {gameObject.name}");
    }
}
