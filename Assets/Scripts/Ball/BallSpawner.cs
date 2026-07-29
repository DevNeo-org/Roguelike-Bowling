using System.Collections;
using UnityEngine;

// 공의 생성·삭제를 전담한다. Ball_Spawner 오브젝트에 부착.
// Play 시작 시 ballPrefab을 자신의 위치에 스폰하고,
// 공이 레인에서 이탈하면 삭제 후 새 공을 다시 생성한다.
public class BallSpawner : MonoBehaviour
{
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private BallLauncher launcher;
    [SerializeField] private ThrowInputHandler inputHandler;
    [Tooltip("공 삭제 후 새 공 생성까지 대기 시간 (초)")]
    [SerializeField] private float respawnDelay = 1f;

    private void Start()
    {
        SpawnBall();
    }

    /// <summary>BallResetter가 낙하 감지 시 호출.</summary>
    public void Respawn(GameObject ballToDestroy)
    {
        StartCoroutine(RespawnRoutine(ballToDestroy));
    }

    private IEnumerator RespawnRoutine(GameObject ballToDestroy)
    {
        Destroy(ballToDestroy);
        inputHandler.Reset();
        yield return new WaitForSeconds(respawnDelay);
        SpawnBall();
    }

    private void SpawnBall()
    {
        var ball = Instantiate(ballPrefab, transform.position, Quaternion.identity);

        var resetter = ball.GetComponent<BallResetter>();
        if (resetter != null)
            resetter.Init(this);

        launcher.SetBall(ball.GetComponent<Rigidbody>());
        launcher.ResetLaunch();

        Debug.Log("[스포너] 새 공 생성 완료");
    }
}
