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
		musicPlayer.Play();
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
	
}
