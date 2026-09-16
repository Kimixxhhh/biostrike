using Godot;

/// <summary>
/// 测试用武器输入 UI（挂在 res://test/test.tscn 下）—— 一个文本输入框。
/// 在输入框里打字后回车（或点击「添加」按钮），会把输入的文字作为参数调用
/// %CharacterBaseScene 的 WeaponController.AddWeapon(武器名)。
/// UI 全部由代码生成（与 scenes/UI 下的其它界面保持一致），
/// 面板固定在屏幕左上角，鼠标事件不拦截其它 UI/游戏。
/// </summary>
public partial class WeaponInputUI : Control
{
	/// <summary>角色场景节点路径，默认用唯一名 %CharacterBaseScene。</summary>
	[Export] public string CharacterNodePath { get; set; } = "%CharacterBaseScene";

	private LineEdit weaponNameEdit;
	private Button addButton;
	private Label statusLabel;

	public override void _Ready()
	{
		// 铺满屏幕，但自身不拦截鼠标事件（子节点仍可正常接收输入）
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Ignore;
		BuildUi();
	}

	private void BuildUi()
	{
		// 左上角面板
		var panel = new PanelContainer();
		panel.SetAnchorsPreset(LayoutPreset.TopLeft);
		panel.Position = new Vector2(20, 20);

		var panelBox = new StyleBoxFlat();
		panelBox.BgColor = new Color(0.08f, 0.09f, 0.12f, 0.92f);
		panelBox.BorderColor = new Color(0.42f, 0.44f, 0.52f, 1f);
		panelBox.SetBorderWidthAll(2);
		panelBox.SetCornerRadiusAll(8);
		panelBox.SetContentMarginAll(10);
		panel.AddThemeStyleboxOverride("panel", panelBox);
		AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 8);
		panel.AddChild(vbox);

		var title = new Label();
		title.Text = "添加武器（AddWeapon）";
		title.AddThemeColorOverride("font_color", new Color(0.85f, 0.88f, 0.95f));
		vbox.AddChild(title);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);
		vbox.AddChild(row);

		weaponNameEdit = new LineEdit();
		weaponNameEdit.CustomMinimumSize = new Vector2(240, 0);
		weaponNameEdit.PlaceholderText = "武器名，如 AR15 / Glock / HelixMkI / M67";
		weaponNameEdit.TextSubmitted += OnWeaponNameSubmitted;
		row.AddChild(weaponNameEdit);

		addButton = new Button();
		addButton.Text = "添加";
		addButton.Pressed += OnAddButtonPressed;
		row.AddChild(addButton);

		statusLabel = new Label();
		statusLabel.Text = "输入武器名后回车，或点击「添加」";
		statusLabel.AddThemeColorOverride("font_color", new Color(0.60f, 0.65f, 0.75f));
		vbox.AddChild(statusLabel);
	}

	/// <summary>回车提交（LineEdit.TextSubmitted）。</summary>
	private void OnWeaponNameSubmitted(string newText)
	{
		AddWeaponFromInput(newText);
	}

	/// <summary>点击「添加」按钮提交。</summary>
	private void OnAddButtonPressed()
	{
		AddWeaponFromInput(weaponNameEdit.Text);
	}

	/// <summary>核心逻辑：把输入的文字作为参数调用 WeaponController.AddWeapon()。</summary>
	private void AddWeaponFromInput(string rawName)
	{
		string weaponName = (rawName ?? string.Empty).Trim();
		if (string.IsNullOrEmpty(weaponName))
		{
			SetStatus("请输入武器名", false);
			return;
		}

		var chara = GetNodeOrNull<CharacterBaseScene>(CharacterNodePath);
		if (chara == null || chara.WeaponController == null)
		{
			SetStatus($"未找到角色节点：{CharacterNodePath}", false);
			return;
		}

		// 先校验场景资源是否存在，给出明确提示（AddWeapon 内部找不到时只会静默 return）
		string weaponPath = Paths.GetWeaponPath(weaponName);
		if (!ResourceLoader.Exists(weaponPath))
		{
			SetStatus($"找不到武器「{weaponName}」：{weaponPath}", false);
			return;
		}

		chara.WeaponController.AddWeapon(weaponName);
		SetStatus($"已调用 AddWeapon(\"{weaponName}\")", true);

		// 释放焦点，让测试场景的 P/N/L 等快捷键恢复可用
		weaponNameEdit.ReleaseFocus();
	}

	private void SetStatus(string text, bool success)
	{
		if (statusLabel == null)
		{
			return;
		}
		statusLabel.Text = text;
		statusLabel.AddThemeColorOverride("font_color",
			success ? new Color(0.55f, 0.90f, 0.60f) : new Color(1.00f, 0.60f, 0.55f));
	}
}
