using Godot;
using System.Collections.Generic;

/// <summary>
/// 5x5 背包槽位 UI —— 横向5列、纵向5行，共25个槽位。
/// 运行时会自动生成槽位网格，支持悬停高亮与点击信号。
/// 可通过 [Export] 属性在编辑器/代码中调整行列数与槽位尺寸。
/// 界面会自动铺满屏幕并居中显示，右下角显示玩家金钱数（PlayerMoney）。
/// </summary>
public partial class SlotGridUI : Control
{
	/// <summary>点击某个槽位时触发，参数为槽位索引（0 ~ 24，行优先）。</summary>
	[Signal]
	public delegate void SlotClickedEventHandler(int index);

	[Export] public int Columns { get; set; } = 5;
	[Export] public int Rows { get; set; } = 5;
	[Export] public Vector2 SlotSize { get; set; } = new Vector2(120, 80);
	[Export] public int SlotSpacing { get; set; } = 8;
	[Export] public string GridTitle { get; set; } = "背包";

	private int playerMoney = 0;

	/// <summary>玩家金钱数，显示在界面右下角。</summary>
	[Export]
	public int PlayerMoney
	{
		get => playerMoney;
		set
		{
			playerMoney = value;
			UpdateMoneyLabel();
		}
	}

	private readonly List<Panel> slots = new List<Panel>();
	private StyleBoxFlat slotNormalBox;
	private StyleBoxFlat slotHoverBox;
	private Label moneyLabel;

	/// <summary>所有槽位（行优先，索引 0..24）。</summary>
	public IReadOnlyList<Panel> Slots => slots;

	public override void _Ready()
	{
		BuildStyleBoxes();
		BuildUi();
		CenterUiOnScreen();
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
		// 屏幕居中容器
		var center = new CenterContainer();
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(center);

		// 背景面板
		var bgPanel = new PanelContainer();
		var bgBox = new StyleBoxFlat();
		bgBox.BgColor = new Color(0.08f, 0.09f, 0.12f, 0.85f);
		bgBox.SetCornerRadiusAll(16);
		bgBox.ContentMarginLeft = 24;
		bgBox.ContentMarginRight = 24;
		bgBox.ContentMarginTop = 20;
		bgBox.ContentMarginBottom = 24;
		bgPanel.AddThemeStyleboxOverride("panel", bgBox);
		center.AddChild(bgPanel);

		// 标题 + 网格的纵向布局
		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 16);
		bgPanel.AddChild(vbox);

		var title = new Label();
		title.Text = GridTitle;
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.AddThemeFontSizeOverride("font_size", 26);
		title.AddThemeColorOverride("font_color", new Color(0.85f, 0.88f, 0.95f));
		vbox.AddChild(title);

		// 5 列网格容器
		var grid = new GridContainer();
		grid.Columns = Columns;
		grid.AddThemeConstantOverride("h_separation", SlotSpacing);
		grid.AddThemeConstantOverride("v_separation", SlotSpacing);
		vbox.AddChild(grid);

		// 生成 5x5 = 25 个槽位
		for (int i = 0; i < Columns * Rows; i++)
		{
			var slot = CreateSlot(i);
			grid.AddChild(slot);
			slots.Add(slot);
		}

		BuildMoneyLabel();
	}

	/// <summary>在界面右下角创建金钱显示标签。</summary>
	private void BuildMoneyLabel()
	{
		var moneyContainer = new MarginContainer();
		moneyContainer.SetAnchorsPreset(LayoutPreset.BottomRight);
		moneyContainer.OffsetLeft = -260;
		moneyContainer.OffsetTop = -64;
		moneyContainer.OffsetRight = -24;
		moneyContainer.OffsetBottom = -24;
		moneyContainer.GrowHorizontal = GrowDirection.Begin;
		moneyContainer.GrowVertical = GrowDirection.Begin;
		AddChild(moneyContainer);

		moneyLabel = new Label();
		moneyLabel.HorizontalAlignment = HorizontalAlignment.Right;
		moneyLabel.AddThemeFontSizeOverride("font_size", 24);
		moneyLabel.AddThemeColorOverride("font_color", new Color(1f, 0.84f, 0.2f));
		moneyContainer.AddChild(moneyLabel);
		UpdateMoneyLabel();
	}

	private void UpdateMoneyLabel()
	{
		if (moneyLabel != null)
		{
			moneyLabel.Text = $"金钱：{playerMoney}";
		}
	}

	private Panel CreateSlot(int index)
	{
		var slot = new Panel();
		slot.CustomMinimumSize = SlotSize;
		slot.MouseFilter = MouseFilterEnum.Stop;
		slot.AddThemeStyleboxOverride("panel", slotNormalBox);

		var label = new Label();
		label.Text = (index + 1).ToString();
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.AddThemeFontSizeOverride("font_size", 20);
		label.AddThemeColorOverride("font_color", new Color(0.7f, 0.74f, 0.82f));
		label.SetAnchorsPreset(LayoutPreset.FullRect);
		label.GrowHorizontal = GrowDirection.Both;
		label.GrowVertical = GrowDirection.Both;
		slot.AddChild(label);

		// 悬停高亮
		slot.MouseEntered += () => OnSlotHovered(slot, true);
		slot.MouseExited += () => OnSlotHovered(slot, false);

		// 点击信号
		slot.GuiInput += (ev) =>
		{
			if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
			{
				EmitSignal(SignalName.SlotClicked, index);
			}
		};

		return slot;
	}

	private void OnSlotHovered(Panel slot, bool hovered)
	{
		slot.AddThemeStyleboxOverride("panel", hovered ? slotHoverBox : slotNormalBox);
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

	private void OnViewportSizeChanged()
	{
		if (GetParent() is not Control)
		{
			Size = GetViewportRect().Size;
		}
	}
}
