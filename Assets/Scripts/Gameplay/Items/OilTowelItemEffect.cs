using UnityEngine;

// 기름 제거 수건: "볼링공에 묻은 기름기를 제거합니다." (ShopItems.json)
// 기름기가 없어지면 공 표면 그립이 좋아진다고 해석 - 공 콜라이더의 마찰을 살짝 높인다.
public class OilTowelItemEffect : ItemEffect
{
    private const float FrictionMultiplier = 1.25f;

    public override string ItemName => "기름 제거 수건";

    public override void OnBallSpawned(Rigidbody ballRb)
    {
        if (ballRb == null) return;

        Collider col = ballRb.GetComponent<Collider>();
        if (col == null || col.material == null) return;

        // col.material은 처음 접근하는 순간 공유 에셋이 아닌 이 콜라이더 전용 인스턴스로
        // 자동 복제되므로, 다른 공/레인에 영향을 주지 않는다.
        PhysicsMaterial mat = col.material;
        mat.dynamicFriction *= FrictionMultiplier;
        mat.staticFriction *= FrictionMultiplier;
    }
}
