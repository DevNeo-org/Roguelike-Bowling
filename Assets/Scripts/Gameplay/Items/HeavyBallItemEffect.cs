using UnityEngine;

// 볼링공: "공의 무게(파운드)를 높여줍니다." (ShopItems.json)
public class HeavyBallItemEffect : ItemEffect
{
    private const float MassMultiplier = 1.3f;

    public override string ItemName => "볼링공";

    public override void OnBallSpawned(Rigidbody ballRb)
    {
        if (ballRb == null) return;

        ballRb.mass *= MassMultiplier;
    }
}
