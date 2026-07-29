using UnityEngine;
using UnityEngine.InputSystem;

// Temporary debug helper to test the bowling scoreboard without full
// pin-detection gameplay hooked up yet.
// N: start new game (reset scoreboard)
// 0-9 (top row) or Numpad 0-9: record a roll with that many pins
// X: record a strike (10)
public class ScoreboardDebugRoller : MonoBehaviour
{
    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || BowlingScoreManager.Instance == null) return;

        if (kb.nKey.wasPressedThisFrame)
        {
            BowlingScoreManager.Instance.StartNewGame();
            Debug.Log("[ScoreboardDebugRoller] New game started");
        }

        if (kb.xKey.wasPressedThisFrame)
        {
            BowlingScoreManager.Instance.RecordRoll(10);
        }

        for (int i = 0; i <= 9; i++)
        {
            if (IsDigitPressed(kb, i))
            {
                BowlingScoreManager.Instance.RecordRoll(i);
            }
        }
    }

    private bool IsDigitPressed(Keyboard kb, int digit)
    {
        Key topRowKey = Key.Digit0 + digit;
        Key numpadKey = Key.Numpad0 + digit;
        return kb[topRowKey].wasPressedThisFrame || kb[numpadKey].wasPressedThisFrame;
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 140, 500, 60),
            "[스코어보드 테스트] N: 새 게임, 0~9: 해당 핀 수만큼 투구, X: 스트라이크(10)");
    }
}
