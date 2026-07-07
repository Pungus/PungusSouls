using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[HarmonyPatch(typeof(ZNetScene), "Awake")]
[HarmonyPriority(Priority.First)]
public static class Diagnostics
{
    private static bool _loggedPatchInfo;

    private static void Prefix(ZNetScene __instance)
    {
        LogPatchInfoOnce();
        InspectAndCleanPrefabList(__instance);
    }

    private static void Finalizer(System.Exception __exception)
    {
        if (__exception == null)
            return;

        Debug.LogError(
            "[ZNetSceneInspector] ZNetScene.Awake threw " +
            __exception.GetType().FullName +
            ": " + __exception.Message +
            "\n" + __exception.StackTrace
        );
    }

    private static void LogPatchInfoOnce()
    {
        if (_loggedPatchInfo)
            return;

        _loggedPatchInfo = true;
        MethodBase method = AccessTools.Method(typeof(ZNetScene), "Awake");
        Patches patches = Harmony.GetPatchInfo(method);

        if (patches == null)
        {
            //Debug.Log("[ZNetSceneInspector] No Harmony patch info found for ZNetScene.Awake");
            return;
        }

        LogPatchList("prefix", patches.Prefixes);
        LogPatchList("postfix", patches.Postfixes);
        LogPatchList("transpiler", patches.Transpilers);
        LogPatchList("finalizer", patches.Finalizers);
    }

    private static void LogPatchList(string label, IReadOnlyCollection<Patch> patches)
    {
        if (patches == null || patches.Count == 0)
        {
            //Debug.Log("[ZNetSceneInspector] " + label + " patches: none");
            return;
        }

        foreach (Patch patch in patches)
        {
            string methodName = patch.PatchMethod != null ? patch.PatchMethod.DeclaringType + "." + patch.PatchMethod.Name : "unknown";
            //Debug.Log("[ZNetSceneInspector] " + label + " owner=" + patch.owner + " priority=" + patch.priority + " method=" + methodName);
        }
    }

    private static void InspectAndCleanPrefabList(ZNetScene scene)
    {
        if (scene == null || scene.m_prefabs == null)
        {
            Debug.LogWarning("[ZNetSceneInspector] ZNetScene or m_prefabs is null before Awake");
            return;
        }

        //Debug.Log("[ZNetSceneInspector] Prefab count before Awake=" + scene.m_prefabs.Count);

        Dictionary<int, string> seen = new Dictionary<int, string>();
        int nullCount = 0;
        int duplicateCount = 0;

        for (int i = 0; i < scene.m_prefabs.Count; i++)
        {
            GameObject prefab = scene.m_prefabs[i];

            if (prefab == null)
            {
                nullCount++;
                LogNeighbors(scene, i, 12);
                continue;
            }

            int hash = prefab.name.GetStableHashCode();

            if (seen.TryGetValue(hash, out string existing))
            {
                duplicateCount++;
                Debug.LogError("[ZNetSceneInspector] Duplicate prefab hash=" + hash + " existing=" + existing + " duplicate=" + prefab.name + " index=" + i);
                continue;
            }

            seen.Add(hash, prefab.name);
        }

        if (nullCount > 0)
            Debug.LogError("[ZNetSceneInspector] Null prefab entries before cleanup=" + nullCount);

        if (duplicateCount > 0)
            Debug.LogError("[ZNetSceneInspector] Duplicate prefab entries before cleanup=" + duplicateCount);

        RemoveNullPrefabs(scene);
    }

    private static void LogNeighbors(ZNetScene scene, int index, int range)
    {
        Debug.LogError("[ZNetSceneInspector] Null prefab at index=" + index + " total=" + scene.m_prefabs.Count);

        int start = Mathf.Max(0, index - range);
        int end = Mathf.Min(scene.m_prefabs.Count - 1, index + range);

        for (int i = start; i <= end; i++)
        {
            GameObject prefab = scene.m_prefabs[i];
            Debug.LogError("[ZNetSceneInspector] neighbor index=" + i + " name=" + (prefab != null ? prefab.name : "<NULL>"));
        }
    }

    private static void RemoveNullPrefabs(ZNetScene scene)
    {
        int removed = 0;

        for (int i = scene.m_prefabs.Count - 1; i >= 0; i--)
        {
            if (scene.m_prefabs[i] != null)
                continue;

            scene.m_prefabs.RemoveAt(i);
            removed++;
        }

        if (removed > 0)
            Debug.LogError("[ZNetSceneInspector] Removed null prefab entries before original Awake. removed=" + removed);
    }
}

public class RootMotionProbe : MonoBehaviour
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    private void OnAnimatorMove()
    {
        if (_animator == null)
            return;

        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        Vector3 delta = _animator.deltaPosition;
        
    }
}

