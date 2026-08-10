using System;
using UnityEngine;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance { get; private set; }

    [SerializeField] private int startingGold = 0;

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

        // 맵 진행(nextMapSceneName)으로 다음 스테이지 씬이 새로 로드되면 이 컴포넌트도 새로
        // 생성되어 기본값(startingGold)으로 초기화된다 - 저장된 값이 있으면 그걸로 이어받는다.
        // "새 게임"을 누르면 StartNewGame()이 곧바로 다시 0으로 덮어쓰므로 충돌하지 않는다.
        if (SaveManager.HasSaveData())
            LoadFromSave();
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
