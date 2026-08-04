using UnityEngine;

// 철핀: "모든 핀들의 재질이 철로 바뀜" (Skills.json)
// 모든 핀이 공유하는 머티리얼을 스킬별로 껐다 켰다 하기 애매해서 재질(색상) 변경은
// 구현하지 않고, "더 안 쓰러진다"는 물리적 효과만 구현한다 - 질량을 늘려서 넘어뜨리기
// 어렵게 만든다. ResetPin()이 위치/회전/속도만 되돌리고 mass는 건드리지 않으므로
// 한 번 늘려두면 이후 리셋에도 계속 유지된다.
public class IronPinSkillEffect : SkillEffect
{
    private const float MassMultiplier = 2.5f;

    public override string SkillName => "철핀";

    public override void OnAcquired()
    {
        if (PinDeckManager.Instance == null) return;

        foreach (BowlingPin pin in PinDeckManager.Instance.GetAllLanePins())
        {
            Rigidbody rb = pin.GetComponent<Rigidbody>();
            if (rb != null)
                rb.mass *= MassMultiplier;
        }

        Debug.Log("[철핀] 발동 - 모든 핀 질량 증가");
    }
}
