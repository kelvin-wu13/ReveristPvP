using UnityEngine;

[CreateAssetMenu(fileName = "Stats", menuName = "Game/Stats", order = 1)]
public class Stats : ScriptableObject
{
    [Header("Health")]
    [Tooltip("Maximum hit points for the player")]
    [SerializeField] private int maxHealth = 100;

    [Header("Combat")]
    [Tooltip("Damage dealt by player bullets")]
    [SerializeField] private int bulletDamage = 10;
    [Tooltip("Speed of player bullets")]
    [SerializeField] private float bulletSpeed = 10f;
    [Tooltip("Cooldown between shots in seconds")]
    [SerializeField] private float shootCooldown = 0.5f;

    [Header("Mana")]
    [Tooltip("Maximum mana points for special abilities")]
    [SerializeField] private int maxMana = 10;
    [Tooltip("Rate at which mana regenerates per second")]
    [SerializeField] private float manaRegenRate = 2f;

    // Public property accessors
    public int MaxHealth => maxHealth;
    public int BulletDamage => bulletDamage;
    public float BulletSpeed => bulletSpeed;
    public float ShootCooldown => shootCooldown;
    public int MaxMana => maxMana;
    public float ManaRegenRate => manaRegenRate;

    // Optional: You can add methods for gameplay mechanics
    // For example:
    public int CalculateDamageWithBuff(float damageMultiplier)
    {
        return Mathf.RoundToInt(bulletDamage * damageMultiplier);
    }
}