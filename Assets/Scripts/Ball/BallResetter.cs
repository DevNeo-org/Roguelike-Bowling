using UnityEngine;

// 공이 레인에서 이탈(낙하)하거나 제한 시간이 초과되면 BallSpawner에 알린다.
// Attach to Ball prefab.
public class BallResetter : MonoBehaviour
{
    [Tooltip("이 Y 좌표 이하로 내려가면 낙하로 판정")]
    [SerializeField] private float fallThreshold = -3f;
    [Tooltip("이 시간(초)이 지나면 낙하 여부와 관계없이 리스폰")]
    [SerializeField] private float respawnTimeout = 10f;

    private BallSpawner _spawner;
    private bool _triggered;
    private float _elapsed;

    /// <summary>BallSpawner가 생성 직후 호출해 참조를 주입한다.</summary>
    public void Init(BallSpawner spawner)
    {
        _spawner = spawner;
    }

    private void Update()
    {
        if (_triggered) return;

        _elapsed += Time.deltaTime;

        if (transform.position.y < fallThreshold || _elapsed >= respawnTimeout)
        {
            _triggered = true;
            _spawner.Respawn(gameObject);
        }
    }
}
