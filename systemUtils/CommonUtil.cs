using Godot;
using System;

public partial class CommonUtil : Node
{
    public static void PlaySfx(Node parent,AudioStream sound, float vol = 0f,string busName = "SFX")
    {
        if (sound == null) return;

        var player = new AudioStreamPlayer();
        player.Stream = sound;
        player.Bus = busName;
        parent.AddChild(player);
		player.VolumeDb = vol; 

        // 播放结束后自动释放
        player.Finished += player.QueueFree;

        player.Play(); 
    }
}
