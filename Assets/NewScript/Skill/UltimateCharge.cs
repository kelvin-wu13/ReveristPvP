using System;
using UnityEngine;

public class UltimateCharge : MonoBehaviour
{
    [Range(0, 100f)][SerializeField] private float meter = 0f;

    public const float GainPerHpDealt = 0.3f;
    public const float GainPerHpTaken = 0.3f;

    public float Value => meter;
    public bool IsReady => meter >= 100f;

    public event Action<float> OnChanged;
    public event Action OnReady;

    public void AddDamageDealt(int hp)
    {
        if (hp <= 0 || IsReady) return;
        Add(GainPerHpDealt * hp);
    }

    public void AddDamageTaken(int hp)
    {
        if (hp <= 0 || IsReady) return;
        Add(GainPerHpTaken * hp);
    }
    public bool TryConsumeForUltimate()
    {
        if (!IsReady) return false;
        Set(0f);
        return true;
    }

    public void Set(float v)
    {
        meter = Mathf.Clamp(v, 0f, 100f);
        OnChanged?.Invoke(meter);
        if (meter >= 100f) OnReady?.Invoke();
    }

    private void Add(float delta)
    {
        if (delta <= 0f) return;
        Set(meter + delta);
    }
}
