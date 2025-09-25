using SkillSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerStats : MonoBehaviour
{
    public enum PlayerId { P1 = 1, P2 = 2 };
    public static bool IsPlayerDead = false;

    [Header("Stats Configuration")]
    [SerializeField] private CharacterStats characterData;

    [Header("Player Identification")]
    [SerializeField] private PlayerId playerId = PlayerId.P1;
    [SerializeField] private bool autoDetectPlayerId = true;

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

    public static System.Action<PlayerId, int, int> OnAnyHealthChanged;
    public static System.Action<PlayerId, float, int> OnAnyManaChanged;

    private int _lastHealthNotified = int.MinValue;
    private float _lastManaNotified = float.MinValue;

    private Color originalColor;

    private bool isDead = false;

    private void AutoAssignPlayerIdIfNeeded()
    {
        if (!autoDetectPlayerId) return;
        var grid = FindObjectOfType<TileGrid>();
        var pm = GetComponent<PlayerMovement>();

        Vector2Int pos = pm ? pm.GetCurrentGridPosition()
                            : (grid ? grid.GetGridPosition(transform.position) : Vector2Int.zero);

        int mid = (grid != null) ? Mathf.Max(1, grid.gridWidth / 2) : 0;
        playerId = (grid != null && pos.x < mid) ? PlayerId.P1 : PlayerId.P2;
    }

    private void NotifyHealthIfChanged()
    {
        if (currentHealth != _lastHealthNotified)
        {
            _lastHealthNotified = currentHealth;
            OnAnyHealthChanged?.Invoke(playerId, currentHealth, MaxHealth);
        }
    }

    private void NotifyManaIfChanged()
    {
        if (Mathf.Abs(currentMana - _lastManaNotified) > 0.001f)
        {
            _lastManaNotified = currentMana;
            OnAnyManaChanged?.Invoke(playerId, currentMana, MaxMana);
        }
    }

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

        AutoAssignPlayerIdIfNeeded();
        NotifyHealthIfChanged();
        NotifyManaIfChanged();
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
        NotifyHealthIfChanged();
        NotifyManaIfChanged();

        AudioManager.Instance?.PlayPlayerSpawnSFX();
    }

    private void Update()
    {
        if (characterData == null || isDead) return;

        if (currentMana < characterData.maxMana)
        {
            currentMana += manaRegenRatePerSecond * Time.deltaTime;
            currentMana = Mathf.Min(currentMana, characterData.maxMana);
            NotifyManaIfChanged();
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
        NotifyHealthIfChanged();
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
            NotifyManaIfChanged();
            return true;
        }

        return false;
    }

    public void RestoreMana(float amount)
    {
        if (characterData == null || isDead) return;

        currentMana = Mathf.Min(characterData.maxMana, currentMana + amount);
        NotifyManaIfChanged();
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
