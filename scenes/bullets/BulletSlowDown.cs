using Godot;
using System;

public partial class BulletSlowDown : Bullet
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);
	}
	public override void _on_body_entered(Node2D body)
	{
		if(body is CharacterBaseScene character)
		{
			character.SlowDown(15);
		}
		QueueFree();
	}
}
