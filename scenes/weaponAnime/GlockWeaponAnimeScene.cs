using Godot;
using System;

public partial class GlockWeaponAnimeScene : BasicWeaponAnime
{
    public override void _Ready()
	{
		base._Ready();
	}
    public override void PlayAttack1Anime()
	{
		anime.Play("shoot");
	}
	public override void PlayAttack2Anime(){}
	public override void PlayReloadAnime()
	{
        anime.Play("RESET");
		anime.Play("reload");
	}
	
}
