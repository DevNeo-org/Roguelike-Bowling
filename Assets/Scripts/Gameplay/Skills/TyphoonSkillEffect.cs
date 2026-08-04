using UnityEngine;

// 태풍: "바람의 힘으로 주변 핀에 데미지를 준다" (Skills.json)
// 투구가 시작되는 순간, 서 있는 핀 각각에 확률적으로 바람을 맞혀 쓰러뜨린다.
public class TyphoonSkillEffect : SkillEffect
{
    private const float PerPinChance = 0.12f;

    public override string SkillName => "태풍";

    public override void OnThrowStart()
    {
        if (PinDeckManager.Instance == null) return;

        foreach (BowlingPin pin in PinDeckManager.Instance.GetStandingPins())
        {
            if (Random.value <= PerPinChance)
                pin.ForceKnockDown();
        }
    }
}
