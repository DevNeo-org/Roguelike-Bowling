using UnityEngine;

// Obstacle: 살얼음 (thin/cracking ice).
// A patch on the lane. The first ball to roll over it each throw cracks
// through: gets a sudden downward+braking jolt and the patch visually
// darkens (cracked). It "re-freezes" (resets) once the throw is judged,
// ready to crack again on the next throw.
public class ThinIceCrack : MonoBehaviour
{
    public float brakeMultiplier = 0.4f;
    public float sinkImpulse = 2.5f;

    private bool cracked;
    private Renderer rend;
    private Color normalColor;
    private Color crackedColor = new Color(0.5f, 0.65f, 0.75f, 0.9f);

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
        if (cracked) return;
        if (!other.CompareTag("Ball")) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        cracked = true;
        SetVisual(crackedColor);

        Vector3 vel = rb.linearVelocity;
        Vector3 horizontal = new Vector3(vel.x, 0f, vel.z) * brakeMultiplier;
        rb.linearVelocity = new Vector3(horizontal.x, vel.y, horizontal.z);
        rb.AddForce(Vector3.down * sinkImpulse, ForceMode.Impulse);

        Debug.Log("[ThinIceCrack] " + other.name + " cracked through the thin ice");
    }

    private void HandleThrowJudged()
    {
        if (!cracked) return;
        cracked = false;
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
