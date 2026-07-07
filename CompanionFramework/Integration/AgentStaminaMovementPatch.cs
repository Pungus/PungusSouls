using HarmonyLib;

[HarmonyPatch(typeof(AgentBehaviourController), "MoveToPoint")]
public static class AgentStaminaMovementPatch
{
    private static void Prefix(AgentBehaviourController __instance, ref bool run)
    {
        if (!run || __instance == null)
            return;

        AgentStamina stamina = __instance.GetComponent<AgentStamina>();

        if (stamina != null && !stamina.CanRun())
            run = false;
    }
}
