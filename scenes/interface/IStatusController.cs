using Godot;
using System;

public interface IStatusController
{
    WeaponStatusCommand StatusChange(WeaponStatus current,WeaponStatus target);
}
