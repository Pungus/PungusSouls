using HarmonyLib;
using System;
using UnityEngine;

[HarmonyPatch(typeof(ZNetScene), "Awake")]
public static class AgentPrefabInstallerPatch
{
    private static bool _installed;

    private static void Postfix(ZNetScene __instance)
    {
        try
        {
            Install(__instance);
        }
        catch (Exception ex)
        {
            Debug.LogError("[AgentInstall] ZNetScene.Awake postfix failed: " + ex);
        }
    }

    private static void Install(ZNetScene scene)
    {
        if (_installed)
            return;

        if (scene == null || scene.m_prefabs == null)
            return;

        int installed = 0;
        int invalid = 0;
        int failures = 0;

        for (int i = 0; i < scene.m_prefabs.Count; i++)
        {
            GameObject prefab = scene.m_prefabs[i];
            if (prefab != null && prefab.name.IndexOf("Hellkite", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Debug.Log("[FlyInstallDebug] Found prefab in ZNetScene: " + prefab.name
                    + " registryMatch=" + AgentPrefabRegistry.TryGetDefinition(prefab, out AgentPrefabDefinition def)
                    + " validAgentBase=" + AgentPrefabRegistry.IsValidAgentBasePrefab(prefab));
            }
            if (prefab == null)
                continue;

            if (!AgentPrefabRegistry.TryGetDefinition(prefab, out AgentPrefabDefinition definition))
                continue;

            try
            {
                if (!AgentPrefabRegistry.IsValidAgentBasePrefab(prefab))
                {
                    invalid++;
                    Debug.LogWarning("[AgentInstall] Matched prefab is not a valid agent base: " + prefab.name);
                    continue;
                }

                if (NpcInstaller.AttachAgentSystem(prefab, definition))
                    installed++;
            }
            catch (Exception ex)
            {
                failures++;
                Debug.LogError("[AgentInstall] Failed processing prefab index=" + i + " name=" + prefab.name + " error=" + ex);
            }
        }

        _installed = true;

        if (installed > 0 || invalid > 0 || failures > 0) ;
            //Debug.Log("[AgentInstall] Installed=" + installed + " invalid=" + invalid + " failures=" + failures + " configured=" + AgentPrefabRegistry.All.Count);
    }
}
