using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
public partial class WeaponController : Node2D
{
    private int weaponSlotsAmount = 4;
    private int weaponSelectionIndex = -1;
    public int WeaponSelectionIndex { get { return weaponSelectionIndex; } }
    private List<Weapon> equippedWeapon;

    public List<Weapon> EquippedWeapon { get { return equippedWeapon; } }
    public override void _Ready()
    {
        equippedWeapon  = new List<Weapon>(weaponSlotsAmount);
        for (int i = 0; i < weaponSlotsAmount; i++)
        {
            equippedWeapon.Add(null);
        }
    }
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("switchNext"))
        {
            switchToNextExistingWeapon();
        }
    }
    public void AddWeapon(string weaponName)
    {
        //Instantiate scene
        PackedScene myScene = ResourceLoader.Load<PackedScene>(Paths.GetWeaponPath(weaponName));
        if(myScene == null){return;}
        Weapon sceneInstance = myScene.Instantiate() as Weapon;

        int weaponSlotType = sceneInstance.WeaponData.LoadoutType;
        // queue free equipped weapon if it exists
        if(!(equippedWeapon[weaponSlotType] == null))
        {
            equippedWeapon[weaponSlotType].QueueFree();
        }
        equippedWeapon[weaponSlotType] = sceneInstance;

        AddChild(sceneInstance);
        // switch to weapon added if no weapon before
        if(weaponSelectionIndex == -1)
        {
            sceneInstance.SelectThis();
            weaponSelectionIndex = weaponSlotType;
        }
       
    }
    public void switchWeaponSelectionTo(int index)
    {
        if (equippedWeapon[weaponSelectionIndex] == null || equippedWeapon[index] == null)
        {
            return;
        }
        equippedWeapon[weaponSelectionIndex].UnselectThis();
        equippedWeapon[index].SelectThis();
        weaponSelectionIndex = index;
    }
    public void switchToNextExistingWeapon()
    {
        foreach (var i in Enumerable.Range(1, weaponSlotsAmount))
        {
            if (equippedWeapon[(weaponSelectionIndex + i)%weaponSlotsAmount] != null)
            {
                switchWeaponSelectionTo((weaponSelectionIndex + i)%weaponSlotsAmount);
                break;
            }
        }
    }
   
}
