using System.Collections.Generic;
using UnityEngine;

// "Crossy road" style hazard: periodically spawns small balls that roll
// across the lane from one side to the other, from a handful of different
// spots along the lane. Interval/timing leaves gaps often enough that the
// player's own ball can usually cross through after a throw or two.
// These balls are only meant to interact with the player's ball (tag
// "Ball") - they ignore collisions with pins, gutters, side walls, and
// each other, so they don't get deflected off course by the environment.
public class CrossingBallSpawner : MonoBehaviour
{
    public float laneX = 9f;
    public float laneWidth = 1.26f;

    [Tooltip("Z 위치 후보들 - 매번 이 중 하나를 랜덤으로 골라서 그 지점에서 공을 굴린다.")]
    public float[] spawnZPoints = { 5f, 9f, 13f, 17f };

    [Tooltip("최소/최대 스폰 간격(초). 매번 이 범위에서 랜덤으로 정해져서, 가끔 틈이 생긴다.")]
    public float minSpawnInterval = 0.5f;
    public float maxSpawnInterval = 1.6f;

    public float rollSpeed = 3f;
    public float ballMass = 3f;
    public float ballDiameter = 0.18f;
    public float ballY = 0.15f;
    public Color ballColor = new Color(0.9f, 0.2f, 0.5f, 1f);

    private float timer;
    private float nextInterval;
    private readonly List<Collider> spawnedColliders = new List<Collider>();

    private void Start()
    {
        nextInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= nextInterval)
        {
            timer = 0f;
            nextInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
            SpawnBall();
        }
    }

    private void SpawnBall()
    {
        float spawnZ = spawnZPoints[Random.Range(0, spawnZPoints.Length)];

        bool fromLeft = Random.value < 0.5f;
        float startX = laneX + (fromLeft ? -laneWidth : laneWidth);
        float dir = fromLeft ? 1f : -1f;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "CrossingBall";
        go.transform.position = new Vector3(startX, ballY, spawnZ);
        go.transform.localScale = Vector3.one * ballDiameter;

        var rend = go.GetComponent<Renderer>();
        var mpb = new MaterialPropertyBlock();
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", ballColor);
        rend.SetPropertyBlock(mpb);

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = ballMass;
        rb.useGravity = false; // stays at a fixed height, doesn't need to rest on the floor
        rb.linearVelocity = new Vector3(dir * rollSpeed, 0f, 0f);
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Collider myCollider = go.GetComponent<Collider>();

        // Ignore everything except the player's ball: pins, gutters, side
        // walls, and other crossing balls.
        Collider[] toIgnore = FindEnvironmentCollidersToIgnore();
        foreach (Collider c in toIgnore)
        {
            Physics.IgnoreCollision(myCollider, c, true);
        }

        spawnedColliders.RemoveAll(c => c == null);
        foreach (Collider other in spawnedColliders)
        {
            Physics.IgnoreCollision(myCollider, other, true);
        }
        spawnedColliders.Add(myCollider);

        Destroy(go, 6f);
    }

    private Collider[] FindEnvironmentCollidersToIgnore()
    {
        List<Collider> result = new List<Collider>();

        BowlingPin[] pins = FindObjectsOfType<BowlingPin>(true);
        foreach (BowlingPin p in pins)
        {
            Collider pc = p.GetComponent<Collider>();
            if (pc != null) result.Add(pc);
        }

        Collider[] allColliders = FindObjectsOfType<Collider>(true);
        foreach (Collider c in allColliders)
        {
            string n = c.gameObject.name;
            if (n.StartsWith("Gutter") || n.StartsWith("SideWall") || n.StartsWith("Wall_"))
                result.Add(c);
        }

        return result.ToArray();
    }
}
