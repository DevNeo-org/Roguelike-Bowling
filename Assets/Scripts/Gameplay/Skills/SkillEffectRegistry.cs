using System.Collections.Generic;

// 스킬 이름(Skills.json의 "name" - SkillManager가 보유 여부를 추적할 때 쓰는 것과 동일한 ID)을
// 실제 SkillEffect 구현체에 연결해주는 등록부.
// 실제 스킬 효과를 구현하면 allEffects에 한 줄 추가하면 된다: new EarthquakeSkillEffect(),
public static class SkillEffectRegistry
{
    private static readonly SkillEffect[] allEffects =
    {
        new CompressSkillEffect(),
        new RemovePinSkillEffect(),
        new EnlargeSkillEffect(),
        new ErrorSkillEffect(),
        new RainSkillEffect(),
        new TyphoonSkillEffect(),
        new IronPinSkillEffect(),
        new TimeBombSkillEffect(),
    };

    private static Dictionary<string, SkillEffect> lookup;

    private static Dictionary<string, SkillEffect> Lookup
    {
        get
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, SkillEffect>();
                foreach (SkillEffect effect in allEffects)
                    lookup[effect.SkillName] = effect;
            }

            return lookup;
        }
    }

    public static bool TryGet(string skillName, out SkillEffect effect)
    {
        return Lookup.TryGetValue(skillName, out effect);
    }
}
