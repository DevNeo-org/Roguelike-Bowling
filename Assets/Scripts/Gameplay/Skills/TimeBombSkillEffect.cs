using System.Collections.Generic;
using UnityEngine;

// 시한폭탄: "투구 이후 일정 시간이 지난 뒤 터진다" (Skills.json)
// 투구 시작 후 일정 시간 뒤, 그 시점에 서 있는 핀 중 최대 2개를 무작위로 타격한다.
public class TimeBombSkillEffect : SkillEffect
{
    private const float FuseSeconds = 2.5f;
    private const int MaxPinsHit = 2;

    public override string SkillName => "시한폭탄";

    public override void OnThrowStart()
    {
        SkillRoutineRunner.RunDelayed(FuseSeconds, Detonate);
    }

    private void Detonate()
    {
        if (PinDeckManager.Instance == null) return;

        List<BowlingPin> standing = PinDeckManager.Instance.GetStandingPins();
        int hits = Mathf.Min(MaxPinsHit, standing.Count);

        for (int i = 0; i < hits; i++)
        {
            int index = Random.Range(0, standing.Count);
            standing[index].ForceKnockDown();
            standing.RemoveAt(index);
        }

        Debug.Log("[시한폭탄] 폭발 - 핀 " + hits + "개 타격");
    }
}
