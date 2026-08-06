using UnityEngine;
using UnityEngine.UI;

// 공 주변에 표시되는 원형 파워 게이지. World Space Canvas에 부착.
// fillAmount는 ThrowInputHandler.SwingPhaseNormalized(원본 스윙 진행도, 0~1, 재형태화 없음)를
// maxFillAmount 비율로 줄여 그대로 반영 — 0~maxFillAmount를 일정한 속도로 왕복한다.
// 색상은 별도로 계산해 그 구간의 정중앙인 peakPosition(기본 0.25, 즉 0.2~0.3 부근)에서
// 빨간색이 되고 양 끝(0 / maxFillAmount)에 가까워질수록 초록색이 된다.
// (실제 투구 파워인 ThrowPowerNormalized는 이미 스윙 중앙에서 최대가 되도록 재형태화되어 있으며
// BallLauncher와 파워 텍스트가 그 값을 직접 사용한다 — 이 게이지는 표시 전용.)
public class PowerGaugeUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [Tooltip("fillAmount가 도달할 수 있는 최댓값(0~1)이자 파워 표시 오실레이션의 진폭. 0.5면 0~0.5 구간을 왕복하고 반원까지 찬다.")]
    [SerializeField, Range(0f, 1f)] private float maxFillAmount = 0.5f;

    [Header("Sweet Spot")]
    [Tooltip("게이지 표시용 절반 스케일 파워(0~0.5) 기준 최대 지점. 0.25(정중앙)면 0.2~0.3 부근에서 최대가 되고 양 끝까지 대칭으로 줄어든다.")]
    [SerializeField, Range(0.05f, 0.45f)] private float peakPosition = 0.25f;
    [Tooltip("최대 지점 주변 폭. peakPosition=0.25, halfWidth=0.05면 0.2~0.3 구간 전체가 최대.")]
    [SerializeField, Range(0.01f, 0.2f)] private float peakHalfWidth = 0.05f;

    [Header("Color")]
    [SerializeField] private Color lowColor = Color.green;
    [SerializeField] private Color midColor = new Color(1f, 0.55f, 0f);
    [SerializeField] private Color highColor = Color.red;

    private Camera _camera;
    private ThrowInputHandler _input;
    private Canvas _canvas;

    private void Awake()
    {
        _camera = Camera.main;
        _input = FindFirstObjectByType<ThrowInputHandler>();
        _canvas = GetComponent<Canvas>();
    }

    private void LateUpdate()
    {
        if (_input == null || _camera == null) return;

        // 조준 중(위치조정/파워진동/스핀드래그)에만 표시 — 투구가 확정(Done)되면 즉시 숨긴다.
        ThrowInputHandler.ThrowState state = _input.State;
        bool visible = state == ThrowInputHandler.ThrowState.Positioning
            || state == ThrowInputHandler.ThrowState.Oscillating
            || state == ThrowInputHandler.ThrowState.ForwardDrag;

        if (_canvas != null && _canvas.enabled != visible)
            _canvas.enabled = visible;

        if (!visible) return;

        // 원본 스윙 진행도(재형태화 없음)를 maxFillAmount 비율로 줄여 0~maxFillAmount 구간을
        // 일정한 속도로 왕복하는 표시용 값을 만든다. fillAmount는 이 값을 그대로 사용 —
        // 게이지가 꽉 찼는지와 색상은 서로 다른 기준이므로 분리한다.
        float displayRaw = _input.SwingPhaseNormalized * maxFillAmount;
        fillImage.fillAmount = displayRaw;

        // 색상은 peakPosition(기본 0.25, 즉 0.2~0.3 부근)에 가까울수록 빨강, 양 끝(0 / maxFillAmount)에
        // 가까울수록 초록 — fillAmount 값과는 별개로 계산한다.
        float dist = Mathf.Abs(displayRaw - peakPosition);
        float maxDist = displayRaw < peakPosition ? peakPosition : maxFillAmount - peakPosition;
        float gaugeValue = dist <= peakHalfWidth
            ? 1f
            : 1f - Mathf.InverseLerp(peakHalfWidth, maxDist, dist);

        fillImage.color = gaugeValue < 0.5f
            ? Color.Lerp(lowColor, midColor, gaugeValue * 2f)
            : Color.Lerp(midColor, highColor, (gaugeValue - 0.5f) * 2f);

        // 항상 카메라를 정면으로 바라보게 빌보드 처리.
        transform.rotation = Quaternion.LookRotation(transform.position - _camera.transform.position);
    }
}
