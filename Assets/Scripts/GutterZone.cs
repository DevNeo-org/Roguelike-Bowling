using UnityEngine;

// Gutter channel alongside a lane. Purely visual/physical for now - it no
// longer forces an immediate 0-score judgement. The throw still gets
// scored normally once the ball comes to rest (BallFinishWatcher) or
// reaches the end-of-lane zones, based on whatever pins actually got hit.
public class GutterZone : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ball")) return;

        Debug.Log("[GutterZone] " + collision.gameObject.name + " fell into the gutter");
    }
}
