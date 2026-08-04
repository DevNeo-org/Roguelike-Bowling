// 스킬 하나의 실제 효과를 구현하는 베이스 클래스.
// 필요한 시점에만 override해서 쓴다 - 아직 아무 스킬도 쓰지 않는 훅은 추가하지 않는다.
public abstract class SkillEffect
{
    // Skills.json의 "name" 값과 동일해야 SkillEffectRegistry가 찾아 연결할 수 있다.
    public abstract string SkillName { get; }

    // 이 스킬을 새로 획득한 순간(상점 구매, 정비 화면 선택, 게임 시작 시 선택) 한 번 호출된다.
    public virtual void OnAcquired() { }

    // 공이 새로 생성될 때마다 호출된다(패시브 효과 - 압축/거대화처럼 매 공에 적용돼야 하는 스킬용).
    public virtual void OnBallSpawned(UnityEngine.Rigidbody ballRb) { }

    // 플레이어가 드래그를 놓아 실제로 투구가 시작되는 순간 호출된다(발동형 스킬용).
    public virtual void OnThrowStart() { }

    // 공이 멈춘 뒤 핀 집계 결과, 딱 1개만 서 있는 상태로 판정됐을 때 호출된다(오류 등).
    // standingPins는 그 순간 서 있는 핀 목록(길이 1)이며, 여기서 쓰러뜨리면 이번 투구
    // 판정에 반영된다.
    public virtual void OnPinsSettled(System.Collections.Generic.List<BowlingPin> standingPins) { }
}
