using UnityEngine;

// Attached to each bowling pin. IsDown reflects the pin's CURRENT state
// (angle from upright, or how far it's been shoved from its spot) rather
// than latching permanently the instant it ever crosses a threshold.
// PinDeckManager only reads IsDown after waiting for the pin to come to
// rest, so this checks the final settled state, not a momentary spike
// while the pin is still tumbling/wobbling from an impact.
public class BowlingPin : MonoBehaviour
{
    [SerializeField] private float fallAngleThreshold = 40f;
    [SerializeField] private float displacementThreshold = 0.08f;
    [SerializeField] private float gracePeriod = 0.3f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody rb;
    private float graceTimer;

    public bool IsDown
    {
        get
        {
            float angle = Vector3.Angle(transform.up, Vector3.up);
            bool tippedOver = angle > fallAngleThreshold;

            bool shoveAway = false;
            if (graceTimer <= 0f)
            {
                float horizontalDist = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(initialPosition.x, initialPosition.z));
                shoveAway = horizontalDist > displacementThreshold;
            }

            return tippedOver || shoveAway;
        }
    }

    private void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        rb = GetComponent<Rigidbody>();
        graceTimer = gracePeriod;
    }

    private void Update()
    {
        if (graceTimer > 0f)
            graceTimer -= Time.deltaTime;
    }

    public void ResetPin()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        graceTimer = gracePeriod;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
