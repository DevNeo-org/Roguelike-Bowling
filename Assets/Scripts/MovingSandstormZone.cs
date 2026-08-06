using UnityEngine;

// Obstacle: 모래폭풍 (sandstorm zone).
// Unlike the mountain blizzard (which stays put), this one physically
// patrols back and forth along the lane while it keeps gusting the ball
// sideways whenever it's inside.
public class MovingSandstormZone : MonoBehaviour
{
    public float zMin = 5f;
    public float zMax = 15f;
    public float moveSpeed = 1.8f;
    public float windStrength = 3f;
    public float gustSpeed = 2f;

    private Rigidbody rb;
    private int direction = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void FixedUpdate()
    {
        Vector3 pos = rb.position;
        float newZ = pos.z + direction * moveSpeed * Time.fixedDeltaTime;

        if (newZ >= zMax) { newZ = zMax; direction = -1; }
        else if (newZ <= zMin) { newZ = zMin; direction = 1; }

        rb.MovePosition(new Vector3(pos.x, pos.y, newZ));
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Ball")) return;

        Rigidbody ballRb = other.attachedRigidbody;
        if (ballRb == null) return;

        float gust = Mathf.Sin(Time.time * gustSpeed) * windStrength;
        ballRb.AddForce(new Vector3(gust, 0f, 0f), ForceMode.Force);
    }
}
