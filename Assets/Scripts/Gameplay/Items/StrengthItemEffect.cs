using UnityEngine;

// 근력: "더 높은 파운드로, 더 빠르게 던질 수 있습니다." (ShopItems.json)
public class StrengthItemEffect : ItemEffect
{
    private const float MassMultiplier = 1.15f;
    private const float SpeedMultiplier = 1.15f;

    public override string ItemName => "근력";

    public override void OnBallSpawned(Rigidbody ballRb)
    {
        if (ballRb == null) return;

        ballRb.mass *= MassMultiplier;
        BallSkillModifiers.GetOrAdd(ballRb.gameObject).LaunchSpeedMultiplier *= SpeedMultiplier;
    }
}
