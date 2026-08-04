using System;
using System.Collections.Generic;
using UnityEngine;

// 정비 타임에 선택해서 획득한 스킬 목록을 관리한다. InventoryManager와 동일한 패턴.
// 실제 스킬 효과는 SkillEffectRegistry에 등록된 SkillEffect 구현체가 담당하며,
// 여기서는 "무엇을 보유하고 있는지" 추적과 획득 시점에 그 효과를 한 번 호출해주는 역할만 한다.
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    private readonly HashSet<string> ownedSkills = new HashSet<string>();

    public event Action<string> OnSkillAdded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool IsOwned(string skillName)
    {
        return !string.IsNullOrEmpty(skillName) && ownedSkills.Contains(skillName);
    }

    public IEnumerable<string> GetOwnedSkills()
    {
        return ownedSkills;
    }

    // 공 생성/투구 시작처럼 "보유한 모든 스킬에 대해 훅을 호출"해야 하는 곳에서 쓴다.
    public IEnumerable<SkillEffect> GetOwnedEffects()
    {
        foreach (string skillName in ownedSkills)
        {
            if (SkillEffectRegistry.TryGet(skillName, out SkillEffect effect))
                yield return effect;
        }
    }

    public bool TryAddSkill(string skillName)
    {
        if (string.IsNullOrEmpty(skillName) || ownedSkills.Contains(skillName))
            return false;

        ownedSkills.Add(skillName);
        OnSkillAdded?.Invoke(skillName);
        AutoSave();

        if (SkillEffectRegistry.TryGet(skillName, out SkillEffect effect))
            effect.OnAcquired();

        return true;
    }

    public void StartNewGame()
    {
        ownedSkills.Clear();
        AutoSave();
    }

    public void LoadFromSave()
    {
        ownedSkills.Clear();

        foreach (string skillName in SaveManager.LoadSkills())
            ownedSkills.Add(skillName);
    }

    private void AutoSave()
    {
        SaveManager.SaveSkills(ownedSkills);
    }
}
