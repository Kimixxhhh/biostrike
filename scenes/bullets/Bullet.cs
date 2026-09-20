using Godot;
using System;

public partial class Bullet : Area2D
{
	protected Weapon weapon;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(weapon == null)
		{
			return;
		}
		MoveLocalX((float)(weapon.WeaponData.BulletSpeed*delta));
	}
	public void Setup(Weapon weaponParam)
	{
		weapon = weaponParam;
	}
	public virtual void _on_body_entered(Node2D body)
	{
		if(body is CharacterBaseScene character)
		{
			character.TakeImpact(Vector2.FromAngle(GlobalRotation)*weapon.WeaponData.BulletImpact);
			character.HealthComponent.TakeDamage(weapon.WeaponData.Damage);
		}
		QueueFree();
	}
}
