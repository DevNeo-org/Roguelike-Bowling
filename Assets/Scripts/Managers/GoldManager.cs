using System;
using UnityEngine;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance { get; private set; }

    [SerializeField] private int startingGold = 500;

    public int CurrentGold { get; private set; }
    public event Action<int> OnGoldChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CurrentGold = startingGold;
    }

    public void StartNewGame()
    {
        CurrentGold = startingGold;
        OnGoldChanged?.Invoke(CurrentGold);
        AutoSave();
    }

    public void LoadFromSave()
    {
        CurrentGold = SaveManager.LoadGold(startingGold);
        OnGoldChanged?.Invoke(CurrentGold);
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0 || CurrentGold < amount)
            return false;

        CurrentGold -= amount;
        OnGoldChanged?.Invoke(CurrentGold);
        AutoSave();
        return true;
    }

    public void AddGold(int amount)
    {
        if (amount < 0)
            return;

        CurrentGold += amount;
        OnGoldChanged?.Invoke(CurrentGold);
        AutoSave();
    }

    private void AutoSave()
    {
        SaveManager.SaveGame(CurrentGold);
    }
}
