using System;
using UnityEngine;

// Controls which lane is active based on the current stage number.
// Normal stages use the basic (wood) lane. Every 5th stage is a "boss
// stage" that swaps in one of the special lanes (Ice/Sand/Magma/Trampoline)
// instead of the basic lane.
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

        SetLaneActive(basicKey, activeLaneKey == basicKey);
        foreach (string key in specialLaneKeys)
            SetLaneActive(key, activeLaneKey == key);

        Debug.Log("[LaneStageManager] Stage " + CurrentStage +
            (IsBossStage(CurrentStage) ? " (보스 스테이지)" : "") +
            " -> 활성 레인: " + activeLaneKey);

        OnStageChanged?.Invoke(CurrentStage, activeLaneKey);
    }

    private void SetLaneActive(string key, bool active)
    {
        GameObject mapRoot = GameObject.Find(mapRootName);
        if (mapRoot == null) return;

        Transform[] all = mapRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            string n = t.name;
            bool matches = n.Contains("_" + key) || n == "Lane_" + key;

            // The magma fire visual doesn't follow the naming convention.
            if (key == "Magma" && n == "FireVisual")
                matches = true;

            if (matches)
                t.gameObject.SetActive(active);
        }
    }
}
