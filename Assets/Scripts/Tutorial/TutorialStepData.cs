using UnityEngine;

public enum TutorialCompletionCondition { OnNextClick, OnPositioning, OnThrow, OnSpin }

// 튜토리얼 단계의 순수 데이터 에셋.
// Project 창에서 우클릭 → Create → Tutorial → Step 으로 생성.
// 씬 오브젝트 참조(waitVisual 등)는 TutorialController에서 관리.
[CreateAssetMenu(fileName = "TutorialStep_New", menuName = "Tutorial/Step")]
public class TutorialStepData : ScriptableObject
{
    [Tooltip("조건 달성 전 순서대로 표시할 대사")]
    [TextArea(1, 4)] public string[] lines;

    [Tooltip("이 단계의 완료 조건")]
    public TutorialCompletionCondition condition;

    [Tooltip("조건 달성 후 표시할 대사. 비어있으면 바로 다음 단계로.")]
    [TextArea(1, 4)] public string[] completionLines;
}
