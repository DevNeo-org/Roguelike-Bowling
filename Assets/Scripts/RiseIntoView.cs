using UnityEngine;

// Makes an obstacle start hidden below the ground and rise up into view
// once (on scene start / stage start) instead of just being instantly
// present. Purely a one-time reveal animation - after it finishes rising,
// the object just sits at its normal resting position as usual.
public class RiseIntoView : MonoBehaviour
{
    public float riseDuration = 1.5f;
    public float startDelay = 0f;
    public float hiddenDepth = 1.5f;

    private Vector3 restPosition;
    private float timer;
    private bool rising;
    private bool started;

    private void Start()
    {
        restPosition = transform.position;
        transform.position = restPosition + Vector3.down * hiddenDepth;
        Invoke(nameof(BeginRise), startDelay);
    }

    private void BeginRise()
    {
        rising = true;
    }

    private void Update()
    {
        if (!rising) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / riseDuration);
        // ease-out so it settles smoothly instead of stopping abruptly
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        transform.position = Vector3.Lerp(restPosition + Vector3.down * hiddenDepth, restPosition, eased);

        if (t >= 1f)
        {
            transform.position = restPosition;
            rising = false;
            enabled = false; // done, no more per-frame work needed
        }
    }
}
