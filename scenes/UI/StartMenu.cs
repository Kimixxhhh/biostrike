using Godot;

/// <summary>
/// 开始菜单（StartMenu）UI —— 顶部 Tab 栏，从左到右：设置、开始、装备。
/// Tab 栏左右居中（Alignment = Center）、上下靠上（TabAlignment = Top）。
/// 点击 Tab 切换下方对应页面。"装备"页接入 LoadoutSetting.tscn；
/// "设置"页顶部再套一层子 Tab 栏：视频 / 音频 / 游戏 / 键盘/鼠标，
/// 每个子标签下是可滚动的内容区（可用 GetSettingsPageContent(index) 取到内容容器往里加真实设置项）。
/// </summary>
public partial class StartMenu : Control
{
	/// <summary>点击"开始游戏"按钮时触发。</summary>
	[Signal]
	public delegate void StartGamePressedEventHandler();

	[Export] public string SettingsTabTitle { get; set; } = "设置";
	[Export] public string StartTabTitle { get; set; } = "开始";
	[Export] public string LoadoutTabTitle { get; set; } = "装备";

	// 设置页里的四个子标签标题（从左到右），可在 Inspector 里改
	[Export] public string VideoTabTitle { get; set; } = "视频";
	[Export] public string AudioTabTitle { get; set; } = "音频";
	[Export] public string GameTabTitle { get; set; } = "游戏";
	[Export] public string KeyboardMouseTabTitle { get; set; } = "键盘/鼠标";

	private TabContainer tabs;

	/// <summary>设置页的子 Tab 栏（视频 / 音频 / 游戏 / 键盘/鼠标）。</summary>
	private TabContainer settingsTabs;

	/// <summary>四个子标签页的内容 VBox（0=视频 1=音频 2=游戏；3=键盘/鼠标 由 CrosshairSetting 占据，为 null）。</summary>
	private readonly VBoxContainer[] settingsPages = new VBoxContainer[4];

	/// <summary>“键盘/鼠标”子标签在设置页里的下标（接入 CrosshairSetting.tscn 的那一页）。</summary>
	private const int KeyboardMousePageIndex = 3;

	/// <summary>“键盘/鼠标”子页里使用的准星设置场景。</summary>
	private const string CrosshairSettingScenePath = "res://scenes/UI/CrosshairSetting.tscn";

	/// <summary>
	/// 设置页“键盘/鼠标”子页里的准星设置界面（CrosshairSetting.tscn 实例）。
	/// 需要读/写准星设置时可以直接访问它。
	/// </summary>
	public CrosshairSetting CrosshairSettings { get; private set; }

	public override void _Ready()
	{
		// 先加载外部 JSON 配装数据，再构建 UI（装备页会读取 LoadoutSettingData 的数据）
		LoadLoadoutData();
		BuildUi();
		CenterUiOnScreen();
	}

	private void BuildUi()
	{
		// TabContainer 铺满屏幕
		tabs = new TabContainer();
		tabs.SetAnchorsPreset(LayoutPreset.FullRect);
		tabs.TabsPosition = TabContainer.TabPosition.Top; // 上下靠上（Tab 栏在顶部）
		tabs.TabAlignment = TabBar.AlignmentMode.Center;  // 左右居中
		StyleTabBar(tabs, 20);                            // 主 Tab 字号比设置页子 Tab 大
		AddChild(tabs);

		// 三个页面：Tab 标题 = 页面节点名（子节点顺序即从左到右顺序）
		tabs.AddChild(BuildSettingsPage());
		tabs.AddChild(BuildStartPage());
		tabs.AddChild(BuildLoadoutPage());

		// 默认选中"开始"页（中间 Tab）
		tabs.CurrentTab = 1;

		// 从"装备"页切换到其他页时，把配装数据序列化保存到外部 JSON
		tabs.TabChanged += OnTabChanged;
	}

	/// <summary>给 Tab 栏设置透明背景 + 琥珀色选中样式，与游戏整体风格一致。
	/// 外层主 Tab 与设置页的内层子 Tab 共用这套样式（子 Tab 字号小一些）。</summary>
	private void StyleTabBar(TabContainer target, int fontSize = 16)
	{
		// Tab 栏整体背景透明，仅保留各 Tab 选项自身的选中/悬停样式。
		// 注意：TabContainer 的 tab 栏背景主题项是 "tabbar_background"（不是 "tab_background"，那是个无效名）。
		var barBg = new StyleBoxFlat();
		barBg.BgColor = new Color(0f, 0f, 0f, 0f);
		barBg.SetCornerRadiusAll(8);
		target.AddThemeStyleboxOverride("tabbar_background", barBg);

		// 内容区背景也透明，否则装备页等透明页面会透出默认深灰 panel。
		var contentBg = new StyleBoxFlat();
		contentBg.BgColor = new Color(0f, 0f, 0f, 0f);
		target.AddThemeStyleboxOverride("panel", contentBg);

		var tabSelected = new StyleBoxFlat();
		tabSelected.BgColor = new Color(0.40f, 0.36f, 0.18f, 1f);
		tabSelected.BorderColor = new Color(1f, 0.84f, 0.40f, 1f);
		tabSelected.SetBorderWidthAll(1);
		tabSelected.SetCornerRadiusAll(6);
		// 左右留白：拉开相邻选项的间隔（三个状态一致，避免悬停时跳变）
		tabSelected.ContentMarginLeft = 14;
		tabSelected.ContentMarginRight = 14;
		target.AddThemeStyleboxOverride("tab_selected", tabSelected);

		var tabHovered = new StyleBoxFlat();
		tabHovered.BgColor = new Color(0.20f, 0.22f, 0.28f, 1f);
		tabHovered.SetCornerRadiusAll(6);
		tabHovered.ContentMarginLeft = 14;
		tabHovered.ContentMarginRight = 14;
		target.AddThemeStyleboxOverride("tab_hovered", tabHovered);

		var tabNormal = new StyleBoxFlat();
		tabNormal.BgColor = new Color(0f, 0f, 0f, 0f);
		tabNormal.SetCornerRadiusAll(6);
		tabNormal.ContentMarginLeft = 14;
		tabNormal.ContentMarginRight = 14;
		target.AddThemeStyleboxOverride("tab_unselected", tabNormal);

		target.AddThemeColorOverride("font_selected_color", new Color(1f, 0.90f, 0.60f));
		target.AddThemeColorOverride("font_unselected_color", new Color(0.70f, 0.74f, 0.82f));
		target.AddThemeColorOverride("font_hovered_color", new Color(0.90f, 0.93f, 1f));
		target.AddThemeFontSizeOverride("font_size", fontSize);
	}

	/// <summary>
	/// 设置页：顶部再套一层子 Tab 栏，从左到右四个标签 —— 视频 / 音频 / 游戏 / 键盘/鼠标。
	/// 每个子标签下是可纵向滚动的占位内容区，后续用 GetSettingsPageContent(index) 取到容器填真实设置项。
	/// </summary>
	private Control BuildSettingsPage()
	{
		// 设置页背景透明（true），露出 StartMenu 后面的画面
		var page = BuildPageBase(SettingsTabTitle, true);

		// 子 Tab 栏：与外层主 Tab 同样的样式（透明背景 + 琥珀色选中）
		settingsTabs = new TabContainer();
		settingsTabs.Name = "SettingsTabs";
		settingsTabs.SetAnchorsPreset(LayoutPreset.FullRect);
		settingsTabs.TabsPosition = TabContainer.TabPosition.Top; // 标签栏在页面最上面
		settingsTabs.TabAlignment = TabBar.AlignmentMode.Center;  // 左右居中
		StyleTabBar(settingsTabs, 15);                            // 子 Tab 字号比主 Tab 小一档
		page.AddChild(settingsTabs);

		string[] nodeNames = { "Video", "Audio", "Game", "KeyboardMouse" };
		string[] tabTitles =
		{
			VideoTabTitle, AudioTabTitle, GameTabTitle, KeyboardMouseTabTitle,
		};
		string[] hints =
		{
			"分辨率 / 显示模式 / 画质等选项（待实现）",
			"主音量 / 音乐 / 音效等选项（待实现）",
			"难度 / 镜头灵敏度等选项（待实现）",
		};

		// 前三个子页暂时是占位内容（往 settingsPages[i] 里加真实设置项）
		for (int i = 0; i < hints.Length; i++)
		{
			settingsPages[i] = BuildSettingsSubPage(nodeNames[i], tabTitles[i], hints[i]);
		}

		// “键盘/鼠标”页直接接入 CrosshairSetting.tscn（准星设置）
		BuildSettingsCrosshairPage(nodeNames[KeyboardMousePageIndex], tabTitles[KeyboardMousePageIndex]);

		settingsTabs.CurrentTab = 0; // 默认显示“视频”
		return page;
	}

	/// <summary>
	/// “键盘/鼠标”子页：不建占位内容，直接把 CrosshairSetting.tscn（准星设置）铺满整个子页。
	/// CrosshairSetting 自己会把设置面板摆到所在矩形正中，所以这里只需给它一块够大的区域。
	/// </summary>
	private void BuildSettingsCrosshairPage(string nodeName, string tabTitle)
	{
		var page = new PanelContainer();
		page.Name = nodeName;

		// 透明背景：让 CrosshairSetting 自带的面板成为这一页唯一的卡片
		var box = new StyleBoxFlat();
		box.BgColor = new Color(0f, 0f, 0f, 0f);
		page.AddThemeStyleboxOverride("panel", box);

		var scene = GD.Load<PackedScene>(CrosshairSettingScenePath);
		if (scene == null)
		{
			GD.PushError($"[StartMenu] 无法加载 {CrosshairSettingScenePath}，键盘/鼠标页为空");
		}
		else
		{
			CrosshairSettings = (CrosshairSetting)scene.Instantiate();
			page.AddChild(CrosshairSettings);
		}

		settingsTabs.AddChild(page);
		settingsTabs.SetTabTitle(settingsTabs.GetTabCount() - 1, tabTitle);
	}

	/// <summary>
	/// 构建设置页里的一个子标签页（透明背景 + 标题 + 分隔线 + 提示文字），并挂到 settingsTabs 下，
	/// 同时用 SetTabTitle 设置显示标题（节点名必须合法，"键盘/鼠标"含 "/" 不能直接当节点名）。
	/// 返回内容 VBox，后续可直接 AddChild 真实设置项。
	/// </summary>
	private VBoxContainer BuildSettingsSubPage(string nodeName, string tabTitle, string hint)
	{
		var page = new PanelContainer();
		page.Name = nodeName;

		// 透明背景：露出外层"设置"页的深色面板
		var box = new StyleBoxFlat();
		box.BgColor = new Color(0f, 0f, 0f, 0f);
		box.SetContentMarginAll(18);
		page.AddThemeStyleboxOverride("panel", box);

		var scroll = new ScrollContainer();
		scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
		scroll.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
		scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
		page.AddChild(scroll);

		var content = new VBoxContainer();
		content.Name = "Content";
		content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		content.AddThemeConstantOverride("separation", 12);
		scroll.AddChild(content);

		var heading = new Label();
		heading.Text = tabTitle;
		heading.AddThemeFontSizeOverride("font_size", 20);
		heading.AddThemeColorOverride("font_color", new Color(1f, 0.84f, 0.40f));
		content.AddChild(heading);

		content.AddChild(new HSeparator());

		var hintLabel = new Label();
		hintLabel.Text = hint;
		hintLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		hintLabel.AddThemeFontSizeOverride("font_size", 14);
		hintLabel.AddThemeColorOverride("font_color", new Color(0.70f, 0.74f, 0.82f));
		content.AddChild(hintLabel);

		settingsTabs.AddChild(page);
		settingsTabs.SetTabTitle(settingsTabs.GetTabCount() - 1, tabTitle);

		return content;
	}

	/// <summary>
	/// 取设置页某个子标签的内容容器：0=视频 1=音频 2=游戏，越界 / 3=键盘/鼠标 返回 null
	/// （键盘/鼠标页由 CrosshairSetting 占据，见 CrosshairSettings 属性）。
	/// 后续填真实设置项时，直接往这个 VBox 里 AddChild 即可。
	/// </summary>
	public VBoxContainer GetSettingsPageContent(int index)
	{
		if (index < 0 || index >= settingsPages.Length)
		{
			return null;
		}
		return settingsPages[index];
	}

	private Control BuildStartPage()
	{
		var page = BuildPageBase(StartTabTitle);

		// CenterContainer 把内容在整个面板内水平垂直居中
		var center = new CenterContainer();
		page.AddChild(center);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 20);
		center.AddChild(box);

		var title = new Label();
		title.Text = "Bio Strike";
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.AddThemeFontSizeOverride("font_size", 44);
		title.AddThemeColorOverride("font_color", new Color(1f, 0.90f, 0.60f));
		box.AddChild(title);

		var sub = new Label();
		sub.Text = "生物突击";
		sub.HorizontalAlignment = HorizontalAlignment.Center;
		sub.AddThemeFontSizeOverride("font_size", 18);
		sub.AddThemeColorOverride("font_color", new Color(0.85f, 0.88f, 0.95f));
		box.AddChild(sub);

		var startButton = new Button();
		startButton.Text = "开始游戏";
		startButton.CustomMinimumSize = new Vector2(240, 52);
		StyleStartButton(startButton);
		startButton.Pressed += () => EmitSignal(SignalName.StartGamePressed);
		box.AddChild(startButton);

		return page;
	}

	private Control BuildLoadoutPage()
	{
		// 装备页直接接入 LoadoutSetting：上方 5x5 配装选择 + 下方武器装备列表
		var scene = GD.Load<PackedScene>("res://scenes/UI/LoadoutSetting.tscn");
		var page = (Control)scene.Instantiate();
		page.Name = LoadoutTabTitle; // 节点名即 Tab 标题
		return page;
	}

	/// <summary>Tab 切换时触发：若从"装备"页切到其他页，把配装数据序列化保存到外部 JSON。</summary>
	private void OnTabChanged(long newTab)
	{
		int loadoutTab = GetLoadoutTabIndex();
		if (loadoutTab < 0 || newTab == loadoutTab)
		{
			return;
		}
		// 只有确实是从"装备"页切走时才保存
		if (tabs.GetPreviousTab() == loadoutTab)
		{
			SaveLoadoutData();
		}
	}

	/// <summary>查找"装备"页对应的 Tab 索引。</summary>
	private int GetLoadoutTabIndex()
	{
		for (int i = 0; i < tabs.GetTabCount(); i++)
		{
			if (tabs.GetTabTitle(i) == LoadoutTabTitle)
			{
				return i;
			}
		}
		return -1;
	}

	/// <summary>
	/// 把 LoadoutSettingData.LoadoutSelections（5 个 LoadoutSelectionData）序列化到
	/// user://LoadoutSelectionData.json（外部 JSON 文件）。
	/// </summary>
	private void SaveLoadoutData()
	{
		try
		{
			var array = new Godot.Collections.Array();
			var loadouts = LoadoutSettingData.LoadoutSelections;
			if (loadouts != null)
			{
				foreach (var loadout in loadouts)
				{
					if (loadout == null)
					{
						// 空配装保留 null，保持数组下标与 JSON 位置一致
						array.Add(default(Variant));
						continue;
					}

					var dict = new Godot.Collections.Dictionary();
					var strings = new Godot.Collections.Array();
					if (loadout.LoadoutSelectionStrings != null)
					{
						foreach (var s in loadout.LoadoutSelectionStrings)
						{
							strings.Add(s); // null 字符串在 JSON 中保持 null
						}
					}
					dict["LoadoutSelectionStrings"] = strings;
					dict["LoadoutSelectionName"] = loadout.LoadoutSelectionName;
					array.Add(dict);
				}
			}

			string json = Json.Stringify(array, "\t");
			using var file = FileAccess.Open("user://LoadoutSelectionData.json", FileAccess.ModeFlags.Write);
			if (file != null)
			{
				file.StoreString(json);
				GD.Print("[StartMenu] 配装数据已保存到 user://LoadoutSelectionData.json");
			}
			else
			{
				GD.PushError("[StartMenu] 无法打开 user://LoadoutSelectionData.json 进行写入");
			}
		}
		catch (System.Exception e)
		{
			GD.PushError($"[StartMenu] 保存配装数据失败：{e.Message}");
		}
	}

	/// <summary>
	/// 启动时从 user://LoadoutSelectionData.json 加载配装数据到
	/// LoadoutSettingData.LoadoutSelections（LoadoutSelectionData 数组）。
	/// 文件不存在或解析失败时保持默认空配装，不中断启动。
	/// </summary>
	private void LoadLoadoutData()
	{
		try
		{
			if (!FileAccess.FileExists("user://LoadoutSelectionData.json"))
			{
				GD.Print("[StartMenu] 未找到 user://LoadoutSelectionData.json，跳过加载");
				return;
			}

			using var file = FileAccess.Open("user://LoadoutSelectionData.json", FileAccess.ModeFlags.Read);
			if (file == null)
			{
				GD.PushWarning("[StartMenu] 无法打开 user://LoadoutSelectionData.json 读取");
				return;
			}

			var json = new Json();
			Error err = json.Parse(file.GetAsText());
			if (err != Error.Ok || json.Data.VariantType != Variant.Type.Array)
			{
				GD.PushWarning($"[StartMenu] LoadoutSelectionData.json 解析失败或顶层不是数组（{err}）");
				return;
			}

			var array = json.Data.AsGodotArray();
			var loadouts = new LoadoutSelectionData[array.Count];
			for (int i = 0; i < array.Count; i++)
			{
				if (array[i].VariantType != Variant.Type.Dictionary)
				{
					loadouts[i] = null; // 空配装
					continue;
				}

				var dict = array[i].AsGodotDictionary();
				var loadout = new LoadoutSelectionData();

				if (dict.TryGetValue("LoadoutSelectionName", out Variant nameV))
				{
					loadout.LoadoutSelectionName = nameV.AsString();
				}

				if (dict.TryGetValue("LoadoutSelectionStrings", out Variant strsV) &&
					strsV.VariantType == Variant.Type.Array)
				{
					var strsArr = strsV.AsGodotArray();
					var strArray = new string[strsArr.Count];
					for (int j = 0; j < strsArr.Count; j++)
					{
						strArray[j] = strsArr[j].VariantType == Variant.Type.Nil
							? null
							: strsArr[j].AsString();
					}
					loadout.LoadoutSelectionStrings = strArray;
				}

				loadouts[i] = loadout;
			}

			LoadoutSettingData.LoadoutSelections = loadouts;
			GD.Print($"[StartMenu] 已从 user://LoadoutSelectionData.json 加载 {loadouts.Length} 个配装");
		}
		catch (System.Exception e)
		{
			GD.PushError($"[StartMenu] 加载配装数据失败：{e.Message}");
		}
	}

	/// <summary>创建带圆角背景的面板页面，节点名即 Tab 标题。
	/// transparentBackground = true 时面板背景全透明（内容直接叠在后面的画面上，如设置页）。</summary>
	private Control BuildPageBase(string tabTitle, bool transparentBackground = false)
	{
		var page = new PanelContainer();
		page.Name = tabTitle;

		var box = new StyleBoxFlat();
		box.BgColor = transparentBackground ? new Color(0f, 0f, 0f, 0f) : new Color(0.08f, 0.09f, 0.12f, 0.92f);
		box.SetCornerRadiusAll(12);
		box.ContentMarginLeft = 24;
		box.ContentMarginRight = 24;
		box.ContentMarginTop = 24;
		box.ContentMarginBottom = 24;
		page.AddThemeStyleboxOverride("panel", box);

		return page;
	}

	private void StyleStartButton(Button button)
	{
		var normal = new StyleBoxFlat();
		normal.BgColor = new Color(0.40f, 0.36f, 0.18f, 1f);
		normal.BorderColor = new Color(1f, 0.84f, 0.40f, 1f);
		normal.SetBorderWidthAll(1);
		normal.SetCornerRadiusAll(8);
		normal.ContentMarginLeft = 24;
		normal.ContentMarginRight = 24;
		normal.ContentMarginTop = 10;
		normal.ContentMarginBottom = 10;
		button.AddThemeStyleboxOverride("normal", normal);

		var hovered = (StyleBoxFlat)normal.Duplicate();
		hovered.BgColor = new Color(0.55f, 0.48f, 0.22f, 1f);
		button.AddThemeStyleboxOverride("hover", hovered);

		var pressed = (StyleBoxFlat)normal.Duplicate();
		pressed.BgColor = new Color(0.28f, 0.24f, 0.12f, 1f);
		button.AddThemeStyleboxOverride("pressed", pressed);

		button.AddThemeColorOverride("font_color", new Color(1f, 0.90f, 0.60f));
		button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.95f, 0.80f));
		button.AddThemeFontSizeOverride("font_size", 20);
	}

	/// <summary>
	/// 让 UI 始终铺满屏幕。父节点非 Control（如测试场景挂在 Node2D 下）时，
	/// 基于视口尺寸手动定位。
	/// </summary>
	private void CenterUiOnScreen()
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
}
