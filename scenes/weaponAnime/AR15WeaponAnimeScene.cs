using Godot;
using System;
using System.Transactions;

public partial class AR15WeaponAnimeScene : BasicWeaponAnime
{
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	public override void _Ready()
	{
		base._Ready();
	}

	public override void PlayAttack1Anime()
	{
		anime.Play("shoot");
		PlaySfx(GD.Load<AudioStream>("res://assets/sfx/weapons/AR15Rifle_shot.wav"),-7f);
	}
	public override void PlayAttack2Anime()
	{
		anime.Play("attack2");
	}
	public override void PlayReloadAnime()
	{
		anime.Play("RESET");
		anime.Play("reload");
	}
	public  void PlayRemoveMagSound()
	{
		PlaySfx(GD.Load<AudioStream>("res://assets/sfx/weapons/removemag.wav"),2f);
	}
	public  void PlayInsertMagSound()
	{
		PlaySfx(GD.Load<AudioStream>("res://assets/sfx/weapons/insertmag.wav"),2f);
	}
}
