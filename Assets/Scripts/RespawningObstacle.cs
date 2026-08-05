using UnityEngine;

// Makes a single-use obstacle (like ObstacleRock or ObstacleBananaPeel,
// which deactivate themselves after being triggered once) come back
// automatically after every throw, instead of staying gone for the rest
// of the game. Relocates it to a random spot within the lane both at
// the very start and every time it comes back.
public class RespawningObstacle : MonoBehaviour
{
    public bool randomizePosition = true;
    public float laneCenterX = -3f;
    public float xRange = 0.35f;
    public float zMin = 3f;
    public float zMax = 14f;

    private float fixedY;

    private void Start()
    {
        fixedY = transform.position.y;

        if (randomizePosition)
            Relocate();

        if (PinDeckManager.Instance != null)
            PinDeckManager.Instance.OnThrowJudged += HandleThrowJudged;
    }

    private void OnDestroy()
    {
        if (PinDeckManager.Instance != null)
            PinDeckManager.Instance.OnThrowJudged -= HandleThrowJudged;
    }

    private void HandleThrowJudged()
    {
        if (!gameObject.activeSelf)
        {
            if (randomizePosition)
                Relocate();
            gameObject.SetActive(true);
        }
    }

    private void Relocate()
    {
        float x = laneCenterX + Random.Range(-xRange, xRange);
        float z = Random.Range(zMin, zMax);
        transform.position = new Vector3(x, fixedY, z);
    }
}
