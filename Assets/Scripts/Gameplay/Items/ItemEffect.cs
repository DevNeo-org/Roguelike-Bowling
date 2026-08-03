using UnityEngine;

// 상점 아이템 하나의 실제 효과를 구현하는 베이스 클래스. SkillEffect와 동일한 패턴.
// 필요한 시점에만 override해서 쓴다 - 아직 아무 아이템도 쓰지 않는 훅은 추가하지 않는다.
public abstract class ItemEffect
{
    // ShopItems.json의 "name" 값과 동일해야 ItemEffectRegistry가 찾아 연결할 수 있다.
    public abstract string ItemName { get; }

    // 이 아이템을 새로 구매한 순간 한 번 호출된다.
    public virtual void OnAcquired() { }

    // 공이 새로 생성될 때마다 호출된다(장비류 아이템처럼 매 공에 적용돼야 하는 패시브용).
    public virtual void OnBallSpawned(Rigidbody ballRb) { }
}
