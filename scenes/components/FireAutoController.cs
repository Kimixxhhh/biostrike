using Godot;
using System;

// control automatic weapon fire
public partial class FireAutoController : Node
{
    protected Weapon weapon;
    protected AmmoController ammoController;
    protected ReloadController reloadController;
    //how much time past after one shot
    protected double cooldown = 0;
    protected double spread = 0;
    public double Spread
    {
        get { return spread; }
    }
    
    public void Setup(Weapon weaponParam,AmmoController ammoControllerParam,ReloadController reloadControllerParam)
    {
        weapon = weaponParam;
        ammoController = ammoControllerParam;
        reloadController = reloadControllerParam;
    }
    public override void _Process(double delta)
    {
       if (weapon.WeaponData.Cooldown > cooldown)
        {
            cooldown+=delta;
        }
        if (Input.IsActionPressed("attack1"))
        {
            Fire();
        }
        SpreadDecrease(delta);
    }
   

    public virtual void Fire()
    {
        if(weapon.WeaponStatus != WeaponStatus.READY){return;}
        if(ammoController.magazineRounds <= 0){return;}
        if (cooldown >= weapon.WeaponData.Cooldown)
        {
            cooldown = 0;
            ammoController.magazineRounds-=1;
            generateBullet();
            SpreadIncrease();
            weapon.Anime.PlayAttack1Anime();
            if (ammoController.magazineRounds == 0)
            {
                reloadController.Reload();
            }

        }
    }
     public virtual void generateBullet()
    {
        Bullet bullet = weapon.WeaponData.BulletScene.Instantiate() as Bullet;
        bullet.Setup(weapon);
        bullet.GlobalPosition = weapon.Anime.marker2D.GlobalPosition;
        bullet.GlobalRotation = 
        weapon.Pivot.GlobalRotation+Mathf.DegToRad((float)GD.RandRange(-spread, spread));
        GetTree().Root.AddChild(bullet);
    }
    public void SpreadDecrease(double delta)
    {
        if(weapon.WeaponData.Cooldown <= cooldown)
        {
            if (weapon.WeaponData.SpreadMin<spread)
            {
                spread-=weapon.WeaponData.SpreadDecrease*delta;
            }
        }
    }
    public void SpreadIncrease()
    {
        if(weapon.WeaponData.Spread>spread)
        {
            spread+=weapon.WeaponData.SpreadIncrease;
        }
    }
}
