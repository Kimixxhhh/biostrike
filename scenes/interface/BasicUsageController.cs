using Godot;
using System;

public partial class BasicUsageController : Node,IUsageController
{
	private Weapon weaponScene;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready(){}
	public void SetUp(Weapon weaponSceneParam)
	{
		weaponScene = weaponSceneParam;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta){}
	 public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("attack1"))
		{
			UsageAttack1();
		}
		if (@event.IsActionPressed("reload"))
		{
			UsageReload();
		}
	}

	public void StatusChange(){}
	public void UsageAttack1()
	{
		weaponScene.Anime.PlayAttack1Anime();
	}
	public void UsageAttack2(){}
	public void SelectThisweapon()
	{
		
	}
	public void UnselectThisweapon(){}
	public void UsageReload()
	{
		weaponScene.Anime.PlayReloadAnime();
	}
}
