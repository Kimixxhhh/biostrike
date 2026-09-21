using Godot;

/// <summary>
/// 准星设置（CrosshairSetting）UI —— 在游戏内实时调整准星样式：
/// 十字线长度、线条粗细、中心间隙、颜色（R / G / B 三条 0-255 滑条，竖着排）、中心点显示开关。
/// 面板中间有实时预览区，直接显示调整后的准星。
///
/// 数据流：本界面把值写入全局静态 <see cref="CrosshairSettingData"/>，再调用 NotifyChanged()；
/// 场景里每个 Crosshair 在 _Ready 时 Attach 到这份数据并订阅变更，收到通知后立刻重绘。
/// 因此本界面不依赖场景里是否 / 何时存在准星节点，设置在同一局游戏内也能跨场景保留。
/// 用法：把 scenes/UI/CrosshairSetting.tscn 实例化到任意 UI 层，显示 / 隐藏即可；
/// 也可以直接嵌进别的页面（如 StartMenu 设置页的“键盘/鼠标”子标签），
/// 面板会自动居中到本控件所在的矩形。
/// </summary>
public partial class CrosshairSetting : Control
{
	/// <summary>任一设置项变化后触发（需要存盘等后续处理时可监听它）。</summary>
	[Signal]
	public delegate void SettingsChangedEventHandler();

	/// <summary>面板标题。</summary>
	[Export] public string UiTitle { get; set; } = "准星设置";

	/// <summary>面板最小尺寸（内容更宽/更高时会被撑大）。</summary>
	[Export] public Vector2 PanelMinSize { get; set; } = new Vector2(460f, 0f);

	/// <summary>面板背景色（半透明）：Alpha 越小越能透出后面的画面。</summary>
	[Export] public Color PanelBgColor { get; set; } = new Color(0.08f, 0.09f, 0.12f, 0.60f);

	/// <summary>十字线长度滑条上限（像素）。</summary>
	[Export] public float MaxArmLength { get; set; } = 64f;

	/// <summary>中心间隙滑条上限（像素）。</summary>
	[Export] public float MaxGap { get; set; } = 32f;

	/// <summary>线条粗细滑条上限（像素）。</summary>
	[Export] public float MaxThickness { get; set; } = 10f;

	/// <summary>预览区的描边加粗像素（与 Crosshair 的 OutlineExtra 保持一致即可）。</summary>
	[Export] public float PreviewOutlineExtra { get; set; } = 2f;

	private PanelContainer panel;
	private Control preview;
	private HSlider armSlider;
	private HSlider gapSlider;
	private HSlider thicknessSlider;
	private Label armValueLabel;
	private Label gapValueLabel;
	private Label thicknessValueLabel;
	private HSlider redSlider;
	private HSlider greenSlider;
	private HSlider blueSlider;
	private Label redValueLabel;
	private Label greenValueLabel;
	private Label blueValueLabel;
	private ColorRect colorSwatch;
	private CheckBox dotCheck;

	/// <summary>为 true 时忽略控件回调（从数据回填控件值时使用，避免又写回数据）。</summary>
	private bool suppressApply;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		// 根节点不拦截鼠标：面板之外的点击照常传给游戏 / 其它 UI
		MouseFilter = MouseFilterEnum.Ignore;

		BuildUi();
		CenterUiOnScreen();
		SyncControlsFromData();

		// 每次显示时都用最新数据刷新一遍，避免与运行中被改过的准星不一致
		VisibilityChanged += OnVisibilityChanged;
		// 尺寸变化时重新把面板摆到正中：被放进容器（如 StartMenu 设置页的"键盘/鼠标"子页）时，
		// 本控件的矩形由容器决定，需要跟着容器布局走
		Resized += CenterPanelInSelf;
	}

	private void OnVisibilityChanged()
	{
		if (Visible)
		{
			SyncControlsFromData();
			// 延迟一帧：隐藏→显示时容器才刚给出尺寸，等布局完成再摆正面板
			Callable.From(CenterPanelInSelf).CallDeferred();
		}
	}

	// ---------------------------------------------------------------- UI 构建

	private void BuildUi()
	{
		// 背景面板（位置与尺寸由 CenterUiOnScreen 决定：屏幕正中）
		panel = new PanelContainer();
		panel.CustomMinimumSize = PanelMinSize;

		var bgBox = new StyleBoxFlat();
		bgBox.BgColor = PanelBgColor; // 半透明：透出后面的游戏画面
		bgBox.SetCornerRadiusAll(16); // 圆角保留；不画描边（无边框）
		bgBox.SetContentMarginAll(18);
		panel.AddThemeStyleboxOverride("panel", bgBox);
		AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 12);
		panel.AddChild(vbox);

		// 标题
		var title = new Label();
		title.Text = UiTitle;
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.AddThemeFontSizeOverride("font_size", 20);
		title.AddThemeColorOverride("font_color", new Color(1f, 0.84f, 0.40f));
		vbox.AddChild(title);

		// 预览区：实时显示当前准星样式（用 Crosshair 的绘制逻辑，所见即所得）
		preview = new Control();
		preview.CustomMinimumSize = new Vector2(0f, 120f);
		preview.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		preview.MouseFilter = MouseFilterEnum.Ignore;
		preview.Draw += OnPreviewDraw;
		vbox.AddChild(preview);

		// 长度 / 间隙 / 粗细
		armSlider = BuildSliderRow(vbox, "十字线长度", 0f, MaxArmLength, 0.5f, out armValueLabel);
		armSlider.ValueChanged += OnAnyValueChanged;

		gapSlider = BuildSliderRow(vbox, "十字线间隙", 0f, MaxGap, 0.5f, out gapValueLabel);
		gapSlider.ValueChanged += OnAnyValueChanged;

		thicknessSlider = BuildSliderRow(vbox, "线条粗细", 0.5f, MaxThickness, 0.5f, out thicknessValueLabel);
		thicknessSlider.ValueChanged += OnAnyValueChanged;

		// 颜色（RGB 0-255，三行滑条竖着排）
		BuildColorRows(vbox);

		// 中心点开关
		var dotRow = new HBoxContainer();
		dotRow.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(dotRow);

		dotCheck = new CheckBox();
		dotCheck.Text = "显示准星中心点";
		dotCheck.Toggled += OnAnyValueChanged;
		dotRow.AddChild(dotCheck);

		// 底部按钮行：恢复默认靠右
		var buttonRow = new HBoxContainer();
		vbox.AddChild(buttonRow);

		var spacer = new Control();
		spacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		buttonRow.AddChild(spacer);

		var resetButton = new Button();
		resetButton.Text = "恢复默认";
		resetButton.Pressed += OnResetPressed;
		buttonRow.AddChild(resetButton);
	}

	/// <summary>一行滑条：左侧标题 + 中间 HSlider + 右侧数值。
	/// labelWidth 控制左侧标题宽度，用来让各行的滑条左边缘对齐。</summary>
	private HSlider BuildSliderRow(VBoxContainer parent, string labelText, float minValue, float maxValue,
		float step, out Label valueLabel, float labelWidth = 110f)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);
		parent.AddChild(row);

		var label = new Label();
		label.Text = labelText;
		label.CustomMinimumSize = new Vector2(labelWidth, 0f);
		label.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		label.AddThemeColorOverride("font_color", new Color(0.85f, 0.88f, 0.95f));
		row.AddChild(label);

		var slider = new HSlider();
		slider.MinValue = minValue;
		slider.MaxValue = maxValue;
		slider.Step = step;
		slider.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		slider.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		slider.CustomMinimumSize = new Vector2(180f, 0f);
		row.AddChild(slider);

		valueLabel = new Label();
		valueLabel.CustomMinimumSize = new Vector2(48f, 0f);
		valueLabel.HorizontalAlignment = HorizontalAlignment.Right;
		valueLabel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		valueLabel.AddThemeColorOverride("font_color", new Color(1f, 0.84f, 0.40f));
		row.AddChild(valueLabel);

		return slider;
	}

	/// <summary>颜色区：标题（右侧带当前颜色色块）+ R / G / B 三行滑条，竖着排，
	/// 每行都是「名称 + 0-255 滑条 + 数值」，与长度 / 间隙 / 粗细的调法一致。</summary>
	private void BuildColorRows(VBoxContainer parent)
	{
		// 标题行：左“十字线颜色” + 右侧颜色色块
		var header = new HBoxContainer();
		header.AddThemeConstantOverride("separation", 10);
		parent.AddChild(header);

		var titleLabel = new Label();
		titleLabel.Text = "十字线颜色";
		titleLabel.CustomMinimumSize = new Vector2(110f, 0f);
		titleLabel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		titleLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.88f, 0.95f));
		header.AddChild(titleLabel);

		var spacer = new Control();
		spacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		header.AddChild(spacer);

		colorSwatch = new ColorRect();
		colorSwatch.CustomMinimumSize = new Vector2(48f, 22f);
		colorSwatch.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		colorSwatch.MouseFilter = MouseFilterEnum.Ignore;
		header.AddChild(colorSwatch);

		// R / G / B 三行竖着排（标签宽度与其它行一致，滑条左边缘对齐）
		redSlider = BuildSliderRow(parent, "R", 0f, 255f, 1f, out redValueLabel);
		redSlider.ValueChanged += OnAnyValueChanged;

		greenSlider = BuildSliderRow(parent, "G", 0f, 255f, 1f, out greenValueLabel);
		greenSlider.ValueChanged += OnAnyValueChanged;

		blueSlider = BuildSliderRow(parent, "B", 0f, 255f, 1f, out blueValueLabel);
		blueSlider.ValueChanged += OnAnyValueChanged;
	}

	// ------------------------------------------------------------------ 数据

	/// <summary>把全局数据回填到控件（不触发写回）。</summary>
	private void SyncControlsFromData()
	{
		suppressApply = true;
		armSlider.Value = CrosshairSettingData.ArmLength;
		gapSlider.Value = CrosshairSettingData.Gap;
		thicknessSlider.Value = CrosshairSettingData.Thickness;
		redSlider.Value = CrosshairSettingData.ColorR;
		greenSlider.Value = CrosshairSettingData.ColorG;
		blueSlider.Value = CrosshairSettingData.ColorB;
		dotCheck.ButtonPressed = CrosshairSettingData.ShowDot;
		suppressApply = false;

		// 上面的赋值被抑制了回调，这里统一刷新一次数值文字 / 色块 / 预览
		RefreshVisuals();
	}

	/// <summary>滑条（double）与勾选框（bool）共用的变化回调，由重载区分。</summary>
	private void OnAnyValueChanged(double value)
	{
		if (suppressApply)
		{
			return;
		}
		ApplyToData();
	}

	private void OnAnyValueChanged(bool toggled)
	{
		if (suppressApply)
		{
			return;
		}
		ApplyToData();
	}

	/// <summary>把控件上的值写入全局数据，通知所有准星重绘。</summary>
	private void ApplyToData()
	{
		CrosshairSettingData.ArmLength = (float)armSlider.Value;
		CrosshairSettingData.Gap = (float)gapSlider.Value;
		CrosshairSettingData.Thickness = (float)thicknessSlider.Value;
		CrosshairSettingData.ColorR = Mathf.Clamp(Mathf.RoundToInt((float)redSlider.Value), 0, 255);
		CrosshairSettingData.ColorG = Mathf.Clamp(Mathf.RoundToInt((float)greenSlider.Value), 0, 255);
		CrosshairSettingData.ColorB = Mathf.Clamp(Mathf.RoundToInt((float)blueSlider.Value), 0, 255);
		CrosshairSettingData.ShowDot = dotCheck.ButtonPressed;

		RefreshVisuals();
		CrosshairSettingData.NotifyChanged();
		EmitSignal(SignalName.SettingsChanged);
	}

	/// <summary>刷新数值文字、颜色色块与预览区。</summary>
	private void RefreshVisuals()
	{
		armValueLabel.Text = ((float)armSlider.Value).ToString("0.#");
		gapValueLabel.Text = ((float)gapSlider.Value).ToString("0.#");
		thicknessValueLabel.Text = ((float)thicknessSlider.Value).ToString("0.#");

		// RGB 只显示整数（滑条步长 1）
		redValueLabel.Text = Mathf.RoundToInt((float)redSlider.Value).ToString();
		greenValueLabel.Text = Mathf.RoundToInt((float)greenSlider.Value).ToString();
		blueValueLabel.Text = Mathf.RoundToInt((float)blueSlider.Value).ToString();

		colorSwatch.Color = CrosshairSettingData.GetColor();
		preview.QueueRedraw();
	}

	private void OnResetPressed()
	{
		CrosshairSettingData.ResetToDefaults();
		SyncControlsFromData();
		// SyncControlsFromData 不会写回数据 / 通知准星，这里补一次（值已经是默认值了）
		ApplyToData();
	}

	// ------------------------------------------------------------------ 绘制

	/// <summary>预览区绘制：暗色底板 + 当前样式的准星。</summary>
	private void OnPreviewDraw()
	{
		Vector2 size = preview.Size;

		preview.DrawRect(new Rect2(Vector2.Zero, size), new Color(0.13f, 0.14f, 0.18f, 1f));
		preview.DrawRect(new Rect2(Vector2.Zero, size), new Color(0.42f, 0.44f, 0.52f, 0.8f), false, 1f);

		Crosshair.DrawShape(
			preview,
			size * 0.5f,
			CrosshairSettingData.ArmLength,
			CrosshairSettingData.Gap,
			CrosshairSettingData.Thickness,
			PreviewOutlineExtra,
			CrosshairSettingData.ShowDot ? CrosshairSettingData.DotRadius : 0f,
			CrosshairSettingData.GetColor(),
			new Color(0f, 0f, 0f, 0.7f));
	}

	// ------------------------------------------------------------------ 布局

	/// <summary>
	/// 让本控件铺满屏幕、面板居中。
	/// 父节点不是 Control（例如直接挂在 Node2D / CanvasLayer 下）时锚点不会自动生效，
	/// 需要基于视口尺寸手动定位（与 LoadoutSetting / LoadoutUI 的做法一致）。
	/// </summary>
	private void CenterUiOnScreen()
	{
		if (GetParent() is Control)
		{
			SetAnchorsPreset(LayoutPreset.FullRect);
		}
		else
		{
			Position = Vector2.Zero;
			Size = GetViewportRect().Size;
		}

		CenterPanelInSelf();
	}

	/// <summary>
	/// 把面板摆到本控件矩形的正中：独立使用时本控件铺满屏幕 → 面板居中于屏幕；
	/// 被容器装进某个标签页时本控件占满那一页 → 面板居中于该页。
	/// </summary>
	private void CenterPanelInSelf()
	{
		// 先让容器算出面板需要的最小尺寸
		panel.ResetSize();
		panel.Size = panel.GetCombinedMinimumSize();

		// 尚未完成布局（尺寸还是 0）时退回视口尺寸
		Vector2 area = Size;
		if (area.X <= 0f || area.Y <= 0f)
		{
			area = GetViewportRect().Size;
		}

		panel.Position = (area - panel.Size) * 0.5f;
	}
}
