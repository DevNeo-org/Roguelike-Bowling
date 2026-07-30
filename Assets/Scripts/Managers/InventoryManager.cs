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
