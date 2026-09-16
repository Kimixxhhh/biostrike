using Godot;
using System;

public partial class BasicWeaponAnime : Node2D,IWeaponAnime
{
	protected AnimationPlayer anime;
	public AnimationPlayer Anime { get => anime; }
	protected Sprite2D pivot;
	public Marker2D marker2D;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		anime = GetNode<AnimationPlayer>("%AnimationPlayer");
		pivot = GetNode<Sprite2D>("%Pivot");
		marker2D = GetNode<Marker2D>("%Marker2D");
	}
	public void PlayResetAnime()
	{
		anime.Play("RESET");
	}
	
	public void PlaySwitchAnime()
	{
		anime.Play("switch");
	}
	public virtual void FlipWeaponFollowingMouse(bool isFlipable)
	{
		if (isFlipable)
		{
			pivot.Scale = new Vector2(1f,-1f);
		}
		else
		{
			pivot.Scale = new Vector2(1f,1f);
		}
		
	}
	public virtual void PlayAttack1Anime(){}
	public virtual void PlayAttack2Anime(){}
	public virtual void PlayReloadAnime(){}
	public void SetSpriteVisible(bool isVisible)
	{
		pivot.Visible = isVisible;
	}

}
