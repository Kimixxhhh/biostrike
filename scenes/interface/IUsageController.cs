using Godot;
using System;

public interface IUsageController 
{
	void StatusChange();
	void UsageAttack1();
	void UsageAttack2();
	void SelectThisweapon();
	void UnselectThisweapon();
	void UsageReload();
}
