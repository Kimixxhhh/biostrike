using Godot;
using System;

public class StatsModifier
{
    public string StatName;
    public float Value; 
    public ModifierType Type;
}
public enum ModifierType { Flat, Percent }