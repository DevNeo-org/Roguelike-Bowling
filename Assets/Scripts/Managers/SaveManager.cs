using System.Collections.Generic;
using UnityEngine;

public static class SaveManager
{
    private const string GoldKey = "Save_Gold";
    private const string InventoryKey = "Save_Inventory";
    private const string HasSaveKey = "Save_Exists";
    private const char InventoryDelimiter = '|';

    public static bool HasSaveData()
    {
        return PlayerPrefs.GetInt(HasSaveKey, 0) == 1;
    }

    public static void SaveGame(int gold)
    {
        PlayerPrefs.SetInt(GoldKey, gold);
        PlayerPrefs.SetInt(HasSaveKey, 1);
        PlayerPrefs.Save();

        Debug.Log($"[저장] 자동 저장 완료 (골드: {gold})");
    }

    public static int LoadGold(int defaultValue)
    {
        return PlayerPrefs.GetInt(GoldKey, defaultValue);
    }

    public static void SaveInventory(IEnumerable<string> itemIds)
    {
        string joined = string.Join(InventoryDelimiter.ToString(), itemIds);
        PlayerPrefs.SetString(InventoryKey, joined);
        PlayerPrefs.SetInt(HasSaveKey, 1);
        PlayerPrefs.Save();

        Debug.Log($"[저장] 인벤토리 자동 저장 완료 ({joined})");
    }

    public static List<string> LoadInventory()
    {
        string joined = PlayerPrefs.GetString(InventoryKey, "");

        var result = new List<string>();
        if (string.IsNullOrEmpty(joined))
            return result;

        result.AddRange(joined.Split(InventoryDelimiter));
        return result;
    }

    public static void DeleteSave()
    {
        PlayerPrefs.DeleteKey(GoldKey);
        PlayerPrefs.DeleteKey(InventoryKey);
        PlayerPrefs.DeleteKey(HasSaveKey);
        PlayerPrefs.Save();

        Debug.Log("[저장] 저장 데이터 삭제");
    }
}
