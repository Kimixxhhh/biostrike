using Godot;
using System;

/// <summary>
/// 血量组件：挂在角色下，血量变化时通过 EventBus 广播给 HUD 等 UI。
/// 广播两个全局信号（定义见 autoloadAndStatic/EventBus.cs）：
///   MaxHealthChanged —— 血量上限（先发，UI 先拿到刻度）
///   HealthChanged    —— 当前血量（后发）
/// </summary>
public partial class HealthComponent : Node
{
    /// <summary>血量上限（Inspector 可按角色改，开局会把当前血量填满）。</summary>
    [Export] public float MaxHealth { get; set; } = 100f;

    private float currentHealth;

    public override void _Ready()
    {
        currentHealth = MaxHealth;
        BroadcastHealth();
    }

    /// <summary>受伤：血量不低于 0，然后广播当前血量（上限不变）。</summary>
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        EventBus.Instance.EmitSignal(EventBus.SignalName.HealthChanged, currentHealth);
    }

    /// <summary>改上限（当前血量截到上限内），并广播上限 + 当前血量。</summary>
    public void SetMaxHealth(float newMaxHealth)
    {
        MaxHealth = Mathf.Max(1f, newMaxHealth);
        currentHealth = Mathf.Min(currentHealth, MaxHealth);
        BroadcastHealth();
    }

    /// <summary>先发上限再发当前值，保证 UI 先拿到刻度再拿到数值。</summary>
    private void BroadcastHealth()
    {
        EventBus.Instance.EmitSignal(EventBus.SignalName.MaxHealthChanged, MaxHealth);
        EventBus.Instance.EmitSignal(EventBus.SignalName.HealthChanged, currentHealth);
    }
}
