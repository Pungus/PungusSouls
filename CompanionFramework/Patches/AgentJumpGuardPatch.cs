using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class AgentJumpUtility
{
    public static bool CanUseJump(Component component)
    {
        if (component == null)
            return false;

        AgentComponent agent = component.GetComponent<AgentComponent>();

        if (agent == null)
            return true;

        AgentPrefabProfile profile = component.GetComponent<AgentPrefabProfile>();

        if (profile != null)
            return profile.CanJump;

        return true;
    }
}

[HarmonyPatch]
public static class AgentNativeJumpGuardPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        MethodInfo[] methods = typeof(Character).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];

            if (method != null && method.Name == "Jump")
                yield return method;
        }
    }

    private static bool Prefix(Character __instance)
    {
        if (__instance == null)
            return true;

        AgentComponent agent = __instance.GetComponent<AgentComponent>();

        if (agent == null)
            return true;

        return AgentJumpUtility.CanUseJump(__instance);
    }
}
