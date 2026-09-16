using Godot;

/// <summary>
/// 鼠标十字准星（HUD）。
/// 跟随鼠标位置绘制一个「十字 + 中心点」的准星，用于 test/test.tscn 等测试场景。
/// - 根 Control 铺满屏幕，MouseFilter = Ignore，不拦截任何鼠标点击/悬停（UI 照常可用）。
/// - 建议挂在 CanvasLayer 下（见 scenes/UI/Crosshair.tscn 的用法），这样即使以后加入
///   Camera2D 移动镜头，准星也固定在屏幕坐标 = 鼠标位置。
/// 样式既可在 Inspector 里直接调（颜色 / 臂长 / 间隙 / 粗细 / 描边 / 中心点），
/// 也可以在运行时用 scenes/UI/CrosshairSetting.tscn 界面实时调整：
/// 本节点 _Ready 时会 Attach 到全局 CrosshairSettingData（第一个准星的 Inspector 值
/// 作为全局初始值），界面改动数据后通知本节点立刻重绘。
/// </summary>
public partial class Crosshair : Control
{
	/// <summary>准星主色（十字与中心点）。</summary>
	[Export] public Color CrosshairColor { get; set; } = new Color(0.88f, 0.96f, 1.00f, 1.00f);

	/// <summary>描边色（先画一圈深色描边，再画主色，保证在浅色/杂乱背景上也看得清）。</summary>
	[Export] public Color OutlineColor { get; set; } = new Color(0.00f, 0.00f, 0.00f, 0.70f);

	/// <summary>每条准星臂的长度（像素）。</summary>
	[Export(PropertyHint.Range, "0,64,0.5")] public float ArmLength { get; set; } = 9.0f;

	/// <summary>中心留空的间隙（像素），0 表示四臂连成完整十字。</summary>
	[Export(PropertyHint.Range, "0,32,0.5")] public float Gap { get; set; } = 4.0f;

	/// <summary>准星线条粗细（像素）。</summary>
	[Export(PropertyHint.Range, "0.5,10,0.5")] public float Thickness { get; set; } = 2.0f;

	/// <summary>描边比主色线条额外加粗的像素数，0 表示不描边。</summary>
	[Export(PropertyHint.Range, "0,8,0.5")] public float OutlineExtra { get; set; } = 2.0f;

	/// <summary>中心点半径，0 表示不画中心点。</summary>
	[Export(PropertyHint.Range, "0,8,0.5")] public float DotRadius { get; set; } = 1.5f;

	/// <summary>是否顺便隐藏系统箭头光标（只显示准星）。注意：隐藏后菜单/输入框仍然可点，
	/// 但看不到箭头，所以默认关闭；想要纯游戏准星时把它勾上。</summary>
	[Export] public bool HideSystemCursor { get; set; } = false;

	/// <summary>是否由本节点隐藏了系统光标（用于退出场景时还原，避免影响其它场景）。</summary>
	private bool cursorHiddenByUs;

	/// <summary>上一帧的鼠标位置，仅在鼠标移动时重绘。</summary>
	private Vector2 lastMousePosition = new Vector2(float.NaN, float.NaN);

	public override void _Ready()
	{
		// 铺满屏幕，坐标原点即屏幕左上角（配合 CanvasLayer 时）
		SetAnchorsPreset(LayoutPreset.FullRect);
		// 关键：不拦截鼠标事件，否则准星会挡住下面的按钮 / 输入框 / 场景点击
		MouseFilter = MouseFilterEnum.Ignore;

		if (HideSystemCursor)
		{
			Input.MouseMode = Input.MouseModeEnum.Hidden;
			cursorHiddenByUs = true;
		}

		// 接入全局准星设置（长度 / 间隙 / 粗细 / 颜色 / 中心点）：
		// 首次会把本节点的 Inspector 值作为全局初始值，之后 CrosshairSetting 界面的改动
		// 会通过 Changed 事件即时应用到这里。
		CrosshairSettingData.Attach(this);
		CrosshairSettingData.Changed += ApplySettingsFromData;

		QueueRedraw();
	}

	public override void _ExitTree()
	{
		// 退场时取消订阅，避免静态事件一直持有已释放的节点
		CrosshairSettingData.Changed -= ApplySettingsFromData;

		// 只还原被自己隐藏的光标，避免覆盖别人设置的 MouseMode
		if (cursorHiddenByUs && Input.MouseMode == Input.MouseModeEnum.Hidden)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		cursorHiddenByUs = false;
	}

	/// <summary>把全局 CrosshairSettingData 的设置套用到本准星并重绘。
	/// 既用于界面改动后的通知，也用于 _Ready 时的初始化。</summary>
	public void ApplySettingsFromData()
	{
		ArmLength = CrosshairSettingData.ArmLength;
		Gap = CrosshairSettingData.Gap;
		Thickness = CrosshairSettingData.Thickness;
		CrosshairColor = CrosshairSettingData.GetColor();
		DotRadius = CrosshairSettingData.ShowDot ? CrosshairSettingData.DotRadius : 0f;

		// 鼠标不动时 _Process 不会触发重绘，所以这里主动重绘一次
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		Vector2 mousePosition = GetLocalMousePosition();
		if (!mousePosition.IsEqualApprox(lastMousePosition))
		{
			lastMousePosition = mousePosition;
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		// GetLocalMousePosition() 已把鼠标坐标换算到本 Control 的坐标系，
		// 铺满屏幕时就是屏幕坐标，无需再处理拉伸 / 父级变换。
		DrawShape(this, GetLocalMousePosition(), ArmLength, Gap, Thickness, OutlineExtra, DotRadius, CrosshairColor, OutlineColor);
	}

	/// <summary>
	/// 绘制一个准星（供本节点与 CrosshairSetting 界面的预览区复用）。
	/// </summary>
	/// <param name="canvas">在哪块画布上绘制（通常就是调用者自己；必须在其绘制回调内调用）。</param>
	/// <param name="center">十字中心（canvas 的局部坐标）。</param>
	/// <param name="armLength">每条臂的长度。</param>
	/// <param name="gap">中心留空的间隙，0 表示四臂连成完整十字。</param>
	/// <param name="thickness">线条粗细。</param>
	/// <param name="outlineExtra">描边额外加粗的像素数，0 表示不描边。</param>
	/// <param name="dotRadius">中心点半径，0 表示不画中心点。</param>
	/// <param name="color">准星主色。</param>
	/// <param name="outlineColor">描边色。</param>
	public static void DrawShape(CanvasItem canvas, Vector2 center, float armLength, float gap, float thickness,
		float outlineExtra, float dotRadius, Color color, Color outlineColor)
	{
		Vector2[] directions = { Vector2.Left, Vector2.Right, Vector2.Up, Vector2.Down };
		float outlineWidth = thickness + outlineExtra;

		// 先画描边（更宽的深色线），再画主色细线 → 任何背景下都清晰
		if (outlineExtra > 0.0f)
		{
			foreach (Vector2 direction in directions)
			{
				DrawArm(canvas, center, direction, gap, armLength, outlineWidth, outlineColor);
			}
		}

		foreach (Vector2 direction in directions)
		{
			DrawArm(canvas, center, direction, gap, armLength, thickness, color);
		}

		// 中心点
		if (dotRadius > 0.0f)
		{
			if (outlineExtra > 0.0f)
			{
				canvas.DrawCircle(center, dotRadius + outlineExtra * 0.5f, outlineColor);
			}
			canvas.DrawCircle(center, dotRadius, color);
		}
	}

	/// <summary>画一条准星臂：从中心间隙外沿向外延伸 armLength。</summary>
	private static void DrawArm(CanvasItem canvas, Vector2 center, Vector2 direction, float gap,
		float armLength, float width, Color color)
	{
		canvas.DrawLine(
			center + direction * gap,
			center + direction * (gap + armLength),
			color,
			width,
			true);
	}
}
