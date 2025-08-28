using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class UIManager : MonoBehaviour
{
    [Header("Health UI")]
    [SerializeField] private Slider healthSlider;

    [Header("Mana UI")]
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TextMeshProUGUI manaText;

    [Header("Enemy Health UI")]
    [SerializeField] private Slider enemyHealthSlider;

    [Header("Colors")]
    [SerializeField] private Color healthColor = Color.red;
    [SerializeField] private Color manaColor = Color.blue;

    [Header("Animation")]
    [SerializeField] private bool enableSmoothTransitions = true;
    [SerializeField] private float transitionSpeed = 5f;

    private PlayerStats playerStats;
    private float targetHealthValue, currentHealthDisplay;
    private float targetManaValue, currentManaDisplay;
    private float targetEnemyHealthValue, currentEnemyHealthDisplay;
    private int lastHealth;
    private float lastMana;
    private int totalMaxEnemyHealthCached = -1;

    private Image playerHealthImage;
    private Image enemyHealthImage;

    private void Start()
    {
        InitializeUI();
        FindAndConnectToPlayerStats();
    }

    private void Update()
    {
        CheckForPlayerStatsChanges();
        //UpdateEnemyHealthBar();
        if (enableSmoothTransitions) HandleSmoothTransitions();
    }

    private void InitializeUI()
    {
        if (healthSlider != null) healthSlider.value = 1f;
        if (manaSlider != null) manaSlider.value = 1f;

        if (enemyHealthSlider != null)
        {
            enemyHealthSlider.minValue = 0f;
            enemyHealthSlider.maxValue = 1f;
            enemyHealthSlider.value = 1f;
        }

        playerHealthImage = healthSlider?.fillRect?.GetComponent<Image>();
        enemyHealthImage = enemyHealthSlider?.fillRect?.GetComponent<Image>();

        currentHealthDisplay = targetHealthValue = 1f;
        currentManaDisplay = targetManaValue = 1f;
        currentEnemyHealthDisplay = targetEnemyHealthValue = 1f;
    }

    private void FindAndConnectToPlayerStats()
    {
        playerStats = FindObjectOfType<PlayerStats>();
        if (playerStats != null)
        {
            lastHealth = playerStats.CurrentHealth;
            lastMana = playerStats.CurrentMana;
            OnHealthChanged(playerStats.CurrentHealth, playerStats.MaxHealth);
            OnManaChanged(playerStats.CurrentMana, playerStats.MaxMana);
        }
    }

    private void CheckForPlayerStatsChanges()
    {
        if (playerStats == null) return;

        if (playerStats.CurrentHealth != lastHealth)
        {
            OnHealthChanged(playerStats.CurrentHealth, playerStats.MaxHealth);
            lastHealth = playerStats.CurrentHealth;
        }

        if (!Mathf.Approximately(playerStats.CurrentMana, lastMana))
        {
            OnManaChanged(playerStats.CurrentMana, playerStats.MaxMana);
            lastMana = playerStats.CurrentMana;
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        float percent = max == 0 ? 0f : (float)current / max;
        if (enableSmoothTransitions)
            targetHealthValue = percent;
        else
            UpdateHealthSlider(percent);
    }

    private void OnManaChanged(float current, int max)
    {
        float percent = max == 0 ? 0f : current / max;
        if (enableSmoothTransitions)
            targetManaValue = percent;
        else
            UpdateManaSlider(percent);

        if (manaText != null)
            manaText.text = $"{current:F1}";
    }

    //private void UpdateEnemyHealthBar()
    //{
    //    var enemies = EnemyManager.Instance?.GetAllEnemies();
    //    if (enemies == null || enemies.Count == 0) return;

    //    int totalCurrent = enemies.Sum(e => e != null ? e.currentHealth : 0);
    //    int totalMax = enemies.Sum(e => e != null ? e.maxHealth : 0);

    //    if (totalMaxEnemyHealthCached <= 0)
    //        totalMaxEnemyHealthCached = totalMax;

    //    if (totalMaxEnemyHealthCached == 0) return;

    //    float percent = (float)totalCurrent / totalMaxEnemyHealthCached;

    //    if (enableSmoothTransitions)
    //        targetEnemyHealthValue = percent;
    //    else
    //        SetEnemyHealthSlider(percent);
    //}

    private void HandleSmoothTransitions()
    {
        if (!Mathf.Approximately(currentHealthDisplay, targetHealthValue))
        {
            float newValue = Mathf.Lerp(currentHealthDisplay, targetHealthValue, transitionSpeed * Time.deltaTime);
            UpdateHealthSlider(newValue);
        }

        if (!Mathf.Approximately(currentManaDisplay, targetManaValue))
        {
            float newValue = Mathf.Lerp(currentManaDisplay, targetManaValue, transitionSpeed * Time.deltaTime);
            UpdateManaSlider(newValue);
        }

        if (!Mathf.Approximately(currentEnemyHealthDisplay, targetEnemyHealthValue))
        {
            float newValue = Mathf.Lerp(currentEnemyHealthDisplay, targetEnemyHealthValue, transitionSpeed * Time.deltaTime);
            SetEnemyHealthSlider(newValue);
        }
    }

    private void UpdateHealthSlider(float value)
    {
        currentHealthDisplay = value;
        if (healthSlider != null) healthSlider.value = value;
    }

    private void UpdateManaSlider(float value)
    {
        currentManaDisplay = value;
        if (manaSlider != null) manaSlider.value = value;
    }

    private void SetEnemyHealthSlider(float value)
    {
        currentEnemyHealthDisplay = value;
        if (enemyHealthSlider != null) enemyHealthSlider.value = value;
    }
}
