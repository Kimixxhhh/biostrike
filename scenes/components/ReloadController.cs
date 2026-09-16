using Godot;
using System;

public partial class ReloadController : Node
{
    private Weapon weapon;
	private AmmoController ammoController;
	private Timer timer;
	private int roundsToFillMag;
    public override void _Ready()
	{
		timer = GetNode<Timer>("%Timer");
		timer.Timeout+=FromReloadingToReady;
	}
    public void Setup(Weapon weaponParam,AmmoController ammoControllerParam)
	{
		weapon = weaponParam;
		timer.WaitTime = weapon.WeaponData.ReloadTime;
		ammoController = ammoControllerParam;

	}
    public void Reload()
	{
		if(ammoController.magazineRounds == ammoController.maxMagazineRounds){return;}
		weapon.WeaponStatus = WeaponStatus.RELOADING;
		weapon.Anime.PlayReloadAnime();
		timer.Start();
	}
    public void FromReloadingToReady()
	{
		roundsToFillMag = ammoController.maxMagazineRounds - ammoController.magazineRounds;
		// if enough rest ammo to fullfill magazine
		if(ammoController.totalRounds >= roundsToFillMag)
		{
			ammoController.totalRounds -=roundsToFillMag;
			ammoController.magazineRounds = ammoController.maxMagazineRounds;
		}
		else// if not enough reest ammo to fullfill magazine
		{
			ammoController.magazineRounds+=ammoController.totalRounds;
			ammoController.totalRounds =0;
		}
		roundsToFillMag = 0;
		weapon.WeaponStatus = WeaponStatus.READY;
	}
	public void Cancel()
	{
		weapon.WeaponStatus = WeaponStatus.READY;
		weapon.Anime.PlayResetAnime();
		timer.Stop();
		// test
		GD.Print("reload Cancel triggered");
	}
	 public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("reload"))
		{
			if (weapon.StatusChange(WeaponStatus.RELOADING))
			{
				Reload();
			}
		}
		
	}
}
