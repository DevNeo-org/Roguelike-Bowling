using UnityEngine;

// Gutter channel alongside a lane. Purely visual/physical for now - it no
// longer forces an immediate 0-score judgement. The throw still gets
// scored normally once the ball comes to rest (BallFinishWatcher) or
// reaches the end-of-lane zones, based on whatever pins actually got hit.
public class GutterZone : MonoBehaviour
{
    [Tooltip("거터에 부딪힐 때마다 수평 속도에 곱하는 감소 비율.")]
    [SerializeField] private float speedLossMultiplier = 0.9f;

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ball")) return;

        Rigidbody rb = collision.rigidbody;
        if (rb != null)
        {
            Vector3 vel = rb.linearVelocity;
            Vector3 horizontal = new Vector3(vel.x, 0f, vel.z) * speedLossMultiplier;
            rb.linearVelocity = new Vector3(horizontal.x, vel.y, horizontal.z);

            BallMagnusEffect magnus = rb.GetComponent<BallMagnusEffect>();
            if (magnus != null) magnus.DisableCurve();
        }

        Debug.Log("[GutterZone] " + collision.gameObject.name + " fell into the gutter");
    }
}
