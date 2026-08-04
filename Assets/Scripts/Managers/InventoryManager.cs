using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private readonly HashSet<string> ownedItems = new HashSet<string>();

    public event Action<string> OnItemAdded;

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

    public bool TryAddItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || ownedItems.Contains(itemId))
            return false;

        ownedItems.Add(itemId);
        OnItemAdded?.Invoke(itemId);
        AutoSave();

        if (ItemEffectRegistry.TryGet(itemId, out ItemEffect effect))
            effect.OnAcquired();

        return true;
    }

    // 공 생성처럼 "보유한 모든 아이템에 대해 훅을 호출"해야 하는 곳에서 쓴다.
    public IEnumerable<ItemEffect> GetOwnedEffects()
    {
        foreach (string itemId in ownedItems)
        {
            if (ItemEffectRegistry.TryGet(itemId, out ItemEffect effect))
                yield return effect;
        }
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
