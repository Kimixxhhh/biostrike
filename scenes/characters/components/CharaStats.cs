using Godot;
using System;
using System.Collections.Generic;
public partial class CharaStats : Node
{
    private Dictionary<string, float> values = new()
    {
        { "CurrentHealth", 100f },
        { "MaxHealth", 100f },
        { "AccIndex", 1200f },
        { "ResisIndex", 12f },
    };

    private List<StatsModifier> modifiers = new();

    public float GetStat(string name)
    {
        float baseValue = values.GetValueOrDefault(name, 0f);
        float flatBonus = 0f;
        float percentBonus = 0f;

        foreach (var mod in modifiers)
        {
            if (mod.StatName != name) continue;
            if (mod.Type == ModifierType.Flat)
                flatBonus += mod.Value;
            else if (mod.Type == ModifierType.Percent)
                percentBonus += mod.Value;
        }

        return (baseValue + flatBonus) * (1f + percentBonus);
    }

    public void AddModifier(StatsModifier mod)
    {
        modifiers.Add(mod);
        //EmitSignal(SignalName.StatChanged, mod.StatName, GetStat(mod.StatName));
    }

    public void RemoveModifier(StatsModifier mod)
    {
        modifiers.Remove(mod);
        //EmitSignal(SignalName.StatChanged, mod.StatName, GetStat(mod.StatName));
    }
}
