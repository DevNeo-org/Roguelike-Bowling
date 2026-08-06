using System.Collections.Generic;
using UnityEngine;

// "Crossy road" style hazard: periodically spawns small balls that roll
// (with gravity and proper rolling rotation, not floating) across the
// lane from one side to the other, at a fully random spot within a Z
// range each time. Interval/timing leaves gaps often enough that the
// player's own ball can usually cross through after a throw or two.
// These balls are only meant to interact with the player's ball (tag
// "Ball") - they ignore collisions with everything except the lane floor
// (named "Floor") and the player's ball, so decor props/pins/walls never
// deflect them off course.
public class CrossingBallSpawner : MonoBehaviour
{
    public float laneX = 9f;
    public float laneWidth = 1.26f;

    [Tooltip("이 범위 안에서 매번 완전히 랜덤한 Z 위치를 골라서 그 지점에서 공을 굴린다.")]
    public float zMin = 3f;
    public float zMax = 17f;

    [Tooltip("최소/최대 스폰 간격(초). 매번 이 범위에서 랜덤으로 정해져서, 가끔 틈이 생긴다.")]
    public float minSpawnInterval = 0.5f;
    public float maxSpawnInterval = 1.6f;

    public float rollSpeed = 3f;
    public float ballMass = 3f;
    public float ballDiameter = 0.18f;
    public Color ballColor = new Color(0.9f, 0.2f, 0.5f, 1f);

    [Tooltip("켜면 매끈한 구체 대신 울퉁불퉁한 바위 덩어리 모양으로 만든다 (물리는 그대로 구체 콜라이더).")]
    public bool jaggedRockLook = false;
    public Material jaggedChunkMaterial;

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
        float spawnZ = Random.Range(zMin, zMax);

        bool fromLeft = Random.value < 0.5f;
        float startX = laneX + (fromLeft ? -laneWidth : laneWidth);
        float dir = fromLeft ? 1f : -1f;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "CrossingBall";

        float radius = ballDiameter * 0.5f;
        float restingY = 0.025f + radius;
        go.transform.position = new Vector3(startX, restingY, spawnZ);
        go.transform.localScale = Vector3.one * ballDiameter;

        var rend = go.GetComponent<Renderer>();
        var mpb = new MaterialPropertyBlock();
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", ballColor);
        rend.SetPropertyBlock(mpb);

        if (jaggedRockLook)
        {
            AddJaggedChunks(go);
        }

        Collider myCollider = go.GetComponent<Collider>();
        PhysicsMaterial noBounce = new PhysicsMaterial("CrossingBallNoBounce");
        noBounce.bounciness = 0f;
        noBounce.dynamicFriction = 0.4f;
        noBounce.staticFriction = 0.4f;
        noBounce.bounceCombine = PhysicsMaterialCombine.Minimum;
        myCollider.sharedMaterial = noBounce;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = ballMass;
        rb.useGravity = true;
        rb.linearVelocity = new Vector3(dir * rollSpeed, 0f, 0f);
        rb.angularVelocity = new Vector3(0f, 0f, -dir * (rollSpeed / radius));
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

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

        float crossingDistance = laneWidth * 2f;
        float lifetime = (crossingDistance / rollSpeed) + 0.4f;
        Destroy(go, lifetime);
    }

    private void AddJaggedChunks(GameObject rockRoot)
    {
        Vector3[] offsets = { new Vector3(0.5f,0.3f,0.2f), new Vector3(-0.4f,-0.2f,0.3f), new Vector3(0.2f,-0.4f,-0.4f), new Vector3(-0.3f,0.4f,-0.3f) };
        float[] scales = { 0.5f, 0.45f, 0.4f, 0.35f };
        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chunk.name = "RockChunk";
            chunk.transform.SetParent(rockRoot.transform, false);
            chunk.transform.localPosition = offsets[i];
            chunk.transform.localRotation = Quaternion.Euler(Random.Range(0f,90f), Random.Range(0f,90f), Random.Range(0f,90f));
            chunk.transform.localScale = Vector3.one * scales[i];
            Object.Destroy(chunk.GetComponent<Collider>());
            if (jaggedChunkMaterial != null) chunk.GetComponent<Renderer>().sharedMaterial = jaggedChunkMaterial;
        }
    }

    private Collider[] FindEnvironmentCollidersToIgnore()
    {
        // Ignore EVERYTHING in the scene except lane floors - decor props
        // (bench, plant pots, neon signs, ball return rail, etc.) aren't in
        // any explicit list, so a name-based blacklist kept missing things
        // and the crossing ball would ricochet off whatever wasn't listed.
        // Instead: only keep collision with "Floor"-named objects; ignore
        // every other collider (pins, gutters, walls, decor, everything).
        List<Collider> result = new List<Collider>();

        Collider[] allColliders = FindObjectsOfType<Collider>(true);
        foreach (Collider c in allColliders)
        {
            if (c.gameObject.CompareTag("Ball")) continue;
            if (c.gameObject.name == "Floor") continue;
            result.Add(c);
        }

        return result.ToArray();
    }
}
