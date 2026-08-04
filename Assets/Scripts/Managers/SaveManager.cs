using System.Collections.Generic;
using UnityEngine;

public static class SaveManager
{
    private const string GoldKey = "Save_Gold";
    private const string InventoryKey = "Save_Inventory";
    private const string StageKey = "Save_Stage";
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

    // 스테이지 진행도는 "스테이지가 시작되는 시점"을 저장 단위로 삼는다 - 불러오면 그
    // 스테이지의 1프레임부터 다시 시작하며, 프레임 중간 물리 상태까지는 복원하지 않는다.
    public static void SaveStage(int stage)
    {
        PlayerPrefs.SetInt(StageKey, stage);
        PlayerPrefs.SetInt(HasSaveKey, 1);
        PlayerPrefs.Save();

        Debug.Log($"[저장] 스테이지 자동 저장 완료 (스테이지: {stage})");
    }

    public static int LoadStage(int defaultValue)
    {
        return PlayerPrefs.GetInt(StageKey, defaultValue);
    }

    public static void DeleteSave()
    {
        PlayerPrefs.DeleteKey(GoldKey);
        PlayerPrefs.DeleteKey(InventoryKey);
        PlayerPrefs.DeleteKey(StageKey);
        PlayerPrefs.DeleteKey(HasSaveKey);
        PlayerPrefs.Save();

        Debug.Log("[저장] 저장 데이터 삭제");
    }
}
