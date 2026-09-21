using System.Collections.Generic;
using Godot;

/// <summary>
/// 极简标签页容器：顶部一排无框文字标签（TextTabBar），下方是页面区域，同一时刻只显示当前页。
/// 选中效果 = 字体放大 20% + 字体变亮，默认不画任何背景框 / 描边。
/// 可选：把标签栏背景做成「毛玻璃」（半透明 + 模糊其后面的画面），见 BarBackdropEnabled 一组属性。
/// 内置 TabContainer 的 font_size 是全局的（按状态只能改颜色），做不到选中项单独放大字号，
/// 所以用它替代：页面用 AddTab(title, page) 加入，显示/隐藏由容器自动切换。
/// 用法：tabs.AddTab("设置", settingsPage); tabs.AddTab("开始", startPage); tabs.CurrentTab = 1;
/// </summary>
public partial class TextTabContainer : VBoxContainer
{
	/// <summary>切换标签时触发（点击标签或程序设置 CurrentTab 都会触发），参数为新标签下标。</summary>
	[Signal]
	public delegate void TabChangedEventHandler(int index);

	/// <summary>毛玻璃背景用的着色器（相对项目根目录）。</summary>
	private const string BackdropBlurShaderPath = "res://scenes/UI/BackdropBlur.gdshader";

	private readonly TextTabBar tabBar = new();

	/// <summary>
	/// 包住标签栏的 PanelContainer：它唯一的作用就是画出「标签栏背景」那一整块矩形
	/// （PanelContainer 会按标签栏的最小高度自动撑高，不用手算高度）。
	/// 默认样式盒全透明 = 与以前一样看不见。
	/// </summary>
	private readonly PanelContainer barHolder = new();

	/// <summary>标签栏背景的样式盒（留引用，方便运行时改底色 / 内边距）。</summary>
	private readonly StyleBoxFlat barHolderStyle = new();

	private readonly MarginContainer pagesHolder = new();
	private readonly List<Control> pages = new();

	/// <summary>毛玻璃材质（着色器加载成功后才创建）。</summary>
	private ShaderMaterial barBackdropMaterial;

	private bool barBackdropEnabled;
	private Color barBackdropTint = new(0.05f, 0.06f, 0.09f, 0.70f);
	private float barBackdropBlurRadius = 16f;
	private float barBackdropTintStrength = 0.45f;

	// 默认 0 = 毛玻璃条高度与标签栏完全一致，不会把下方页面区往下推（老布局尺寸不变）
	private int barBackdropPadding;

	public TextTabContainer()
	{
		AddThemeConstantOverride("separation", BarPageSeparation); // 标签栏与页面区域的间距

		// 标签栏包一层 PanelContainer：位置/大小与旧的「HBox 直接当 VBox 子节点」完全一致，
		// 只是多了一个可以画背景的矩形（默认透明）。
		barHolder.Name = "TabBarHolder";
		barHolder.MouseFilter = MouseFilterEnum.Ignore; // 空白处不拦截鼠标（子节点照常接收）
		barHolderStyle.BgColor = new Color(0f, 0f, 0f, 0f);
		barHolder.AddThemeStyleboxOverride("panel", barHolderStyle);
		AddChild(barHolder);

		tabBar.Name = "TabBar";
		tabBar.TabChanged += OnTabChangedInBar;
		barHolder.AddChild(tabBar);

		pagesHolder.Name = "Pages";
		pagesHolder.MouseFilter = MouseFilterEnum.Ignore; // 空白处不拦截鼠标
		pagesHolder.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		pagesHolder.SizeFlagsVertical = SizeFlags.ExpandFill;
		AddChild(pagesHolder);

		ApplyBarBackdrop();
	}

	/// <summary>标签栏与页面区域之间的竖直间距（构造时生效）。</summary>
	public int BarPageSeparation { get; set; } = 8;

	/// <summary>内部的文字标签栏（需要细调样式时用）。</summary>
	public TextTabBar Bar => tabBar;

	/// <summary>
	/// 标签栏背景是否用「毛玻璃」：半透明底色 + 模糊它后面的画面。
	/// 默认关闭（背景全透明，与旧行为一致）。注意：着色器读的是「已经画好的画面」，
	/// 所以它只会模糊标签栏底下的内容，不会糊到标签文字本身。
	/// </summary>
	public bool BarBackdropEnabled
	{
		get => barBackdropEnabled;
		set
		{
			barBackdropEnabled = value;
			ApplyBarBackdrop();
		}
	}

	/// <summary>毛玻璃底色：Alpha = 面板透明度（0 = 全透明，1 = 不透明）；RGB 按 BarBackdropTintStrength 叠在模糊画面上。</summary>
	public Color BarBackdropTint
	{
		get => barBackdropTint;
		set
		{
			barBackdropTint = value;
			ApplyBarBackdrop();
		}
	}

	/// <summary>背景模糊半径（像素），越大越糊。</summary>
	public float BarBackdropBlurRadius
	{
		get => barBackdropBlurRadius;
		set
		{
			barBackdropBlurRadius = Mathf.Max(0f, value);
			ApplyBarBackdrop();
		}
	}

	/// <summary>底色 RGB 的叠加强度：0 = 只用模糊画面（清透），1 = 只用底色（浑浊）。</summary>
	public float BarBackdropTintStrength
	{
		get => barBackdropTintStrength;
		set
		{
			barBackdropTintStrength = Mathf.Clamp(value, 0f, 1f);
			ApplyBarBackdrop();
		}
	}

	/// <summary>
	/// 标签栏背景的上下内边距（像素）：默认 0 = 毛玻璃条高度与标签栏一致（不会挤动下方页面区）；
	/// 调大一些会让这条毛玻璃更「厚」，看起来更像一条完整的顶部横条。
	/// </summary>
	public int BarBackdropPadding
	{
		get => barBackdropPadding;
		set
		{
			barBackdropPadding = Mathf.Max(0, value);
			ApplyBarBackdrop();
		}
	}

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

	/// <summary>
	/// 把当前的毛玻璃设置应用到标签栏背景：
	/// 关闭 = 全透明样式盒 + 不挂着色器（与旧行为完全一致）；
	/// 开启 = 挂上 BackdropBlur 着色器，同时保留半透明底色做兜底（着色器加载失败也还能看到底色）。
	/// </summary>
	private void ApplyBarBackdrop()
	{
		barHolderStyle.ContentMarginLeft = 0;
		barHolderStyle.ContentMarginRight = 0;
		barHolderStyle.ContentMarginTop = barBackdropPadding;
		barHolderStyle.ContentMarginBottom = barBackdropPadding;

		if (barBackdropEnabled)
		{
			barHolderStyle.BgColor = barBackdropTint; // 仅兜底：着色器会接管画出来的颜色
			barHolder.Material = GetOrCreateBackdropMaterial();
		}
		else
		{
			barHolderStyle.BgColor = new Color(0f, 0f, 0f, 0f);
			barHolder.Material = null;
		}

		// 内边距变了会影响最小高度，手动催一次重排 / 重绘
		barHolder.UpdateMinimumSize();
		barHolder.QueueRedraw();
	}

	/// <summary>懒加载毛玻璃材质；着色器缺失或加载失败时返回 null（退化成纯半透明底色）。</summary>
	private ShaderMaterial GetOrCreateBackdropMaterial()
	{
		if (barBackdropMaterial == null)
		{
			Shader shader = ResourceLoader.Exists(BackdropBlurShaderPath)
				? GD.Load<Shader>(BackdropBlurShaderPath)
				: null;
			if (shader == null)
			{
				GD.PushWarning($"[TextTabContainer] 无法加载 {BackdropBlurShaderPath}，标签栏背景退化为纯半透明色");
				return null;
			}

			barBackdropMaterial = new ShaderMaterial { Shader = shader };
		}

		barBackdropMaterial.SetShaderParameter("tint", barBackdropTint);
		barBackdropMaterial.SetShaderParameter("blur_radius", barBackdropBlurRadius);
		barBackdropMaterial.SetShaderParameter("tint_strength", barBackdropTintStrength);
		return barBackdropMaterial;
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
