using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
public partial class WeaponController : Node2D
{
    /// <summary>武器槽位数量（HUD 里弹药 UI 的数量也跟这个对齐）。</summary>
    public const int WeaponSlotsAmount = 4;

    private int weaponSelectionIndex = -1;
    public int WeaponSelectionIndex { get { return weaponSelectionIndex; } }
    private List<Weapon> equippedWeapon;

    public List<Weapon> EquippedWeapon { get { return equippedWeapon; } }
    public override void _Ready()
    {
        equippedWeapon  = new List<Weapon>(WeaponSlotsAmount);
        for (int i = 0; i < WeaponSlotsAmount; i++)
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

        // 先告诉武器的弹药组件它属于哪个槽位，再入树（_Ready 里的初始化广播才带得上槽位号）
        AmmoController ammoController = sceneInstance.GetNodeOrNull<AmmoController>("AmmoController");
        if (ammoController == null)
        {
            GD.PushWarning($"WeaponController: 武器 {sceneInstance.Name} 下找不到 AmmoController，HUD 不会显示它的弹药。");
        }
        else
        {
            ammoController.WeaponSlot = weaponSlotType;
        }

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
            // 第一把武器：告诉 HUD 现在该显示哪个槽位的弹药
            EventBus.ReportWeaponSlotSelected(weaponSlotType);
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
        // 换成新武器了：先让 HUD 切到新槽位的那块弹药 UI，再把新武器的弹药数补一遍
        EventBus.ReportWeaponSlotSelected(index);
        NotifyHudAmmo(equippedWeapon[index]);
    }

    /// <summary>
    /// 通过 EventBus 更新 HUD 的弹药量（走 AmmoController.BroadcastAmmo）。
    /// 场景里没挂 AmmoController 的武器（如近战 / 投掷物）会安静跳过。
    /// </summary>
    private static void NotifyHudAmmo(Weapon weapon)
    {
        if (weapon == null)
        {
            return;
        }
        AmmoController ammoController = weapon.GetNodeOrNull<AmmoController>("AmmoController");
        if (ammoController == null)
        {
            GD.PushWarning($"WeaponController: 武器 {weapon.Name} 下找不到 AmmoController，HUD 弹药量未更新。");
            return;
        }
        ammoController.BroadcastAmmo();
    }
    public void switchToNextExistingWeapon()
    {
        foreach (var i in Enumerable.Range(1, WeaponSlotsAmount))
        {
            if (equippedWeapon[(weaponSelectionIndex + i)%WeaponSlotsAmount] != null)
            {
                switchWeaponSelectionTo((weaponSelectionIndex + i)%WeaponSlotsAmount);
                break;
            }
        }
    }
   
}
