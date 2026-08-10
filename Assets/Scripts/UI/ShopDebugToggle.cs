using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

// 개발 중 상점/아이템 UI를 빠르게 켜보기 위한 에디터 전용 단축키(P키).
// StageManager.EnterMaintenance()와 동일하게 StagePlayUI/MaintenanceUI를 상호 배타로 전환한다.
// 클래스 자체는 항상 컴파일되어야 빌드에서 "미싱 스크립트"가 되지 않으므로, 실제 로직만
// UNITY_EDITOR 안에 두어 실제 빌드에서는 아무 필드도 로직도 없는 빈 컴포넌트가 된다.
public class ShopDebugToggle : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private GameObject stagePlayUI;
    [SerializeField] private GameObject maintenanceUI;

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.pKey.wasPressedThisFrame) return;
        if (maintenanceUI == null) return;

        bool willOpen = !maintenanceUI.activeSelf;
        maintenanceUI.SetActive(willOpen);
        if (stagePlayUI != null)
            stagePlayUI.SetActive(!willOpen);
    }
#endif
}
