using System.Collections;
using TMPro;
using UnityEngine;

// 투구 결과("스트라이크"/"거터"/"9점" 등)를 잠깐 띄웠다가 자동으로 닫는 알림 팝업.
// 이 컴포넌트 자신은 항상 활성 상태인 부모(StagePlayUI)에 붙어 있어야 한다 - popupRoot를
// 직접 끄고 켜는 코루틴이 자기 자신의 GameObject가 비활성화되면 같이 멈춰버리기 때문.
public class ThrowResultPopupController : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float displayDuration = 1f;

    public float DisplayDuration => displayDuration;

    private Coroutine hideRoutine;

    public void Show(string message)
    {
        if (popupRoot == null || messageText == null) return;

        messageText.text = message;
        popupRoot.SetActive(true);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        popupRoot.SetActive(false);
        hideRoutine = null;
    }
}
