using Godot;

/// <summary>
/// 准星（Crosshair）的全局设置数据 —— CrosshairSetting UI 写入，Crosshair 读取。
/// 静态类、不挂节点，设置在同一局游戏内跨场景保留（需要存盘时在这里扩展即可）。
///
/// 用法：
///   1) Crosshair 在 _Ready 里 <c>CrosshairSettingData.Attach(this)</c>，并订阅 <c>Changed</c>
///      （第一个加入的准星会把自身 Inspector 里的值作为全局初始值；之后都以全局数据为准）。
///   2) CrosshairSetting UI 修改字段后调用 <c>NotifyChanged()</c>，所有准星立刻重绘。
/// </summary>
public static class CrosshairSettingData
{
    // ---- 默认值（与 Crosshair 的 Export 默认值保持一致）----

    /// <summary>十字线默认长度（像素）。</summary>
    public const float DefaultArmLength = 9f;

    /// <summary>十字线默认间隙（像素）。</summary>
    public const float DefaultGap = 4f;

    /// <summary>线条默认粗细（像素）。</summary>
    public const float DefaultThickness = 2f;

    /// <summary>默认颜色 R（0-255，约等于 0.88 * 255）。</summary>
    public const int DefaultColorR = 224;

    /// <summary>默认颜色 G（0-255，约等于 0.96 * 255）。</summary>
    public const int DefaultColorG = 245;

    /// <summary>默认颜色 B（0-255）。</summary>
    public const int DefaultColorB = 255;

    /// <summary>默认是否显示中心点。</summary>
    public const bool DefaultShowDot = true;

    /// <summary>默认中心点半径（像素）。</summary>
    public const float DefaultDotRadius = 1.5f;

    // ---- 当前设置 ----

    /// <summary>十字线长度（像素，0-64）。</summary>
    public static float ArmLength { get; set; } = DefaultArmLength;

    /// <summary>十字线中心间隙（像素，0-32）。</summary>
    public static float Gap { get; set; } = DefaultGap;

    /// <summary>线条粗细（像素，0.5-10）。</summary>
    public static float Thickness { get; set; } = DefaultThickness;

    /// <summary>颜色 R（0-255）。</summary>
    public static int ColorR { get; set; } = DefaultColorR;

    /// <summary>颜色 G（0-255）。</summary>
    public static int ColorG { get; set; } = DefaultColorG;

    /// <summary>颜色 B（0-255）。</summary>
    public static int ColorB { get; set; } = DefaultColorB;

    /// <summary>是否显示准星中心点。</summary>
    public static bool ShowDot { get; set; } = DefaultShowDot;

    /// <summary>中心点半径（像素，ShowDot 为 false 时不绘制）。</summary>
    public static float DotRadius { get; set; } = DefaultDotRadius;

    /// <summary>设置变化时触发；已 Attach 的准星据此重绘。</summary>
    public static event System.Action Changed;

    /// <summary>全局数据是否已被初始化（决定第一个准星是否用自身 Inspector 值播种）。</summary>
    private static bool initialized;

    /// <summary>按 0-255 的 RGB 得到颜色（alpha 固定 1.0）。</summary>
    public static Color GetColor()
    {
        return new Color(ColorR / 255f, ColorG / 255f, ColorB / 255f, 1f);
    }

    /// <summary>准星加入时调用：首次播种 + 把当前设置套用到该准星。</summary>
    public static void Attach(Crosshair crosshair)
    {
        if (crosshair == null)
        {
            return;
        }

        if (!initialized)
        {
            SeedFrom(crosshair);
            initialized = true;
        }

        crosshair.ApplySettingsFromData();
    }

    /// <summary>通知所有准星设置已变化（会顺带把全局数据标记为已初始化）。</summary>
    public static void NotifyChanged()
    {
        initialized = true;
        Changed?.Invoke();
    }

    /// <summary>恢复默认设置（不会自动通知，调用方按需再 NotifyChanged）。</summary>
    public static void ResetToDefaults()
    {
        ArmLength = DefaultArmLength;
        Gap = DefaultGap;
        Thickness = DefaultThickness;
        ColorR = DefaultColorR;
        ColorG = DefaultColorG;
        ColorB = DefaultColorB;
        ShowDot = DefaultShowDot;
        DotRadius = DefaultDotRadius;
        initialized = true;
    }

    /// <summary>用某个准星 Inspector 里的值作为全局初始值（只在第一个准星加入时使用）。</summary>
    private static void SeedFrom(Crosshair crosshair)
    {
        ArmLength = crosshair.ArmLength;
        Gap = crosshair.Gap;
        Thickness = crosshair.Thickness;

        ColorR = Mathf.RoundToInt(crosshair.CrosshairColor.R * 255f);
        ColorG = Mathf.RoundToInt(crosshair.CrosshairColor.G * 255f);
        ColorB = Mathf.RoundToInt(crosshair.CrosshairColor.B * 255f);

        ShowDot = crosshair.DotRadius > 0f;
        if (crosshair.DotRadius > 0f)
        {
            DotRadius = crosshair.DotRadius;
        }
    }
}
