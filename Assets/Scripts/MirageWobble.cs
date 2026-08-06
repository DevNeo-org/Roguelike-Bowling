using UnityEngine;

// Obstacle: 신기루 (mirage).
// Purely visual decoy pins that shimmer/waver near the real pin deck,
// making it harder to tell at a glance which pins are actually real.
// No collider - the ball passes straight through these.
public class MirageWobble : MonoBehaviour
{
    public float bobAmplitude = 0.05f;
    public float bobSpeed = 1.5f;
    public float flickerSpeed = 2f;

    private Vector3 basePos;
    private Renderer rend;
    private float phaseOffset;

    private void Start()
    {
        basePos = transform.localPosition;
        rend = GetComponent<Renderer>();
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float wobble = Mathf.Sin(Time.time * bobSpeed + phaseOffset) * bobAmplitude;
        transform.localPosition = basePos + new Vector3(wobble * 0.3f, wobble, 0f);

        if (rend != null)
        {
            float alpha = 0.35f + Mathf.Sin(Time.time * flickerSpeed + phaseOffset) * 0.15f;
            var mpb = new MaterialPropertyBlock();
            rend.GetPropertyBlock(mpb);
            Color c = mpb.GetColor("_BaseColor");
            if (c.a <= 0f) c = new Color(1f, 1f, 1f, alpha);
            else c.a = alpha;
            mpb.SetColor("_BaseColor", c);
            rend.SetPropertyBlock(mpb);
        }
    }
}
