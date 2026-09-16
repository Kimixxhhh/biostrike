using Godot;
using System;

public partial class WeaponUtilityStatusController : Node,IStatusController
{
    public WeaponStatusCommand StatusChange(WeaponStatus current,WeaponStatus target)
    {
        switch (current)
    {
        case WeaponStatus.PUTAWAY:
            switch (target)
            {
                case WeaponStatus.SWITCHING:
                    return WeaponStatusCommand.ALLOWED;
                default:
                    
                    break;
            }
            break; 
        case WeaponStatus.READY:
            switch (target)
            {
                case WeaponStatus.RELOADING:
                    return WeaponStatusCommand.ALLOWED;
                case WeaponStatus.PUTAWAY:
                    return WeaponStatusCommand.ALLOWED;
                default:
                    
                    break;
            }
            break; 
        case WeaponStatus.RELOADING:
            switch (target)
            {
                case WeaponStatus.PUTAWAY:
                    return WeaponStatusCommand.CANCEL_THEN_ALLOWED;
                
                default:
                    
                    break;
            }
            break; 
        case WeaponStatus.SWITCHING:
            switch (target)
            {
                case WeaponStatus.PUTAWAY:
                    return WeaponStatusCommand.CANCEL_THEN_ALLOWED;
                default:
                    
                    break;
            }
            break;
        
        default:
            
            break;
    }

        return WeaponStatusCommand.NOT_ALLOWED;
    }
}
