using Godot;
using System;

public partial class FireSingleController : FireAutoController
{
    public override void _Process(double delta)
    {
       if (weapon.WeaponData.Cooldown > cooldown)
        {
            cooldown+=delta;
        }
        if (Input.IsActionJustPressed("attack1"))
        {
            Fire();
        }
        SpreadDecrease(delta);
    }
}
