using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

[HarmonyPatch]
public static class AgentAttackStaminaGatePatch
{
    public static bool AllowResourceWorkAnimationAttack;
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

        bool controlledTask = agent.Context != null &&
            (agent.Context.TaskMode == Core.Agent.AgentContext.AgentTaskMode.Gather ||
             agent.Context.TaskMode == Core.Agent.AgentContext.AgentTaskMode.Lumbering ||
             agent.Context.TaskMode == Core.Agent.AgentContext.AgentTaskMode.Mining ||
             agent.Context.TaskMode == Core.Agent.AgentContext.AgentTaskMode.Quarrying ||
             agent.Context.TaskMode == Core.Agent.AgentContext.AgentTaskMode.Hunt ||
             agent.Context.TaskMode == Core.Agent.AgentContext.AgentTaskMode.Patrol);

        if (controlledTask && !AllowResourceWorkAnimationAttack && !HasNativeCombatTarget(__instance))
            return false;

        AgentStamina stamina = __instance.GetComponent<AgentStamina>();

        if (stamina == null)
            return true;

        if (!stamina.CanAttack(MinimumAttackStamina))
            return false;

        stamina.UseStamina(AttackStaminaCost);
        return true;
    }

    private static bool HasNativeCombatTarget(Humanoid humanoid)
    {
        if (humanoid == null)
            return false;

        MonsterAI ai = humanoid.GetComponent<MonsterAI>();

        if (ai == null)
            return false;

        FieldInfo field = ai.GetType().GetField("m_targetCreature", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (field == null)
            return false;

        Character target = field.GetValue(ai) as Character;
        return target != null && !target.IsDead();
    }
}
