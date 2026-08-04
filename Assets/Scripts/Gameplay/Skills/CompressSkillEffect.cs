using UnityEngine;

// 압축: "공 크기가 작아지고 속도가 증가" (Skills.json)
public class CompressSkillEffect : SkillEffect
{
    private const float ScaleMultiplier = 0.75f;
    private const float SpeedMultiplier = 1.25f;

    public override string SkillName => "압축";

    public override void OnBallSpawned(Rigidbody ballRb)
    {
        if (ballRb == null) return;

        ballRb.transform.localScale *= ScaleMultiplier;
        BallSkillModifiers.GetOrAdd(ballRb.gameObject).LaunchSpeedMultiplier *= SpeedMultiplier;
    }
}
