using System;
using System.Collections.Generic;
using UnityEngine;

// 정비 타임에 선택해서 획득한 스킬 목록을 관리한다. InventoryManager와 동일한 패턴.
// 스킬 효과 자체는 아직 구현되지 않았고, 여기서는 "무엇을 보유하고 있는지"만 추적한다.
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

    public bool TryAddSkill(string skillName)
    {
        if (string.IsNullOrEmpty(skillName) || ownedSkills.Contains(skillName))
            return false;

        ownedSkills.Add(skillName);
        OnSkillAdded?.Invoke(skillName);
        return true;
    }

    public void StartNewGame()
    {
        ownedSkills.Clear();
    }
}
