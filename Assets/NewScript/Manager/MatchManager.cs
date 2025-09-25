using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
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

    [Header("Players (opsional di-assign manual)")]
    public PlayerStats player1; // drag Player 1 (opsional)
    public PlayerStats player2; // drag Player 2 (opsional)

    [Header("UI")]
    public TextMeshProUGUI timerText;

    [Header("Scenes")]
    public string p1WinScene = "P1Wins";
    public string p2WinScene = "P2Wins";
    [Tooltip("Opsional. Kosong = pilih pemenang acak jika HP imbang.")]
    public string drawScene = "";

    [Header("FX")]
    public FadeController fadeController; // opsional. Jika null -> SceneManager.LoadScene

    // runtime
    float timeLeft;
    bool ended;
    bool timerRunning;

    void OnEnable()
    {
        // Dipanggil setiap HP berubah dari siapapun
        PlayerStats.OnAnyHealthChanged += OnAnyHealthChanged; // KO detection. 
    }

    void OnDisable()
    {
        PlayerStats.OnAnyHealthChanged -= OnAnyHealthChanged;
    }

    IEnumerator Start()
    {
        timeLeft = Mathf.Max(1f, matchDurationSeconds);

        // Mulai loop untuk melacak pemain yang spawn belakangan
        if (autoFindPlayers)
            StartCoroutine(TrackPlayersLoop());

        // Seed UI
        UpdateTimerUI();

        // Nyalakan timer jika tidak menunggu pemain
        if (!startTimerWhenPlayersReady) timerRunning = true;

        // Kalau menunggu pemain, timerRunning akan dinyalakan oleh TrackPlayersLoop saat keduanya ketemu
        yield break;
    }

    void Update()
    {
        if (ended) return;

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

    // ===== Player tracking =====
    IEnumerator TrackPlayersLoop()
    {
        var wait = new WaitForSeconds(autoFindInterval);

        while (!ended)
        {
            // refresh bila ada yang null/destroyed
            if (player1 == null || player2 == null)
                FindPlayersBySide();

            // nyalakan timer ketika kedua pemain sudah ada (jika disetel menunggu)
            if (!timerRunning && startTimerWhenPlayersReady && player1 && player2)
            {
                timerRunning = true;
            }

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

    // ===== Event HP & penentuan pemenang =====
    void OnAnyHealthChanged(PlayerId who, int current, int max)
    {
        if (ended) return;

        // Saat ada perubahan HP, manfaatkan juga untuk caching referensi jika belum ada
        if (autoFindPlayers && (player1 == null || player2 == null))
            FindPlayersBySide();

        if (current <= 0)
        {
            // siapa yang mati -> lawannya menang
            EndMatch(who == PlayerId.P1 ? PlayerId.P2 : PlayerId.P1);
        }
    }

    void DecideWinnerOnTimeUp()
    {
        // Pastikan sekali lagi kita punya referensi (pemain mungkin spawn sangat telat)
        if (player1 == null || player2 == null)
        {
            FindPlayersBySide();
        }

        if (!player1 || !player2)
        {
            Debug.LogWarning("[MatchManager] Players missing, cannot decide winner on time up.");
            return;
        }

        int hp1 = player1.CurrentHealth;
        int hp2 = player2.CurrentHealth;

        if (hp1 > hp2) EndMatch(PlayerId.P1);
        else if (hp2 > hp1) EndMatch(PlayerId.P2);
        else
        {
            // draw
            if (!string.IsNullOrEmpty(drawScene)) LoadScene(drawScene);
            else EndMatch((Random.value < 0.5f) ? PlayerId.P1 : PlayerId.P2);
        }
    }

    void EndMatch(PlayerId winner)
    {
        if (ended) return;
        ended = true;
        timerRunning = false;

        string scene = (winner == PlayerId.P1) ? p1WinScene : p2WinScene;
        LoadScene(scene);
    }

    void LoadScene(string sceneName)
    {
        if (fadeController != null)
            fadeController.FadeOutAndLoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    // ===== UI =====
    void UpdateTimerUI()
    {
        if (!timerText) return;
        int sec = Mathf.CeilToInt(timeLeft);
        int m = sec / 60;
        int s = sec % 60;
        timerText.text = $"{m:0}:{s:00}";
    }
}
