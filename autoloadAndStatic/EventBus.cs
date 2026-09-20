using Godot;
using System;

public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }

    // 声明全局信号
    [Signal]
    public delegate void HealthChangedEventHandler(float newHealth);

    /// <summary>血量上限变化（HUD 用它设置血条最大值）。</summary>
    [Signal]
    public delegate void MaxHealthChangedEventHandler(float newMaxHealth);

    /// <summary>没有武器槽位（独立使用武器场景、没经过 WeaponController 时）的槽位号。</summary>
    public const int NoWeaponSlot = -1;

    /// <summary>
    /// 某个武器槽位的弹药变化（weaponSlot 槽位号 + 弹匣内子弹数 magazineRounds + 备弹总数 totalRounds）。
    /// HUD 每个槽位一块弹药 UI，用自己的槽位号刷新对应那块。
    /// 发出方：AmmoController.Setup（初始化）/ FireAutoController.Fire（打出一发）/ ReloadController.FromReloadingToReady（换弹完成）。
    /// </summary>
    [Signal]
    public delegate void AmmoChangedEventHandler(int weaponSlot, int magazineRounds, int totalRounds);

    /// <summary>
    /// 当前选中的武器槽位变化（HUD 靠它决定显示四块弹药 UI 中的哪一块）。
    /// 发出方：WeaponController（装备第一把武器 / 切枪）。
    /// </summary>
    [Signal]
    public delegate void WeaponSlotSelectedEventHandler(int weaponSlot);

    /// <summary>某个武器槽位的武器名（HUD 弹药块上方那行小字）。发出方：AmmoController.Setup。</summary>
    [Signal]
    public delegate void WeaponSlotNameChangedEventHandler(int weaponSlot, string weaponName);

    public override void _Ready()
    {
        Instance = this;
    }

    /// <summary>
    /// 广播某个槽位的弹药变化（弹匣 + 备弹一次给全，避免 HUD 出现不同步的中间态）。
    /// 用静态方法是因为它可能在 autoload 之外的初始化时机被调用，顺便做了空引用保护。
    /// </summary>
    public static void ReportAmmo(int weaponSlot, int magazineRounds, int totalRounds)
    {
        if (!TryGetInstance("弹药变化"))
        {
            return;
        }

        Instance.EmitSignal(SignalName.AmmoChanged, weaponSlot, magazineRounds, totalRounds);
    }

    /// <summary>广播当前选中的武器槽位（HUD 据此显示 / 隐藏对应的弹药 UI）。</summary>
    public static void ReportWeaponSlotSelected(int weaponSlot)
    {
        if (!TryGetInstance("槽位选中"))
        {
            return;
        }

        Instance.EmitSignal(SignalName.WeaponSlotSelected, weaponSlot);
    }

    /// <summary>广播某个槽位的武器名（HUD 显示在弹药块上）。</summary>
    public static void ReportWeaponSlotName(int weaponSlot, string weaponName)
    {
        if (!TryGetInstance("武器名"))
        {
            return;
        }

        Instance.EmitSignal(SignalName.WeaponSlotNameChanged, weaponSlot, weaponName ?? string.Empty);
    }

    /// <summary>取自动加载实例，缺了就提示一次并不发信号（不让 Game 逻辑因为 UI 信号挂掉）。</summary>
    private static bool TryGetInstance(string signalDesc)
    {
        if (Instance == null)
        {
            GD.PushWarning($"EventBus: 找不到自动加载实例，{signalDesc} 信号未发出（HUD 不会刷新）。");
            return false;
        }

        return true;
    }
}
