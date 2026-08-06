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

    private GameObject _currentBall;
    private bool _subscribedToWeightSelector;

    private void Start()
    {
        SpawnBall();
        TrySubscribeToWeightSelector();
    }

    // StagePlayUI(따라서 BallWeightSelector)가 Start() 이후 한참 뒤에야 활성화되는 경우
    // (메인메뉴 -> 새 게임 흐름 등) Start()/SpawnBall() 시점 재시도만으로는 못 잡을 수 있어
    // 구독 전까지만 매 프레임 가볍게 재확인한다 - 구독되는 순간 더 이상 호출되지 않는다.
    private void Update()
    {
        if (!_subscribedToWeightSelector)
            TrySubscribeToWeightSelector();
    }

    private void OnDestroy()
    {
        if (_subscribedToWeightSelector && BallWeightSelector.Instance != null)
            BallWeightSelector.Instance.OnWeightChanged -= HandleWeightChanged;
    }

    // BallWeightSelector는 StagePlayUI 하위(처음엔 비활성) 오브젝트에 붙어있어서 Awake()가
    // Ball_Spawner의 Start()보다 늦게 실행될 수 있다 - 그 시점에 한 번만 구독을 시도하면
    // 놓칠 수 있으므로, Instance가 생기는 걸 확인할 때마다(Start 및 매 SpawnBall) 다시 시도한다.
    // 뒤늦게 구독에 성공하면 이미 스폰되어 있던 공에도 현재 선택을 즉시 따라잡게 한다.
    private void TrySubscribeToWeightSelector()
    {
        if (_subscribedToWeightSelector || BallWeightSelector.Instance == null) return;

        BallWeightSelector.Instance.OnWeightChanged += HandleWeightChanged;
        _subscribedToWeightSelector = true;
        HandleWeightChanged(BallWeightSelector.Instance.SelectedIndex);
    }

    // 아직 던지지 않고 대기 중인 공이 있으면, 무게 버튼을 누른 즉시 그 공에도 바로 반영한다
    // (다음 리스폰까지 기다리지 않고 지금 들고 있는 공부터 바뀌어야 자연스럽다).
    private void HandleWeightChanged(int index)
    {
        if (_currentBall == null) return;
        ApplySelectedWeight(_currentBall);
    }

    private void ApplySelectedWeight(GameObject ball)
    {
        if (BallWeightSelector.Instance == null) return;

        var rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
            rb.mass = BallWeightSelector.Instance.GetMassForSelected();

        var bodyRenderer = ball.GetComponent<MeshRenderer>();
        if (bodyRenderer != null)
        {
            var mpb = new MaterialPropertyBlock();
            bodyRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", BallWeightSelector.Instance.GetColorForSelected());
            bodyRenderer.SetPropertyBlock(mpb);
        }
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

        _currentBall = ball;

        var rb = ball.GetComponent<Rigidbody>();

        // 무게 선택 UI(BallWeightSelector)가 있으면 선택된 파운드에 맞춰 질량/색을 적용한다.
        // launcher.SetBall()이 이 질량을 "기준값"으로 삼아 상점 아이템 배율을 곱하므로 반드시 그 전에 설정한다.
        ApplySelectedWeight(ball);

        // 플레이어가 실제로 던지기(Launch) 전까지는 중력 영향을 받지 않도록 고정한다.
        // 이걸 안 하면 스폰 직후 공이 바로 떨어져서(중력) 플레이어 입력 없이 거터로 판정돼버린다.
        // (새로 생성된 Rigidbody는 이미 속도가 0이므로 별도로 초기화할 필요는 없다 — kinematic body에
        //  linearVelocity/angularVelocity를 대입하면 콘솔에 지원되지 않는다는 경고만 찍힌다.)
        rb.isKinematic = true;

        launcher.SetBall(rb);
        launcher.ResetLaunch();

        Debug.Log("[스포너] 새 공 생성 완료");
    }
}
