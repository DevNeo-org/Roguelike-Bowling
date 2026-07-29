using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Play mode test controller. Launches a single ball down the currently
// active lane (matches LaneStageManager.ActiveLaneKey) so it can hit the
// pins and score through PinDeckManager -> BowlingScoreManager.
// Throws now have some randomness (sideways drift, spin, speed variance)
// so they can miss center and test the gutters.
// Space: launch a ball
// R: remove any leftover test balls
// E: spawn a banana peel + rock at random spots on the current lane
public class TestThrowController : MonoBehaviour
{
    [Header("Launch Settings")]
    public float launchSpeed = 8f;
    public float ballMass = 7f;
    public float ballDiameter = 0.22f;
    public Color ballColor = new Color(0.15f, 0.35f, 0.95f, 1f);

    [Header("Spawn Position")]
    public float startY = 0.14f;
    public float startZ = 0.5f;

    [Header("Randomness")]
    [Tooltip("Max sideways speed (m/s) randomly added, positive or negative.")]
    public float maxSidewaysSpeed = 2.2f;
    [Tooltip("Max random variation in forward launch speed (m/s).")]
    public float speedVariance = 1.0f;

    [Header("Obstacle Prefabs")]
    public string bananaPeelPrefabPath = "Assets/Prefabs/Obstacles/BananaPeel.prefab";
    public string rockPrefabPath = "Assets/Prefabs/Obstacles/Rock.prefab";
    public string obstaclesParentName = "Obstacles";
    public Color bananaPeelColor = new Color(0.95f, 0.5f, 0.05f, 1f);
    public Color rockColor = new Color(0.35f, 0.3f, 0.28f, 1f);

    [Tooltip("Min/max Z range along the lane where obstacles can spawn (0 = start, ~17 = pins).")]
    public float obstacleZMin = 3f;
    public float obstacleZMax = 14f;

    [Tooltip("Max sideways offset from lane center for obstacle spawn position.")]
    public float obstacleXRange = 0.35f;

    private readonly List<GameObject> spawned = new List<GameObject>();
    private readonly List<GameObject> spawnedObstacles = new List<GameObject>();

    private static readonly Dictionary<string, float> laneX = new Dictionary<string, float>
    {
        { "Basic", -3f },
        { "Ice", 0f },
        { "Sand", 3f },
        { "Magma", 6f },
        { "Trampoline", 9f }
    };

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.spaceKey.wasPressedThisFrame) Launch();
        if (kb.rKey.wasPressedThisFrame) ClearBalls();
        if (kb.eKey.wasPressedThisFrame) SpawnRandomObstacles();
    }

    void Launch()
    {
        string activeLane = LaneStageManager.Instance != null ? LaneStageManager.Instance.ActiveLaneKey : "Basic";
        float x = laneX.ContainsKey(activeLane) ? laneX[activeLane] : -3f;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "TestBall_" + activeLane;
        go.tag = "Ball";
        go.transform.position = new Vector3(x, startY, startZ);
        go.transform.localScale = Vector3.one * ballDiameter;

        SetColor(go, ballColor);

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = ballMass;

        float sideways = Random.Range(-maxSidewaysSpeed, maxSidewaysSpeed);
        float forwardSpeed = launchSpeed + Random.Range(-speedVariance, speedVariance);
        rb.linearVelocity = new Vector3(sideways, 0f, forwardSpeed);

        // A bit of random spin so the ball can curve like a real hook shot.
        rb.angularVelocity = new Vector3(0f, Random.Range(-3f, 3f), 0f);

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        go.AddComponent<BallFinishWatcher>();

        spawned.Add(go);

        Debug.Log("[TestThrowController] 투구: sideways=" + sideways.ToString("F2") + " forwardSpeed=" + forwardSpeed.ToString("F2"));
    }

    void ClearBalls()
    {
        foreach (var go in spawned)
        {
            if (go != null) Destroy(go);
        }
        spawned.Clear();
    }

    void SpawnRandomObstacles()
    {
#if UNITY_EDITOR
        string activeLane = LaneStageManager.Instance != null ? LaneStageManager.Instance.ActiveLaneKey : "Basic";
        float laneCenterX = laneX.ContainsKey(activeLane) ? laneX[activeLane] : -3f;

        GameObject parent = GameObject.Find(obstaclesParentName);

        // Clear any obstacles we previously spawned for testing.
        foreach (var go in spawnedObstacles)
        {
            if (go != null) Destroy(go);
        }
        spawnedObstacles.Clear();

        SpawnOneObstacle(bananaPeelPrefabPath, "TestBananaPeel_" + activeLane, laneCenterX, parent, 0.03f, bananaPeelColor);
        SpawnOneObstacle(rockPrefabPath, "TestRock_" + activeLane, laneCenterX, parent, 0.1f, rockColor);

        Debug.Log("[TestThrowController] " + activeLane + " 레인에 장애물 랜덤 스폰");
#endif
    }

#if UNITY_EDITOR
    void SpawnOneObstacle(string prefabPath, string name, float laneCenterX, GameObject parent, float y, Color color)
    {
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[TestThrowController] Prefab not found: " + prefabPath);
            return;
        }

        float x = laneCenterX + Random.Range(-obstacleXRange, obstacleXRange);
        float z = Random.Range(obstacleZMin, obstacleZMax);

        GameObject instance = Instantiate(prefab, new Vector3(x, y, z), Quaternion.identity);
        instance.name = name;
        if (parent != null) instance.transform.SetParent(parent.transform, true);

        SetColor(instance, color);

        spawnedObstacles.Add(instance);
    }
#endif

    void SetColor(GameObject go, Color color)
    {
        Renderer rend = go.GetComponent<Renderer>();
        if (rend == null) return;

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", color);
        mpb.SetColor("_Color", color);
        rend.SetPropertyBlock(mpb);
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 500, 100),
            "Space : 공 투구 (현재 레인, 랜덤 방향)\n" +
            "R : 남은 테스트 공 제거\n" +
            "E : 현재 레인에 장애물 랜덤 스폰\n" +
            "투구 속도: " + launchSpeed + " m/s");
    }
}
