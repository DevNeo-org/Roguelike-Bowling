using System.Collections.Generic;

// 아이템 이름(ShopItems.json의 "name" - InventoryManager가 보유 여부를 추적할 때 쓰는 것과
// 동일한 ID)을 실제 ItemEffect 구현체에 연결해주는 등록부. SkillEffectRegistry와 동일한 패턴.
public static class ItemEffectRegistry
{
    private static readonly ItemEffect[] allEffects =
    {
        new GloveItemEffect(),
        new HeavyBallItemEffect(),
        new OilTowelItemEffect(),
        new StrengthItemEffect(),
    };

    private static Dictionary<string, ItemEffect> lookup;

    private static Dictionary<string, ItemEffect> Lookup
    {
        get
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, ItemEffect>();
                foreach (ItemEffect effect in allEffects)
                    lookup[effect.ItemName] = effect;
            }

            return lookup;
        }
    }

    public static bool TryGet(string itemName, out ItemEffect effect)
    {
        return Lookup.TryGetValue(itemName, out effect);
    }
}
