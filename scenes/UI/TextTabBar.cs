using System.Collections.Generic;
using Godot;

/// <summary>
/// 无背景框的文字标签栏：选中项用「字号放大 + 字体变亮」区分，不画任何背景框 / 描边 / 焦点框。
/// 之所以不用内置的 TabBar：它的 font_size 主题项是全局的（按状态只能改颜色），
/// 没法让选中项单独放大字号；所以这里用一排完全扁平的 Button 自己实现，
/// 每个标签分别设置字号与字体颜色。
/// 用法：new TextTabBar() → AddTab("设置") / AddTab("开始") → Select(0)（或 CurrentTab = 0）。
/// </summary>
public partial class TextTabBar : HBoxContainer
{
	/// <summary>选中的标签发生变化时触发（点击标签、程序调用 Select / 设置 CurrentTab 都会触发）。</summary>
	[Signal]
	public delegate void TabChangedEventHandler(int index);

	/// <summary>未选中标签的字号；选中标签的字号 = FontSize * SelectedFontScale。</summary>
	[Export] public int FontSize { get; set; } = 20;

	/// <summary>选中标签的字号放大倍数（1.2 = 放大 20%）。</summary>
	[Export] public float SelectedFontScale { get; set; } = 1.2f;

	/// <summary>相邻标签之间的水平间距（像素）。</summary>
	[Export] public int TabSeparation { get; set; } = 16;

	/// <summary>选中标签的字体颜色（三种状态里最亮）。</summary>
	[Export] public Color SelectedFontColor { get; set; } = new Color(1f, 0.94f, 0.68f);

	/// <summary>未选中标签的字体颜色。</summary>
	[Export] public Color UnselectedFontColor { get; set; } = new Color(0.70f, 0.74f, 0.82f);

	/// <summary>鼠标悬停（未选中）时的字体颜色，比常态略亮。</summary>
	[Export] public Color HoveredFontColor { get; set; } = new Color(0.88f, 0.91f, 0.98f);

	private readonly List<Button> tabButtons = new();
	private readonly List<string> tabTitles = new();

	private int currentTab = -1;
	private int previousTab = -1;

	/// <summary>所有标签、所有状态共用的空样式盒（完全透明，只留一点内边距）。</summary>
	private StyleBoxEmpty emptyBox;

	public override void _Ready()
	{
		Alignment = AlignmentMode.Center; // 整排标签水平居中
		RefreshStyles();                  // 若 FontSize 等属性在建树后改动过，这里补一次
	}

	/// <summary>标签数量。</summary>
	public int TabCount => tabButtons.Count;

	/// <summary>当前选中的标签下标（-1 = 未选中）。</summary>
	public int CurrentTab
	{
		get => currentTab;
		set => Select(value);
	}

	/// <summary>上一次选中的标签下标（-1 = 还没切换过）。</summary>
	public int PreviousTab => previousTab;

	/// <summary>
	/// 追加一个标签，返回它的下标。首次添加时会自动选中下标 0，保证始终有一项是高亮的。
	/// 选中状态不会让标签变宽以外的东西：这里不画背景框。
	/// </summary>
	public int AddTab(string title)
	{
		int index = tabButtons.Count;

		var button = new Button();
		button.Name = $"Tab{index}";
		button.Text = title;
		button.Flat = true;                                  // 常态不画背景
		button.FocusMode = FocusModeEnum.None;               // 不要键盘焦点框
		button.SizeFlagsVertical = SizeFlags.ShrinkCenter;   // 不被最高（选中）项拉伸，垂直居中
		button.MouseDefaultCursorShape = CursorShape.PointingHand;
		ApplyEmptyStyleBoxes(button);

		button.Pressed += () => Select(index);

		tabButtons.Add(button);
		tabTitles.Add(title);
		AddChild(button);

		ApplyTabStyle(index);

		if (currentTab < 0)
		{
			Select(0); // 第一次添加时默认选中第一项
		}

		return index;
	}

	/// <summary>取标签标题。</summary>
	public string GetTabTitle(int index)
	{
		return index >= 0 && index < tabTitles.Count ? tabTitles[index] : string.Empty;
	}

	/// <summary>改标签标题。</summary>
	public void SetTabTitle(int index, string title)
	{
		if (index < 0 || index >= tabButtons.Count)
		{
			return;
		}

		tabTitles[index] = title;
		tabButtons[index].Text = title;
	}

	/// <summary>选中某个标签（已经是选中状态则什么都不做）。</summary>
	public void Select(int index)
	{
		if (index < 0 || index >= tabButtons.Count || index == currentTab)
		{
			return;
		}

		previousTab = currentTab;
		currentTab = index;
		RefreshStyles();
		EmitSignal(SignalName.TabChanged, index);
	}

	/// <summary>按当前选中状态刷新所有标签的字号与字体颜色。</summary>
	public void RefreshStyles()
	{
		AddThemeConstantOverride("separation", TabSeparation);

		for (int i = 0; i < tabButtons.Count; i++)
		{
			ApplyTabStyle(i);
		}
	}

	/// <summary>选中项 = 字号放大（+20%）+ 字体变亮；悬停 = 略亮；其余 = 常态色。全程不画框。</summary>
	private void ApplyTabStyle(int index)
	{
		var button = tabButtons[index];
		bool selected = index == currentTab;

		int size = selected ? Mathf.RoundToInt(FontSize * SelectedFontScale) : FontSize;
		button.AddThemeFontSizeOverride("font_size", size);

		Color normal = selected ? SelectedFontColor : UnselectedFontColor;
		Color hovered = selected ? SelectedFontColor : HoveredFontColor;

		button.AddThemeColorOverride("font_color", normal);
		button.AddThemeColorOverride("font_hover_color", hovered);
		button.AddThemeColorOverride("font_pressed_color", hovered);
		button.AddThemeColorOverride("font_hover_pressed_color", hovered);
		button.AddThemeColorOverride("font_focus_color", normal);
		button.AddThemeColorOverride("font_disabled_color", UnselectedFontColor);
	}

	/// <summary>把按钮的全部状态样式盒换成同一个透明样式盒，彻底去掉背景框与焦点框。</summary>
	private void ApplyEmptyStyleBoxes(Button button)
	{
		emptyBox ??= CreateEmptyBox();

		button.AddThemeStyleboxOverride("normal", emptyBox);
		button.AddThemeStyleboxOverride("hover", emptyBox);
		button.AddThemeStyleboxOverride("pressed", emptyBox);
		button.AddThemeStyleboxOverride("disabled", emptyBox);
		button.AddThemeStyleboxOverride("focus", emptyBox);
	}

	/// <summary>透明样式盒：没有背景色 / 描边，只有一点内边距（让点击区域和标签间距舒服些）。</summary>
	private static StyleBoxEmpty CreateEmptyBox()
	{
		var box = new StyleBoxEmpty();
		box.ContentMarginLeft = 6;
		box.ContentMarginRight = 6;
		box.ContentMarginTop = 6;
		box.ContentMarginBottom = 6;
		return box;
	}
}
