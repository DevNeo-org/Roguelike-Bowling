// 보유 상점 아이템(InventoryManager)을 실제 수치 배율/플래그로 변환한다.
// 투구·장애물 스크립트는 이 클래스가 계산한 값만 한두 줄로 읽어가고,
// 아이템별 판단 로직 자체는 전부 여기 모아둔다.
//
// 아이템은 두 종류다:
//  - 패시브 아이템(밸런스화): 보유만 하고 있으면 항상 적용 - Owned() 기준.
//  - 액티브 아이템(장애물 통과/파괴 볼, 스트레이트 슈즈, 낡은 수건, 왁스 코팅 타월): 우측 사용
//    버튼(ActiveItemSelector)으로 "다음 투구에 켜기"를 직접 눌러야 적용 - ItemActivationManager
//    기준. 보유만으로는 효과가 켜지지 않는다.
public static class ItemEffectManager
{
    private static bool Owned(string itemId)
        => InventoryManager.Instance != null && InventoryManager.Instance.IsOwned(itemId);

    /// <summary>밸런스화(패시브): 파워 게이지 진동 속도 배율(1보다 작을수록 느려져 타이밍 맞추기 쉬움).</summary>
    public static float PowerOscillationSpeedMultiplier => Owned(ItemIds.BalanceShoes) ? 0.7f : 1f;

    /// <summary>스트레이트 슈즈(액티브): 이번 투구에 켜져 있으면 드래그 곡률을 무시하고 직선으로 발사.</summary>
    public static bool ForceStraightTrajectory => ItemActivationManager.IsActive(ItemIds.StraightShoes);

    // 낡은 수건/왁스 코팅 타월: 아이템 ID·상점 등록·사용 버튼(활성화 토글)까지는 있지만,
    // 실제 효과는 아직 구현하지 않는다(사용자 요청) - 나중에 곡률이 "더 일찍/늦게 반영되는"
    // 타이밍 효과로 구현할 예정.

    /// <summary>장애물 통과 볼(액티브): 이번 투구에 켜져 있으면 장애물과의 물리 충돌 자체를 무시.</summary>
    public static bool HasObstaclePass => ItemActivationManager.IsActive(ItemIds.ObstaclePassBall);

    /// <summary>장애물 파괴 볼(액티브): 이번 투구에 켜져 있으면 장애물과 부딪힐 때 파괴.</summary>
    public static bool HasObstacleBreaker => ItemActivationManager.IsActive(ItemIds.ObstacleBreakerBall);

    /// <summary>
    /// 장애물 통과 볼을 1회 소모한다(보유 목록에서 제거). BallItemEffects가 이미 자체적으로
    /// "이번 공에 켜져 있었는지" 스냅샷을 들고 있을 때만 호출하므로 여기서는 별도 확인 없이
    /// 바로 시도한다.
    /// </summary>
    public static bool ConsumeObstaclePass()
        => InventoryManager.Instance != null && InventoryManager.Instance.RemoveItem(ItemIds.ObstaclePassBall);

    /// <summary>장애물 파괴 볼을 1회 소모한다(보유 목록에서 제거). 위와 동일한 이유로 별도 확인 없음.</summary>
    public static bool ConsumeObstacleBreaker()
        => InventoryManager.Instance != null && InventoryManager.Instance.RemoveItem(ItemIds.ObstacleBreakerBall);
}
