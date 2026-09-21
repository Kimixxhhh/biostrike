using Godot;
using System;

public partial class CharaAnimatedSprite2d : AnimatedSprite2D
{
	private Vector2 direction;
	private float footstepSoundCoolTime = 0f;
	private float loopTime = 0.25f;// interval between two footstep sounds
	private bool isOdd = true;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		footstepSoundTimerDec((float)delta);
		direction = judgeMoveDirection();
		if (direction == Vector2.Zero)
		{
			Play("idle");
		}
		else
		{
			Play("walk");
			PlayFootStepSfx();
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
	private void footstepSoundTimerDec(float time)
	{
		if(footstepSoundCoolTime<0f)
		{
			return;
		}
		footstepSoundCoolTime -= time;
	}
	private void PlayFootStepSfx()
	{
		if(footstepSoundCoolTime<=0)
			{
				CommonUtil.PlaySfx(
					this,
					isOdd == true ? GD.Load<AudioStream>("res://assets/sfx/weapons/footstep1.wav") : GD.Load<AudioStream>("res://assets/sfx/weapons/footstep2.wav"),
					-10f
				);
				footstepSoundCoolTime = loopTime;
				isOdd = !isOdd;
			}
	}
}
