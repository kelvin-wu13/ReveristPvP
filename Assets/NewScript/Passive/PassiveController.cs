using UnityEngine;

public interface IPassiveAbility { void OnEquip(GameObject owner); void OnUnequip(); }

[DisallowMultipleComponent]
public class PassiveController : MonoBehaviour
{
    [SerializeField] private CharacterStats characterStats;
    private GameObject instance;
    private IPassiveAbility iPassive;

    public void SetCharacter(CharacterStats cs)
    {
        characterStats = cs;
        Equip();
    }

    private void Equip()
    {
        if (instance != null) { iPassive?.OnUnequip(); Destroy(instance); instance = null; iPassive = null; }

        if (characterStats == null || characterStats.passivePrefab == null) return;

        instance = Instantiate(characterStats.passivePrefab, transform);
        var ownerRef = instance.GetComponent<OwnerRef>() ?? instance.AddComponent<OwnerRef>();
        ownerRef.owner = gameObject;

        iPassive = instance.GetComponent<IPassiveAbility>();
        iPassive?.OnEquip(gameObject);
    }
}
