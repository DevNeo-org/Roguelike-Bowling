using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Displays the 10-frame bowling scoreboard by looking up the Frame_1..Frame_10
// child objects created under this GameObject and filling their throw/total
// text fields from BowlingScoreManager. Also updates a separate TotalBox
// showing the overall running score.
public class BowlingScoreboardUI : MonoBehaviour
{
    private class FrameSlots
    {
        public TextMeshProUGUI throw1;
        public TextMeshProUGUI throw2;
        public TextMeshProUGUI throw3;
        public TextMeshProUGUI total;
    }

    private readonly FrameSlots[] slots = new FrameSlots[BowlingScoreManager.FrameCount];
    private TextMeshProUGUI grandTotalText;

    private void Awake()
    {
        Transform totalBox = transform.Find("TotalBox");
        if (totalBox != null)
            grandTotalText = totalBox.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();

        for (int i = 0; i < BowlingScoreManager.FrameCount; i++)
        {
            Transform frame = transform.Find("Frame_" + (i + 1));
            if (frame == null)
            {
                Debug.LogWarning("[BowlingScoreboardUI] Frame_" + (i + 1) + " not found under " + name);
                continue;
            }

            slots[i] = new FrameSlots
            {
                throw1 = frame.Find("Throw1Text")?.GetComponent<TextMeshProUGUI>(),
                throw2 = frame.Find("Throw2Text")?.GetComponent<TextMeshProUGUI>(),
                throw3 = frame.Find("Throw3Text")?.GetComponent<TextMeshProUGUI>(),
                total = frame.Find("TotalText")?.GetComponent<TextMeshProUGUI>()
            };
        }
    }

    private void Start()
    {
        if (BowlingScoreManager.Instance != null)
        {
            BowlingScoreManager.Instance.OnScoreChanged += Refresh;
            Refresh();
        }
        else
        {
            Debug.LogWarning("[BowlingScoreboardUI] BowlingScoreManager instance not found.");
        }
    }

    private void OnDestroy()
    {
        if (BowlingScoreManager.Instance != null)
            BowlingScoreManager.Instance.OnScoreChanged -= Refresh;
    }

    public void Refresh()
    {
        var mgr = BowlingScoreManager.Instance;
        if (mgr == null) return;

        mgr.ComputeScores(out int?[] frameScores, out int?[] runningTotals);

        int? latestKnownTotal = null;

        for (int i = 0; i < BowlingScoreManager.FrameCount; i++)
        {
            if (runningTotals[i].HasValue)
                latestKnownTotal = runningTotals[i];

            FrameSlots s = slots[i];
            if (s == null) continue;

            List<int> rolls = mgr.GetFrameRolls(i);
            bool isLastFrame = i == BowlingScoreManager.FrameCount - 1;

            SetThrowText(s.throw1, rolls, 0, isLastFrame);
            SetThrowText(s.throw2, rolls, 1, isLastFrame);
            if (s.throw3 != null)
                SetThrowText(s.throw3, rolls, 2, isLastFrame);

            if (s.total != null)
                s.total.text = runningTotals[i].HasValue ? runningTotals[i].Value.ToString() : "-";
        }

        if (grandTotalText != null)
            grandTotalText.text = latestKnownTotal.HasValue ? latestKnownTotal.Value.ToString() : "-";
    }

    private void SetThrowText(TextMeshProUGUI label, List<int> rolls, int rollIdx, bool isLastFrame)
    {
        if (label == null) return;

        if (rollIdx >= rolls.Count)
        {
            label.text = "";
            return;
        }

        label.text = FormatRoll(rolls, rollIdx, isLastFrame);
    }

private string FormatRoll(List<int> rolls, int idx, bool isLastFrame)
    {
        int val = rolls[idx];

        if (val == 10) return "X";

        if (!isLastFrame)
        {
            if (idx == 1 && rolls[0] + rolls[1] == 10) return "/";
            return val == 0 ? "-" : val.ToString();
        }

        // 10th frame: spare detection resets after a strike, mirroring
        // standard bowling scorecard notation.
        if (idx == 1 && rolls[0] != 10 && rolls[0] + rolls[1] == 10) return "/";
        if (idx == 2)
        {
            if (rolls[0] == 10 && rolls[1] != 10 && rolls[1] + val == 10) return "/";
            if (rolls[0] != 10 && rolls[1] != 10 && rolls[1] + val == 10) return "/";
        }

        return val == 0 ? "-" : val.ToString();
    }
}
