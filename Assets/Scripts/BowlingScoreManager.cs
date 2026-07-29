using System;
using System.Collections.Generic;
using UnityEngine;

// Tracks rolls for all 10 frames of a standard bowling game and computes
// scores with proper strike/spare bonus lookahead. Singleton, following
// the same pattern as GoldManager/InventoryManager.
public class BowlingScoreManager : MonoBehaviour
{
    public static BowlingScoreManager Instance { get; private set; }

    public const int FrameCount = 10;

    private List<int>[] frameRolls = new List<int>[FrameCount];
    private int currentFrame;

    public int CurrentFrame => currentFrame;
    public bool IsGameComplete => currentFrame >= FrameCount;

    public event Action OnScoreChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitFrames();
    }

    private void InitFrames()
    {
        for (int i = 0; i < FrameCount; i++)
            frameRolls[i] = new List<int>();
        currentFrame = 0;
    }

    public void StartNewGame()
    {
        InitFrames();
        OnScoreChanged?.Invoke();
    }

    public List<int> GetFrameRolls(int frameIndex)
    {
        return frameRolls[frameIndex];
    }

    // Call this once per throw with the number of pins knocked down (0-10).
// Call this once per throw with the number of pins knocked down (0-10).
    public void RecordRoll(int pinsKnocked)
    {
        if (IsGameComplete)
        {
            Debug.Log("[BowlingScoreManager] RecordRoll(" + pinsKnocked + ") ignored - game already complete");
            return;
        }

        pinsKnocked = Mathf.Clamp(pinsKnocked, 0, 10);
        List<int> rolls = frameRolls[currentFrame];
        rolls.Add(pinsKnocked);

        bool frameDone;
        if (currentFrame < FrameCount - 1)
        {
            if (rolls.Count == 1 && pinsKnocked == 10) frameDone = true;
            else if (rolls.Count == 2) frameDone = true;
            else frameDone = false;
        }
        else
        {
            frameDone = IsFrame10Finished(rolls);
        }

        Debug.Log("[BowlingScoreManager] RecordRoll(" + pinsKnocked + ") -> frame=" + (currentFrame + 1) +
            " rollsInFrame=" + rolls.Count + " frameDone=" + frameDone);

        if (frameDone)
            currentFrame++;

        OnScoreChanged?.Invoke();
    }

    private bool IsFrame10Finished(List<int> rolls)
    {
        if (rolls.Count < 2) return false;
        if (rolls.Count == 2)
        {
            bool strike = rolls[0] == 10;
            bool spare = rolls[0] + rolls[1] == 10;
            return !(strike || spare);
        }
        return rolls.Count >= 3;
    }

    private List<int> FlattenRolls()
    {
        List<int> flat = new List<int>();
        for (int i = 0; i < FrameCount; i++)
            flat.AddRange(frameRolls[i]);
        return flat;
    }

    // Computes per-frame score and cumulative running total.
    // Entries are null where the frame isn't fully determinable yet
    // (e.g. a strike whose bonus rolls haven't happened).
    public void ComputeScores(out int?[] frameScores, out int?[] runningTotals)
    {
        frameScores = new int?[FrameCount];
        runningTotals = new int?[FrameCount];

        List<int> rolls = FlattenRolls();
        int rollIndex = 0;
        int running = 0;
        bool chainBroken = false;

        for (int frame = 0; frame < FrameCount; frame++)
        {
            if (chainBroken) continue;

            List<int> thisFrame = frameRolls[frame];
            if (thisFrame.Count == 0)
            {
                chainBroken = true;
                continue;
            }

            if (frame < FrameCount - 1)
            {
                bool isStrike = thisFrame[0] == 10;
                bool isSpare = !isStrike && thisFrame.Count >= 2 && thisFrame[0] + thisFrame[1] == 10;

                if (isStrike)
                {
                    if (rollIndex + 2 < rolls.Count)
                    {
                        int frameScore = 10 + rolls[rollIndex + 1] + rolls[rollIndex + 2];
                        running += frameScore;
                        frameScores[frame] = frameScore;
                        runningTotals[frame] = running;
                    }
                    else
                    {
                        chainBroken = true;
                    }
                    rollIndex += 1;
                }
                else if (isSpare)
                {
                    if (rollIndex + 2 < rolls.Count)
                    {
                        int frameScore = 10 + rolls[rollIndex + 2];
                        running += frameScore;
                        frameScores[frame] = frameScore;
                        runningTotals[frame] = running;
                    }
                    else
                    {
                        chainBroken = true;
                    }
                    rollIndex += 2;
                }
                else
                {
                    if (thisFrame.Count >= 2)
                    {
                        int frameScore = thisFrame[0] + thisFrame[1];
                        running += frameScore;
                        frameScores[frame] = frameScore;
                        runningTotals[frame] = running;
                        rollIndex += 2;
                    }
                    else
                    {
                        chainBroken = true;
                    }
                }
            }
            else
            {
                if (IsFrame10Finished(thisFrame))
                {
                    int frameScore = 0;
                    foreach (int r in thisFrame) frameScore += r;
                    running += frameScore;
                    frameScores[frame] = frameScore;
                    runningTotals[frame] = running;
                }
                else
                {
                    chainBroken = true;
                }
            }
        }
    }
}
