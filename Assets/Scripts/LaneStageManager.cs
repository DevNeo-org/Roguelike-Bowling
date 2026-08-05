using System;
using UnityEngine;

// Was previously used to show/hide individual lanes so only one was ever
// active at a time. The current design (1 stage = 1 place + 5 lane
// variations, all visible together) no longer wants that auto-hiding,
// so ApplyStage() no longer touches any GameObject's active state.
// ActiveLaneKey / CurrentStage are kept for now since other systems
// (TestThrowController, obstacle spawning) still read ActiveLaneKey as
// a fallback default.
public class LaneStageManager : MonoBehaviour
{
    public static LaneStageManager Instance { get; private set; }

    [SerializeField] private string mapRootName = "Map";
    [SerializeField] private int bossStageInterval = 5;

    private readonly string[] specialLaneKeys = { "Ice", "Sand", "Magma", "Trampoline" };
    private const string basicKey = "Basic";

    public int CurrentStage { get; private set; } = 1;
    public string ActiveLaneKey { get; private set; } = basicKey;

    public event Action<int, string> OnStageChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartNewGame()
    {
        CurrentStage = 1;
        ApplyStage();
    }

    public void AdvanceStage()
    {
        CurrentStage++;
        ApplyStage();
    }

    public bool IsBossStage(int stage)
    {
        return stage % bossStageInterval == 0;
    }

    public void ApplyStage()
    {
        string activeLaneKey;
        if (IsBossStage(CurrentStage))
        {
            int bossIndex = (CurrentStage / bossStageInterval - 1) % specialLaneKeys.Length;
            activeLaneKey = specialLaneKeys[bossIndex];
        }
        else
        {
            activeLaneKey = basicKey;
        }

        ActiveLaneKey = activeLaneKey;

        // NOTE: no longer calling SetLaneActive here - all 5 lanes of the
        // current place/stage stay visible together now.

        Debug.Log("[LaneStageManager] Stage " + CurrentStage +
            (IsBossStage(CurrentStage) ? " (보스 스테이지)" : "") +
            " -> 기본 선택 레인: " + activeLaneKey);

        OnStageChanged?.Invoke(CurrentStage, activeLaneKey);
    }
}
