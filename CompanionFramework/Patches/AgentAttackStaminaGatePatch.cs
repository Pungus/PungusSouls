using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

[HarmonyPatch]
public static class AgentAttackStaminaGatePatch
{
    private const float MinimumAttackStamina = 8f;
    private const float AttackStaminaCost = 8f;

    private static IEnumerable<MethodBase> TargetMethods()
    {
        MethodInfo[] methods = typeof(Humanoid).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];

            if (method != null && method.Name == "StartAttack")
                yield return method;
        }
    }

    private static bool Prefix(Humanoid __instance)
    {
        if (__instance == null)
            return true;

        AgentComponent agent = __instance.GetComponent<AgentComponent>();

        if (agent == null)
            return true;

        AgentStamina stamina = __instance.GetComponent<AgentStamina>();

        if (stamina == null)
            return true;

        if (!stamina.CanAttack(MinimumAttackStamina))
            return false;

        stamina.UseStamina(AttackStaminaCost);
        return true;
    }
}
