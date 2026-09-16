using Godot;
using System;

public partial class AmmoController : Node
{
    public int magazineRounds;
    public int maxMagazineRounds;
    public int totalRounds;
    public int maxTotalRounds;
    private Weapon weapon;

    public void Setup(Weapon weaponParam)
    {
        weapon = weaponParam;
        totalRounds = weapon.WeaponData.TotalRounds;
        magazineRounds = weapon.WeaponData.MagazineRounds;
        maxMagazineRounds = weapon.WeaponData.MagazineRounds;
    }

}
