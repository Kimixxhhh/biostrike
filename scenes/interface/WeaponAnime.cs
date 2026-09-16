using Godot;
using System;

public interface IWeaponAnime
{
	void PlayAttack1Anime();
	void PlayAttack2Anime();
	void PlaySwitchAnime();
	void PlayReloadAnime();
	void FlipWeaponFollowingMouse(bool isFlipable);
	void SetSpriteVisible(bool isVisible);

}
