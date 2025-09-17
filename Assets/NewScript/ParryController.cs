using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ParryController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference parryAction;

    [Header("Timing")]
    [SerializeField] private float parryWindow = 0.25f;
    [SerializeField] private float parryCooldown = 0.30f;

    [Header("Mana Cost")]
    [Tooltip("Biaya saat parry whiff/tidak deflect")]
    [SerializeField] private float costOnMiss = 2f;
    [Tooltip("Biaya saat parry sukses deflect (net)")]
    [SerializeField] private float costOnSuccess = 1f;

    [Header("Deflect Power")]
    [SerializeField] private float damageMultOnDeflect = 2f;  // 2x damage
    [SerializeField] private float speedMultFirst = 1.5f;
    [SerializeField] private float speedMultStep = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public bool IsParryActive => isActive;

    private bool isActive;
    private bool onCooldown;
    private bool windowHadSuccess;
    private int deflectCountInWindow;

    // mana handling
    private PlayerStats stats;
    private float prepaid = 0f;           // jumlah yang sudah dipotong di awal (biasanya 2)
    private bool refundedThisWindow = false;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        if (parryAction == null || parryAction.action == null)
            Debug.LogError("[Parry] Parry ActionReference belum di-assign!");
    }

    private void OnEnable()
    {
        if (parryAction != null && parryAction.action != null)
        {
            parryAction.action.performed += OnParryPressed;     // tap-to-parry
            parryAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (parryAction != null && parryAction.action != null)
        {
            parryAction.action.performed -= OnParryPressed;
            parryAction.action.Disable();
        }
    }

    private void OnParryPressed(InputAction.CallbackContext ctx) => TryStartParry();

    private void TryStartParry()
    {
        if (onCooldown || isActive) { if (debugLog) Debug.Log("[Parry] gagal start (cooldown/aktif)."); return; }
        if (stats == null) { Debug.LogError("[Parry] PlayerStats tidak ada!"); return; }

        // Prepay biaya maksimum (2). Jika nantinya sukses, kita REFUND 1 ⇒ net -1.
        float prepayAmount = Mathf.Max(costOnMiss, costOnSuccess); // 2
        if (!stats.TryUseMana(prepayAmount)) { if (debugLog) Debug.Log("[Parry] gagal start: mana < 2."); return; }

        prepaid = prepayAmount;
        refundedThisWindow = false;

        isActive = true;
        windowHadSuccess = false;
        deflectCountInWindow = 0;

        if (debugLog) Debug.Log($"[Parry] START window {parryWindow:0.###}s | prepaid={prepaid}");

        StartCoroutine(ParryWindowRoutine());
    }

    private IEnumerator ParryWindowRoutine()
    {
        yield return new WaitForSeconds(parryWindow);
        EndParryWindow();
    }

    private void EndParryWindow()
    {
        if (!isActive) return;
        isActive = false;

        // Jika sukses & belum direfund (mis. deflect terjadi di akhir), amankan refund di sini.
        if (windowHadSuccess && !refundedThisWindow && prepaid > 0f && costOnSuccess < prepaid)
        {
            float refund = prepaid - costOnSuccess; // 1
            stats.RestoreMana(refund);
            refundedThisWindow = true;
            if (debugLog) Debug.Log($"[Parry] REFUND on end: +{refund} (net cost {costOnSuccess}).");
        }

        if (debugLog) Debug.Log(windowHadSuccess ? "[Parry] END: SUCCESS" : "[Parry] END: WHIFF (net cost 2).");

        // reset prepaid tracker
        prepaid = 0f;
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        onCooldown = true;
        yield return new WaitForSeconds(parryCooldown);
        onCooldown = false;
        if (debugLog) Debug.Log("[Parry] READY.");
    }

    /// Dipanggil proyektil saat akan mengenai pemain ini.
    public bool TryDeflect(IonBolt bolt)
    {
        if (!isActive || bolt == null) { if (debugLog) Debug.Log("[Parry] TryDeflect: window tidak aktif / bolt null."); return false; }

        windowHadSuccess = true;
        deflectCountInWindow++;
        float speedMult = speedMultFirst + Mathf.Max(0, deflectCountInWindow - 1) * speedMultStep;

        // Refund 1x (sekali per window) → net -1
        if (!refundedThisWindow && prepaid > 0f && costOnSuccess < prepaid)
        {
            float refund = prepaid - costOnSuccess; // 1
            stats.RestoreMana(refund);
            refundedThisWindow = true;
            if (debugLog) Debug.Log($"[Parry] DEFLECT! refund +{refund} | count={deflectCountInWindow}");
        }
        else if (debugLog)
        {
            Debug.Log($"[Parry] DEFLECT! (no additional refund) | count={deflectCountInWindow}");
        }

        // Balikkan proyektil ke caster dengan 2x damage dan speed buff
        bolt.DeflectTo(gameObject, damageMultOnDeflect, speedMult, deflectCountInWindow);
        return true;
    }
}
