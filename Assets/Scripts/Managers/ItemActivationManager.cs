using System;
using System.Collections.Generic;

// 액티브 아이템(사용 버튼이 있는 아이템: 장애물 통과/파괴 볼, 낡은 수건, 왁스 코팅 타월,
// 스트레이트 슈즈)의 "다음 투구에 켜짐" 상태를 관리한다. 보유(InventoryManager)와는 별개 -
// 보유하고 있어도 버튼으로 켜지 않으면 효과가 적용되지 않는다.
// 켜진 상태는 딱 한 번의 투구에만 적용되고, 그 투구가 발사되면(BallLauncher.Launch()) 자동으로
// 전부 꺼진다 - 매번 다시 눌러야 한다.
public static class ItemActivationManager
{
    private static readonly HashSet<string> activeForNextThrow = new HashSet<string>();

    /// <summary>켜짐/꺼짐이 바뀔 때마다 발생 - UI(ActiveItemSelector)가 버튼 표시를 갱신하는 데 사용.</summary>
    public static event Action Changed;

    public static bool IsActive(string itemId) => activeForNextThrow.Contains(itemId);

    /// <summary>버튼으로 켜고 끈다. 보유하지 않은 아이템은 켤 수 없다.</summary>
    public static void SetActive(string itemId, bool active)
    {
        if (active)
        {
            if (InventoryManager.Instance == null || !InventoryManager.Instance.IsOwned(itemId))
                return;

            if (!activeForNextThrow.Add(itemId)) return;
        }
        else
        {
            if (!activeForNextThrow.Remove(itemId)) return;
        }

        Changed?.Invoke();
    }

    public static void Toggle(string itemId) => SetActive(itemId, !IsActive(itemId));

    /// <summary>투구(Launch) 직후 호출 - "다음 투구 1번만" 적용되도록 전부 끈다.</summary>
    public static void ClearAfterThrow()
    {
        if (activeForNextThrow.Count == 0) return;
        activeForNextThrow.Clear();
        Changed?.Invoke();
    }
}
