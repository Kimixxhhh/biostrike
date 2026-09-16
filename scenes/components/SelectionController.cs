using Godot;
using System;
using System.Dynamic;

public partial class SelectionController : Node
{
	private Weapon weapon;
	private Timer timer;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		timer = GetNode<Timer>("%Timer");
		timer.Timeout+=FromSwitchingToReady;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	public void Setup(Weapon weaponParam)
	{
		weapon = weaponParam;
		timer.WaitTime = weapon.WeaponData.SwitchTime;
	}
	public void SwitchThis()
	{
		weapon.WeaponStatus = WeaponStatus.SWITCHING;
		weapon.Anime.SetSpriteVisible(true);
		weapon.Anime.PlaySwitchAnime();
		timer.Start();
	}
	public void FromSwitchingToReady()
	{
		weapon.WeaponStatus = WeaponStatus.READY;
	}
	public void Cancel()
	{
		weapon.WeaponStatus = WeaponStatus.READY;
		weapon.Anime.PlayResetAnime();
		timer.Stop();
	}
}
