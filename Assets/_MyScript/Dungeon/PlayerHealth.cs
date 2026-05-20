// PlayerHealth.cs
// ระบบ HP ของผู้เล่น — ใช้ใน Dungeon Scene

using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Header("Stats")]
    public int maxHP = 100;
    public int currentHP { get; private set; }

    // Events
    public static event Action<int, int> OnHPChanged;  // (current, max)
    public static event Action           OnDeath;

    public bool IsDead { get; private set; }

    // ──────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        IsDead    = false;
        currentHP = maxHP;
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────────────────

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0) return;

        currentHP = Mathf.Max(0, currentHP - damage);
        OnHPChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0) Die();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Private
    // ──────────────────────────────────────────────────────────────────────

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;
        OnDeath?.Invoke();
    }
}
