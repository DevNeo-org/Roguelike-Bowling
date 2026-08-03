using UnityEngine;

// 패시브 스킬/아이템이 공 하나에 적용한 배율을 들고 있는 데이터 홀더.
// SkillEffect/ItemEffect의 OnBallSpawned()가 필요할 때 GetOrAdd로 붙이고, BallLauncher가 발사 시 읽어간다.
public class BallSkillModifiers : MonoBehaviour
{
    public float LaunchSpeedMultiplier = 1f;
    public float SpinMultiplier = 1f;

    public static BallSkillModifiers GetOrAdd(GameObject ball)
    {
        var existing = ball.GetComponent<BallSkillModifiers>();
        return existing != null ? existing : ball.AddComponent<BallSkillModifiers>();
    }
}
