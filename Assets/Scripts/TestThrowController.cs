using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Play mode test controller for lane friction comparison.
// Space: launch a ball on all three lanes
// 1 / 2 / 3: launch on Basic / Ice / Sand lane individually
// R: remove all spawned balls
public class TestThrowController : MonoBehaviour
{
    [Header("Launch Settings")]
    public float launchSpeed = 8f;
    public float ballMass = 7f;
    public float ballDiameter = 0.22f;

    [Header("Spawn Position")]
    public float startY = 0.14f;
    public float startZ = 0.5f;

    private readonly float[] laneX = { -3f, 0f, 3f };
    private readonly string[] laneNames = { "Basic", "Ice", "Sand" };
    private readonly List<GameObject> spawned = new List<GameObject>();

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.spaceKey.wasPressedThisFrame) LaunchAll();
        if (kb.digit1Key.wasPressedThisFrame) Launch(0);
        if (kb.digit2Key.wasPressedThisFrame) Launch(1);
        if (kb.digit3Key.wasPressedThisFrame) Launch(2);
        if (kb.rKey.wasPressedThisFrame) ClearBalls();
    }

    void LaunchAll()
    {
        for (int i = 0; i < laneX.Length; i++) Launch(i);
    }

    void Launch(int laneIndex)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "TestBall_" + laneNames[laneIndex];
        go.transform.position = new Vector3(laneX[laneIndex], startY, startZ);
        go.transform.localScale = Vector3.one * ballDiameter;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = ballMass;
        rb.linearVelocity = new Vector3(0f, 0f, launchSpeed);

        spawned.Add(go);
    }

    void ClearBalls()
    {
        foreach (var go in spawned)
        {
            if (go != null) Destroy(go);
        }
        spawned.Clear();
    }

    private GUIStyle debugTextStyle;

    void OnGUI()
    {
        string text =
            "Space : 3개 레인 동시 투구\n" +
            "1 / 2 / 3 : 기본 / 얼음 / 모래 레인 개별 투구\n" +
            "R : 공 전부 제거\n" +
            "투구 속도: " + launchSpeed + " m/s";

        Rect rect = new Rect(10, 10, 500, 120);

        if (debugTextStyle == null)
        {
            debugTextStyle = new GUIStyle(GUI.skin.label);
            debugTextStyle.fontStyle = FontStyle.Bold;
        }

        debugTextStyle.normal.textColor = Color.black;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                Rect outlineRect = new Rect(rect.x + dx, rect.y + dy, rect.width, rect.height);
                GUI.Label(outlineRect, text, debugTextStyle);
            }
        }

        debugTextStyle.normal.textColor = Color.white;
        GUI.Label(rect, text, debugTextStyle);
    }
}
