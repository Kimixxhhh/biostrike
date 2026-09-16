using Godot;
using System;

public partial class TestWeaponController : Node2D
{
    private Weapon weapon;
    public override void _Ready()
    {
        weapon = GetNode<Weapon>("WeaponSceneBase"); 
        weapon.SelectThis();
    }
}
