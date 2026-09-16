using Godot;
using System;

public partial class M67WeaponAnimeScene : BasicWeaponAnime
{
    public override void PlayReloadAnime()
	{
		anime.Play("RESET");
		anime.Play("reload");
	}
	public override void FlipWeaponFollowingMouse(bool isFlipable)
	{
		if (isFlipable)
		{
			pivot.Scale = new Vector2(-1f,1f);
		}
		else
		{
			pivot.Scale = new Vector2(1f,1f);
		}
		
	}
}
