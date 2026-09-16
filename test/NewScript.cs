using Godot;
using System;

public partial class NewScript : Node
{
	private CharacterBaseScene chara;
	private Weapon weaponTest ;
	private SlotGridUI buyMenu;
	private LoadoutUI loadoutUI;
	private CrosshairSetting crosshairSetting;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		chara = GetNode<CharacterBaseScene>("%CharacterBaseScene");
		buyMenu = GetNode<SlotGridUI>("SlotGridUI");
		buyMenu.PlayerMoney = 9999; // 演示：设置玩家金钱并显示在右下角
		loadoutUI = GetNodeOrNull<LoadoutUI>("LoadoutUI");
		if (loadoutUI == null)
		{
			GD.PushError("未找到 LoadoutUI 节点（脚本可能未挂载）");
		}
		crosshairSetting = GetNodeOrNull<CrosshairSetting>("HUD/CrosshairSetting");
		if (crosshairSetting == null)
		{
			GD.PushError("未找到 CrosshairSetting 节点（准星设置界面）");
		}
		chara.WeaponController.AddWeapon("AR15");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	 public override void _Input(InputEvent @event)
	{
		// 焦点在文本输入框（如 WeaponInputUI 的 LineEdit）时，不响应测试快捷键，
		// 否则打字会误触发 testp / OpenBuyMenu / OpenLoadout 等动作。
		var focusOwner = GetViewport().GuiGetFocusOwner();
		if (focusOwner is LineEdit || focusOwner is TextEdit)
		{
			return;
		}

		// if (@event.IsActionPressed("attack1"))
		// {
		// 	anime.PlayAttack1Anime();
		// }
		// if (@event.IsActionPressed("reload"))
		// {
		// 	anime.PlayReloadAnime();
		// }
		// if (@event.IsActionPressed("switchPrevious"))
		// {
		// 	weaponTest.SelectThis();
		// }
		// if (@event.IsActionPressed("switchNext"))
		// {
		// 	weaponTest.UnselectThis();
		// }
		if (@event.IsActionPressed("testp"))
		{
			//chara.WeaponController.AddWeapon("AR15");
			chara.WeaponController.AddWeapon("Glock");
		}
		if (@event.IsActionPressed("OpenBuyMenu"))
		{
			buyMenu.Visible = !buyMenu.Visible;
		}
		if (@event.IsActionPressed("OpenLoadout"))
		{
			if (loadoutUI != null)
			{
				loadoutUI.Visible = !loadoutUI.Visible;
			}
		}
		if (@event.IsActionPressed("OpenCrosshairSetting"))
		{
			if (crosshairSetting != null)
			{
				crosshairSetting.Visible = !crosshairSetting.Visible;
			}
		}
	}

}
