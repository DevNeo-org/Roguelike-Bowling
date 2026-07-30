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
        currentOptions.Clear();
        currentOptions.AddRange(PickRandomDistinct(GameDataManager.LoadSkills(), nameTexts.Length));

        for (int i = 0; i < nameTexts.Length; i++)
        {
            bool hasSkill = i < currentOptions.Count;
            var skill = hasSkill ? currentOptions[i] : null;

            if (nameTexts[i] != null)
                nameTexts[i].text = hasSkill ? skill.name : "";

            if (descriptionTexts[i] != null)
                descriptionTexts[i].text = hasSkill ? skill.description : "";

            if (selectButtons[i] != null)
                selectButtons[i].interactable = hasSkill;
        }
    }

    private void OnSelectClicked(int slotIndex)
    {
        if (slotIndex >= currentOptions.Count)
            return;

        // TODO: 스킬 시스템은 아직 구현되지 않았습니다.
        Debug.Log($"[정비 타임] 스킬 선택 시도: {currentOptions[slotIndex].name} (스킬 시스템 미구현)");
    }

    private static List<SkillEntry> PickRandomDistinct(List<SkillEntry> source, int count)
    {
        var pool = new List<SkillEntry>(source);
        var result = new List<SkillEntry>();
        int pickCount = Mathf.Min(count, pool.Count);

        for (int i = 0; i < pickCount; i++)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }
}
