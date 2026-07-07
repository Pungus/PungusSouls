using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(ZNetScene), "Awake")]
[HarmonyPriority(Priority.First)]
public static class ZNetSceneAwakeDebugPatch
{
    private static void Prefix(ZNetScene __instance)
    {
        try
        {
            Debug.Log("[ZNetSceneDebug] Awake prefix entered");

            if (__instance == null)
            {
                Debug.LogError("[ZNetSceneDebug] ZNetScene instance is null before original Awake");
                return;
            }

            if (__instance.m_prefabs == null)
            {
                Debug.LogError("[ZNetSceneDebug] ZNetScene.m_prefabs is null before original Awake");
                return;
            }

            Debug.Log("[ZNetSceneDebug] Prefab count before original Awake=" + __instance.m_prefabs.Count);

            Dictionary<int, string> seen = new Dictionary<int, string>();
            int nullCount = 0;

            for (int i = 0; i < __instance.m_prefabs.Count; i++)
            {
                GameObject prefab = __instance.m_prefabs[i];

                if (prefab == null)
                {
                    nullCount++;
                    Debug.LogError("[ZNetSceneDebug] Null prefab entry at index=" + i);
                    continue;
                }

                int hash = prefab.name.GetStableHashCode();

                if (seen.TryGetValue(hash, out string existing))
                {
                    Debug.LogError("[ZNetSceneDebug] Duplicate prefab hash before original Awake. hash=" + hash + " existing=" + existing + " duplicate=" + prefab.name + " duplicateIndex=" + i);
                    continue;
                }

                seen.Add(hash, prefab.name);
            }

            if (nullCount > 0)
                Debug.LogError("[ZNetSceneDebug] Null prefab entries before original Awake=" + nullCount);
        }
        catch (Exception ex)
        {
            Debug.LogError("[ZNetSceneDebug] Prefix diagnostics failed: " + ex);
        }
    }

    private static Exception Finalizer(Exception __exception)
    {
        if (__exception != null)
        {
            Debug.LogError("[ZNetSceneDebug] Awake threw exception type=" + __exception.GetType().FullName + " message=" + __exception.Message + " stack=" + __exception.StackTrace);
        }

        return __exception;
    }
}
