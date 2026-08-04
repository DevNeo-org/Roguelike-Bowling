using UnityEngine;

// 리무브: "낮은 확률로 핀 하나를 즉시 제거" (Skills.json)
public class RemovePinSkillEffect : SkillEffect
{
    private const float TriggerChance = 0.15f;

    public override string SkillName => "리무브";

    public override void OnThrowStart()
    {
        if (Random.value > TriggerChance) return;
        if (PinDeckManager.Instance == null) return;

        var standingPins = PinDeckManager.Instance.GetStandingPins();
        if (standingPins.Count == 0) return;

        BowlingPin target = standingPins[Random.Range(0, standingPins.Count)];
        target.ForceKnockDown();

        Debug.Log("[리무브] 발동 - " + target.name + " 즉시 제거");
    }
}
