using UnityEngine;

// 막대형 파워 게이지. Screen Space - Camera Canvas에 부착.
// SwingPhaseNormalized(0~1 PingPong)에 따라 화살표가 막대 위를 좌우로 왕복한다.
// 앞으로 밀기(ForwardDrag)로 전환되는 순간의 화살표 위치가 파워를 결정한다.
// 스윗스팟 구간은 ThrowInputHandler의 PowerPeakPosition/HalfWidth를 직접 읽어 표시한다.
//
// 필요한 씬 구조:
//   BarBackground  — 가로 막대 배경 Image (RectTransform)
//   SweetSpotZone  — 중앙 스윗스팟 구간 강조 Image (선택, RectTransform)
//   ArrowIndicator — 좌우로 이동하는 화살표 Image (RectTransform)
public class PowerGaugeUI : MonoBehaviour
{
    [Header("Bar References")]
    [SerializeField] private RectTransform barBackground;
    [SerializeField] private RectTransform arrowIndicator;
    [SerializeField] private RectTransform sweetSpotZone; // 선택 — null이면 무시

    private ThrowInputHandler _input;
    private Canvas _canvas;

    private void Awake()
    {
        _input = FindFirstObjectByType<ThrowInputHandler>();
        _canvas = GetComponent<Canvas>();
    }

    private void Start()
    {
        // Canvas 레이아웃이 확정된 Start 시점에 스윗스팟 구간을 배치한다.
        LayoutSweetSpotZone();
    }

    // 스윗스팟 구간 Image의 위치·너비를 barBackground 기준으로 초기 설정한다.
    // ThrowInputHandler의 값을 직접 읽어 인스펙터 중복 설정을 방지한다.
    private void LayoutSweetSpotZone()
    {
        if (sweetSpotZone == null || barBackground == null || _input == null) return;

        float barWidth  = barBackground.rect.width;
        float centerX   = (_input.PowerPeakPosition - 0.5f) * barWidth;
        float zoneWidth = _input.PowerPeakHalfWidth * 2f * barWidth;

        sweetSpotZone.anchoredPosition = new Vector2(centerX, sweetSpotZone.anchoredPosition.y);
        sweetSpotZone.sizeDelta = new Vector2(zoneWidth, sweetSpotZone.sizeDelta.y);
    }

    private void LateUpdate()
    {
        if (_input == null) return;

        // 파워 진동/스핀 드래그 중에만 표시 — 투구가 확정(Done)되면 즉시 숨긴다.
        ThrowInputHandler.ThrowState state = _input.State;
        bool visible = state == ThrowInputHandler.ThrowState.Oscillating
            || state == ThrowInputHandler.ThrowState.ForwardDrag;

        if (_canvas != null && _canvas.enabled != visible)
            _canvas.enabled = visible;

        if (!visible) return;

        // 화살표 X 위치: SwingPhaseNormalized(0~1)를 막대 너비 전체로 매핑한다.
        // 0 → 왼쪽 끝, 0.5 → 중앙, 1 → 오른쪽 끝.
        if (arrowIndicator != null && barBackground != null)
        {
            float barWidth = barBackground.rect.width;
            float arrowX   = (_input.SwingPhaseNormalized - 0.5f) * barWidth;
            arrowIndicator.anchoredPosition = new Vector2(arrowX, arrowIndicator.anchoredPosition.y);
        }
    }
}
