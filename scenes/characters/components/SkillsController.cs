using Godot;
using System;

public partial class SkillsController : Node
{
    private CharaStats charaStats;

    public void Setup(CharaStats charaStatsParam)
    {
        charaStats = charaStatsParam;
    }

    private void ApplyEffect(StatsModifier mod, double duration)
    {
        charaStats.AddModifier(mod);

        var effect = new ActiveEffect(charaStats, mod, duration);
        AddChild(effect);
    }

    public void CastSpeedBoost()
    {
        var mod = new StatsModifier
        {
            StatName = "AccIndex",
            Value = 1.0f,           // +100%
            Type = ModifierType.Percent
        };
        ApplyEffect(mod, 5.0);
    }

    public override void _Input(InputEvent @event)
    {
        // 只在"刚按下"这一帧触发，长按不会重复
        if (@event.IsActionPressed("castSkill1"))
        {
            CastSpeedBoost();
            //GetViewport().SetInputAsHandled();   // 可选：阻止其他节点继续处理
        }

    }
}


