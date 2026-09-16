using Godot;
using System;

public partial class Paths 
{
    public static string GetWeaponPath(string weaponName)
    {
        //res://scenes/weapon/AR15WeaponScene.tscn
        return $"res://scenes/weapon/{weaponName}WeaponScene.tscn";
    }
}
