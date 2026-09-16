using Godot;
using System;

public partial class CharaAnimatedSprite2d : AnimatedSprite2D
{
	private Vector2 direction;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		direction = judgeMoveDirection();
		if (direction == Vector2.Zero)
		{
			Play("idle");
		}
		else
		{
			Play("walk");
		}
		// character flip
		if ((GetGlobalMousePosition()-GlobalPosition).X > 0f)
		{
			Scale = new Vector2(1,1);
		}else 
		{
			Scale = new Vector2(-1,1);
		}
	}
	private Vector2 judgeMoveDirection()
	{
		return new Vector2(Input.GetActionStrength("moveRight")-Input.GetActionStrength("moveLeft"),
		Input.GetActionStrength("moveDown")-Input.GetActionStrength("moveUp"));
	}
}
