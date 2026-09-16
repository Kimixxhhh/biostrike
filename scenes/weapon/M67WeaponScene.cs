using Godot;
using System;

public partial class M67WeaponScene : Weapon
{
    //private BasicUsageController usageContrtoller;
    private SelectionController selectionContrtoller; 
    private ReloadController reloadContrtoller;
    private IStatusController weaponStatusController;   
    private AmmoController ammoController;
    private UtilityFireController fireController;
    public override void _Ready()
	{
		base._Ready();
        
        selectionContrtoller = GetNode<SelectionController>("%SelectionController");
        selectionContrtoller.Setup(this);

        ammoController = GetNode<AmmoController>("%AmmoController");
        ammoController.Setup(this);

        reloadContrtoller = GetNode<ReloadController>("%ReloadController");
        reloadContrtoller.Setup(this,ammoController);

        weaponStatusController = 
        GetNode<WeaponUtilityStatusController>("%WeaponUtilityStatusController");

        fireController = GetNode<UtilityFireController>("%UtilityFireController");
        fireController.Setup(this,ammoController,reloadContrtoller);

    }
    public override void _Process(double delta)
	{
        // utility does not rotate like weapon range
		FlipWeaponFollowingMousePosition();
	}
    public override bool StatusChange(WeaponStatus target)
	{
		WeaponStatusCommand cmd =
        weaponStatusController.StatusChange(WeaponStatus,target);
        
        if(cmd == WeaponStatusCommand.ALLOWED)
        {
            return true;
        }else if(cmd == WeaponStatusCommand.CANCEL_THEN_ALLOWED) {
            StatusCancel();
            return true;
        }
        return false;
	}
    protected override void StatusCancel()
    {
        if(WeaponStatus == WeaponStatus.RELOADING)
        {
            reloadContrtoller.Cancel();
        }
        if(WeaponStatus == WeaponStatus.SWITCHING)
        {
            selectionContrtoller.Cancel();
        }

    }
    public override void SelectThis()
    {
        if (StatusChange(WeaponStatus.SWITCHING))
        {
            selectionContrtoller.SwitchThis();
        }
    }
    public override void UnselectThis()
    {
        if (StatusChange(WeaponStatus.PUTAWAY))
        {
            DoUnselectThis();
        }
    }
}
