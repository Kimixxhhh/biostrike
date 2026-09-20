using Godot;

/// <summary>
/// 游戏内 HUD（平视信息层）—— 左下角血量、右下角弹药 / 备弹、底部居中击杀数。
/// 血量已接全局数据：订阅 EventBus 的 MaxHealthChanged（血条上限）与 HealthChanged（当前血量）两个信号，
/// 由 HealthComponent 发出；
/// 弹药按武器槽位分块：每块弹药 UI 对应 WeaponController 的一个槽位（数量 = AmmoSlotCount，默认 4），
/// 各槽位的武器用 AmmoChanged(槽位, 弹匣内, 备弹) / WeaponSlotNameChanged(槽位, 名) 只更新自己那块，
/// 哪一块显示由 WeaponSlotSelected(槽位) 决定；发出方为 AmmoController（初始化）、FireAutoController（开火）、
/// ReloadController（换弹完成）、WeaponController（装备第一把 / 切枪）；
/// 击杀仍是导出属性，可用 SetKills 直接改，以后接数据时同理转发。
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

	/// <summary>弹药 UI 的块数 = 武器槽位数（与 WeaponController.WeaponSlotsAmount 对应）。</summary>
	[Export] public int AmmoSlotCount { get; set; } = 4;

	/// <summary>当前显示哪一块弹药 UI（槽位号；-1 = 都不显示）。会由 WeaponSlotSelected 信号覆盖。</summary>
	[Export] public int SelectedAmmoSlot { get; set; } = 0;

	/// <summary>各槽位弹匣内子弹数的初始值（收到 AmmoChanged 后按槽位覆盖）。</summary>
	[Export] public int MagazineRounds { get; set; } = 30;

	/// <summary>各槽位备弹数的初始值（收到 AmmoChanged 后按槽位覆盖）。</summary>
	[Export] public int ReserveRounds { get; set; } = 120;

	/// <summary>击杀数（底部居中）。</summary>
	[Export] public int Kills { get; set; } = 0;

	/// <summary>各槽位武器名的初始值（收到 WeaponSlotNameChanged 后按槽位覆盖）。</summary>
	[Export] public string WeaponName { get; set; } = "AR-15";

	// 配色（与开始菜单 / 准星设置一致的琥珀色风格）
	private static readonly Color Amber = new Color(1f, 0.84f, 0.40f);
	private static readonly Color TextDim = new Color(0.70f, 0.74f, 0.82f);
	private static readonly Color HealthBar = new Color(0.80f, 0.18f, 0.18f);

	private PanelContainer healthPanel;
	private PanelContainer killsPanel;

	/// <summary>各槽位弹药块共用的容器（只显示选中槽位那一块）。</summary>
	private VBoxContainer ammoStack;

	// 每个武器槽位一套弹药 UI（下标 = 槽位号）
	private PanelContainer[] ammoPanels;
	private Label[] weaponLabels;
	private Label[] magazineLabels;
	private Label[] reserveLabels;

	// 各槽位缓存的数值 / 武器名（与控件分开存，没显示的那几块也能先收下数据）
	private int[] slotMagazine;
	private int[] slotReserve;
	private string[] slotWeaponName;

	private ProgressBar healthBar;
	private Label healthLabel;

	private Label killsLabel;

	/// <summary>是否已监听过视口尺寸变化（父级不是 Control 时才需要）。</summary>
	private bool viewportHooked;

	/// <summary>是否已订阅 EventBus 的全局信号（防重复订阅 / 重复退订）。</summary>
	private bool eventBusHooked;

	public override void _Ready()
	{
		// HUD 不拦截鼠标：所有点击都照常传给游戏
		MouseFilter = MouseFilterEnum.Ignore;

		SyncSizeToParent();
		BuildUi();
		RefreshDisplay();
		SubscribeToEventBus();

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
		if (eventBusHooked && EventBus.Instance != null)
		{
			EventBus.Instance.HealthChanged -= OnHealthChanged;
			EventBus.Instance.MaxHealthChanged -= OnMaxHealthChanged;
			EventBus.Instance.AmmoChanged -= OnAmmoChanged;
			EventBus.Instance.WeaponSlotSelected -= OnWeaponSlotSelected;
			EventBus.Instance.WeaponSlotNameChanged -= OnWeaponSlotNameChanged;
			eventBusHooked = false;
		}

		if (viewportHooked && GetViewport() != null)
		{
			GetViewport().SizeChanged -= OnViewportSizeChanged;
			viewportHooked = false;
		}
	}

	// ------------------------------------------------------------------ 全局信号（EventBus）

	/// <summary>
	/// 订阅 EventBus 的全局信号：血条跟随 HealthComponent，弹药跟随各武器槽位的 AmmoController / WeaponController。
	/// EventBus 是 autoload，HUD 的 _Ready 时其实例已经就绪。
	/// </summary>
	private void SubscribeToEventBus()
	{
		EventBus bus = EventBus.Instance;
		if (bus == null)
		{
			GD.PushWarning("HUD: 找不到 EventBus 自动加载实例，血条 / 弹药不会跟随全局信号刷新。");
			return;
		}

		if (!eventBusHooked)
		{
			bus.MaxHealthChanged += OnMaxHealthChanged;       // 血量上限 → 血条最大值
			bus.HealthChanged += OnHealthChanged;             // 当前血量 → 血条当前值
			bus.AmmoChanged += OnAmmoChanged;                 // 槽位弹药 → 对应那块弹药 UI
			bus.WeaponSlotSelected += OnWeaponSlotSelected;   // 选中槽位 → 显示哪一块弹药 UI
			bus.WeaponSlotNameChanged += OnWeaponSlotNameChanged; // 槽位武器名
			eventBusHooked = true;
		}
	}

	/// <summary>EventBus.MaxHealthChanged 的回调：设置血条上限（当前血量同时截到上限内）。</summary>
	private void OnMaxHealthChanged(float newMaxHealth)
	{
		int max = Mathf.Max(1, Mathf.RoundToInt(newMaxHealth));
		SetHealth(Mathf.Min(Health, max), max);
	}

	/// <summary>
	/// EventBus.HealthChanged 的回调。上限正常由 MaxHealthChanged 单独给（HealthComponent 会先发上限再发当前值）；
	/// 万一只收到血量没收到上限，就把上限抬到不低于当前值，避免血条被截断。
	/// </summary>
	private void OnHealthChanged(float newHealth)
	{
		int value = Mathf.Max(0, Mathf.RoundToInt(newHealth));
		int max = Mathf.Max(Mathf.Max(MaxHealth, value), 1);
		SetHealth(value, max);
	}

	/// <summary>
	/// EventBus.AmmoChanged 的回调：某槽位的 AmmoController 初始化 / 开火，以及 ReloadController 换弹完成时发出。
	/// 只刷新该槽位自己的那块弹药 UI（没显示的那几块先存着）。
	/// </summary>
	private void OnAmmoChanged(int weaponSlot, int magazineRounds, int totalRounds)
	{
		SetSlotAmmo(weaponSlot, magazineRounds, totalRounds);
	}

	/// <summary>EventBus.WeaponSlotSelected 的回调：切换显示哪一块弹药 UI（-1 = 全部隐藏）。</summary>
	private void OnWeaponSlotSelected(int weaponSlot)
	{
		SetSelectedAmmoSlot(weaponSlot);
	}

	/// <summary>EventBus.WeaponSlotNameChanged 的回调：刷新某槽位弹药块上方的武器名。</summary>
	private void OnWeaponSlotNameChanged(int weaponSlot, string weaponName)
	{
		SetSlotWeaponName(weaponSlot, weaponName);
	}

	// ------------------------------------------------------------------ 对外接口

	/// <summary>设置血量并立即刷新（也可由外部在收到数据后直接调用）。</summary>
	public void SetHealth(int current, int max)
	{
		Health = Mathf.Max(0, current);
		MaxHealth = Mathf.Max(1, max);
		RefreshDisplay();
	}

	/// <summary>设置某个武器槽位的弹药：弹匣内子弹数 + 备弹数。</summary>
	public void SetSlotAmmo(int weaponSlot, int magazine, int reserve)
	{
		if (!IsValidAmmoSlot(weaponSlot))
		{
			return;
		}

		slotMagazine[weaponSlot] = Mathf.Max(0, magazine);
		slotReserve[weaponSlot] = Mathf.Max(0, reserve);
		RefreshAmmoSlot(weaponSlot);

		if (weaponSlot == SelectedAmmoSlot)
		{
			LayoutBlocks(); // 数字宽度变了，重新贴到屏幕右下角
		}
	}

	/// <summary>切换显示哪一块弹药 UI（槽位号；-1 = 都不显示）。</summary>
	public void SetSelectedAmmoSlot(int weaponSlot)
	{
		SelectedAmmoSlot = weaponSlot;
		ApplyAmmoSlotVisibility();
		LayoutBlocks();
	}

	/// <summary>设置击杀数。</summary>
	public void SetKills(int count)
	{
		Kills = Mathf.Max(0, count);
		RefreshDisplay();
	}

	/// <summary>设置某个武器槽位显示的武器名。</summary>
	public void SetSlotWeaponName(int weaponSlot, string name)
	{
		if (!IsValidAmmoSlot(weaponSlot))
		{
			return;
		}

		slotWeaponName[weaponSlot] = name ?? string.Empty;
		RefreshAmmoSlot(weaponSlot);
	}

	/// <summary>槽位号是否对应一块存在的弹药 UI（字段没建好 / 越界 / EventBus.NoWeaponSlot 都算无效）。</summary>
	private bool IsValidAmmoSlot(int weaponSlot)
	{
		return magazineLabels != null && weaponSlot >= 0 && weaponSlot < magazineLabels.Length;
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

	/// <summary>右下角：每个武器槽位一块「武器名 + 大号弹匣数 + 斜杠备弹数」，同一时刻只显示选中槽位那一块。</summary>
	private void BuildAmmoBlock()
	{
		// 四块弹药 UI 叠在同一个 VBox 里：隐藏的块不占位置，容器尺寸 = 当前显示那块
		ammoStack = new VBoxContainer();
		ammoStack.Alignment = BoxContainer.AlignmentMode.End;
		ammoStack.MouseFilter = MouseFilterEnum.Ignore;
		ammoStack.AddThemeConstantOverride("separation", 0);
		AddChild(ammoStack);

		int slotCount = Mathf.Max(1, AmmoSlotCount);
		ammoPanels = new PanelContainer[slotCount];
		weaponLabels = new Label[slotCount];
		magazineLabels = new Label[slotCount];
		reserveLabels = new Label[slotCount];
		slotMagazine = new int[slotCount];
		slotReserve = new int[slotCount];
		slotWeaponName = new string[slotCount];

		for (int i = 0; i < slotCount; i++)
		{
			// 导出属性作为初始值：武器接上后会用自己的 AmmoChanged / WeaponSlotNameChanged 覆盖
			slotMagazine[i] = Mathf.Max(0, MagazineRounds);
			slotReserve[i] = Mathf.Max(0, ReserveRounds);
			slotWeaponName[i] = WeaponName ?? string.Empty;
			BuildAmmoSlot(i);
		}

		ApplyAmmoSlotVisibility();
	}

	/// <summary>构建单个槽位的弹药块（挂在 ammoStack 下）。</summary>
	private void BuildAmmoSlot(int weaponSlot)
	{
		PanelContainer panel = BuildBlockContainer();
		ammoStack.AddChild(panel);
		ammoPanels[weaponSlot] = panel;

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 0);
		box.Alignment = BoxContainer.AlignmentMode.End; // 内容整体靠右
		panel.AddChild(box);

		var weaponLabel = new Label();
		weaponLabel.HorizontalAlignment = HorizontalAlignment.Right;
		weaponLabel.AddThemeFontSizeOverride("font_size", 13);
		weaponLabel.AddThemeColorOverride("font_color", TextDim);
		box.AddChild(weaponLabel);
		weaponLabels[weaponSlot] = weaponLabel;

		var row = new HBoxContainer();
		row.Alignment = BoxContainer.AlignmentMode.End;
		row.AddThemeConstantOverride("separation", 6);
		box.AddChild(row);

		var magazineLabel = new Label();
		magazineLabel.CustomMinimumSize = new Vector2(62f, 0f);
		magazineLabel.HorizontalAlignment = HorizontalAlignment.Right;
		magazineLabel.VerticalAlignment = VerticalAlignment.Bottom;
		magazineLabel.AddThemeFontSizeOverride("font_size", 38);
		magazineLabel.AddThemeColorOverride("font_color", Amber);
		row.AddChild(magazineLabel);
		magazineLabels[weaponSlot] = magazineLabel;

		var reserveLabel = new Label();
		reserveLabel.CustomMinimumSize = new Vector2(64f, 0f);
		reserveLabel.VerticalAlignment = VerticalAlignment.Bottom;
		reserveLabel.AddThemeFontSizeOverride("font_size", 20);
		reserveLabel.AddThemeColorOverride("font_color", TextDim);
		row.AddChild(reserveLabel);
		reserveLabels[weaponSlot] = reserveLabel;
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

	/// <summary>把缓存的数值刷到画面上（以后接真实数据时改这里即可）。</summary>
	public void RefreshDisplay()
	{
		int maxHealth = Mathf.Max(MaxHealth, 1);
		int currentHealth = Mathf.Clamp(Health, 0, maxHealth);

		healthBar.MaxValue = maxHealth;
		healthBar.Value = currentHealth;
		healthLabel.Text = $"{currentHealth} / {maxHealth}";

		for (int i = 0; i < magazineLabels.Length; i++)
		{
			RefreshAmmoSlot(i);
		}

		killsLabel.Text = Kills.ToString();

		LayoutBlocks();
	}

	/// <summary>把某个槽位缓存的弹药数值 / 武器名刷到它那块控件上。</summary>
	private void RefreshAmmoSlot(int weaponSlot)
	{
		if (!IsValidAmmoSlot(weaponSlot))
		{
			return;
		}

		weaponLabels[weaponSlot].Text = slotWeaponName[weaponSlot];
		magazineLabels[weaponSlot].Text = slotMagazine[weaponSlot].ToString();
		reserveLabels[weaponSlot].Text = $"/ {slotReserve[weaponSlot]}";
	}

	/// <summary>只让当前选中槽位的弹药块可见（SelectedAmmoSlot 为 -1 / 越界时四块全隐藏）。</summary>
	private void ApplyAmmoSlotVisibility()
	{
		if (ammoPanels == null)
		{
			return;
		}

		for (int i = 0; i < ammoPanels.Length; i++)
		{
			ammoPanels[i].Visible = i == SelectedAmmoSlot;
		}
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

		Vector2 ammoSize = ammoStack.GetCombinedMinimumSize();
		ammoStack.Size = ammoSize;
		ammoStack.Position = new Vector2(area.X - ammoSize.X - margin, area.Y - ammoSize.Y - margin);

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
