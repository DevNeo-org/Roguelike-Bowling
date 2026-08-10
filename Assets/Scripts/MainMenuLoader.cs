using UnityEngine;
using UnityEngine.SceneManagement;

// 메인 화면의 "게임 시작" 버튼에 연결. 누르면 실제 플레이 씬(Map)으로 넘어간다.
public class MainMenuLoader : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Map";

    public void OnStartGameClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
