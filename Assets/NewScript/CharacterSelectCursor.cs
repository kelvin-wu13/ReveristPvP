using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CharacterSelectCursor : MonoBehaviour
{
    // callbacks (diset manager saat Init)
    public Action<int, int> onPick;
    public Action<int> onReady;
    public Action<int> onUnready;
    public Action<int> onLeave;

    [Header("Visual")]
    public RectTransform selector;     // ikon/highlight posisi
    public Image selectorImage;        // komponen Image buat ubah warna
    public Color p1Color = Color.cyan; // warna highlight P1
    public Color p2Color = Color.magenta; // warna highlight P2
    public float moveRepeatDelay = 0.12f;

    private int playerIndex = -1;
    private RectTransform[] slots;
    private int slotIndex;
    private int cols;
    private float lastMoveTime;
    private bool isReady;

    public void Init(CharacterSelectManager mgr, int index, RectTransform[] sharedSlots)
    {
        playerIndex = index;
        slots = sharedSlots;
        slotIndex = Mathf.Clamp(slotIndex, 0, slots.Length - 1);

        // coba tebak kolom dari jumlah slot (grid kotak)
        cols = Mathf.RoundToInt(Mathf.Sqrt(Mathf.Max(1, slots.Length)));

        UpdateSelector(true);

        // kasih warna sesuai player index
        if (selectorImage)
            selectorImage.color = (playerIndex == 1 ? p1Color : p2Color);
    }

    void UpdateSelector(bool notify = false)
    {
        if (selector == null || slots == null || slots.Length == 0) return;

        slotIndex = Mathf.Clamp(slotIndex, 0, slots.Length - 1);
        selector.position = slots[slotIndex].position;

        if (notify)
            onPick?.Invoke(playerIndex, slotIndex);
    }

    // === Input System callbacks ===
    public void OnNavigate(InputValue v)
    {
        Vector2 dir = v.Get<Vector2>();
        if (Time.unscaledTime - lastMoveTime < moveRepeatDelay) return;

        int dx = 0, dy = 0;
        if (dir.x > 0.5f) dx = +1;
        else if (dir.x < -0.5f) dx = -1;
        if (dir.y > 0.5f) dy = +1;
        else if (dir.y < -0.5f) dy = -1;

        if (dx != 0 || dy != 0)
        {
            lastMoveTime = Time.unscaledTime;
            Move(dx, dy);
        }
    }

    public void OnSubmit(InputValue v)
    {
        if (!v.isPressed) return;
        isReady = !isReady;
        if (isReady) onReady?.Invoke(playerIndex);
        else onUnready?.Invoke(playerIndex);
    }

    public void OnCancel(InputValue v)
    {
        if (!v.isPressed) return;
        if (isReady)
        {
            isReady = false;
            onUnready?.Invoke(playerIndex);
        }
        else
        {
            // kalau belum ready, Cancel = leave
            onLeave?.Invoke(playerIndex);
        }
    }

    void Move(int dx, int dy)
    {
        int rows = Mathf.CeilToInt(slots.Length / (float)cols);

        int x = slotIndex % cols;
        int y = slotIndex / cols;

        x = Mathf.Clamp(x + dx, 0, cols - 1);
        y = Mathf.Clamp(y - dy, 0, rows - 1);

        int newIndex = Mathf.Clamp(y * cols + x, 0, slots.Length - 1);
        if (newIndex != slotIndex)
        {
            slotIndex = newIndex;
            UpdateSelector(true);
        }
    }
}
