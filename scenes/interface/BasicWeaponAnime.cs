using Godot;
using System;

public partial class BasicWeaponAnime : Node2D,IWeaponAnime
{
	protected AnimationPlayer anime;
	protected AudioStreamPlayer2D musicPlayer;
	public AnimationPlayer Anime { get => anime; }
	protected Sprite2D pivot;
	public Marker2D marker2D;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		anime = GetNode<AnimationPlayer>("%AnimationPlayer");
		pivot = GetNode<Sprite2D>("%Pivot");
		marker2D = GetNode<Marker2D>("%Marker2D");
		musicPlayer = GetNode<AudioStreamPlayer2D>("%AudioStreamPlayer2D"); 
		musicPlayer.Bus = "SFX";
		musicPlayer.MaxPolyphony = 5;
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
	public void PlaySfx(AudioStream sound, float vol = 0f,string busName = "SFX")
    {
        if (sound == null) return;

        var player = new AudioStreamPlayer();
        player.Stream = sound;
        player.Bus = busName;
        AddChild(player);
		player.VolumeDb = vol; 

        // 播放结束后自动释放
        player.Finished += player.QueueFree;

        player.Play(); 
    }

}
