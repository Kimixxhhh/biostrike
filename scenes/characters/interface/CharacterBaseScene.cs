using Godot;
using System;

public partial class CharacterBaseScene : CharacterBody2D
{
	//zombie/human
	public string camp; 
	//direction of velocity
	private Vector2 direction;
	//Acceleration 
	private float accIndex = 1200;
	//resistance index (prevent CharacterBody from Accelerating infinitely)
	private float resisIndex = 12;
	private float actualResisIndex ;
	private float ResisRecoveryIndex = 80;
	//divergence limit = 2 * physics fps, beyond this velocity explodes
	private float maxResisIndex;

	private CharaStats charaStats;
	private SkillsController skillsController;
	private WeaponController weaponController;
	private HealthComponent healthComponent;
	public HealthComponent HealthComponent
	{
		get { return healthComponent; }
	}
	public WeaponController WeaponController
	{
		get { return weaponController; }
	}

	public override void _Ready()
	{
		weaponController = GetNode<WeaponController>("WeaponController");
		healthComponent = GetNode<HealthComponent>("HealthComponent");
		charaStats = GetNode<CharaStats>("CharaStats");
		skillsController = GetNode<SkillsController>("SkillsController");
		skillsController.Setup(charaStats);
		actualResisIndex = resisIndex;
		maxResisIndex = Engine.PhysicsTicksPerSecond*2f;
	}
	public override void _PhysicsProcess(double delta)
	{
		direction = getMovementVector().Normalized();
		setMyVelocity(delta);
		MoveAndSlide();
		RecoverResisIndex((float)delta);
	}
	private Vector2 getMovementVector()
	{
		return new Vector2(Input.GetActionStrength("moveRight")-Input.GetActionStrength("moveLeft"),
		Input.GetActionStrength("moveDown")-Input.GetActionStrength("moveUp"));
	}
	private void setMyVelocity(double delta)
	{
		if(weaponController.WeaponSelectionIndex == -1
		||weaponController.EquippedWeapon[weaponController.WeaponSelectionIndex]==null)
		{
			//friction goes stronger when speed get faster
			Velocity+= -Velocity*actualResisIndex*(float)delta;

			Velocity+= direction*charaStats.GetStat("AccIndex")*(float)delta;
			return;
		}
		//friction goes stronger when speed get faster
		Velocity+= -Velocity*actualResisIndex*(float)delta;

		Velocity+= direction*charaStats.GetStat("AccIndex")*(float)delta*
		weaponController.EquippedWeapon[weaponController.WeaponSelectionIndex].WeaponData.MoveInertia;
	}
	public void TakeImpact(Vector2 velocity)
	{
		Velocity+=velocity; 
	}
	public void SlowDown(float resisIndexIncreasement)
	{
		//clamp stacking so it can never exceed the divergence limit
		actualResisIndex = Mathf.Min(actualResisIndex+resisIndexIncreasement, maxResisIndex);
	}
	private void RecoverResisIndex(float delta)
	{
		actualResisIndex = Mathf.MoveToward(actualResisIndex, resisIndex, ResisRecoveryIndex*(float)delta);
	}
	
}
