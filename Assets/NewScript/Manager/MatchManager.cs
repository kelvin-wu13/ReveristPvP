using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using PlayerId = PlayerStats.PlayerId;

public class MatchManager : MonoBehaviour
{
    [Header("Match Settings")]
    [Min(1f)] public float matchDurationSeconds = 60f;

    [Tooltip("Kalau true, MatchManager akan terus mencari P1/P2 secara periodik sampai ketemu.")]
    public bool autoFindPlayers = true;

    [Tooltip("Interval pengecekan ulang auto-find (detik).")]
    [Min(0.05f)] public float autoFindInterval = 0.25f;

    [Tooltip("Jika true, timer baru berjalan setelah P1 & P2 keduanya ditemukan.")]
    public bool startTimerWhenPlayersReady = true;

    [Header("Series (Best Of)")]
    [Tooltip("Contoh: 5 untuk BO5 (first to 3), 3 untuk BO3 (first to 2).")]
    [Min(1)] public int bestOf = 5;               // BO5 = first to 3
    [Tooltip("Nama scene PvP ini (untuk reload ronde). Default: scene aktif saat Start().")]
    public string pvpSceneNameOverride = "";       // opsional; kalau kosong pakai SceneManager.GetActiveScene().name

    [Header("Players (opsional di-assign manual)")]
    public PlayerStats player1; // drag Player 1 (opsional)
    public PlayerStats player2; // drag Player 2 (opsional)

    [Header("UI")]
    public TextMeshProUGUI timerText;

    [Header("Round Orbs (Tekken-style)")]
    [Tooltip("3 slot untuk BO5 (first to 3). Boleh lebih, tapi hanya 'roundsToWin' terpakai.")]
    public Image[] p1Orbs;
    public Image[] p2Orbs;
    public Sprite orbEmpty;
    public Sprite orbFilled;

    [Header("Scenes")]
    [Tooltip("Scene tujuan setelah seri berakhir.")]
    public string endingSceneName = "Ending";

    [Header("FX")]
    public FadeController fadeController; // opsional. Jika null -> SceneManager.LoadScene

    // ====================== Series State (persist antar reload scene) ======================
    // Catatan: Static akan tetap hidup antar LoadScene di Play Mode (selama domain reload dimatikan default).
    // Jika kamu pulang ke Main Menu (scene lain), kita reset otomatis.
    private static class Series
    {
        public static bool active;
        public static string pvpSceneName;
        public static int p1Wins;
        public static int p2Wins;
        public static int roundsToWin;
        public static PlayerId lastWinner; // opsional untuk Ending scene

        public static void Begin(string sceneName, int bestOf)
        {
            active = true;
            pvpSceneName = sceneName;
            p1Wins = 0;
            p2Wins = 0;
            roundsToWin = Mathf.Max(1, (bestOf + 1) / 2);
            lastWinner = PlayerId.P1;
        }

        public static void End()
        {
            active = false;
            pvpSceneName = "";
            // p1Wins/p2Wins dibiarkan kalau Ending mau akses; bebas di-reset di scene berikutnya.
        }
    }

    // ====================== runtime per-ronde ======================
    float timeLeft;
    bool roundEnded;
    bool timerRunning;

    // ===== Unity Hooks =====
    void OnEnable()
    {
        PlayerStats.OnAnyHealthChanged += OnAnyHealthChanged; // KO detection
    }

    void OnDisable()
    {
        PlayerStats.OnAnyHealthChanged -= OnAnyHealthChanged;
    }

    IEnumerator Start()
    {
        // Tentukan nama scene PvP saat ini
        string thisScene = string.IsNullOrEmpty(pvpSceneNameOverride)
            ? SceneManager.GetActiveScene().name
            : pvpSceneNameOverride;

        // Inisialisasi seri jika belum aktif atau scene berbeda (misal dari Main Menu)
        if (!Series.active || Series.pvpSceneName != thisScene)
        {
            Series.Begin(thisScene, bestOf);
            Debug.Log($"[MatchManager] Begin Series on '{thisScene}' | roundsToWin={Series.roundsToWin} (bestOf={bestOf})");
        }
        else
        {
            Debug.Log($"[MatchManager] Continue Series on '{thisScene}' | score P1:{Series.p1Wins} P2:{Series.p2Wins}");
        }

        timeLeft = Mathf.Max(1f, matchDurationSeconds);

        // Mulai loop untuk melacak pemain yang spawn belakangan
        if (autoFindPlayers)
            StartCoroutine(TrackPlayersLoop());

        // Seed UI
        UpdateTimerUI();
        RefreshRoundOrbs();

        // Jalan timer segera atau menunggu pemain
        if (!startTimerWhenPlayersReady) timerRunning = true;

        yield break;
    }

    void Update()
    {
        if (roundEnded) return;

        if (timerRunning)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft < 0f) timeLeft = 0f;
            UpdateTimerUI();

            if (timeLeft <= 0f)
            {
                DecideWinnerOnTimeUp();
            }
        }
    }

    // ====================== Player tracking ======================
    IEnumerator TrackPlayersLoop()
    {
        var wait = new WaitForSeconds(autoFindInterval);

        while (!roundEnded)
        {
            if (player1 == null || player2 == null)
                FindPlayersBySide();

            if (!timerRunning && startTimerWhenPlayersReady && player1 && player2)
                timerRunning = true;

            yield return wait;
        }
    }

    void FindPlayersBySide()
    {
        var all = FindObjectsOfType<PlayerStats>();
        PlayerStats p1 = null, p2 = null;

        var grid = FindObjectOfType<TileGrid>();
        if (!grid) return;

        foreach (var ps in all)
        {
            if (!ps) continue;
            var pm = ps.GetComponent<PlayerMovement>();
            if (!pm) continue;

            var pos = pm.GetCurrentGridPosition();
            int mid = Mathf.Max(1, grid.gridWidth / 2);
            bool left = pos.x < mid;

            if (left)
            {
                if (p1 == null) p1 = ps;
            }
            else
            {
                if (p2 == null) p2 = ps;
            }
        }

        if (player1 == null && p1 != null) player1 = p1;
        if (player2 == null && p2 != null) player2 = p2;
    }

    // ====================== Event HP & penentuan pemenang ======================
    void OnAnyHealthChanged(PlayerId who, int current, int max)
    {
        if (roundEnded) return;

        if (autoFindPlayers && (player1 == null || player2 == null))
            FindPlayersBySide();

        if (current <= 0)
        {
            // siapa yang mati -> lawannya menang ronde
            EndRound(who == PlayerId.P1 ? PlayerId.P2 : PlayerId.P1, reason: "KO");
        }
    }

    void DecideWinnerOnTimeUp()
    {
        if (player1 == null || player2 == null) FindPlayersBySide();
        if (!player1 || !player2)
        {
            Debug.LogWarning("[MatchManager] Players missing, cannot decide winner on time up.");
            return;
        }

        int hp1 = player1.CurrentHealth;
        int hp2 = player2.CurrentHealth;

        if (hp1 > hp2)
        {
            EndRound(PlayerId.P1, reason: "TIME");
        }
        else if (hp2 > hp1)
        {
            EndRound(PlayerId.P2, reason: "TIME");
        }
        else
        {
            // DRAW → keduanya dapat 1 poin (orb kedua player menyala)
            EndRoundDraw("TIME-DRAW");
        }
    }

    void EndRoundDraw(string reason)
    {
        if (roundEnded) return;
        roundEnded = true;
        timerRunning = false;

        // Keduanya dapat 1 poin
        Series.p1Wins++;
        Series.p2Wins++;
        Debug.Log($"[MatchManager] Round End DRAW ({reason}). Score P1:{Series.p1Wins} P2:{Series.p2Wins} (to {Series.roundsToWin})");

        RefreshRoundOrbs();

        int toWin = Series.roundsToWin;

        // Jika keduanya mencapai target secara bersamaan (mis. 2–2 → 3–3), lanjut ronde tambahan
        if (Series.p1Wins >= toWin && Series.p2Wins >= toWin && Series.p1Wins == Series.p2Wins)
        {
            StartCoroutine(Co_ReloadRound());
            return;
        }

        // Jika salah satu sudah mencapai target dan unggul → Ending
        if ((Series.p1Wins >= toWin || Series.p2Wins >= toWin) && Series.p1Wins != Series.p2Wins)
        {
            StartCoroutine(Co_GotoEnding());
            return;
        }

        // Selain itu lanjut ronde berikutnya
        StartCoroutine(Co_ReloadRound());
    }


    void EndRound(PlayerId winner, string reason)
    {
        if (roundEnded) return;
        roundEnded = true;
        timerRunning = false;

        // Tambah skor
        if (winner == PlayerId.P1) Series.p1Wins++;
        else Series.p2Wins++;
        Series.lastWinner = winner;

        Debug.Log($"[MatchManager] Round End -> {winner} win by {reason}. Score P1:{Series.p1Wins} P2:{Series.p2Wins} (to {Series.roundsToWin})");

        // Update UI orb
        RefreshRoundOrbs();

        // Cek selesai seri?
        if (Series.p1Wins >= Series.roundsToWin || Series.p2Wins >= Series.roundsToWin)
        {
            // Seri selesai → Ending
            StartCoroutine(Co_GotoEnding());
        }
        else
        {
            // Lanjut ronde berikutnya → reload scene PvP (reset penuh & respawn)
            StartCoroutine(Co_ReloadRound());
        }
    }

    IEnumerator Co_ReloadRound()
    {
        // Delay pendek buat memberi waktu FX/VFX terakhir
        yield return new WaitForSeconds(0.75f);

        string sceneName = string.IsNullOrEmpty(Series.pvpSceneName)
            ? SceneManager.GetActiveScene().name
            : Series.pvpSceneName;

        if (fadeController != null)
            fadeController.FadeOutAndLoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    IEnumerator Co_GotoEnding()
    {
        // Akhiri seri (biar tidak carry ke match berikutnya)
        Series.End();

        yield return new WaitForSeconds(1.0f); // beri napas sebentar

        if (!string.IsNullOrEmpty(endingSceneName))
        {
            if (fadeController != null)
                fadeController.FadeOutAndLoadScene(endingSceneName);
            else
                SceneManager.LoadScene(endingSceneName);
        }
        else
        {
            Debug.LogWarning("[MatchManager] endingSceneName kosong. Tetap di scene ini.");
        }
    }

    void UpdateTimerUI()
    {
        if (!timerText) return;
        int sec = Mathf.CeilToInt(timeLeft);
        int m = sec / 60;
        int s = sec % 60;
        timerText.text = $"{m:0}:{s:00}";
    }

    void RefreshRoundOrbs()
    {
        int toWin = Mathf.Max(1, (bestOf + 1) / 2);
        // P1
        if (p1Orbs != null)
        {
            for (int i = 0; i < p1Orbs.Length; i++)
            {
                if (!p1Orbs[i]) continue;
                bool used = i < toWin;
                p1Orbs[i].enabled = used;
                if (used && orbEmpty) p1Orbs[i].sprite = (i < Series.p1Wins) ? (orbFilled ? orbFilled : orbEmpty) : orbEmpty;
            }
        }
        // P2
        if (p2Orbs != null)
        {
            for (int i = 0; i < p2Orbs.Length; i++)
            {
                if (!p2Orbs[i]) continue;
                bool used = i < toWin;
                p2Orbs[i].enabled = used;
                if (used && orbEmpty) p2Orbs[i].sprite = (i < Series.p2Wins) ? (orbFilled ? orbFilled : orbEmpty) : orbEmpty;
            }
        }
    }
}
