using UnityEngine;

// Pure decoration: a squirrel that occasionally dashes across the
// background near a lane. No physics/gameplay effect - it's on a trigger
// collider (or no collider at all) purely for atmosphere.
public class SquirrelDasher : MonoBehaviour
{
    public float laneX = -3f;
    public float dashZ = 10f;
    public float sideOffset = 1.6f;
    public float dashSpeed = 4f;
    public float minInterval = 4f;
    public float maxInterval = 10f;
    public float y = 0.15f;

    private float timer;
    private float nextInterval;

    private void Start()
    {
        nextInterval = Random.Range(minInterval, maxInterval);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= nextInterval)
        {
            timer = 0f;
            nextInterval = Random.Range(minInterval, maxInterval);
            Dash();
        }
    }

    private void Dash()
    {
        bool fromLeft = Random.value < 0.5f;
        float startX = laneX + (fromLeft ? -sideOffset : sideOffset);
        float dir = fromLeft ? 1f : -1f;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Squirrel";
        go.transform.position = new Vector3(startX, y, dashZ);
        go.transform.localScale = new Vector3(0.08f, 0.12f, 0.08f);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        var rend = go.GetComponent<Renderer>();
        var mpb = new MaterialPropertyBlock();
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", new Color(0.55f, 0.35f, 0.2f, 1f));
        rend.SetPropertyBlock(mpb);

        // No physics interaction at all - purely visual, so disable its collider.
        var col = go.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        StartCoroutine(MoveAcross(go.transform, dir));
    }

    private System.Collections.IEnumerator MoveAcross(Transform t, float dir)
    {
        float traveled = 0f;
        float totalDistance = sideOffset * 2.2f;

        while (t != null && traveled < totalDistance)
        {
            float step = dashSpeed * Time.deltaTime;
            t.position += new Vector3(dir * step, 0f, 0f);
            traveled += step;
            yield return null;
        }

        if (t != null) Destroy(t.gameObject);
    }
}
