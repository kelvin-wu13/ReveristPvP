using UnityEngine;

[System.Serializable]
public class SkillSlot
{
    public string displayName = "Skill";
    public GameObject prefab;
    public float manaCost = 1f;
    public bool useCrosshairTarget = true;
}

[CreateAssetMenu(fileName = "CharacterStats", menuName = "Game/Character Stats", order = 1)]
public class CharacterStats : ScriptableObject
{
    [Header("Identity")]
    public string characterName = "New Character";

    [Header("Vitals")]
    public int maxHealth = 200;
    public int maxMana = 5;
    public float manaRegenRate = 1f;

    [Header("Basic Attack")]
    public int bulletDamage = 10;
    public float bulletSpeed = 10f;
    public float shootCooldown = 0.5f;

    [Header("Skills (Gamepad Face Buttons)")]
    public SkillSlot skill1;
    public SkillSlot skill2;
    public SkillSlot skill3;

    public GameObject passivePrefab;

    public GameObject ultimatePrefab;
    public float ultimateManaCost = 3f;
    public bool ultimateUsesCrosshairTarget = true;
}
