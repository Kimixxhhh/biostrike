using Godot;
using System;

public partial class M67Bullet : Bullet
{
	protected AnimationPlayer anime;
	public override void _on_body_entered(Node2D body)
	{
		return;
	}
	public override void _Process(double delta)
	{
		base._Process(delta);
		if (!anime.IsPlaying()){anime.Play("spin");}
	}
	public override void _Ready()
	{
		base._Ready();
		anime = GetNode<AnimationPlayer>("%AnimationPlayer");
	}
}
