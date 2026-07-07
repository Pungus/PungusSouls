using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(ZNetScene), "Awake")]
[HarmonyPriority(Priority.First)]
public static class ZNetSceneNullPrefabGuardPatch
{
    private static void Prefix(ZNetScene __instance)
    {
        if (__instance == null || __instance.m_prefabs == null)
            return;

        int removed = 0;

        for (int i = __instance.m_prefabs.Count - 1; i >= 0; i--)
        {
            if (__instance.m_prefabs[i] != null)
                continue;

            __instance.m_prefabs.RemoveAt(i);
            removed++;
        }

        if (removed > 0)
            Debug.LogError("[ZNetSceneNullPrefabGuard] Removed null prefab entries before ZNetScene.Awake. removed=" + removed);
    }
}
