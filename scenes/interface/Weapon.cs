using Godot;
using System;

public partial class Weapon : Node2D
{
	//flip weapon
	//rotate weapon
	//Weapon Status
	//virtual StatusChange
	//select/unselect weapon
	private WeaponStatus weaponStatus = WeaponStatus.PUTAWAY;
	[Export]
    private WeaponData weaponData;
	public WeaponData WeaponData
	{
		get { return weaponData; }
		
	}
	public WeaponStatus WeaponStatus
	{
		get { return weaponStatus; }
		set { weaponStatus = value; }
	}
	private Vector2 actualWeaponTowards;
	private BasicWeaponAnime anime;
	public BasicWeaponAnime Anime
	{
		get { return anime; }
		set { anime = value; }
	}
	
	private Node2D pivot;
	public Node2D Pivot
	{
		get { return pivot; }
		set { pivot = value; }
	}
	private Marker2D marker;
	public Marker2D Marker
	{
		get { return marker; }
		set { marker = value; }
	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Pivot = GetNode<Node2D>("%Pivot");
		Anime = GetNode<BasicWeaponAnime>("%WeaponAnimeScene");
		Marker = GetNode<Marker2D>("%Marker2D");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		FlipWeaponFollowingMousePosition();
		RotateWeaponFollowingMousePosition(delta);
	}
	
	protected void FlipWeaponFollowingMousePosition()
	{
		Anime.FlipWeaponFollowingMouse((GetGlobalMousePosition()-GlobalPosition).X<0);
	}
	private void RotateWeaponFollowingMousePosition(double delta)
	{
		actualWeaponTowards = actualWeaponTowards.Lerp(
			GetGlobalMousePosition(), (float)(1.0-Mathf.Exp(-delta*10))
			);
		Pivot.LookAt(actualWeaponTowards.Lerp(
			GetGlobalMousePosition(), (float)(1.0-Mathf.Exp(-delta*10))
			));
	}
	public virtual bool StatusChange(WeaponStatus target){ return false;}
	protected virtual void StatusCancel(){}
	// switch weapon
	public virtual void SelectThis()
	{
		StatusChange(WeaponStatus.SWITCHING);
	}
	// triggered when switvh to other weapon
	public virtual void UnselectThis()
	{
		StatusChange(WeaponStatus.PUTAWAY);
	}
	protected void DoUnselectThis()
    {
        Anime.SetSpriteVisible(false);
        WeaponStatus = WeaponStatus.PUTAWAY;
    }
	
}

