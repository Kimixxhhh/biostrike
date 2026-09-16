using Godot;
using System.Collections.Generic;

/// <summary>
/// 装备（Loadout）UI —— 读取 res://data/weaponData/ 下所有 .tres 武器数据，
/// 每个槽位显示武器的图标、名称与价格。横向 5 列，行数随数据数量自动扩展。
/// 界面会自动铺满屏幕并居中显示。
/// </summary>
public partial class LoadoutUI : Control
{
	/// <summary>点击某个武器槽位时触发，参数为该槽位对应的武器数据。</summary>
	[Signal]
	public delegate void WeaponSelectedEventHandler(WeaponData weapon);

	[Export] public int Columns { get; set; } = 10;
	[Export] public Vector2 SlotSize { get; set; } = new Vector2(80, 63);
	[Export] public int SlotSpacing { get; set; } = 10;
	[Export] public string UiTitle { get; set; } = "装备";
	[Export] public string WeaponDataDir { get; set; } = "res://data/weaponData/";
	[Export] public float PanelWidthRatio { get; set; } = 0.8f;
	[Export] public float PanelHeightRatio { get; set; } = 0.3f;

	/// <summary>为 true 时面板贴住父容器底边（水平仍居中）；false 时居中显示。</summary>
	[Export] public bool AlignToBottom { get; set; } = false;

	private readonly List<WeaponData> weapons = new List<WeaponData>();
	private readonly List<Control> slotWidgets = new List<Control>();
	private OptionButton filterDropdown;
	private StyleBoxFlat slotNormalBox;
	private StyleBoxFlat slotHoverBox;
	private PanelContainer bgPanel;

	/// <summary>当前加载的全部武器数据（与槽位顺序一致）。</summary>
	public IReadOnlyList<WeaponData> Weapons => weapons;

	public override void _Ready()
	{
		GD.Print("[LoadoutUI] _Ready start");
		BuildStyleBoxes();
		BuildUi();
		CenterUiOnScreen();
		ResizePanelToViewport();
		GD.Print($"[LoadoutUI] _Ready done, weapons loaded = {weapons.Count}");
	}

	private void BuildStyleBoxes()
	{
		slotNormalBox = new StyleBoxFlat();
		slotNormalBox.BgColor = new Color(0.13f, 0.14f, 0.18f, 0.92f);
		slotNormalBox.BorderColor = new Color(0.42f, 0.44f, 0.52f, 1f);
		slotNormalBox.SetBorderWidthAll(2);
		slotNormalBox.SetCornerRadiusAll(8);

		slotHoverBox = (StyleBoxFlat)slotNormalBox.Duplicate();
		slotHoverBox.BgColor = new Color(0.20f, 0.34f, 0.48f, 0.95f);
		slotHoverBox.BorderColor = new Color(0.42f, 0.82f, 1f, 1f);
	}

	private void BuildUi()
	{
		// 面板的父容器：铺满整个控件。
		// 面板位置由 ResizePanelToViewport 按 AlignToBottom 决定（默认居中，或贴底）。
		var panelHost = new Control();
		panelHost.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(panelHost);

		// 背景面板（尺寸由 ResizePanelToViewport 固定为屏幕的 80% x 30%）
		bgPanel = new PanelContainer();
		var bgBox = new StyleBoxFlat();
		bgBox.BgColor = new Color(0.08f, 0.09f, 0.12f, 0.9f);
		bgBox.SetCornerRadiusAll(16);
		bgBox.ContentMarginLeft = 16;
		bgBox.ContentMarginRight = 16;
		bgBox.ContentMarginTop = 12;
		bgBox.ContentMarginBottom = 16;
		bgPanel.AddThemeStyleboxOverride("panel", bgBox);
		panelHost.AddChild(bgPanel);

		// 标题 + 网格的纵向布局
		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 8);
		bgPanel.AddChild(vbox);

		// 顶部标题栏：标题靠左，筛选下拉框靠右
		var header = new HBoxContainer();
		vbox.AddChild(header);

		// 标题：位于背景左上角，字号缩小，使布局更紧凑
		var title = new Label();
		title.Text = UiTitle;
		title.HorizontalAlignment = HorizontalAlignment.Left;
		title.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		title.AddThemeFontSizeOverride("font_size", 10);
		title.AddThemeColorOverride("font_color", new Color(0.85f, 0.88f, 0.95f));
		header.AddChild(title);

		// 占位空白：把下拉框推到最右侧
		var headerSpacer = new Control();
		headerSpacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		header.AddChild(headerSpacer);

		// 筛选下拉框：全部 / 主武器 / 副武器 / 近战武器 / 投掷物
		filterDropdown = new OptionButton();
		filterDropdown.AddItem("全部", 0);
		filterDropdown.AddItem("主武器", 1);
		filterDropdown.AddItem("副武器", 2);
		filterDropdown.AddItem("近战武器", 3);
		filterDropdown.AddItem("投掷物", 4);
		filterDropdown.Selected = 0;
		filterDropdown.ItemSelected += OnFilterSelected;
		header.AddChild(filterDropdown);

		// 5 列网格：放入 ScrollContainer，当武器槽数量超过面板显示范围时可纵向滚动。
		// 关键：ScrollContainer 必须 SizeFlagsVertical = ExpandFill 占满 VBox 剩余高度，
		// 否则其最小尺寸为 0，会被压缩到不可见（网格也随之看不见）。
		var scroll = new ScrollContainer();
		scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
		scroll.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
		scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
		vbox.AddChild(scroll);

		var grid = new GridContainer();
		grid.Columns = Columns;
		grid.AddThemeConstantOverride("h_separation", SlotSpacing);
		grid.AddThemeConstantOverride("v_separation", SlotSpacing);
		scroll.AddChild(grid);

		LoadWeaponData(grid);
	}

	/// <summary>扫描武器数据目录，为每个 .tres 生成一个槽位。</summary>
	private void LoadWeaponData(GridContainer grid)
	{
		var dir = DirAccess.Open(WeaponDataDir);
		if (dir == null)
		{
			GD.PushError($"无法打开武器数据目录：{WeaponDataDir}");
			return;
		}

		var fileNames = new List<string>();
		dir.ListDirBegin();
		while (true)
		{
			var fileName = dir.GetNext();
			if (string.IsNullOrEmpty(fileName))
				break;
			if (!dir.CurrentIsDir() && fileName.EndsWith(".tres"))
			{
				fileNames.Add(fileName);
			}
		}
		dir.ListDirEnd();
		fileNames.Sort();
		GD.Print($"[LoadoutUI] found {fileNames.Count} .tres in {WeaponDataDir}");

		foreach (var fileName in fileNames)
		{
			var res = ResourceLoader.Load(WeaponDataDir + fileName);
			if (res is WeaponData weapon)
			{
				weapons.Add(weapon);
				CreateWeaponSlot(grid, weapon);
				GD.Print($"[LoadoutUI] loaded weapon: {weapon.WeaponName}, price={weapon.Price}");
			}
			else
			{
				GD.PushError($"[LoadoutUI] 无法将 {fileName} 加载为 WeaponData，实际类型：{res?.GetType().Name ?? "null"}");
			}
		}
	}

	private void CreateWeaponSlot(GridContainer grid, WeaponData weapon)
	{
		var slot = new Panel();
		slot.CustomMinimumSize = SlotSize;
		slot.MouseFilter = MouseFilterEnum.Stop;
		slot.AddThemeStyleboxOverride("panel", slotNormalBox);

		// 武器图标：铺满槽位并居中（带 8px 内边距）。
		// 图标仅作装饰，不参与鼠标事件，保证槽位 Panel 能稳定收到 MouseEntered/MouseExited。
		var icon = new TextureRect();
		icon.MouseFilter = MouseFilterEnum.Ignore;
		icon.Texture = weapon.WeaponIcon;
		icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		icon.SetAnchorsPreset(LayoutPreset.FullRect);
		icon.OffsetLeft = 4;
		icon.OffsetTop = 4;
		icon.OffsetRight = -4;
		icon.OffsetBottom = -4;
		slot.AddChild(icon);

		// 武器名称：右上角
		var nameLabel = new Label();
		nameLabel.Text = weapon.WeaponName;
		nameLabel.HorizontalAlignment = HorizontalAlignment.Right;
		nameLabel.SetAnchorsPreset(LayoutPreset.TopRight);
		nameLabel.OffsetLeft = -58;
		nameLabel.OffsetTop = 2;
		nameLabel.OffsetRight = -5;
		nameLabel.OffsetBottom = 17;
		nameLabel.GrowHorizontal = GrowDirection.Begin;
		nameLabel.AddThemeFontSizeOverride("font_size", 11);
		nameLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.88f, 0.95f));
		nameLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.85f));
		nameLabel.AddThemeConstantOverride("outline_size", 4);
		slot.AddChild(nameLabel);

		// 价格：右下角，用美元符号
		var priceLabel = new Label();
		priceLabel.Text = $"${weapon.Price}";
		priceLabel.HorizontalAlignment = HorizontalAlignment.Right;
		priceLabel.SetAnchorsPreset(LayoutPreset.BottomRight);
		priceLabel.OffsetLeft = -48;
		priceLabel.OffsetTop = -19;
		priceLabel.OffsetRight = -5;
		priceLabel.OffsetBottom = -4;
		priceLabel.GrowHorizontal = GrowDirection.Begin;
		priceLabel.GrowVertical = GrowDirection.Begin;
		priceLabel.AddThemeFontSizeOverride("font_size", 11);
		priceLabel.AddThemeColorOverride("font_color", new Color(1f, 0.84f, 0.2f));
		priceLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.85f));
		priceLabel.AddThemeConstantOverride("outline_size", 4);
		slot.AddChild(priceLabel);

		// 悬停高亮
		slot.MouseEntered += () => slot.AddThemeStyleboxOverride("panel", slotHoverBox);
		slot.MouseExited += () => slot.AddThemeStyleboxOverride("panel", slotNormalBox);

		// 点击信号
		slot.GuiInput += (ev) =>
		{
			if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
			{
				EmitSignal(SignalName.WeaponSelected, weapon);
			}
		};

		slotWidgets.Add(slot);
		grid.AddChild(slot);
	}

	/// <summary>下拉框选项变化时筛选武器槽位。</summary>
	private void OnFilterSelected(long index)
	{
		ApplyFilter((int)index);
	}

	/// <summary>
	/// 按下拉框选中项显示/隐藏武器槽位。
	/// index 0 = 全部；其余依次对应 LoadoutType 0(主武器)、1(副武器)、2(近战)、3(投掷物)。
	/// </summary>
	private void ApplyFilter(int index)
	{
		int targetType = index - 1; // 1→0, 2→1, 3→2, 4→3
		for (int i = 0; i < weapons.Count && i < slotWidgets.Count; i++)
		{
			bool visible = index == 0 || weapons[i].LoadoutType == targetType;
			slotWidgets[i].Visible = visible;
		}
	}

	/// <summary>
	/// 让 UI 始终铺满屏幕并居中。
	/// 当父节点不是 Control（例如 test.scene 中直接挂在 Node2D 下）时，
	/// 锚点布局不会自动生效，需要基于视口尺寸手动定位。
	/// </summary>
	private void CenterUiOnScreen()
	{
		if (GetParent() is Control)
		{
			// 父节点是 Control：使用锚点铺满父节点即可
			SetAnchorsPreset(LayoutPreset.FullRect);
		}
		else
		{
			// 父节点非 Control：基于视口尺寸手动铺满，保证面板居中
			SetAnchorsPreset(LayoutPreset.TopLeft);
			Position = Vector2.Zero;
			Size = GetViewportRect().Size;
			GetViewport().SizeChanged += OnViewportSizeChanged;
		}
	}

	/// <summary>背景面板：宽度固定为屏幕的 80%，高度固定为屏幕的 30%。默认居中，AlignToBottom 时贴底。</summary>
	private void ResizePanelToViewport()
	{
		var vsize = GetViewportRect().Size;
		var panelSize = new Vector2(vsize.X * PanelWidthRatio, vsize.Y * PanelHeightRatio);
		bgPanel.CustomMinimumSize = panelSize;
		bgPanel.CustomMaximumSize = panelSize;

		if (AlignToBottom)
		{
			// 贴底：面板底边贴着父容器底边，水平居中
			bgPanel.SetAnchorsPreset(LayoutPreset.CenterBottom);
			bgPanel.OffsetLeft = -panelSize.X / 2f;
			bgPanel.OffsetRight = panelSize.X / 2f;
			bgPanel.OffsetTop = -panelSize.Y;
			bgPanel.OffsetBottom = 0f;
		}
		else
		{
			// 居中：面板在父容器内水平垂直居中
			bgPanel.SetAnchorsPreset(LayoutPreset.Center);
			bgPanel.OffsetLeft = -panelSize.X / 2f;
			bgPanel.OffsetRight = panelSize.X / 2f;
			bgPanel.OffsetTop = -panelSize.Y / 2f;
			bgPanel.OffsetBottom = panelSize.Y / 2f;
		}
		// 注意：面板高度被锁死为屏幕的 30%，因此内容（标题 + 网格）的总高度必须小于
		// 面板内容区高度，否则 GridContainer 会被压缩，悬停切换样式触发重排时
		// 槽位高度会跳变且无法恢复。
		// 当前槽位尺寸（80×63）在现有武器数量下可正常容纳；若武器增多导致内容超高，
		// 需要再缩小槽位/间距，或提高该比例。
	}

	private void OnViewportSizeChanged()
	{
		if (GetParent() is not Control)
		{
			Size = GetViewportRect().Size;
		}
		ResizePanelToViewport();
	}
}
