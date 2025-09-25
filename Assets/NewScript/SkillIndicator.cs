using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
// Agar tanda PlayerId di file ini persis seperti di PlayerStats
using PlayerId = PlayerStats.PlayerId;

[DisallowMultipleComponent]
public class SkillIndicator : MonoBehaviour
{
    [Header("Target Player")]
    [SerializeField] private PlayerId player = PlayerId.P1;

    [Header("Mana Requirement")]
    [SerializeField] private float manaCost = 1f;

    [Tooltip("Jika aktif, ambil 'manaCost' dari komponen skill (field/property publik).")]
    [SerializeField] private bool autoReadFromSkill = false;

    [Tooltip("Drag komponen skill (mis. Fireball/IonBolt) bila autoReadFromSkill aktif.")]
    [SerializeField] private MonoBehaviour skillComponent;

    [Header("Visual")]
    [Tooltip("UI Image untuk ikon (jika pakai UI). Biarkan kosong: akan dicari otomatis.")]
    [SerializeField] private Image uiImage;

    [Tooltip("Warna saat cukup mana.")]
    [SerializeField] private Color availableColor = Color.white;

    [Tooltip("Warna saat tidak cukup mana (lebih gelap).")]
    [SerializeField] private Color unavailableColor = new Color(1f, 1f, 1f, 0.45f);

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    // cache untuk mengurangi set-color berulang
    private bool lastAvailableState = true;
    private float lastShownManaCost = -999f;

    private void Awake()
    {
        // Autodetect renderer
        if (!uiImage) uiImage = GetComponent<Image>();
        if (!uiImage) uiImage = GetComponentInChildren<Image>(true);

        // Seed awal visual
        ApplyColor(true);
    }

    private void OnEnable()
    {
        PlayerStats.OnAnyManaChanged += HandleManaChanged; // event: (PlayerId, current, max)
        // Seed tampilan awal dengan membaca semua pemain yang ada (opsional)
        RefreshFromCurrentWorldState();
    }

    private void OnDisable()
    {
        PlayerStats.OnAnyManaChanged -= HandleManaChanged;
    }

    private void Update()
    {
        // Kalau cost diset otomatis dari komponen skill, baca ulang jika nilainya berubah.
        if (autoReadFromSkill && skillComponent)
        {
            float cost = TryReadManaCost(skillComponent, manaCost);
            if (!Mathf.Approximately(cost, lastShownManaCost))
            {
                manaCost = cost;
                lastShownManaCost = cost;
                // Tidak memaksa redraw; akan terjadi saat event mana masuk berikutnya.
            }
        }
    }

    private void HandleManaChanged(PlayerId who, float current, int max)
    {
        if (who != player) return;

        bool available = (current >= manaCost - 1e-4f);
        if (available != lastAvailableState)
        {
            ApplyColor(available);
            lastAvailableState = available;
            if (debugLog)
                Debug.Log($"[SkillIndicator] {name} ({player}) mana={current:0.##}, cost={manaCost:0.##} → {(available ? "OK" : "NOT ENOUGH")}");
        }
    }

    private void ApplyColor(bool available)
    {
        var col = available ? availableColor : unavailableColor;

        if (uiImage)
            uiImage.color = col;
    }

    private void RefreshFromCurrentWorldState()
    {
        // Cari semua PlayerStats lalu pilih yang id-nya cocok, ambil mana saat ini untuk seed state.
        var all = Object.FindObjectsOfType<PlayerStats>();
        foreach (var ps in all)
        {
            // Bandingkan via properti public-nya
            // (Kalau tidak ada accessor, bisa tambahkan getter CurrentMana di PlayerStats)
            float current = ps.GetManaPercentage() * ps.MaxMana;
            int max = ps.MaxMana;
        }
    }

    private float TryReadManaCost(MonoBehaviour comp, float fallback)
    {
        if (comp == null) return fallback;

        var t = comp.GetType();

        // Coba property "manaCost"
        var prop = t.GetProperty("manaCost", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.PropertyType == typeof(float))
        {
            try { return (float)prop.GetValue(comp); } catch { }
        }

        // Coba field "manaCost"
        var field = t.GetField("manaCost", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(float))
        {
            try { return (float)field.GetValue(comp); } catch { }
        }

        return fallback;
    }

    // API opsional: bisa dipanggil dari luar untuk mengubah target player / cost runtime
    public void SetPlayer(PlayerId id)
    {
        player = id;
        // biar cepat update, paksa state berubah supaya ApplyColor dipanggil di event berikutnya
        lastAvailableState = !lastAvailableState;
    }
    public void SetManaCost(float cost)
    {
        manaCost = Mathf.Max(0f, cost);
        lastShownManaCost = manaCost;
    }
}
