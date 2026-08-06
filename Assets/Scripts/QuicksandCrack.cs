using UnityEngine;

// Obstacle: 유사 (quicksand).
// A patch on the lane. The first ball to roll over it each throw sinks
// in and gets bogged down (strong braking + slight downward jolt). It
// dries back out (resets) once the throw is judged.
public class QuicksandCrack : MonoBehaviour
{
    public float brakeMultiplier = 0.3f;
    public float sinkImpulse = 3f;

    private bool triggered;
    private Renderer rend;
    private Color normalColor;
    private Color sunkenColor = new Color(0.45f, 0.35f, 0.2f, 1f);

    private void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null) normalColor = rend.sharedMaterial.color;

        if (PinDeckManager.Instance != null)
            PinDeckManager.Instance.OnThrowJudged += HandleThrowJudged;
    }

    private void OnDestroy()
    {
        if (PinDeckManager.Instance != null)
            PinDeckManager.Instance.OnThrowJudged -= HandleThrowJudged;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Ball")) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        triggered = true;
        SetVisual(sunkenColor);

        Vector3 vel = rb.linearVelocity;
        Vector3 horizontal = new Vector3(vel.x, 0f, vel.z) * brakeMultiplier;
        rb.linearVelocity = new Vector3(horizontal.x, vel.y, horizontal.z);
        rb.AddForce(Vector3.down * sinkImpulse, ForceMode.Impulse);

        Debug.Log("[QuicksandCrack] " + other.name + " got bogged down in quicksand");
    }

    private void HandleThrowJudged()
    {
        if (!triggered) return;
        triggered = false;
        SetVisual(normalColor);
    }

    private void SetVisual(Color c)
    {
        if (rend == null) return;
        var mpb = new MaterialPropertyBlock();
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", c);
        rend.SetPropertyBlock(mpb);
    }
}
