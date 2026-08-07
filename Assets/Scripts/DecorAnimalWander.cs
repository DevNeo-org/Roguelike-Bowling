using UnityEngine;

// Purely decorative animal wander - no physics, no gameplay effect. Picks a
// nearby random point, ambles over to it, pauses, repeats. If hoverHeight
// is set above 0, it floats/bobs at that height instead of walking on the
// ground (for bees etc).
public class DecorAnimalWander : MonoBehaviour
{
    public float wanderRadius = 3f;
    public float moveSpeed = 0.8f;
    public float minWaitTime = 1f;
    public float maxWaitTime = 4f;

    [Tooltip("0이면 땅에서 걸어다니고, 0보다 크면 그 높이에서 붕붕 날아다닌다(벌 등).")]
    public float hoverHeight = 0f;
    public float bobAmplitude = 0.15f;
    public float bobSpeed = 3f;

    private Vector3 homePos;
    private Vector3 target;
    private bool waiting;
    private float waitTimer;
    private float currentWaitTime;

    private void Start()
    {
        homePos = transform.position;
        if (hoverHeight > 0f) homePos.y += hoverHeight;
        PickNewTarget();
        currentWaitTime = Random.Range(minWaitTime, maxWaitTime);
    }

    private void PickNewTarget()
    {
        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        target = homePos + new Vector3(offset.x, 0f, offset.y);
    }

    private void Update()
    {
        if (waiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= currentWaitTime)
            {
                waiting = false;
                waitTimer = 0f;
                PickNewTarget();
            }
            return;
        }

        Vector3 toTarget = target - transform.position;
        toTarget.y = 0f;
        float step = moveSpeed * Time.deltaTime;
        float bob = hoverHeight > 0f ? Mathf.Sin(Time.time * bobSpeed) * bobAmplitude : 0f;

        if (toTarget.magnitude <= step)
        {
            transform.position = new Vector3(target.x, homePos.y + bob, target.z);
            waiting = true;
            currentWaitTime = Random.Range(minWaitTime, maxWaitTime);
        }
        else
        {
            Vector3 move = toTarget.normalized * step;
            transform.position += move;
            transform.position = new Vector3(transform.position.x, homePos.y + bob, transform.position.z);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget.normalized), 5f * Time.deltaTime);
        }
    }
}
