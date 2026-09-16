using Godot;
using System;

public partial class ButtstockAttackController : Node
{
    private Weapon weapon;
    private Timer timer;
    public void Setup(Weapon weaponParam)
    {
        this.weapon = weaponParam;
        timer.WaitTime = weaponParam.Anime.Anime.GetAnimation("attack2").Length;
    }
    public override void _Ready()
	{
		timer = GetNode<Timer>("%Timer");
		timer.Timeout+=SetStatusReady;
        
	}
    public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("attack2"))
		{
			if (weapon.StatusChange(WeaponStatus.BUTTSTOCK_ATTACKING))
			{
				weapon.WeaponStatus = WeaponStatus.BUTTSTOCK_ATTACKING;
                weapon.Anime.PlayAttack2Anime();
                timer.Start();
			}
		}
		
	}
    public void SetStatusReady()
	{
		weapon.WeaponStatus = WeaponStatus.READY;
	}
}
