using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 새 게임 시작 시 뜨는 스킬 선택 화면. skillPool 중 매번 무작위로 3개를 뽑아 보여주고,
// 하나를 고르면 그 즉시 스테이지를 시작한다. (실제 효과가 구현된 스킬은 SkillEffectRegistry
// 참고 - 풀에 있어도 미구현 스킬은 그냥 보유만 되고 아무 효과가 없다.)
public class InitialSkillPickController : MonoBehaviour
{
    [SerializeField] private string[] skillPool = { "압축", "거대화", "리무브", "거구", "탄성벽" };
    [SerializeField] private TextMeshProUGUI[] nameTexts;
    [SerializeField] private TextMeshProUGUI[] descriptionTexts;
    [SerializeField] private Button[] selectButtons;

    [SerializeField] private StageManager stageManager;
    [SerializeField] private GameObject playScreen;
    [SerializeField] private GameObject screenRoot;
    [SerializeField] private BallSpawner ballSpawner;

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
        var allSkills = GameDataManager.LoadSkills();
        var poolEntries = allSkills.FindAll(s => System.Array.IndexOf(skillPool, s.name) >= 0);

        currentOptions.Clear();
        currentOptions.AddRange(GameDataManager.PickRandomDistinct(poolEntries, nameTexts.Length));

        for (int i = 0; i < nameTexts.Length; i++)
        {
            bool hasSlot = i < currentOptions.Count;
            SkillEntry entry = hasSlot ? currentOptions[i] : null;

            if (nameTexts[i] != null)
                nameTexts[i].text = entry != null ? entry.name : "";

            if (descriptionTexts[i] != null)
                descriptionTexts[i].text = entry != null ? entry.description : "";

            if (selectButtons[i] != null)
                selectButtons[i].interactable = entry != null;
        }
    }

    private void OnSelectClicked(int slotIndex)
    {
        if (slotIndex >= currentOptions.Count)
            return;

        string skillName = currentOptions[slotIndex].name;
        SkillManager.Instance?.TryAddSkill(skillName);

        Debug.Log($"[게임 시작] 스킬 선택: {skillName}");

        if (stageManager != null)
            stageManager.StartFromStageOne();

        // Ball_Spawner.Start()가 스킬 선택 전에 이미 첫 공을 스폰해둔 상태라, 지금 고른
        // 패시브 스킬(압축/거대화 등)이 첫 공부터 반영되도록 즉시 다시 스폰한다.
        if (ballSpawner != null)
            ballSpawner.RespawnImmediately();

        if (playScreen != null)
            playScreen.SetActive(true);

        if (screenRoot != null)
            screenRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        for (int i = 0; i < selectButtons.Length; i++)
        {
            if (selectButtons[i] != null)
                selectButtons[i].onClick.RemoveAllListeners();
        }
    }
}
