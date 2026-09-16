using Godot;
using System;

public partial class UtilityFireController : FireSingleController
{
    protected Node2D parent;
    public override void _Ready()
    {
        parent = GetParentOrNull<Node2D>();
    }
    public override void generateBullet()
    {
        Bullet bullet = weapon.WeaponData.BulletScene.Instantiate() as Bullet;
        bullet.Setup(weapon);
        bullet.GlobalPosition = weapon.Anime.marker2D.GlobalPosition;
        bullet.GlobalRotation = (parent.GetGlobalMousePosition() - weapon.Anime.marker2D.GlobalPosition).Angle();
        GetTree().Root.AddChild(bullet);
    }
}
