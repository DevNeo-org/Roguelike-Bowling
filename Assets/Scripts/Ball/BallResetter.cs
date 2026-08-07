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

    private BallSpawner _spawner;
    private Rigidbody _rb;
    private bool _triggered;
    private bool _reported;
    private float _elapsed;

    // BallLauncher가 Launch() 호출 후 SetLaunched()로 명시적으로 활성화
    private bool _hasLaunched;

    /// <summary>BallSpawner가 생성 직후 호출해 참조를 주입한다.</summary>
    public void Init(BallSpawner spawner)
    {
        _spawner = spawner;
        _rb = GetComponent<Rigidbody>();
        gameObject.tag = "Ball";

        if (GetComponent<BallFinishWatcher>() == null)
            gameObject.AddComponent<BallFinishWatcher>();

        if (PinDeckManager.Instance != null)
            PinDeckManager.Instance.OnThrowJudged += HandleThrowJudged;

        IgnorePinDeckBackCollisions();
    }

    // 실제 볼링장처럼 핀을 지나친 공은 벽에 부딪혀 튕기는 대신 뒤쪽으로 빠져나가야 한다.
    // PinBackstop/BackWall은 핀이 뒤로 날아가지 않도록 막는 용도라 그대로 두고,
    // 공만 이 콜라이더들과의 충돌을 무시하도록 한다.
    private void IgnorePinDeckBackCollisions()
    {
        Collider ballCollider = GetComponent<Collider>();
        if (ballCollider == null) return;

        GameObject backstop = GameObject.Find("PinBackstop");
        if (backstop != null)
        {
            var col = backstop.GetComponent<Collider>();
            if (col != null) Physics.IgnoreCollision(ballCollider, col, true);
        }

        GameObject backWall = GameObject.Find("BackWall");
        if (backWall != null)
        {
            var col = backWall.GetComponent<Collider>();
            if (col != null) Physics.IgnoreCollision(ballCollider, col, true);
        }
    }

    private void OnDestroy()
    {
        if (PinDeckManager.Instance != null)
            PinDeckManager.Instance.OnThrowJudged -= HandleThrowJudged;
    }

    private void HandleThrowJudged()
    {
        if (_triggered) return;
        _triggered = true;
        _spawner.Respawn(gameObject);
    }

    /// <summary>BallLauncher가 실제 발사 직후 호출 — 타임아웃 감지를 활성화한다.</summary>
    public void SetLaunched()
    {
        _hasLaunched = true;
        GetComponent<BallFinishWatcher>()?.SetLaunched();
    }

    private void Update()
    {
        if (_reported) return;

        // 낙하 감지는 발사 전후 항상 활성
        if (transform.position.y < fallThreshold)
        {
            Trigger();
            return;
        }

        // 타임아웃 감지는 발사 이후에만 동작 (정지 감지는 BallFinishWatcher가 담당)
        if (!_hasLaunched) return;

        _elapsed += Time.deltaTime;
        if (_elapsed >= respawnTimeout)
            Trigger();
    }

    // 낙하/타임아웃으로 인한 리스폰도 일반 거터/미스와 동일하게 PinDeckManager 판정을
    // 거치도록 한다 - 그래야 이번 투구가 0핀으로 정상 집계되고, HandleThrowJudged를 통해
    // 리스폰된다. 예전에는 여기서 바로 Respawn()을 불러서 구멍에 빠지거나 타임아웃이 나도
    // 투구 자체가 기록되지 않고 그냥 새 공만 나오는 문제가 있었다.
    // _reported(여기서만 사용)와 _triggered(HandleThrowJudged의 최종 리스폰 가드)를 분리해야
    // 한다 - 같은 플래그를 쓰면 OnBallFinished가 비동기로 판정을 끝내고 이벤트를 쏘기도
    // 전에 이미 true가 되어버려서 정작 리스폰 자체가 무시돼버린다.
    private void Trigger()
    {
        if (_reported) return;
        _reported = true;

        if (PinDeckManager.Instance != null)
            PinDeckManager.Instance.OnBallFinished(gameObject);
        else
        {
            _triggered = true;
            _spawner.Respawn(gameObject);
        }
    }
}
