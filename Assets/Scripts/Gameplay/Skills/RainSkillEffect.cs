using UnityEngine;

// 비: "레인에 비를 내려 더 미끄럽게 한다" (Skills.json)
// 획득한 순간부터 남은 게임 내내 현재 레인이 더 미끄러워진다(구름 저항 감소).
public class RainSkillEffect : SkillEffect
{
    private const float ResistanceMultiplier = 0.4f;

    public override string SkillName => "비";

    public override void OnAcquired()
    {
        GameObject lane = GameObject.Find("Lane_Basic");
        if (lane == null) return;

        LaneFrictionZone friction = lane.GetComponent<LaneFrictionZone>();
        if (friction == null) return;

        friction.rollingResistanceCoefficient *= ResistanceMultiplier;
        Debug.Log("[비] 발동 - 레인 마찰 감소");
    }
}
