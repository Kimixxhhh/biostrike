using Godot;

/// <summary>
/// 游戏内 HUD（平视信息层）—— 左下角血量、右下角弹药 / 备弹、底部居中击杀数。
/// 目前只做画面效果：数值来自导出属性，可用 SetHealth / SetAmmo / SetKills / SetWeaponName 直接改；
/// 尚未与玩家真实数据绑定，以后接数据时只需把调用处换成从玩家 / 武器组件读值（或在这些方法里转发）。
/// 用法：把 scenes/UI/HUD.tscn 实例化到任意 CanvasLayer（例如 test.tscn 里的 HUD）下。
/// 独立预览：test/testHUD.tscn。
/// </summary>
public partial class HUD : Control
{
	/// <summary>信息块与屏幕边缘的距离（像素）。</summary>
	[Export] public float ScreenMargin { get; set; } = 36f;

	/// <summary>当前血量。</summary>
	[Export] public int Health { get; set; } = 100;

	/// <summary>最大血量。</summary>
	[Export] public int MaxHealth { get; set; } = 100;

	/// <summary>当前弹匣内子弹数（右下角大号数字）。</summary>
	[Export] public int MagazineRounds { get; set; } = 30;

	/// <summary>备弹数（右下角斜杠后的小号数字）。</summary>
	[Export] public int ReserveRounds { get; set; } = 120;

	/// <summary>击杀数（底部居中）。</summary>
	[Export] public int Kills { get; set; } = 0;

	/// <summary>右下角显示的武器名。</summary>
	[Export] public string WeaponName { get; set; } = "AR-15";

	// 配色（与开始菜单 / 准星设置一致的琥珀色风格）
	private static readonly Color Amber = new Color(1f, 0.84f, 0.40f);
	private static readonly Color TextDim = new Color(0.70f, 0.74f, 0.82f);
	private static readonly Color HealthBar = new Color(0.80f, 0.18f, 0.18f);

	private PanelContainer healthPanel;
	private PanelContainer ammoPanel;
	private PanelContainer killsPanel;

	private ProgressBar healthBar;
	private Label healthLabel;

	private Label weaponLabel;
	private Label magazineLabel;
	private Label reserveLabel;

	private Label killsLabel;

	/// <summary>是否已监听过视口尺寸变化（父级不是 Control 时才需要）。</summary>
	private bool viewportHooked;

	public override void _Ready()
	{
		// HUD 不拦截鼠标：所有点击都照常传给游戏
		MouseFilter = MouseFilterEnum.Ignore;

		SyncSizeToParent();
		BuildUi();
		RefreshDisplay();

		// 屏幕 / 父级尺寸变化时重新把三块信息摆到角上
		Resized += LayoutBlocks;
		if (GetParent() is not Control)
		{
			// 挂在 CanvasLayer / Node2D 下时锚点不生效，窗口尺寸变化要自己跟
			GetViewport().SizeChanged += OnViewportSizeChanged;
			viewportHooked = true;
		}
	}

	public override void _ExitTree()
	{
		if (viewportHooked && GetViewport() != null)
		{
			GetViewport().SizeChanged -= OnViewportSizeChanged;
			viewportHooked = false;
		}
	}

	// ------------------------------------------------------------------ 对外接口

	/// <summary>设置血量（以后接真实数据时调它）。</summary>
	public void SetHealth(int current, int max)
	{
		Health = Mathf.Max(0, current);
		MaxHealth = Mathf.Max(1, max);
		RefreshDisplay();
	}

	/// <summary>设置弹药：弹匣内子弹数 + 备弹数。</summary>
	public void SetAmmo(int magazine, int reserve)
	{
		MagazineRounds = Mathf.Max(0, magazine);
		ReserveRounds = Mathf.Max(0, reserve);
		RefreshDisplay();
	}

	/// <summary>设置击杀数。</summary>
	public void SetKills(int count)
	{
		Kills = Mathf.Max(0, count);
		RefreshDisplay();
	}

	/// <summary>设置右下角武器名。</summary>
	public void SetWeaponName(string name)
	{
		WeaponName = name ?? string.Empty;
		RefreshDisplay();
	}

	// ------------------------------------------------------------------ UI 构建

	private void BuildUi()
	{
		BuildHealthBlock();
		BuildAmmoBlock();
		BuildKillsBlock();
	}

	/// <summary>左下角：生命值标题 + 血条（红色，数值直接叠在血条上，省一行空间）。</summary>
	private void BuildHealthBlock()
	{
		healthPanel = BuildBlockContainer();
		AddChild(healthPanel);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 2);
		healthPanel.AddChild(box);

		box.AddChild(BuildCaption("生命值"));

		// 血条与数值叠在同一个 Control 里（数值后加 → 画在血条之上）
		var stack = new Control();
		stack.CustomMinimumSize = new Vector2(220f, 26f);
		stack.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		stack.MouseFilter = MouseFilterEnum.Ignore;
		box.AddChild(stack);

		healthBar = new ProgressBar();
		healthBar.MinValue = 0;
		healthBar.MaxValue = MaxHealth;
		healthBar.Value = Health;
		healthBar.ShowPercentage = false; // 不要默认的百分比文字，数值用下面叠着的 Label
		healthBar.MouseFilter = MouseFilterEnum.Ignore;
		healthBar.SetAnchorsPreset(LayoutPreset.FullRect); // 铺满 stack

		var barBg = new StyleBoxFlat();
		barBg.BgColor = new Color(0f, 0f, 0f, 0.55f);
		barBg.SetCornerRadiusAll(6);
		healthBar.AddThemeStyleboxOverride("background", barBg);

		var barFill = new StyleBoxFlat();
		barFill.BgColor = HealthBar;
		barFill.SetCornerRadiusAll(6);
		healthBar.AddThemeStyleboxOverride("fill", barFill);
		stack.AddChild(healthBar);

		healthLabel = new Label();
		healthLabel.HorizontalAlignment = HorizontalAlignment.Center;
		healthLabel.VerticalAlignment = VerticalAlignment.Center;
		healthLabel.MouseFilter = MouseFilterEnum.Ignore;
		healthLabel.SetAnchorsPreset(LayoutPreset.FullRect); // 与血条重叠
		healthLabel.AddThemeFontSizeOverride("font_size", 15);
		healthLabel.AddThemeColorOverride("font_color", new Color(1f, 0.96f, 0.92f));
		healthLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.85f));
		healthLabel.AddThemeConstantOverride("outline_size", 4); // 描边，压在红条上也能看清
		stack.AddChild(healthLabel);
	}

	/// <summary>右下角：武器名 + 大号弹匣数 + 斜杠备弹数。</summary>
	private void BuildAmmoBlock()
	{
		ammoPanel = BuildBlockContainer();
		AddChild(ammoPanel);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 0);
		box.Alignment = BoxContainer.AlignmentMode.End; // 内容整体靠右
		ammoPanel.AddChild(box);

		weaponLabel = new Label();
		weaponLabel.HorizontalAlignment = HorizontalAlignment.Right;
		weaponLabel.AddThemeFontSizeOverride("font_size", 13);
		weaponLabel.AddThemeColorOverride("font_color", TextDim);
		box.AddChild(weaponLabel);

		var row = new HBoxContainer();
		row.Alignment = BoxContainer.AlignmentMode.End;
		row.AddThemeConstantOverride("separation", 6);
		box.AddChild(row);

		magazineLabel = new Label();
		magazineLabel.CustomMinimumSize = new Vector2(62f, 0f);
		magazineLabel.HorizontalAlignment = HorizontalAlignment.Right;
		magazineLabel.VerticalAlignment = VerticalAlignment.Bottom;
		magazineLabel.AddThemeFontSizeOverride("font_size", 38);
		magazineLabel.AddThemeColorOverride("font_color", Amber);
		row.AddChild(magazineLabel);

		reserveLabel = new Label();
		reserveLabel.CustomMinimumSize = new Vector2(64f, 0f);
		reserveLabel.VerticalAlignment = VerticalAlignment.Bottom;
		reserveLabel.AddThemeFontSizeOverride("font_size", 20);
		reserveLabel.AddThemeColorOverride("font_color", TextDim);
		row.AddChild(reserveLabel);
	}

	/// <summary>底部居中：击杀数。</summary>
	private void BuildKillsBlock()
	{
		killsPanel = BuildBlockContainer();
		AddChild(killsPanel);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);
		killsPanel.AddChild(row);

		var caption = BuildCaption("击杀");
		caption.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		row.AddChild(caption);

		killsLabel = new Label();
		killsLabel.CustomMinimumSize = new Vector2(26f, 0f);
		killsLabel.HorizontalAlignment = HorizontalAlignment.Center;
		killsLabel.VerticalAlignment = VerticalAlignment.Center;
		killsLabel.AddThemeFontSizeOverride("font_size", 26);
		killsLabel.AddThemeColorOverride("font_color", Amber);
		row.AddChild(killsLabel);
	}

	/// <summary>信息块容器：无底板、无描边（HUD 直接浮在画面上）。</summary>
	private PanelContainer BuildBlockContainer()
	{
		var panel = new PanelContainer();
		// HUD 是纯展示层：连容器也不拦截鼠标，避免挡住底下的点击 / 拖拽
		panel.MouseFilter = MouseFilterEnum.Ignore;

		var box = new StyleBoxFlat();
		box.BgColor = new Color(0f, 0f, 0f, 0f); // 不要背景色
		box.SetContentMarginAll(0);
		panel.AddThemeStyleboxOverride("panel", box);

		return panel;
	}

	/// <summary>小号灰色标题（"生命值" / "击杀"）。</summary>
	private Label BuildCaption(string text)
	{
		var label = new Label();
		label.Text = text;
		label.AddThemeFontSizeOverride("font_size", 13);
		label.AddThemeColorOverride("font_color", TextDim);
		return label;
	}

	// ------------------------------------------------------------------ 数值 / 布局

	/// <summary>把导出属性里的数值刷到画面上（以后接真实数据时改这里即可）。</summary>
	public void RefreshDisplay()
	{
		int maxHealth = Mathf.Max(MaxHealth, 1);
		int currentHealth = Mathf.Clamp(Health, 0, maxHealth);

		healthBar.MaxValue = maxHealth;
		healthBar.Value = currentHealth;
		healthLabel.Text = $"{currentHealth} / {maxHealth}";

		weaponLabel.Text = WeaponName;
		magazineLabel.Text = MagazineRounds.ToString();
		reserveLabel.Text = $"/ {ReserveRounds}";
		killsLabel.Text = Kills.ToString();

		LayoutBlocks();
	}

	/// <summary>左下血量、右下弹药、底部居中击杀数（父级尺寸变化时重算）。</summary>
	private void LayoutBlocks()
	{
		Vector2 area = Size;
		if (area.X <= 0f || area.Y <= 0f)
		{
			area = GetViewportRect().Size; // 尚未完成布局时退回视口尺寸
		}

		float margin = ScreenMargin;

		Vector2 healthSize = healthPanel.GetCombinedMinimumSize();
		healthPanel.Size = healthSize;
		healthPanel.Position = new Vector2(margin, area.Y - healthSize.Y - margin);

		Vector2 ammoSize = ammoPanel.GetCombinedMinimumSize();
		ammoPanel.Size = ammoSize;
		ammoPanel.Position = new Vector2(area.X - ammoSize.X - margin, area.Y - ammoSize.Y - margin);

		Vector2 killsSize = killsPanel.GetCombinedMinimumSize();
		killsPanel.Size = killsSize;
		killsPanel.Position = new Vector2((area.X - killsSize.X) * 0.5f, area.Y - killsSize.Y - margin);
	}

	/// <summary>
	/// 铺满父级：父节点是 Control 时用锚点全屏；挂在 CanvasLayer / Node2D 下时锚点不生效，
	/// 基于视口尺寸手动铺满（与 Crosshair / CrosshairSetting 的做法一致）。
	/// </summary>
	private void SyncSizeToParent()
	{
		if (GetParent() is Control)
		{
			SetAnchorsPreset(LayoutPreset.FullRect);
		}
		else
		{
			SetAnchorsPreset(LayoutPreset.TopLeft);
			Position = Vector2.Zero;
			Size = GetViewportRect().Size;
		}
	}

	private void OnViewportSizeChanged()
	{
		Size = GetViewportRect().Size;
		LayoutBlocks();
	}
}
