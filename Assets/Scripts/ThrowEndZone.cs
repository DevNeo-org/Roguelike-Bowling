using UnityEngine;

// Placed as a trigger zone past the pins. When a ball crosses it, the
// throw is considered finished: PinDeckManager tallies newly-downed pins
// and records the roll, then the ball is cleaned up.
[RequireComponent(typeof(Collider))]
public class ThrowEndZone : MonoBehaviour
{
    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball")) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        // Guard against the trigger firing more than once for the same ball
        // (e.g. re-entry caused by toggling isKinematic mid-callback).
        other.gameObject.tag = "Untagged";

        if (PinDeckManager.Instance != null)
            PinDeckManager.Instance.OnBallFinished(rb.gameObject);
    }
}
