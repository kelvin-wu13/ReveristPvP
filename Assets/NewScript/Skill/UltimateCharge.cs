using System;
using System.Collections;
using UnityEngine;
using PlayerId = PlayerStats.PlayerId;

public class UltimateCharge : MonoBehaviour
{
    [Header("Owner (optional)")]
    [SerializeField] private PlayerStats ownerStats;   // boleh dikosongkan; auto-find

    [Header("Meter")]
    [Range(0, 100f)][SerializeField] private float meter = 0f;
    private const float MaxMeter = 100f;

    public const float GainPerHpDealt = 0.3f;
    public const float GainPerHpTaken = 0.7f;

    public PlayerId ownerId = PlayerId.P1; // default; akan di-resolve ulang

    public float Value => meter;
    public bool IsReady => meter >= MaxMeter;

    public event Action<float> OnChanged;
    public event Action OnReady;

    // UI dengarkan event ini
    public static Action<PlayerId, float, float> OnAnyUltimateChanged; // (who, current, max)
    public static Action<PlayerId> OnAnyUltimateReady;

    private void Awake()
    {
        // Cari owner stats kalau belum diset di inspector
        if (!ownerStats)
            ownerStats = GetComponent<PlayerStats>()
                      ?? GetComponentInParent<PlayerStats>()
                      ?? GetComponentInChildren<PlayerStats>(true);

        TryResolveOwnerId("Awake");
    }

    private void Start()
    {
        // Re-resolve 1 frame setelah Start untuk kasus spawn/teleport awal
        StartCoroutine(DelayedResolve());
    }

    private IEnumerator DelayedResolve()
    {
        yield return null; // tunggu 1 frame
        TryResolveOwnerId("Start+1");
        Notify("OnEnable/Start seed");
    }

    private void OnEnable()
    {
        // Seed UI jika sudah keburu aktif lebih dulu
        Notify("OnEnable");
    }

    // ---------- PUBLIC API ----------

    public void AddDamageDealt(int hp)
    {
        if (hp <= 0 || IsReady) return;
        TryResolveOwnerId("AddDamageDealt");
        Add(GainPerHpDealt * hp, "AddDamageDealt");
    }

    public void AddDamageTaken(int hp)
    {
        if (hp <= 0 || IsReady) return;
        TryResolveOwnerId("AddDamageTaken");
        Add(GainPerHpTaken * hp, "AddDamageTaken");
    }

    public bool TryConsumeForUltimate()
    {
        if (!IsReady) return false;
        Set(0f, "Consume");
        return true;
    }

    public void ForceAssignOwner(PlayerStats ps, PlayerId? overrideId = null)
    {
        ownerStats = ps;
        if (overrideId.HasValue)
        {
            var old = ownerId;
            ownerId = overrideId.Value;
            Debug.Log($"[Ultimate] ForceAssignOwner override ID {old} -> {ownerId} on {name}");
        }
        else
        {
            TryResolveOwnerId("ForceAssignOwner");
        }
        Notify("ForceAssignOwner");
    }

    // ---------- INTERNAL ----------

    private void Set(float v, string src = null)
    {
        meter = Mathf.Clamp(v, 0f, MaxMeter);
        Notify(src ?? "Set");
    }

    private void Add(float delta, string src)
    {
        if (delta <= 0f) return;
        Set(meter + delta, src);
    }

    private void Notify(string src)
    {
        OnChanged?.Invoke(meter);
        OnAnyUltimateChanged?.Invoke(ownerId, meter, MaxMeter);
        Debug.Log($"[Ultimate.Notify] {ownerId} meter={meter:0.##}/100 ({src}) on {name}");

        if (meter >= MaxMeter)
        {
            OnReady?.Invoke();
            OnAnyUltimateReady?.Invoke(ownerId);
            Debug.Log($"[Ultimate.Ready] {ownerId} ready on {name}");
        }
    }

    private void TryResolveOwnerId(string reason)
    {
        var newId = ResolveOwnerIdByGrid();
        if (newId != ownerId)
        {
            Debug.Log($"[Ultimate] Resolve ownerId {ownerId} -> {newId} ({reason}) on {name}");
            ownerId = newId;
        }
        else
        {
            // Trace sekali untuk kepastian
            Debug.Log($"[Ultimate] ownerId confirmed {ownerId} ({reason}) on {name}");
        }
    }

    // Heuristik yang stabil: lihat posisi di grid (kiri= P1, kanan= P2)
    private PlayerId ResolveOwnerIdByGrid()
    {
        // Ambil movement dari owner
        var pm = ownerStats ? ownerStats.GetComponent<PlayerMovement>() : null;
        if (!pm) pm = GetComponentInParent<PlayerMovement>();

        var grid = FindObjectOfType<TileGrid>();
        if (grid != null && pm != null)
        {
            var pos = pm.GetCurrentGridPosition();
            int mid = Mathf.Max(1, grid.gridWidth / 2);
            return (pos.x < mid) ? PlayerId.P1 : PlayerId.P2;
        }

        // fallback paling aman
        return PlayerId.P1;
    }
}
