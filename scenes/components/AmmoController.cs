using Godot;
using System;

public partial class AmmoController : Node
{
    public int magazineRounds;
    public int maxMagazineRounds;
    public int totalRounds;
    public int maxTotalRounds;

    /// <summary>所在的武器槽位号（由 WeaponController 在武器入树前写入，HUD 按它更新对应的弹药 UI）。</summary>
    public int WeaponSlot { get; set; } = EventBus.NoWeaponSlot;

    private Weapon weapon;

    public void Setup(Weapon weaponParam)
    {
        weapon = weaponParam;
        totalRounds = weapon.WeaponData.TotalRounds;
        magazineRounds = weapon.WeaponData.MagazineRounds;
        maxMagazineRounds = weapon.WeaponData.MagazineRounds;

        // 初始化 HUD 里本槽位的弹药量（弹匣内）+ 总弹药量（备弹）与武器名
        BroadcastAmmo();
        EventBus.ReportWeaponSlotName(WeaponSlot, weapon.WeaponData.WeaponName);
    }

    /// <summary>把当前弹匣内子弹数 + 备弹数广播给 HUD 里本槽位的那块弹药 UI（弹药数值变化后调用）。</summary>
    public void BroadcastAmmo()
    {
        EventBus.ReportAmmo(WeaponSlot, magazineRounds, totalRounds);
    }

}
