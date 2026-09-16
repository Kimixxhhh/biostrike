using Godot;
using System;

public partial class ExplosionComponent : Area2D
{
    public string targetCamp;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	private void OnBodyEntered(Node2D body)
    {
        // 判断进入的是不是玩家
        if(body is CharacterBaseScene character&&character.camp==targetCamp)
		{
			character.TakeImpact(new Vector2(100,100));
		}
    }
}
