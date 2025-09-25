using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayerId = PlayerStats.PlayerId;


public class UIManager : MonoBehaviour
{
    [Header("P1 UI")]
    [SerializeField] private Slider p1Health;
    [SerializeField] private Slider p1Mana;
    [SerializeField] private TextMeshProUGUI p1ManaText;
    [SerializeField] private Slider p1Ult;
    [SerializeField] private TextMeshProUGUI p1UltText;

    [Header("P2 UI")]
    [SerializeField] private Slider p2Health;
    [SerializeField] private Slider p2Mana;
    [SerializeField] private TextMeshProUGUI p2ManaText;
    [SerializeField] private Slider p2Ult;
    [SerializeField] private TextMeshProUGUI p2UltText;

    [Header("Options")]
    [SerializeField] private bool smooth = true;
    [SerializeField] private float lerpSpeed = 6f;

    private float p1HCur = 1f, p1HTar = 1f;
    private float p1MCur = 1f, p1MTar = 1f;
    private float p1UCur = 0f, p1UTar = 0f;

    private float p2HCur = 1f, p2HTar = 1f;
    private float p2MCur = 1f, p2MTar = 1f;
    private float p2UCur = 0f, p2UTar = 0f;

    private void OnEnable()
    {
        PlayerStats.OnAnyHealthChanged += HandleHealth;
        PlayerStats.OnAnyManaChanged += HandleMana;
        UltimateCharge.OnAnyUltimateChanged += HandleUltimate;

        InitSliders();
        var all = FindObjectsOfType<PlayerStats>();
        foreach (var ps in all)
        {
            HandleHealth(GetId(ps), ps.CurrentHealth, ps.MaxHealth);
            HandleMana(GetId(ps), ps.CurrentMana, ps.MaxMana);
        }

        var charges = FindObjectsOfType<UltimateCharge>();
        foreach (var uc in charges)
        {
            var id = uc.ownerId;       // <-- pakai id dari sumbernya
            HandleUltimate(id, uc.Value, 100f);
        }
    }

    private void OnDisable()
    {
        PlayerStats.OnAnyHealthChanged -= HandleHealth;
        PlayerStats.OnAnyManaChanged -= HandleMana;
        UltimateCharge.OnAnyUltimateChanged -= HandleUltimate;
    }

    private void Update()
    {
        if (!smooth) return;

        if (!Mathf.Approximately(p1HCur, p1HTar))
            SetHealthSlider(PlayerId.P1, p1HCur = Mathf.Lerp(p1HCur, p1HTar, lerpSpeed * Time.deltaTime));
        if (!Mathf.Approximately(p1MCur, p1MTar))
            SetManaSlider(PlayerId.P1, p1MCur = Mathf.Lerp(p1MCur, p1MTar, lerpSpeed * Time.deltaTime));
        if (!Mathf.Approximately(p1UCur, p1UTar))
            SetUltSlider(PlayerId.P1, p1UCur = Mathf.Lerp(p1UCur, p1UTar, lerpSpeed * Time.deltaTime));

        if (!Mathf.Approximately(p2HCur, p2HTar))
            SetHealthSlider(PlayerId.P2, p2HCur = Mathf.Lerp(p2HCur, p2HTar, lerpSpeed * Time.deltaTime));
        if (!Mathf.Approximately(p2MCur, p2MTar))
            SetManaSlider(PlayerId.P2, p2MCur = Mathf.Lerp(p2MCur, p2MTar, lerpSpeed * Time.deltaTime));
        if (!Mathf.Approximately(p2UCur, p2UTar))
            SetUltSlider(PlayerId.P2, p2UCur = Mathf.Lerp(p2UCur, p2UTar, lerpSpeed * Time.deltaTime));

        if (p1UltText) p1UltText.text = $"{Mathf.RoundToInt(p1UCur * 100f)}%";
        if (p2UltText) p2UltText.text = $"{Mathf.RoundToInt(p2UCur * 100f)}%";
    }

    private void InitSliders()
    {
        if (p1Health) p1Health.value = 1f;
        if (p1Mana) p1Mana.value = 1f;
        if (p1Ult) p1Ult.value = 0f;

        if (p2Health) p2Health.value = 1f;
        if (p2Mana) p2Mana.value = 1f;
        if (p2Ult) p2Ult.value = 0f;

        p1HCur = p1HTar = 1f; p1MCur = p1MTar = 1f;
        p2HCur = p2HTar = 1f; p2MCur = p2MTar = 1f;
        p1UCur = p1UTar = 0f; p2UCur = p2UTar = 0f;
    }

    private void HandleHealth(PlayerId id, int current, int max)
    {
        float pct = (max <= 0) ? 0f : (float)current / max;
        if (smooth) SetHealthTarget(id, pct);
        else SetHealthSlider(id, pct);
    }

    private void HandleMana(PlayerId id, float current, int max)
    {
        float pct = (max <= 0) ? 0f : current / max;
        if (smooth) SetManaTarget(id, pct);
        else SetManaSlider(id, pct);

        if (id == PlayerId.P1 && p1ManaText) p1ManaText.text = $"{current:F1}";
        if (id == PlayerId.P2 && p2ManaText) p2ManaText.text = $"{current:F1}";
    }

    private void HandleUltimate(PlayerId id, float current, float max)
    {
        float pct = (max <= 0f) ? 0f : current / max;
        if (smooth) SetUltTarget(id, pct);
        else SetUltSlider(id, pct);
        Debug.Log($"[UI] Ultimate update for {id} -> {current}");
    }

    private void SetUltTarget(PlayerId id, float v)
    {
        if (id == PlayerId.P1) p1UTar = v;
        else p2UTar = v;
    }

    private void SetHealthTarget(PlayerId id, float v)
    {
        if (id == PlayerId.P1) p1HTar = v;
        else p2HTar = v;
    }
    private void SetManaTarget(PlayerId id, float v)
    {
        if (id == PlayerId.P1) p1MTar = v;
        else p2MTar = v;
    }

    private void SetHealthSlider(PlayerId id, float v)
    {
        if (id == PlayerId.P1 && p1Health) { p1Health.value = v; p1HCur = v; }
        if (id == PlayerId.P2 && p2Health) { p2Health.value = v; p2HCur = v; }
    }

    private void SetManaSlider(PlayerId id, float v)
    {
        if (id == PlayerId.P1 && p1Mana) { p1Mana.value = v; p1MCur = v; }
        if (id == PlayerId.P2 && p2Mana) { p2Mana.value = v; p2MCur = v; }
    }
    private void SetUltSlider(PlayerId id, float v)
    {
        if (id == PlayerId.P1 && p1Ult) { p1Ult.value = v; p1UCur = v; }
        if (id == PlayerId.P2 && p2Ult) { p2Ult.value = v; p2UCur = v; }
    }

    private PlayerId GetId(PlayerStats ps)
    {
        // fallback sederhana: sisi kiri = P1, kanan = P2
        var grid = FindObjectOfType<TileGrid>();
        var pm = ps.GetComponent<PlayerMovement>();
        if (grid && pm)
        {
            var pos = pm.GetCurrentGridPosition();
            int mid = Mathf.Max(1, grid.gridWidth / 2);
            return (pos.x < mid) ? PlayerId.P1 : PlayerId.P2;
        }
        return PlayerId.P1;
    }
}
