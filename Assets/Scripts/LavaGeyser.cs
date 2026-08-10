using UnityEngine;

// Obstacle: 간헐천 분출 (lava geyser).
// The reverse of the icicle drop - stays completely hidden BELOW the
// ground between eruptions (not just sitting at ground level), then
// erupts UPWARD through the floor on a random rhythm at a random spot in
// the lane, launching the ball into the air if it's caught in the burst,
// before sinking back out of sight.
public class LavaGeyser : MonoBehaviour
{
    public float hiddenY = -0.8f;
    public float eruptedY = 1.2f;
    public float eruptSpeed = 10f;
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;
    public float retractTime = 0.4f;
    public float launchForce = 6f;

    public float laneCenterX = -3f;
    public float xRange = 0.35f;
    public float zMin = 3f;
    public float zMax = 17f;

    private enum State { Hidden, Erupting, Retracting }
    private State state = State.Hidden;
    private float timer;
    private float currentWaitTime;
    private Rigidbody rb;
    private Renderer[] renderers;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        renderers = GetComponentsInChildren<Renderer>(true);

        RelocateToRandomSpot();
        currentWaitTime = Random.Range(minWaitTime, maxWaitTime);
        SetVisible(false);
    }

    private void RelocateToRandomSpot()
    {
        float x = laneCenterX + Random.Range(-xRange, xRange);
        float z = Random.Range(zMin, zMax);
        rb.position = new Vector3(x, hiddenY, z);
        transform.position = new Vector3(x, hiddenY, z);
    }

    private void SetVisible(bool visible)
    {
        if (renderers == null) return;
        foreach (var r in renderers)
        {
            if (r != null) r.enabled = visible;
        }
    }

    private void FixedUpdate()
    {
        Vector3 pos = rb.position;
        timer += Time.fixedDeltaTime;

        switch (state)
        {
            case State.Hidden:
                if (timer >= currentWaitTime) { timer = 0f; state = State.Erupting; SetVisible(true); }
                break;

            case State.Erupting:
                {
                    float newY = Mathf.MoveTowards(pos.y, eruptedY, eruptSpeed * Time.fixedDeltaTime);
                    rb.MovePosition(new Vector3(pos.x, newY, pos.z));
                    if (Mathf.Approximately(newY, eruptedY)) { timer = 0f; state = State.Retracting; }
                    break;
                }

            case State.Retracting:
                {
                    float newY = Mathf.MoveTowards(pos.y, hiddenY, eruptSpeed * Time.fixedDeltaTime);
                    rb.MovePosition(new Vector3(pos.x, newY, pos.z));
                    if (timer >= retractTime || Mathf.Approximately(newY, hiddenY))
                    {
                        SetVisible(false);
                        RelocateToRandomSpot();
                        currentWaitTime = Random.Range(minWaitTime, maxWaitTime);
                        timer = 0f;
                        state = State.Hidden;
                    }
                    break;
                }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (state != State.Erupting) return;
        if (!collision.gameObject.CompareTag("Ball")) return;

        Rigidbody ballRb = collision.rigidbody;
        if (ballRb == null) return;

        ballRb.AddForce(Vector3.up * launchForce, ForceMode.Impulse);
        Debug.Log("[LavaGeyser] " + collision.gameObject.name + " got launched by a geyser eruption");
    }
}
