using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ParryController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference parryAction;
    [SerializeField] private string parryActionName = "Parry";

    private PlayerInput _playerInput;
    private InputAction _parryActionInst;

    [Header("Timing")]
    [SerializeField] private float parryWindow = 0.25f;
    [SerializeField] private float parryCooldown = 0.30f;

    [Header("Mana Cost")]
    [Tooltip("Biaya saat parry whiff/tidak deflect")]
    [SerializeField] private float costOnMiss = 2f;
    [Tooltip("Biaya saat parry sukses deflect (net)")]
    [SerializeField] private float costOnSuccess = 1f;

    [Header("Deflect Power")]
    [SerializeField] private float damageMultOnDeflect = 2f;
    [SerializeField] private float speedMultFirst = 1.5f;
    [SerializeField] private float speedMultStep = 0.2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animTrigStart = "Parry";

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public bool IsParryActive => isActive;

    private bool isActive;
    private bool onCooldown;
    private bool windowHadSuccess;
    private int deflectCountInWindow;

    private PlayerStats stats;
    private float prepaid = 0f;
    private bool refundedThisWindow = false;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>() ?? GetComponentInParent<PlayerStats>() ?? GetComponentInChildren<PlayerStats>(true);
        if (animator == null) animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);

        // >>> ambil dari PlayerInput milik prefab (per-player)
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput && _playerInput.actions)
        {
            _parryActionInst = _playerInput.actions.FindAction(parryActionName, false);
            _parryActionInst?.actionMap?.Enable();
        }

        // fallback ke ActionReference jika belum ketemu
        if (_parryActionInst == null && parryAction != null) _parryActionInst = parryAction.action;

        if (_parryActionInst == null) Debug.LogError("[Parry] Tidak menemukan action Parry.");
    }

    private void OnEnable()
    {
        if (_parryActionInst != null)
        {
            _parryActionInst.performed += OnParryPressed;
            _parryActionInst.Enable();
        }
    }

    private void OnDisable()
    {
        if (_parryActionInst != null)
        {
            _parryActionInst.performed -= OnParryPressed;
            _parryActionInst.Disable();
        }
    }
    private void OnParryPressed(InputAction.CallbackContext ctx) => TryStartParry();

    private void TryStartParry()
    {
        if (onCooldown || isActive) { if (debugLog) Debug.Log("[Parry] gagal start (cooldown/aktif)."); return; }
        if (stats == null) { Debug.LogError("[Parry] PlayerStats tidak ada!"); return; }

        float prepayAmount = Mathf.Max(costOnMiss, costOnSuccess);
        if (!stats.TryUseMana(prepayAmount)) { if (debugLog) Debug.Log("[Parry] gagal start: mana < 2."); return; }

        prepaid = prepayAmount;
        refundedThisWindow = false;

        isActive = true;
        windowHadSuccess = false;
        deflectCountInWindow = 0;

        if (debugLog) Debug.Log($"[Parry] START window {parryWindow:0.###}s | prepaid={prepaid}");
        PlayParryAnim();
        StartCoroutine(ParryWindowRoutine());
    }

    private void PlayParryAnim()
    {
        if (animator == null || string.IsNullOrEmpty(animTrigStart)) return;
        animator.ResetTrigger(animTrigStart);
        animator.SetTrigger(animTrigStart);
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

        if (windowHadSuccess && !refundedThisWindow && prepaid > 0f && costOnSuccess < prepaid)
        {
            float refund = prepaid - costOnSuccess; // 1
            stats.RestoreMana(refund);
            refundedThisWindow = true;
            if (debugLog) Debug.Log($"[Parry] REFUND on end: +{refund} (net cost {costOnSuccess}).");
        }

        if (debugLog) Debug.Log(windowHadSuccess ? "[Parry] END: SUCCESS" : "[Parry] END: WHIFF (net cost 2).");

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
    public bool TryDeflect(GameObject projectileGO)
    {
        if (!IsParryActive || !projectileGO) { if (debugLog) Debug.Log("[Parry] inactive/null"); return false; }

        IDeflectableProjectile proj = null;
        if (!projectileGO.TryGetComponent<IDeflectableProjectile>(out proj))
            proj = projectileGO.GetComponentInChildren<IDeflectableProjectile>(true)
                ?? projectileGO.GetComponentInParent<IDeflectableProjectile>();

        if (proj == null)
        {
            if (debugLog) Debug.Log("[Parry] tidak menemukan IDeflectableProjectile pada projectile");
            return false;
        }

        windowHadSuccess = true;
        deflectCountInWindow++;
        var speedMult = speedMultFirst + Mathf.Max(0, deflectCountInWindow - 1) * speedMultStep;

        // refund (net -1)
        if (!refundedThisWindow && prepaid > 0f && costOnSuccess < prepaid)
        {
            var refund = prepaid - costOnSuccess;
            stats.RestoreMana(refund);
            refundedThisWindow = true;
            if (debugLog) Debug.Log($"[Parry] refund +{refund}");
        }

        proj.DeflectTo(gameObject, damageMultOnDeflect, speedMult, deflectCountInWindow);
        if (debugLog) Debug.Log("[Parry] DEFLECT via interface");
        return true;
    }

    public bool TryParryNonProjectileSuccess()
    {
        if (!isActive) return false;
        windowHadSuccess = true;
        deflectCountInWindow++;

        if (!refundedThisWindow && prepaid > 0f && costOnSuccess < prepaid)
        {
            float refund = prepaid - costOnSuccess;
            stats.RestoreMana(refund);
            refundedThisWindow = true;
            if (debugLog) Debug.Log($"[Parry] SUCCESS (non-projectile): refund +{refund}");
        }
        return true;
    }
}
