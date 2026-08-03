using System;
using System.Collections;
using UnityEngine;

// SkillEffect는 MonoBehaviour가 아니라서 코루틴을 직접 못 돌린다 - 지연 실행이 필요한
// 스킬(시한폭탄 등)을 위한 최소한의 실행기. 필요할 때 자동으로 하나 생성된다.
public class SkillRoutineRunner : MonoBehaviour
{
    private static SkillRoutineRunner instance;

    public static void RunDelayed(float delay, Action callback)
    {
        if (instance == null)
        {
            var go = new GameObject("SkillRoutineRunner");
            instance = go.AddComponent<SkillRoutineRunner>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        instance.StartCoroutine(instance.DelayedInvoke(delay, callback));
    }

    private IEnumerator DelayedInvoke(float delay, Action callback)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }
}
