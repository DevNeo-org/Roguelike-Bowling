using UnityEngine;

// Obstacle: 지진 (earthquake).
// Unlike every other obstacle so far (which affects one spot), this one
// is a whole-lane environmental event: periodically the ground "shakes"
// for a short burst, giving the ball random jolts the whole time it's
// happening, rather than a single point-of-contact effect.
public class EarthquakeZone : MonoBehaviour
{
    public float laneCenterX = -3f;
    public float zMin = 0f;
    public float zMax = 21.6f;

    public float minQuietTime = 3f;
    public float maxQuietTime = 6f;
    public float shakeDuration = 1.5f;
    public float shakeForce = 3f;
    public float shakeTickInterval = 0.1f;

    private float timer;
    private float nextQuietTime;
    private bool shaking;
    private float shakeTimer;
    private float tickTimer;

    private void Start()
    {
        nextQuietTime = Random.Range(minQuietTime, maxQuietTime);
    }

    private void Update()
    {
        if (!shaking)
        {
            timer += Time.deltaTime;
            if (timer >= nextQuietTime)
            {
                timer = 0f;
                shaking = true;
                shakeTimer = 0f;
                tickTimer = 0f;
                Debug.Log("[EarthquakeZone] 지진 시작!");
            }
            return;
        }

        shakeTimer += Time.deltaTime;
        tickTimer += Time.deltaTime;

        if (tickTimer >= shakeTickInterval)
        {
            tickTimer = 0f;
            ShakeBallsInRange();
        }

        if (shakeTimer >= shakeDuration)
        {
            shaking = false;
            timer = 0f;
            nextQuietTime = Random.Range(minQuietTime, maxQuietTime);
        }
    }

    private void ShakeBallsInRange()
    {
        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        foreach (GameObject ball in balls)
        {
            Vector3 pos = ball.transform.position;
            if (Mathf.Abs(pos.x - laneCenterX) > 3f) continue; // rough lane filter
            if (pos.z < zMin || pos.z > zMax) continue;

            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb == null) continue;

            Vector3 jolt = new Vector3(Random.Range(-1f, 1f), Random.Range(0f, 0.5f), Random.Range(-1f, 1f)) * shakeForce;
            rb.AddForce(jolt, ForceMode.Impulse);
        }
    }
}
