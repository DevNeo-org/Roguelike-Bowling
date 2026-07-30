using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 정비 화면 좌측 스킬 패널: 활성화될 때마다 GameDataManager의 스킬 목록 중 무작위 3개를 보여준다.
public class SkillPanelController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] nameTexts;
    [SerializeField] private TextMeshProUGUI[] descriptionTexts;
    [SerializeField] private Button[] selectButtons;

    private readonly List<SkillEntry> currentOptions = new List<SkillEntry>();

    // 정비 화면 방문 1회당 장신구는 하나만 고를 수 있다 - 정비 화면에 다시 들어오면(OnEnable) 초기화된다.
    private bool hasPickedThisVisit;

    private void Awake()
    {
        for (int i = 0; i < selectButtons.Length; i++)
        {
            if (selectButtons[i] == null)
                continue;

            int slotIndex = i;
            selectButtons[i].onClick.AddListener(() => OnSelectClicked(slotIndex));
        }
    }

    private void OnEnable()
    {
        ShowRandomSkills();
    }

    private void ShowRandomSkills()
    {
        hasPickedThisVisit = false;
        currentOptions.Clear();

        var available = GameDataManager.LoadSkills();
        if (SkillManager.Instance != null)
            available = available.FindAll(s => !SkillManager.Instance.IsOwned(s.name));

        currentOptions.AddRange(GameDataManager.PickRandomDistinct(available, nameTexts.Length));

        for (int i = 0; i < nameTexts.Length; i++)
        {
            bool hasSkill = i < currentOptions.Count;
            var skill = hasSkill ? currentOptions[i] : null;

            if (nameTexts[i] != null)
                nameTexts[i].text = hasSkill ? skill.name : "";

            if (descriptionTexts[i] != null)
                descriptionTexts[i].text = hasSkill ? skill.description : "";

            if (selectButtons[i] != null)
            {
                selectButtons[i].interactable = hasSkill;

                var label = selectButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = "선택";
            }
        }
    }

    private void OnSelectClicked(int slotIndex)
    {
        if (hasPickedThisVisit || slotIndex >= currentOptions.Count)
            return;

        string skillName = currentOptions[slotIndex].name;

        // 스킬 효과 자체는 아직 구현되지 않았지만, 보유 목록에는 실제로 추가된다.
        if (SkillManager.Instance != null && SkillManager.Instance.TryAddSkill(skillName))
        {
            hasPickedThisVisit = true;
            Debug.Log($"[정비 타임] 스킬 획득: {skillName} (효과는 아직 미구현)");

            for (int i = 0; i < selectButtons.Length; i++)
            {
                if (selectButtons[i] == null)
                    continue;

                selectButtons[i].interactable = false;
            }

            var pickedLabel = selectButtons[slotIndex].GetComponentInChildren<TextMeshProUGUI>();
            if (pickedLabel != null)
                pickedLabel.text = "선택됨";
        }
        else
        {
            Debug.Log($"[정비 타임] 스킬 획득 실패: {skillName} (이미 보유 중이거나 SkillManager 없음)");
        }
    }
}
