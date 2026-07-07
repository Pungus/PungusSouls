using HarmonyLib;

namespace Integration
{
    [HarmonyPatch(typeof(Container), "CheckAccess")]
    public static class AgentContainer_CheckAccess_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(Container __instance, ref bool __result)
        {
            if (__instance.GetComponent<AgentComponent>() == null)
                return true;

            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Container), "RPC_RequestStack")]
    public static class AgentContainer_RPC_RequestStack_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(Container __instance)
        {
            if (__instance.GetComponent<AgentComponent>() == null)
                return true;

            return false;
        }
    }
}