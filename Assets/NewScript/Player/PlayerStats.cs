using System.Collections;
using System.Collections.Generic;
using SkillSystem;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static bool IsPlayerDead = false;

    [Header("Stats Configuration")]
    [SerializeField] private CharacterStats characterData;

    [Header("Mana Regeneration")]
    [SerializeField] private float manaRegenRatePerSecond = 2f;

    [Header("Animation")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string deathAnimationTrigger = "Death";
    [SerializeField] private float deathAnimationDuration = 2f;
    [SerializeField] private bool useDeathAnimation = true;

    [SerializeField] private string spawnAnimationTrigger = "Spawn";
    [SerializeField] private float spawnAnimationDuration = 1.5f;

    [Header("Current Values")]
    [SerializeField] private int currentHealth;
    [SerializeField] private float currentMana;
    private UltimateCharge ultimate;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitColor = Color.red;

    private Color originalColor;

    private bool isDead = false;

    public int CurrentHealth
    {
        get => currentHealth;
        private set => currentHealth = value;
    }
    public int MaxHealth => characterData ? characterData.maxHealth : 0;
    public float CurrentMana
    {
        get => currentMana;
        private set => currentMana = value;
    }
    public int MaxMana => characterData ? characterData.maxMana : 0;
    public bool IsDead => isDead;

    private void Start()
    {
        if (ultimate == null)
            ultimate = GetComponent<UltimateCharge>();

        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (characterData != null)
        {
            currentHealth = characterData.maxHealth;
            currentMana = characterData.maxMana;
        }

        if (currentHealth <= 0 || currentHealth > characterData.maxHealth ||
            currentMana < 0 || currentMana > characterData.maxMana)
        {
            ResetToMaxStats();
        }
    }

    public void ResetToMaxStats()
    {
        if (characterData == null) return;

        currentHealth = characterData.maxHealth;
        currentMana = characterData.maxMana;
        isDead = false;

        PlayerShoot shootComponent = GetComponent<PlayerShoot>();
        if (shootComponent != null) shootComponent.enabled = true;

        PlayerMovement moveComponent = GetComponent<PlayerMovement>();
        if (moveComponent != null) moveComponent.enabled = true;

        SkillCast skillComponent = GetComponent<SkillCast>();
        if (skillComponent != null) skillComponent.enabled = true;

        if (playerAnimator != null)
        {
            bool hasTrigger = false;
            foreach (AnimatorControllerParameter param in playerAnimator.parameters)
            {
                if (param.name == spawnAnimationTrigger && param.type == AnimatorControllerParameterType.Trigger)
                {
                    hasTrigger = true;
                    break;
                }
            }

            if (hasTrigger)
            {
                playerAnimator.SetTrigger(spawnAnimationTrigger);
            }
            else
            {
                Debug.LogWarning($"Spawn animation trigger '{spawnAnimationTrigger}' not found in Animator!");
            }
        }

        StartCoroutine(EnablePlayerAfterSpawn(spawnAnimationDuration));

        AudioManager.Instance?.PlayPlayerSpawnSFX();
    }

    private void Update()
    {
        if (characterData == null || isDead) return;

        if (currentMana < characterData.maxMana)
        {
            currentMana += manaRegenRatePerSecond * Time.deltaTime;
            currentMana = Mathf.Min(currentMana, characterData.maxMana);
        }
    }

    private IEnumerator EnablePlayerAfterSpawn(float delay)
    {
        GetComponent<PlayerMovement>()?.SetCanMove(false);
        yield return new WaitForSeconds(delay);

        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(spawnAnimationTrigger);
        }

        var movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.SetCanMove(true);
            movement.ForceIdle();
        }
    }

    public void TakeDamage(int damage, GameObject attacker)
    {
        if (characterData == null || isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        StartCoroutine(FlashColor());
        
        if (ultimate == null) ultimate = GetComponent<UltimateCharge>();
        ultimate?.AddDamageTaken(damage);

        if (attacker != null)
        {
            var atkMeter = attacker.GetComponent<UltimateCharge>();
            atkMeter?.AddDamageDealt(damage);
        }

        if (currentHealth <= 0)
            Die();
    }

    private IEnumerator FlashColor()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = hitColor;

        yield return new WaitForSeconds(hitFlashDuration);

        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        IsPlayerDead = true;

        PlayerShoot shootComponent = GetComponent<PlayerShoot>();
        if (shootComponent != null) shootComponent.enabled = false;

        PlayerMovement moveComponent = GetComponent<PlayerMovement>();
        if (moveComponent != null) moveComponent.enabled = false;

        SkillCast skillComponent = GetComponent<SkillCast>();
        if (skillComponent != null) skillComponent.enabled = false;

        //Enemy enemy = GetComponent<Enemy>();
        //if (enemy != null) enemy.enabled = false;

        AudioManager.Instance?.PlayPlayerDeathSFX();

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        if (useDeathAnimation && playerAnimator != null)
        {
            bool hasTrigger = false;
            foreach (AnimatorControllerParameter param in playerAnimator.parameters)
            {
                if (param.name == deathAnimationTrigger && param.type == AnimatorControllerParameterType.Trigger)
                {
                    hasTrigger = true;
                    break;
                }
            }

            if (hasTrigger)
            {
                playerAnimator.SetTrigger(deathAnimationTrigger);
                yield return new WaitForSeconds(deathAnimationDuration);
            }
            else
            {
                Debug.LogWarning($"Death animation trigger '{deathAnimationTrigger}' not found in Animator!");
                yield return new WaitForSeconds(1f);
            }
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        HandlePostDeath();
    }

    protected virtual void HandlePostDeath()
    {
        FindObjectOfType<DeathSceneManager>().HandlePlayerDeath();
    }

    public void Respawn()
    {
        IsPlayerDead = false;

        ResetToMaxStats();

        if (playerAnimator != null)
        {
            playerAnimator.Rebind();
            playerAnimator.Update(0f);
        }
    }

    public bool TryUseMana(float amount)
    {
        if (characterData == null || isDead) return false;

        if (currentMana >= amount)
        {
            currentMana -= amount;
            return true;
        }

        return false;
    }

    public void RestoreMana(float amount)
    {
        if (characterData == null || isDead) return;

        currentMana = Mathf.Min(characterData.maxMana, currentMana + amount);
    }

    public float GetHealthPercentage()
    {
        if (characterData == null) return 0f;
        return (float)currentHealth / characterData.maxHealth;
    }

    public float GetManaPercentage()
    {
        if (characterData == null) return 0f;
        return currentMana / characterData.maxMana;
    }
}
