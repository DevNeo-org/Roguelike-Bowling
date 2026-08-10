// 보유 상점 아이템(InventoryManager)을 실제 수치 배율/플래그로 변환한다.
// 투구·장애물 스크립트는 이 클래스가 계산한 값만 한두 줄로 읽어가고,
// 아이템별 판단 로직 자체는 전부 여기 모아둔다.
public static class ItemEffectManager
{
    private static bool Owned(string itemId)
        => InventoryManager.Instance != null && InventoryManager.Instance.IsOwned(itemId);

    /// <summary>가동 범위 확장 벨트: 좌우 포지셔닝 가능 범위 배율.</summary>
    public static float ArmRadiusMultiplier => Owned(ItemIds.ArmRangeBelt) ? 1.4f : 1f;

    /// <summary>밸런스화: 손떨림으로 인식되는 곡률 데드존을 넓혀 오조작을 보정.</summary>
    public static float StraightnessDeadZoneMultiplier => Owned(ItemIds.BalanceShoes) ? 2f : 1f;

    /// <summary>스트레이트 슈즈: 드래그 곡률을 무시하고 항상 직선으로 발사.</summary>
    public static bool ForceStraightTrajectory => Owned(ItemIds.StraightShoes);

    /// <summary>미끄럼 방지 장갑: 스윙 파워가 아무리 낮아도 보장되는 최소치.</summary>
    public static float MinPowerFloor => Owned(ItemIds.AntiSlipGlove) ? 0.3f : 0f;

    /// <summary>무거운 공/경량코팅볼: 공 질량 배율. 무거우면 파괴력↑·속도↓, 가벼우면 반대.</summary>
    public static float BallMassMultiplier
    {
        get
        {
            float mult = 1f;
            if (Owned(ItemIds.HeavyBall)) mult *= 1.3f;
            if (Owned(ItemIds.LightCoatedBall)) mult *= 0.75f;
            return mult;
        }
    }

    /// <summary>스핀 아대: 발사 시 적용되는 스핀(angularVelocity) 배율.</summary>
    public static float SpinScaleMultiplier => Owned(ItemIds.SpinBand) ? 1.5f : 1f;

    /// <summary>낡은 수건/왁스 코팅 타월: 매그너스 커브 힘 배율. 낡은 수건=더 세게, 왁스=더 약하게.</summary>
    public static float CurveStrengthMultiplier
    {
        get
        {
            float mult = 1f;
            if (Owned(ItemIds.OldTowel)) mult *= 1.4f;
            if (Owned(ItemIds.WaxTowel)) mult *= 0.6f;
            return mult;
        }
    }

    /// <summary>장애물 통과 볼: 장애물과의 물리 충돌 자체를 무시.</summary>
    public static bool HasObstaclePass => Owned(ItemIds.ObstaclePassBall);

    /// <summary>장애물 파괴 볼 보유 여부(소모 전 확인용).</summary>
    public static bool HasObstacleBreaker => Owned(ItemIds.ObstacleBreakerBall);

    /// <summary>장애물 파괴 볼을 1회 소모한다. 보유 중이 아니면 아무 일도 하지 않고 false 반환.</summary>
    public static bool TryConsumeObstacleBreaker()
    {
        if (!HasObstacleBreaker || InventoryManager.Instance == null) return false;
        return InventoryManager.Instance.RemoveItem(ItemIds.ObstacleBreakerBall);
    }
}
