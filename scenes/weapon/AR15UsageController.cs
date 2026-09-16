using Godot;
using System;

public partial class AR15UsageController : BasicUsageController
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("attack1"))
		{
			UsageAttack1();
		}
		if (@event.IsActionPressed("reload"))
		{
			UsageReload();
		}
	}
}
