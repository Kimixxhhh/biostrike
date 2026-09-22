using Godot;
using System;
// in charge of adding modifiers and removing modifiers to CharaStats,
// and manage the duration of the effect
public partial class ActiveEffect : Node
{
    public StatsModifier Modifier;
    private CharaStats _target;
    private double _remainingTime;

    public ActiveEffect(CharaStats target, StatsModifier modifier, double duration)
    {
        _target = target; 
        Modifier = modifier;
        _remainingTime = duration;
    }

    public override void _Process(double delta)
    {
        if (_remainingTime < 0) return;

        _remainingTime -= delta;
        if (_remainingTime <= 0)
        {
            _target.RemoveModifier(Modifier);
            QueueFree();
        }
    }
}
