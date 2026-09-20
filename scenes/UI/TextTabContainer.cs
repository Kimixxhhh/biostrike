using System.Collections.Generic;
using Godot;

/// <summary>
/// 极简标签页容器：顶部一排无框文字标签（TextTabBar），下方是页面区域，同一时刻只显示当前页。
/// 选中效果 = 字体放大 20% + 字体变亮，没有任何「框住」的背景框 / 描边。
/// 内置 TabContainer 的 font_size 是全局的（按状态只能改颜色），做不到选中项单独放大字号，
/// 所以用它替代：页面用 AddTab(title, page) 加入，显示/隐藏由容器自动切换。
/// 用法：tabs.AddTab("设置", settingsPage); tabs.AddTab("开始", startPage); tabs.CurrentTab = 1;
/// </summary>
public partial class TextTabContainer : VBoxContainer
{
	/// <summary>切换标签时触发（点击标签或程序设置 CurrentTab 都会触发），参数为新标签下标。</summary>
	[Signal]
	public delegate void TabChangedEventHandler(int index);

	private readonly TextTabBar tabBar = new();
	private readonly MarginContainer pagesHolder = new();
	private readonly List<Control> pages = new();

	public TextTabContainer()
	{
		AddThemeConstantOverride("separation", BarPageSeparation); // 标签栏与页面区域的间距

		tabBar.Name = "TabBar";
		tabBar.TabChanged += OnTabChangedInBar;
		AddChild(tabBar);

		pagesHolder.Name = "Pages";
		pagesHolder.MouseFilter = MouseFilterEnum.Ignore; // 空白处不拦截鼠标
		pagesHolder.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		pagesHolder.SizeFlagsVertical = SizeFlags.ExpandFill;
		AddChild(pagesHolder);
	}

	/// <summary>标签栏与页面区域之间的竖直间距（构造时生效）。</summary>
	public int BarPageSeparation { get; set; } = 8;

	/// <summary>内部的文字标签栏（需要细调样式时用）。</summary>
	public TextTabBar Bar => tabBar;

	/// <summary>页面数量。</summary>
	public int TabCount => pages.Count;

	/// <summary>未选中标签的字号；选中标签 = 该值 × SelectedFontScale。</summary>
	public int FontSize
	{
		get => tabBar.FontSize;
		set => tabBar.FontSize = value;
	}

	/// <summary>选中标签的字号放大倍数（1.2 = 放大 20%）。</summary>
	public float SelectedFontScale
	{
		get => tabBar.SelectedFontScale;
		set => tabBar.SelectedFontScale = value;
	}

	/// <summary>相邻标签之间的水平间距。</summary>
	public int TabSeparation
	{
		get => tabBar.TabSeparation;
		set => tabBar.TabSeparation = value;
	}

	/// <summary>当前选中的标签下标（-1 = 未选中）。设置它会切换页面并触发 TabChanged。</summary>
	public int CurrentTab
	{
		get => tabBar.CurrentTab;
		set => tabBar.Select(value);
	}

	/// <summary>上一次选中的标签下标（-1 = 还没切换过）。</summary>
	public int PreviousTab => tabBar.PreviousTab;

	/// <summary>追加一页（页面会被自动铺满整个页面区域），返回标签下标。</summary>
	public int AddTab(string title, Control page)
	{
		int index = tabBar.AddTab(title);

		page.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		page.SizeFlagsVertical = SizeFlags.ExpandFill;
		pagesHolder.AddChild(page);
		pages.Add(page);

		// 非当前页直接隐藏（tabBar 首次添加会自动选中 0，那时本页还没进列表）
		page.Visible = index == tabBar.CurrentTab;

		return index;
	}

	/// <summary>选中某个标签（等同设置 CurrentTab）。</summary>
	public void SelectTab(int index)
	{
		tabBar.Select(index);
	}

	/// <summary>取标签标题。</summary>
	public string GetTabTitle(int index)
	{
		return tabBar.GetTabTitle(index);
	}

	/// <summary>改标签标题。</summary>
	public void SetTabTitle(int index, string title)
	{
		tabBar.SetTabTitle(index, title);
	}

	/// <summary>取某一页的根节点，越界返回 null。</summary>
	public Control GetPage(int index)
	{
		return index >= 0 && index < pages.Count ? pages[index] : null;
	}

	private void OnTabChangedInBar(int index)
	{
		UpdatePageVisibility();
		EmitSignal(SignalName.TabChanged, index);
	}

	/// <summary>只让当前页可见。</summary>
	private void UpdatePageVisibility()
	{
		for (int i = 0; i < pages.Count; i++)
		{
			pages[i].Visible = i == tabBar.CurrentTab;
		}
	}
}
