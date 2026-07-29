using UnityEngine;

// 공이 레인에서 이탈(낙하)하거나, 제한 시간이 초과되거나,
// 투구 후 속도가 0이 되면 BallSpawner에 알린다.
// Attach to Ball prefab.
public class BallResetter : MonoBehaviour
{
    [Tooltip("이 Y 좌표 이하로 내려가면 낙하로 판정")]
    [SerializeField] private float fallThreshold = -3f;
    [Tooltip("이 시간(초)이 지나면 낙하 여부와 관계없이 리스폰")]
    [SerializeField] private float respawnTimeout = 10f;

    [Header("Stop Detection")]
    [Tooltip("이 속도(m/s) 이하면 정지로 판정")]
    [SerializeField] private float stopSpeedThreshold = 0.15f;
    [Tooltip("정지 판정 후 이 시간(초)이 지나면 리스폰")]
    [SerializeField] private float stopRespawnDelay = 1f;

    private BallSpawner _spawner;
    private Rigidbody _rb;
    private bool _triggered;
    private float _elapsed;

    // BallLauncher가 Launch() 호출 후 SetLaunched()로 명시적으로 활성화
    private bool _hasLaunched;
    private float _stopTimer;

    /// <summary>BallSpawner가 생성 직후 호출해 참조를 주입한다.</summary>
    public void Init(BallSpawner spawner)
    {
        _spawner = spawner;
        _rb = GetComponent<Rigidbody>();
    }

    /// <summary>BallLauncher가 실제 발사 직후 호출 — 정지 감지를 활성화한다.</summary>
    public void SetLaunched()
    {
        _hasLaunched = true;
    }

    private void Update()
    {
        if (_triggered) return;

        // 낙하 감지는 발사 전후 항상 활성
        if (transform.position.y < fallThreshold)
        {
            Trigger();
            return;
        }

        // 타임아웃·정지 감지는 발사 이후에만 동작
        if (!_hasLaunched) return;

        _elapsed += Time.deltaTime;
        if (_elapsed >= respawnTimeout)
        {
            Trigger();
            return;
        }

        if (_rb != null)
        {
            float speed = _rb.linearVelocity.magnitude;

            if (speed <= stopSpeedThreshold)
                _stopTimer += Time.deltaTime;
            else
                _stopTimer = 0f;

            if (_stopTimer >= stopRespawnDelay)
                Trigger();
        }
    }

    private void Trigger()
    {
        _triggered = true;
        _spawner.Respawn(gameObject);
    }
}
