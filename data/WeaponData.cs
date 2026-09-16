using Godot;
using System;

[GlobalClass]
public partial class WeaponData : Resource
{
    [Export]
    public string WeaponName { get; set; }
    [Export]
    public string WeaponId { get; set; }
    [Export]
    public Texture2D WeaponIcon { get; set; }

    [Export]
    public float Damage { get; set; }
    
    [Export]
    public float Damage2 { get; set; }
    
    [Export]
    public float BulletSpeed { get; set; }
    
    [Export]
    public PackedScene BulletScene { get; set; }
    
    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; }
    
    [Export]
    public int Price { get; set; }
    
    // 0:primary weapon   1:secondary weapon    2:melee     3:util
    [Export]
    public int LoadoutType { get; set; }
    
    // spread
    [Export]
    public float Overheat { get; set; }
    
    [Export]
    public float Cooldown { get; set; }
    
    [Export]
    public float Spread { get; set; }
    [Export]
    public float SpreadMin { get; set; }
    [Export]
    public float SpreadIncrease { get; set; }
    [Export]
    public float SpreadDecrease { get; set; }
    
    // rounds
    [Export]
    public int MagazineRounds { get; set; }
    
    [Export]
    public int TotalRounds { get; set; }
    
    // timer
    [Export]
    public float ReloadTime { get; set; }
    
    [Export]
    public float SwitchTime { get; set; }
    
    // define rotate faster or slower
    [Export]
    public float RotationInertia { get; set; }
    
    [Export]
    public float MoveInertia { get; set; }
    
    [Export]
    public float BulletImpact { get; set; }
}
