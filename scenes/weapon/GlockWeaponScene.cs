using Godot;
using System;

public partial class GlockWeaponScene : Weapon
{
    private SelectionController selectionContrtoller;
    private ReloadController reloadContrtoller;
    private WeaponRangeStatusController weaponRangeStatusController;
    private AmmoController ammoController;
    private FireSingleController fireController;
    public override void _Ready()
	{
		base._Ready();
        
        selectionContrtoller = GetNode<SelectionController>("%SelectionController");
        selectionContrtoller.Setup(this);

        ammoController = GetNode<AmmoController>("%AmmoController");
        ammoController.Setup(this);

        reloadContrtoller = GetNode<ReloadController>("%ReloadController");
        reloadContrtoller.Setup(this,ammoController);

        weaponRangeStatusController = 
        GetNode<WeaponRangeStatusController>("%WeaponRangeStatusController");

        fireController = GetNode<FireSingleController>("%FireSingleController");
        fireController.Setup(this,ammoController,reloadContrtoller);

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
    public override bool StatusChange(WeaponStatus target)
	{
		WeaponStatusCommand cmd =
        weaponRangeStatusController.StatusChange(WeaponStatus,target);
        
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
}
