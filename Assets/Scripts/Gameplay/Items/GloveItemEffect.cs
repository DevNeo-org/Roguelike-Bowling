using UnityEngine;

// 장갑(아대): "투구 시 손목 안정성을 높여줍니다." (ShopItems.json)
// 손목이 안정되면 곡률(커브)이 덜 먹는다고 해석 - 스핀을 줄여 더 곧게 나가게 한다.
public class GloveItemEffect : ItemEffect
{
    private const float SpinMultiplier = 0.6f;

    public override string ItemName => "장갑(아대)";

    public override void OnBallSpawned(Rigidbody ballRb)
    {
        if (ballRb == null) return;

        BallSkillModifiers.GetOrAdd(ballRb.gameObject).SpinMultiplier *= SpinMultiplier;
    }
}
