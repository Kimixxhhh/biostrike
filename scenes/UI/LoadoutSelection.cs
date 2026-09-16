using Godot;
using System.Collections.Generic;

/// <summary>
/// 配装选择（LoadoutSelection）UI —— 5列 x 5行 共 25 个槽位。
/// 面板高度固定为屏幕高度的 60%（PanelHeightRatio），宽度默认 50%（PanelWidthRatio）。
/// 槽位尺寸会根据面板大小自动计算，并用 ExpandFill 拉伸填满面板，
/// 保证 5x5 网格完整容纳、不压缩，随窗口大小自适应。
/// 支持点击选中高亮（同一时刻仅一个槽位高亮）与点击信号（SlotClicked）。
/// 槽位可通过 SetSlotWeapon(index, WeaponData) 显示武器数据（图标/名称/价格）；
/// 数据不自动从 res://data/weaponData 加载，由外部按需注入，空槽位显示编号。
/// </summary>
public partial class LoadoutSelection : Control
{
	/// <summary>点击某个槽位时触发，参数为槽位索引（0 ~ 24，行优先，与网格子节点顺序一致）。</summary>
	[Signal]
	public delegate void SlotClickedEventHandler(int index);

	[Export] public int Columns { get; set; } = 5;
	[Export] public int Rows { get; set; } = 5;
	[Export] public int SlotSpacing { get; set; } = 8;
	[Export] public string UiTitle { get; set; } = "配装选择";
	[Export] public float PanelWidthRatio { get; set; } = 0.5f;
	[Export] public float PanelHeightRatio { get; set; } = 0.6f;

	private readonly List<Panel> slots = new List<Panel>();
	private readonly List<TextureRect> slotIcons = new List<TextureRect>();
	private readonly List<Label> slotNameLabels = new List<Label>();
	private readonly List<Label> slotPriceLabels = new List<Label>();
	private readonly List<Label> slotNumberLabels = new List<Label>();
	private readonly List<WeaponData> slotWeapons = new List<WeaponData>();
	private StyleBoxFlat slotNormalBox;
	private StyleBoxFlat slotSelectedBox;
	private PanelContainer bgPanel;
	private OptionButton loadoutDropdown;
	private LineEdit loadoutNameEdit;

	/// <summary>所有槽位（行优先，索引 0..24）。</summary>
	public IReadOnlyList<Panel> Slots => slots;

	/// <summary>当前选中的槽位索引（0..24，行优先），-1 表示未选中。</summary>
	public int SelectedIndex { get; private set; } = -1;

	public override void _Ready()
	{
		BuildStyleBoxes();
		BuildUi();
		CenterUiOnScreen();
		ResizePanelToViewport();
		// 从当前选中配装的 LoadoutSelectionStrings 加载槽位武器数据
		RefreshSlotsFromSelectedLoadout();
	}

	private void BuildStyleBoxes()
	{
		slotNormalBox = new StyleBoxFlat();
		slotNormalBox.BgColor = new Color(0.13f, 0.14f, 0.18f, 0.92f);
		slotNormalBox.BorderColor = new Color(0.42f, 0.44f, 0.52f, 1f);
		slotNormalBox.SetBorderWidthAll(2);
		slotNormalBox.SetCornerRadiusAll(8);

		slotSelectedBox = (StyleBoxFlat)slotNormalBox.Duplicate();
		slotSelectedBox.BgColor = new Color(0.40f, 0.36f, 0.18f, 0.95f);
		slotSelectedBox.BorderColor = new Color(1f, 0.84f, 0.40f, 1f);
	}

	private void BuildUi()
	{
		// 屏幕居中容器
		var center = new CenterContainer();
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(center);

		// 背景面板（尺寸由 ResizePanelToViewport 固定为屏幕的 50% x 50%）
		bgPanel = new PanelContainer();
		var bgBox = new StyleBoxFlat();
		bgBox.BgColor = new Color(0.08f, 0.09f, 0.12f, 0.9f);
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

		// 顶部标题栏：标题靠左，配装下拉框靠右（右上角）
		var header = new HBoxContainer();
		vbox.AddChild(header);

		var title = new Label();
		title.Text = UiTitle;
		title.HorizontalAlignment = HorizontalAlignment.Left;
		title.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		title.AddThemeFontSizeOverride("font_size", 12);
		title.AddThemeColorOverride("font_color", new Color(0.85f, 0.88f, 0.95f));
		header.AddChild(title);

		// 占位空白：把下拉框推到最右侧
		var headerSpacer = new Control();
		headerSpacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		header.AddChild(headerSpacer);

		// 配装下拉框：选项数量 = LoadoutSettingData.LoadoutSelections 数量，
		// 每个选项显示对应 LoadoutSelectionData.LoadoutSelectionName。
		// 下拉框支持双击改名：外层包装容器固定尺寸，LineEdit 叠加在下拉框上。
		var dropdownWrap = new Control();
		dropdownWrap.CustomMinimumSize = new Vector2(140, 30);
		dropdownWrap.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		header.AddChild(dropdownWrap);

		loadoutDropdown = new OptionButton();
		loadoutDropdown.SetAnchorsPreset(LayoutPreset.FullRect);
		loadoutDropdown.GuiInput += OnDropdownGuiInput;
		loadoutDropdown.ItemSelected += OnLoadoutSelected;
		dropdownWrap.AddChild(loadoutDropdown);

		loadoutNameEdit = new LineEdit();
		loadoutNameEdit.Visible = false;
		loadoutNameEdit.SetAnchorsPreset(LayoutPreset.FullRect);
		loadoutNameEdit.TextSubmitted += (_) => CommitRename();
		loadoutNameEdit.FocusExited += () => CommitRename();
		loadoutNameEdit.GuiInput += OnNameEditGuiInput;
		dropdownWrap.AddChild(loadoutNameEdit);

		PopulateLoadoutDropdown();

		// 5x5 网格：占满 VBox 剩余高度，槽位 ExpandFill 拉伸填满
		var grid = new GridContainer();
		grid.Columns = Columns;
		grid.AddThemeConstantOverride("h_separation", SlotSpacing);
		grid.AddThemeConstantOverride("v_separation", SlotSpacing);
		grid.SizeFlagsVertical = SizeFlags.ExpandFill;
		vbox.AddChild(grid);

		for (int i = 0; i < Columns * Rows; i++)
		{
			var slot = CreateSlot(i);
			grid.AddChild(slot);
			slots.Add(slot);
		}
	}

	private Panel CreateSlot(int index)
	{
		var slot = new Panel();
		slot.MouseFilter = MouseFilterEnum.Stop;
		slot.AddThemeStyleboxOverride("panel", slotNormalBox);
		// 槽位在网格内自动拉伸填满可用空间
		slot.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		slot.SizeFlagsVertical = SizeFlags.ExpandFill;

		// 槽位编号（行优先）：左上角小字，空槽位时显示 0..24（与网格子节点索引一致）
		var numberLabel = new Label();
		numberLabel.MouseFilter = MouseFilterEnum.Ignore;
		numberLabel.Text = index.ToString();
		numberLabel.HorizontalAlignment = HorizontalAlignment.Left;
		numberLabel.SetAnchorsPreset(LayoutPreset.TopLeft);
		numberLabel.OffsetLeft = 4;
		numberLabel.OffsetTop = 2;
		numberLabel.OffsetRight = 20;
		numberLabel.OffsetBottom = 14;
		numberLabel.AddThemeFontSizeOverride("font_size", 10);
		numberLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.74f, 0.82f));
		numberLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.85f));
		numberLabel.AddThemeConstantOverride("outline_size", 4);
		slot.AddChild(numberLabel);

		// 武器图标：铺满槽位并居中（带 4px 内边距），仅装饰不参与鼠标事件
		var icon = new TextureRect();
		icon.MouseFilter = MouseFilterEnum.Ignore;
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
		nameLabel.MouseFilter = MouseFilterEnum.Ignore;
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
		priceLabel.MouseFilter = MouseFilterEnum.Ignore;
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

		// 点击选中：同一时刻仅一个槽位高亮
		slot.GuiInput += (ev) =>
		{
			if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
			{
				SelectSlot(index);
				EmitSignal(SignalName.SlotClicked, index);
			}
		};

		slotIcons.Add(icon);
		slotNameLabels.Add(nameLabel);
		slotPriceLabels.Add(priceLabel);
		slotNumberLabels.Add(numberLabel);
		slotWeapons.Add(null);

		return slot;
	}

	/// <summary>选中指定槽位（行优先索引 0..24，与网格子节点索引/显示编号一致），并取消上一次选中。</summary>
	private void SelectSlot(int index)
	{
		if (SelectedIndex >= 0 && SelectedIndex < slots.Count)
		{
			slots[SelectedIndex].AddThemeStyleboxOverride("panel", slotNormalBox);
		}
		SelectedIndex = index;
		slots[index].AddThemeStyleboxOverride("panel", slotSelectedBox);
	}

	/// <summary>
	/// 为指定槽位（行优先索引 0..24，与网格子节点索引/显示编号一致）设置武器数据，
	/// 槽位会显示该武器的图标/名称/价格。传 null 清空该槽位，恢复显示槽位编号。
	/// </summary>
	public void SetSlotWeapon(int slotIndex, WeaponData weapon)
	{
		if (slotIndex < 0 || slotIndex >= slotWeapons.Count) return;
		slotWeapons[slotIndex] = weapon;
		UpdateSlotDisplay(slotIndex);
	}

	/// <summary>获取指定槽位（行优先索引 0..24）的武器数据，无武器时返回 null。</summary>
	public WeaponData GetSlotWeapon(int slotIndex)
		=> slotIndex >= 0 && slotIndex < slotWeapons.Count ? slotWeapons[slotIndex] : null;

	/// <summary>
	/// 根据当前选中配装（LoadoutSettingData.SelectedLoadout）的 LoadoutSelectionStrings
	/// 刷新全部 25 个槽位的武器数据：每个元素是 WeaponData 的资源路径，空字符串表示空槽。
	/// </summary>
	private void RefreshSlotsFromSelectedLoadout()
	{
		var loadouts = LoadoutSettingData.LoadoutSelections;
		int sel = LoadoutSettingData.SelectedLoadout;
		var strings = (loadouts != null && sel >= 0 && sel < loadouts.Length && loadouts[sel] != null)
			? loadouts[sel].LoadoutSelectionStrings : null;

		int total = Columns * Rows;
		for (int i = 0; i < total; i++)
		{
			WeaponData weapon = null;
			if (strings != null && i < strings.Length && !string.IsNullOrEmpty(strings[i]))
			{
				weapon = ResourceLoader.Load(strings[i]) as WeaponData;
				if (weapon == null)
				{
					GD.PushWarning($"[LoadoutSelection] 无法从路径加载武器：{strings[i]}（槽位 {i}）");
				}
			}
			SetSlotWeapon(i, weapon);
		}
	}

	/// <summary>按槽位上的武器数据刷新显示：有武器显示图标/名称/价格，无武器显示编号。</summary>
	private void UpdateSlotDisplay(int index)
	{
		var weapon = slotWeapons[index];
		bool hasWeapon = weapon != null;
		slotIcons[index].Texture = hasWeapon ? weapon.WeaponIcon : null;
		slotNameLabels[index].Text = hasWeapon ? weapon.WeaponName : "";
		slotPriceLabels[index].Text = hasWeapon ? $"${weapon.Price}" : "";
		// 槽位编号（行优先）：有武器时隐藏，避免与名称/价格重叠
		slotNumberLabels[index].Text = hasWeapon ? "" : index.ToString();
	}

	/// <summary>根据 LoadoutSettingData.LoadoutSelections 填充右上角配装下拉框。</summary>
	private void PopulateLoadoutDropdown()
	{
		loadoutDropdown.Clear();
		var loadouts = LoadoutSettingData.LoadoutSelections;
		int count = loadouts?.Length ?? 0;
		for (int i = 0; i < count; i++)
		{
			// 每个配装显示各自的 LoadoutSelectionName（实例成员）
			var name = loadouts[i]?.LoadoutSelectionName;
			loadoutDropdown.AddItem(string.IsNullOrEmpty(name) ? $"配装 {i + 1}" : name, i);
		}
		// 默认选中当前配装
		loadoutDropdown.Selected = Mathf.Clamp(LoadoutSettingData.SelectedLoadout, 0, Mathf.Max(count - 1, 0));
	}

	/// <summary>切换配装下拉框时记录当前选中的配装索引，并按该配装数据刷新槽位。</summary>
	private void OnLoadoutSelected(long index)
	{
		LoadoutSettingData.SelectedLoadout = (int)index;
		RefreshSlotsFromSelectedLoadout();
	}

	/// <summary>双击下拉框进入改名模式：显示 LineEdit 叠加在按钮上。</summary>
	private void OnDropdownGuiInput(InputEvent ev)
	{
		if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left && mb.DoubleClick)
		{
			BeginRename();
		}
	}

	/// <summary>改名输入框内按 Escape 取消修改。</summary>
	private void OnNameEditGuiInput(InputEvent ev)
	{
		if (ev is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
		{
			CancelRename();
		}
	}

	private void BeginRename()
	{
		int sel = loadoutDropdown.Selected;
		LoadoutSettingData.SelectedLoadout = sel;
		loadoutDropdown.GetPopup().Hide();
		loadoutDropdown.Visible = false;

		// 预填当前名称：优先读 LoadoutSelectionData，其次读下拉框当前文本
		var loadouts = LoadoutSettingData.LoadoutSelections;
		string current = (loadouts != null && sel >= 0 && sel < loadouts.Length && loadouts[sel] != null)
			? loadouts[sel].LoadoutSelectionName
			: loadoutDropdown.GetItemText(sel);

		loadoutNameEdit.Text = current;
		loadoutNameEdit.Visible = true;
		loadoutNameEdit.GrabFocus();
		loadoutNameEdit.SelectAll();
	}

	/// <summary>确认改名：同步到 LoadoutSettingData.LoadoutSelections[n].LoadoutSelectionName。</summary>
	private void CommitRename()
	{
		if (!loadoutNameEdit.Visible) return;
		int sel = loadoutDropdown.Selected;
		string name = loadoutNameEdit.Text.Trim();
		var loadouts = LoadoutSettingData.LoadoutSelections;
		if (loadouts != null && sel >= 0 && sel < loadouts.Length)
		{
			// 数组项可能为 null，先补一个实例再写名称
			loadouts[sel] ??= new LoadoutSelectionData();
			loadouts[sel].LoadoutSelectionName = name;
		}
		loadoutDropdown.SetItemText(sel, string.IsNullOrEmpty(name) ? $"配装 {sel + 1}" : name);
		loadoutNameEdit.Visible = false;
		loadoutDropdown.Visible = true;
		loadoutDropdown.GrabFocus();
	}

	/// <summary>取消改名：不保存，恢复下拉框显示。</summary>
	private void CancelRename()
	{
		if (!loadoutNameEdit.Visible) return;
		loadoutNameEdit.Visible = false;
		loadoutDropdown.Visible = true;
		loadoutDropdown.GrabFocus();
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

	/// <summary>背景面板：高度固定为屏幕的 60%（PanelHeightRatio），宽度默认 50%。</summary>
	private void ResizePanelToViewport()
	{
		var vsize = GetViewportRect().Size;
		var panelW = vsize.X * PanelWidthRatio;
		var panelH = vsize.Y * PanelHeightRatio;
		bgPanel.CustomMinimumSize = new Vector2(panelW, panelH);
		bgPanel.CustomMaximumSize = new Vector2(panelW, panelH);

		// 依据面板内容区动态计算槽位尺寸，保证 5x5 网格完整容纳。
		// 估算偏大取整，避免算出的最小尺寸超出实际内容区（防止 GridContainer 被压缩）；
		// 槽位同时带 ExpandFill，会向上补齐剩余的少量空间。
		const float titleAreaHeight = 56f; // 标题行高(约34) + 16px 纵向间距 + 余量
		const float marginsH = 48f;        // ContentMarginLeft + Right
		const float marginsV = 44f;        // ContentMarginTop + Bottom
		float slotW = (panelW - marginsH - (Columns - 1) * SlotSpacing) / Columns;
		float slotH = (panelH - marginsV - titleAreaHeight - (Rows - 1) * SlotSpacing) / Rows;
		slotW = Mathf.Max(slotW, 32f);
		slotH = Mathf.Max(slotH, 32f);
		for (int i = 0; i < slots.Count; i++)
		{
			slots[i].CustomMinimumSize = new Vector2(slotW, slotH);
		}
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
