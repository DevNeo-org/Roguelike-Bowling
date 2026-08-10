using UnityEngine;

// 상점 아이템 중 장애물 관련 2종(장애물 통과 볼 / 장애물 파괴 볼)을 전담한다.
// 장애물 스크립트(ObstacleRock, ObstacleBananaPeel 등)는 한 줄도 수정하지 않고,
// 공 쪽에서 충돌을 가로채거나 사전에 물리 충돌 자체를 무시시키는 방식으로 동작한다.
// Ball 프리팹에 부착.
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class BallItemEffects : MonoBehaviour
{
    // 장애물로 취급할 컴포넌트 타입 목록. 새 장애물 스크립트가 추가되면 여기에만 타입을 더하면 된다.
    private static readonly System.Type[] ObstacleTypes =
    {
        typeof(ObstacleRock), typeof(ObstacleBananaPeel), typeof(TrampolineGate),
        typeof(ThinIceCrack), typeof(SnowplowPatrol), typeof(RotatingIceDisc),
        typeof(QuicksandCrack), typeof(MovingSandstormZone), typeof(MagmaHazard),
        typeof(MagneticPullZone), typeof(LavaGeyser), typeof(IcicleDrop),
        typeof(IceBouncePad), typeof(IcePendulum), typeof(CamelWander), typeof(BlizzardZone),
    };

    private Collider _ballCollider;

    // 장애물 파괴 볼은 공이 날아가는 동안 계속(충돌 시점에) 확인해야 하는데, 그 시점엔 이미
    // ItemActivationManager의 "다음 투구" 켜짐 상태가 (Launch() 직후) 꺼져있을 수 있다 -
    // 그래서 ArmForThrow() 시점에 이번 공 전용으로 스냅샷을 떠서 그 공이 사라질 때까지 들고 있는다.
    private bool _obstacleBreakerArmedThisBall;

    private void Awake()
    {
        _ballCollider = GetComponent<Collider>();
    }

    /// <summary>BallLauncher.Launch()가 발사 직전에 호출 - "이번 투구에 켜진" 액티브 아이템을
    /// 이 공 하나에 확정(스냅샷)한다. 장애물 통과 볼은 여기서 바로 적용+소모하고,
    /// 장애물 파괴 볼은 실제 충돌 시점까지 켜짐 여부만 기억해둔다.</summary>
    public void ArmForThrow()
    {
        if (ItemEffectManager.HasObstaclePass)
        {
            IgnoreAllObstacleCollisions();
            ItemEffectManager.ConsumeObstaclePass();
        }

        _obstacleBreakerArmedThisBall = ItemEffectManager.HasObstacleBreaker;
    }

    // 장애물 통과 볼: 스폰 시점에 씬의 모든 장애물 콜라이더와 물리 충돌 자체를 무시시켜,
    // 장애물 스크립트의 OnCollision/OnTrigger가 아예 호출되지 않게 한다.
    private void IgnoreAllObstacleCollisions()
    {
        if (_ballCollider == null) return;

        var allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mb in allBehaviours)
        {
            if (mb == null || System.Array.IndexOf(ObstacleTypes, mb.GetType()) < 0)
                continue;

            foreach (var col in mb.GetComponentsInChildren<Collider>(true))
                Physics.IgnoreCollision(_ballCollider, col, true);
        }
    }

    // 장애물 파괴 볼: 장애물과 처음 충돌/트리거되는 순간 장애물을 비활성화하고 아이템을 1회 소모한다.
    private void OnCollisionEnter(Collision collision) => TryBreakObstacle(collision.gameObject);
    private void OnTriggerEnter(Collider other) => TryBreakObstacle(other.gameObject);

    private void TryBreakObstacle(GameObject hit)
    {
        if (!_obstacleBreakerArmedThisBall || !IsObstacle(hit))
            return;

        if (ItemEffectManager.ConsumeObstacleBreaker())
        {
            Debug.Log($"[장애물 파괴 볼] {hit.name} 파괴됨 (아이템 소모)");
            hit.SetActive(false);
            _obstacleBreakerArmedThisBall = false; // 이 공에서 다시 못 쓰게(1회용)
        }
    }

    private bool IsObstacle(GameObject go)
    {
        foreach (var type in ObstacleTypes)
        {
            if (go.GetComponent(type) != null)
                return true;
        }
        return false;
    }
}
