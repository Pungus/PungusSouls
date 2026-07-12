using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace PungusSouls
{
    [HarmonyPatch]
    public static class CustomLocationDistancePatch
    {
        public const string AvoidAnyLocationGroup = "PungusAvoidAnyLocation";

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(ZoneSystem),
                "HaveLocationInRange");
        }

        [HarmonyPrefix]
        private static bool Prefix(
            ZoneSystem __instance,
            string prefabName,
            string group,
            Vector3 p,
            float radius,
            bool maxGroup,
            ref bool __result)
        {
            if (group != AvoidAnyLocationGroup)
                return true;

            if (__instance == null)
            {
                __result = false;
                return false;
            }

            foreach (ZoneSystem.LocationInstance instance in __instance.m_locationInstances.Values)
            {
                if (Vector3.Distance(instance.m_position, p) < radius)
                {
                    __result = true;
                    return false;
                }
            }

            __result = false;
            return false;
        }
    }
}