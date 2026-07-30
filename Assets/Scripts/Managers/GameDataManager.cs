using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SkillEntry
{
    public string name;
    public string type;
    public string description;
    public int price;
}

[System.Serializable]
public class ShopEntry
{
    public string name;
    public string description;
    public int price;
    public string icon;
}

[System.Serializable]
internal class SkillDataFile
{
    public List<SkillEntry> skills;
}

[System.Serializable]
internal class ShopDataFile
{
    public List<ShopEntry> items;
}

// Assets/Resources/GameData/Skills.json, ShopItems.json 를 읽어 게임 콘텐츠 목록(JSON 리스트)을 로드한다.
public static class GameDataManager
{
    private static List<SkillEntry> cachedSkills;
    private static List<ShopEntry> cachedShopItems;

    public static List<SkillEntry> LoadSkills()
    {
        if (cachedSkills == null)
        {
            var file = LoadJson<SkillDataFile>("GameData/Skills");
            cachedSkills = file != null ? file.skills : new List<SkillEntry>();
        }

        return cachedSkills;
    }

    public static List<ShopEntry> LoadShopItems()
    {
        if (cachedShopItems == null)
        {
            var file = LoadJson<ShopDataFile>("GameData/ShopItems");
            cachedShopItems = file != null ? file.items : new List<ShopEntry>();
        }

        return cachedShopItems;
    }

    // source에서 겹치지 않게 count개를 무작위로 뽑는다 (SkillPanelController의 무료 픽, ShopAreaController의 장신구 리롤이 공유).
    public static List<SkillEntry> PickRandomDistinct(List<SkillEntry> source, int count)
    {
        var pool = new List<SkillEntry>(source);
        var result = new List<SkillEntry>();
        int pickCount = Mathf.Min(count, pool.Count);

        for (int i = 0; i < pickCount; i++)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }

    private static T LoadJson<T>(string resourcePath) where T : class
    {
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
        {
            Debug.LogError($"[GameDataManager] Resources/{resourcePath}.json 파일을 찾을 수 없습니다.");
            return null;
        }

        return JsonUtility.FromJson<T>(asset.text);
    }
}
