using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Play mode test controller. Multiple separate stages exist side by side in
// the scene (Mountain at x=-3, Classic bowling alley at x=100, Desert at
// x=200), each with 5 lanes laid out one after another along Z.
// Space: launch a ball down the selected lane
// R: remove any leftover test balls
// E: spawn a banana peel + rock at random spots on the current lane
// 1~5: jump to lane 1~5 within the current stage
// Tab: cycle between stages
public class TestThrowController : MonoBehaviour
{
    [Header("Launch Settings")]
    public float launchSpeed = 8f;
    public float ballMass = 7f;
    public float ballDiameter = 0.22f;
    public Color ballColor = new Color(0.15f, 0.35f, 0.95f, 1f);

    [Header("Ball Prefab")]
    [Tooltip("지정하면 이 프리팹을 그대로 생성해서 던진다. 비워두면 기본 구체를 즉석에서 만든다.")]
    public GameObject ballPrefab;

    [Header("Player")]
    public Animator playerAnimator;
    public Transform playerTransform;
    [Tooltip("플레이어를 레인 파울라인 기준 얼마나 뒤에 세울지 (z 오프셋).")]
    public float playerZOffsetFromSpawn = -1f;
    public float throwReleaseDelay = 0.85f;

    [Header("Randomness")]
    public float maxSidewaysSpeed = 2.2f;
    public float speedVariance = 1.0f;

    [Header("Obstacle Prefabs")]
    public string bananaPeelPrefabPath = "Assets/Prefabs/Obstacles/BananaPeel.prefab";
    public string rockPrefabPath = "Assets/Prefabs/Obstacles/Rock.prefab";
    public string obstaclesParentName = "Obstacles";
    public Color bananaPeelColor = new Color(0.95f, 0.5f, 0.05f, 1f);
    public Color rockColor = new Color(0.35f, 0.3f, 0.28f, 1f);

    [Tooltip("레인 시작점(파울라인) 기준 장애물 스폰 z 오프셋 범위 (0=시작, ~17=핀 근처).")]
    public float obstacleZOffsetMin = 3f;
    public float obstacleZOffsetMax = 14f;
    public float obstacleXRange = 0.35f;

    // 스테이지들의 X 좌표, 5개 레인의 중심 Z(모든 스테이지가 같은 Z 패턴을 공유).
    private static readonly float[] stageX = { -3f, 100f, 200f };
    private static readonly string[] stageNames = { "산(Mountain)", "볼링장(Classic)", "사막(Desert)" };
    private static readonly float[] laneCenterZ = { 10.8f, 44.9f, 79.0f, 113.1f, 147.2f };

    private int currentStage = 0;
    private int selectedLaneIndex = 0;
    private float LaneX => stageX[currentStage];

    private readonly List<GameObject> spawned = new List<GameObject>();
    private readonly List<GameObject> spawnedObstacles = new List<GameObject>();
    private bool waitingForThrow;

    void Start()
    {
        if (PinDeckManager.Instance != null)
            PinDeckManager.Instance.OnThrowJudged += HandleThrowJudged;
    }

    void OnDestroy()
    {
        if (PinDeckManager.Instance != null)
            PinDeckManager.Instance.OnThrowJudged -= HandleThrowJudged;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.spaceKey.wasPressedThisFrame && !waitingForThrow) Launch();
        if (kb.rKey.wasPressedThisFrame) ClearBalls();
        if (kb.eKey.wasPressedThisFrame) SpawnRandomObstacles();
        if (kb.tabKey.wasPressedThisFrame) SwitchStage();

        if (kb.digit1Key.wasPressedThisFrame) SelectLane(0);
        if (kb.digit2Key.wasPressedThisFrame) SelectLane(1);
        if (kb.digit3Key.wasPressedThisFrame) SelectLane(2);
        if (kb.digit4Key.wasPressedThisFrame) SelectLane(3);
        if (kb.digit5Key.wasPressedThisFrame) SelectLane(4);
    }

    private void HandleThrowJudged()
    {
        waitingForThrow = false;
    }

    private float LaneStartZ(int index) => laneCenterZ[index] - 10.8f;
    private float LaneSpawnZ(int index) => LaneStartZ(index) + 0.5f; // foul line 살짝 뒤

    private void SwitchStage()
    {
        currentStage = (currentStage + 1) % stageX.Length;
        Debug.Log("[TestThrowController] 스테이지 전환: " + stageNames[currentStage]);
        SelectLane(selectedLaneIndex);
    }

    private void SelectLane(int index)
    {
        selectedLaneIndex = index;
        Debug.Log("[TestThrowController] " + stageNames[currentStage] + " " + (index + 1) + "레인 선택");

        float spawnZ = LaneSpawnZ(index);
        float camRestZ = spawnZ - 2f;
        float pinFrontZ = LaneStartZ(index) + 16f;
        float x = LaneX;

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(x, 1.6f, camRestZ);
            cam.transform.rotation = Quaternion.Euler(3f, 0f, 0f);

            var followCam = cam.GetComponent<BallFollowCamera>();
            if (followCam != null)
                followCam.SetLaneOrigin(camRestZ, pinFrontZ);
        }

        if (playerTransform != null)
        {
            Vector3 p = playerTransform.position;
            playerTransform.position = new Vector3(x, p.y, spawnZ + playerZOffsetFromSpawn);
        }

        // 실제 투구 시스템(BallSpawner)도 자기 위치에서 공을 생성하므로, 같이 옮기고
        // 지금 있는 공을 즉시 새 레인 위치에서 다시 생성하도록 강제 리스폰시킨다.
        GameObject spawnerGO = GameObject.Find("Ball_Spawner");
        if (spawnerGO != null)
        {
            Vector3 sp = spawnerGO.transform.position;
            spawnerGO.transform.position = new Vector3(x, sp.y, spawnZ);

            var spawner = spawnerGO.GetComponent<BallSpawner>();
            var currentBall = FindObjectOfType<BallResetter>();
            if (spawner != null && currentBall != null)
                spawner.Respawn(currentBall.gameObject);
        }
    }

    void Launch()
    {
        if (PinDeckManager.Instance != null)
            waitingForThrow = true;

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Throw");
            StartCoroutine(SpawnBallAfterDelay(throwReleaseDelay));
        }
        else
        {
            SpawnBall();
        }
    }

    private System.Collections.IEnumerator SpawnBallAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnBall();
    }

    void SpawnBall()
    {
        float spawnZ = LaneSpawnZ(selectedLaneIndex);
        float x = LaneX;

        GameObject go;
        Rigidbody rb;

        if (ballPrefab != null)
        {
            go = Instantiate(ballPrefab, new Vector3(x, 0.14f, spawnZ), Quaternion.identity);
            rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = new Vector3(x, 0.14f, spawnZ);
            go.transform.localScale = Vector3.one * ballDiameter;
            SetColor(go, ballColor);

            rb = go.AddComponent<Rigidbody>();
            rb.mass = ballMass;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        go.name = "TestBall_Lane" + (selectedLaneIndex + 1);
        go.tag = "Ball";

        float sideways = Random.Range(-maxSidewaysSpeed, maxSidewaysSpeed);
        float forwardSpeed = launchSpeed + Random.Range(-speedVariance, speedVariance);
        rb.linearVelocity = new Vector3(sideways, 0f, forwardSpeed);
        rb.angularVelocity = new Vector3(0f, Random.Range(-3f, 3f), 0f);

        if (go.GetComponent<BallFinishWatcher>() == null)
            go.AddComponent<BallFinishWatcher>();

        spawned.Add(go);

        Debug.Log("[TestThrowController] 투구(" + stageNames[currentStage] + " " + (selectedLaneIndex + 1) + "레인): sideways=" + sideways.ToString("F2") + " forwardSpeed=" + forwardSpeed.ToString("F2"));
    }

    void ClearBalls()
    {
        foreach (var go in spawned)
        {
            if (go != null) Destroy(go);
        }
        spawned.Clear();
        waitingForThrow = false;
    }

    void SpawnRandomObstacles()
    {
#if UNITY_EDITOR
        float laneStartZ = LaneStartZ(selectedLaneIndex);
        float x = LaneX;
        GameObject parent = GameObject.Find(obstaclesParentName);

        foreach (var go in spawnedObstacles)
        {
            if (go != null) Destroy(go);
        }
        spawnedObstacles.Clear();

        SpawnOneObstacle(bananaPeelPrefabPath, "TestBananaPeel_Lane" + (selectedLaneIndex + 1), x, laneStartZ, parent, 0.03f, bananaPeelColor);
        SpawnOneObstacle(rockPrefabPath, "TestRock_Lane" + (selectedLaneIndex + 1), x, laneStartZ, parent, 0.1f, rockColor);

        Debug.Log("[TestThrowController] " + (selectedLaneIndex + 1) + "레인에 장애물 랜덤 스폰");
#endif
    }

#if UNITY_EDITOR
    void SpawnOneObstacle(string prefabPath, string name, float laneX, float laneStartZ, GameObject parent, float y, Color color)
    {
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[TestThrowController] Prefab not found: " + prefabPath);
            return;
        }

        float x = laneX + Random.Range(-obstacleXRange, obstacleXRange);
        float z = laneStartZ + Random.Range(obstacleZOffsetMin, obstacleZOffsetMax);

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
        string spaceLine = waitingForThrow ? "Space : (이전 투구 판정 대기 중...)" : "Space : 공 투구 (현재 레인, 랜덤 방향)";
        string laneLine = "현재: " + stageNames[currentStage] + " " + (selectedLaneIndex + 1) + "레인";

        GUI.Label(new Rect(10, 10, 500, 140),
            spaceLine + "\n" +
            "R : 남은 테스트 공 제거\n" +
            "E : 현재 레인에 장애물 랜덤 스폰\n" +
            "1~5 : 레인 이동\n" +
            "Tab : 스테이지 전환 (산/볼링장/사막)\n" +
            laneLine);
    }
}
