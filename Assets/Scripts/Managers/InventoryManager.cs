using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // 동시에 보유할 수 없는 아이템 쌍(정반대 효과라 함께 사면 서로 상쇄되는 것들).
    private static readonly (string a, string b)[] ExclusivePairs =
    {
        (ItemIds.OldTowel, ItemIds.WaxTowel),
    };

    private readonly HashSet<string> ownedItems = new HashSet<string>();

    public event Action<string> OnItemAdded;
    public event Action<string> OnItemRemoved;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool IsOwned(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) && ownedItems.Contains(itemId);
    }

    /// <summary>itemId와 함께 보유할 수 없는 아이템이 이미 있으면 그 아이템 ID를 반환한다.</summary>
    public string GetConflictingOwnedItem(string itemId)
    {
        foreach (var pair in ExclusivePairs)
        {
            if (pair.a == itemId && ownedItems.Contains(pair.b)) return pair.b;
            if (pair.b == itemId && ownedItems.Contains(pair.a)) return pair.a;
        }
        return null;
    }

    public bool TryAddItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || ownedItems.Contains(itemId))
            return false;

        if (GetConflictingOwnedItem(itemId) != null)
            return false;

        ownedItems.Add(itemId);
        OnItemAdded?.Invoke(itemId);
        AutoSave();

        return true;
    }

    /// <summary>1회용 소모 아이템 등에 사용 — 보유 목록에서 제거한다.</summary>
    public bool RemoveItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || !ownedItems.Remove(itemId))
            return false;

        OnItemRemoved?.Invoke(itemId);
        AutoSave();

        return true;
    }

    public void StartNewGame()
    {
        ownedItems.Clear();
        AutoSave();
    }

    public void LoadFromSave()
    {
        ownedItems.Clear();

        foreach (var id in SaveManager.LoadInventory())
            ownedItems.Add(id);
    }

    private void AutoSave()
    {
        SaveManager.SaveInventory(ownedItems);
    }
}
