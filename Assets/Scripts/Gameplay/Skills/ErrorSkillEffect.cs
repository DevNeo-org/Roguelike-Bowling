using System.Collections.Generic;
using UnityEngine;

// 오류: "핀이 하나 남았을 때 일정 확률로 쓰러진다" (Skills.json)
public class ErrorSkillEffect : SkillEffect
{
    private const float TriggerChance = 0.35f;

    public override string SkillName => "오류";

    public override void OnPinsSettled(List<BowlingPin> standingPins)
    {
        if (standingPins.Count != 1) return;
        if (Random.value > TriggerChance) return;

        standingPins[0].ForceKnockDown();
        Debug.Log("[오류] 발동 - 마지막 핀 제거");
    }
}
