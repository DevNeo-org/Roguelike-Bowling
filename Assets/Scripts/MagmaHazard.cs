using UnityEngine;

// Lane hazard: magma. Periodically "erupts"; while erupting, any ball in
// contact gets knocked upward (placeholder for future damage system).
[ExecuteAlways]
public class MagmaHazard : MonoBehaviour
{
    [Tooltip("Full cycle length in seconds.")]
    public float cycleDuration = 3f;

    [Tooltip("How long within each cycle the magma is erupting (dangerous).")]
    public float eruptDuration = 1f;

    [Tooltip("Upward knockback force applied while erupting.")]
    public float eruptForce = 10f;

    public Transform fireVisual;

    private float timer;

    public bool IsErupting { get; private set; }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= cycleDuration) timer = 0f;

        IsErupting = timer < eruptDuration;

        if (fireVisual != null)
            fireVisual.gameObject.SetActive(IsErupting);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!IsErupting) return;

        Rigidbody rb = collision.rigidbody;
        if (rb == null) return;

        rb.AddForce(Vector3.up * eruptForce, ForceMode.Impulse);
        Debug.Log("[MagmaHazard] " + rb.name + " got burned!");
    }
}
