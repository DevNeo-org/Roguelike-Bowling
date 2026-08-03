using UnityEngine;

// 거대화: "공 크기가 커짐" (Skills.json)
public class EnlargeSkillEffect : SkillEffect
{
    private const float ScaleMultiplier = 1.35f;

    public override string SkillName => "거대화";

    public override void OnBallSpawned(Rigidbody ballRb)
    {
        if (ballRb == null) return;

        ballRb.transform.localScale *= ScaleMultiplier;
    }
}
