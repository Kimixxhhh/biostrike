using Godot;

/// <summary>
/// 配装设置（LoadoutSetting）UI —— 上方为 LoadoutSelection（5x5 配装选择），
/// 下方为 LoadoutUI（武器装备列表），纵向堆叠，整体左右居中显示。
/// 默认上面板占屏幕 60% 高度（SelectionRatio），下面板占 40%（UiRatio），
/// 两个子面板会各自在水平方向居中，因此组合 UI 左右居中。
/// </summary>
public partial class LoadoutSetting : Control
{
	[Export] public float SelectionRatio { get; set; } = 0.6f;
	[Export] public float UiRatio { get; set; } = 0.4f;

	/// <summary>上方配装选择面板与顶部的空隙（像素）。</summary>
	[Export] public float TopMargin { get; set; } = 20f;

	/// <summary>上方的配装选择 UI。</summary>
	public LoadoutSelection Selection { get; private set; }

	/// <summary>下方的装备 UI。</summary>
	public LoadoutUI Loadout { get; private set; }

	public override void _Ready()
	{
		BuildUi();
		CenterUiOnScreen();

		// 在 LoadoutUI 点击武器时，把武器路径写入配装中当前选中的槽位
		Loadout.WeaponSelected += OnWeaponSelectedFromLoadout;
	}

	private void BuildUi()
	{
		// 纵向堆叠容器：铺满整个根节点，顶部留出空隙
		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 0);
		vbox.SetAnchorsPreset(LayoutPreset.FullRect);
		vbox.OffsetTop = TopMargin; // 让配装选择面板与顶部拉开一点距离
		AddChild(vbox);

		// 上方：LoadoutSelection（5x5 配装选择）
		var selectionScene = GD.Load<PackedScene>("res://scenes/UI/LoadoutSelection.tscn");
		Selection = (LoadoutSelection)selectionScene.Instantiate();
		Selection.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		Selection.SizeFlagsVertical = SizeFlags.ExpandFill;
		Selection.SizeFlagsStretchRatio = SelectionRatio;
		vbox.AddChild(Selection);

		// 下方：LoadoutUI（武器装备列表）
		var loadoutScene = GD.Load<PackedScene>("res://scenes/UI/LoadoutUI.tscn");
		Loadout = (LoadoutUI)loadoutScene.Instantiate();
		Loadout.AlignToBottom = true; // 让下面板贴住底部
		Loadout.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		Loadout.SizeFlagsVertical = SizeFlags.ExpandFill;
		Loadout.SizeFlagsStretchRatio = UiRatio;
		vbox.AddChild(Loadout);
	}

	/// <summary>
	/// LoadoutUI 点击武器时触发：将武器路径写入
	/// LoadoutSettingData.LoadoutSelections[当前配装].LoadoutSelectionStrings[选中的槽位]，
	/// 并同步刷新 LoadoutSelection 中该槽位的显示。
	/// </summary>
	private void OnWeaponSelectedFromLoadout(WeaponData weapon)
	{
		// 需要先在 LoadoutSelection 中点击选中一个槽位（唯一高亮）
		int slot = Selection.SelectedIndex;
		if (slot < 0)
		{
			GD.Print("[LoadoutSetting] 未选中任何槽位，忽略武器写入");
			return;
		}

		if (weapon == null)
		{
			GD.PushWarning("[LoadoutSetting] 武器数据为空");
			return;
		}

		string path = weapon.ResourcePath;
		if (string.IsNullOrEmpty(path))
		{
			GD.PushWarning("[LoadoutSetting] 武器没有资源路径，无法写入配装");
			return;
		}

		// 写入当前配装的 LoadoutSelectionStrings
		var loadouts = LoadoutSettingData.LoadoutSelections;
		int sel = LoadoutSettingData.SelectedLoadout;
		if (loadouts != null && sel >= 0 && sel < loadouts.Length)
		{
			loadouts[sel] ??= new LoadoutSelectionData();
			if (loadouts[sel].LoadoutSelectionStrings == null)
			{
				loadouts[sel].LoadoutSelectionStrings = new string[25];
			}
			if (slot < loadouts[sel].LoadoutSelectionStrings.Length)
			{
				loadouts[sel].LoadoutSelectionStrings[slot] = path;
			}
		}

		// 同步更新选中的武器槽位显示
		Selection.SetSlotWeapon(slot, weapon);
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
