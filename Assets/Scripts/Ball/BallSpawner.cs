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

    /// <summary>
    /// 게임 시작 시 스킬을 고른 직후처럼, Start()에서 이미 스폰된 공을 즉시(딜레이 없이)
    /// 새로 교체해야 할 때 사용 - 이래야 패시브 스킬(압축/거대화 등)이 첫 공부터 반영된다.
    /// </summary>
    public void RespawnImmediately()
    {
        GameObject existing = GameObject.FindGameObjectWithTag("Ball");
        if (existing != null)
            Destroy(existing);

        SpawnBall();
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

        var rb = ball.GetComponent<Rigidbody>();

        // 플레이어가 실제로 던지기(Launch) 전까지는 중력 영향을 받지 않도록 고정한다.
        // 이걸 안 하면 스폰 직후 공이 바로 떨어져서(중력) 플레이어 입력 없이 거터로 판정돼버린다.
        // (새로 생성된 Rigidbody는 이미 속도가 0이므로 별도로 초기화할 필요는 없다 — kinematic body에
        //  linearVelocity/angularVelocity를 대입하면 콘솔에 지원되지 않는다는 경고만 찍힌다.)
        rb.isKinematic = true;

        if (SkillManager.Instance != null)
        {
            foreach (SkillEffect effect in SkillManager.Instance.GetOwnedEffects())
                effect.OnBallSpawned(rb);
        }

        if (InventoryManager.Instance != null)
        {
            foreach (ItemEffect effect in InventoryManager.Instance.GetOwnedEffects())
                effect.OnBallSpawned(rb);
        }

        launcher.SetBall(rb);
        inputHandler.SetBall(rb);
        launcher.ResetLaunch();

        Debug.Log("[스포너] 새 공 생성 완료");
    }
}
