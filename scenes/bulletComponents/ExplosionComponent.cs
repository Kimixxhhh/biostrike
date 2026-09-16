using Godot;
using System;

public partial class ExplosionComponent : Area2D
{
    public string targetCamp;
	public float FuseTime = 1.0f;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Monitoring = false;
        Monitorable = false;

        // 创建一个一次性计时器
        var timer = GetTree().CreateTimer(FuseTime);
        timer.Timeout += Explode;
		BodyEntered += OnBodyEntered;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	private void OnBodyEntered(Node2D body)
    {
        // 判断进入的是不是玩家
        if(body is CharacterBaseScene character
		//&&character.camp==targetCamp
		)
		{
			character.TakeImpact(new Vector2(100,100));
		}
    }
	private void Explode()
    {
        // 开启检测并主动查询当前重叠的物体
        Monitoring = true;

        foreach (var body in GetOverlappingBodies())
        {
            if(body is CharacterBaseScene character
			//&&character.camp==targetCamp
			)
			{
				character.TakeImpact(new Vector2(100,100));
			}
        }

        // 播放爆炸动画/粒子，然后销毁自己
        QueueFree();
    }
}
