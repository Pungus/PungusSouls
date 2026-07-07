using HarmonyLib;
using System.Reflection;
using UnityEngine;

[HarmonyPatch]
public static class AgentFoodCharacterMaxHealthPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(Character), "GetMaxHealth");
    }

    private static bool Prepare()
    {
        return TargetMethod() != null;
    }

    private static void Postfix(Character __instance, ref float __result)
    {
        if (__instance == null)
            return;

        AgentFood food = __instance.GetComponent<AgentFood>();

        if (food == null)
            return;

        __result += food.TotalHealthBonus;
    }
}

[HarmonyPatch]
public static class AgentFoodHumanoidMaxStaminaPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(Humanoid), "GetMaxStamina") ?? AccessTools.Method(typeof(Character), "GetMaxStamina");
    }

    private static bool Prepare()
    {
        return TargetMethod() != null;
    }

    private static void Postfix(Component __instance, ref float __result)
    {
        if (__instance == null)
            return;

        AgentStamina stamina = __instance.GetComponent<AgentStamina>();

        if (stamina == null)
            return;

        __result = stamina.MaxStamina;
    }
}
