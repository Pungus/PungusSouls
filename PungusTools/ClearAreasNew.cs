using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PungusSouls
{
    [HarmonyPatch]
    public static class ExpandClearAreaCheck
    {
        private const float ExtraClearAreaRadius = 10f;
        private const float RefreshInterval = 5f;
        private const bool DebugClearAreaPatch = false;

        private static readonly List<Location> CachedLocations = new List<Location>();
        private static float _lastRefreshTime;
        private static bool _targetWarningLogged;


        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(ZoneSystem),
                "InsideClearArea",
                new[]
                {
            typeof(List<>).MakeGenericType(
                AccessTools.Inner(typeof(ZoneSystem), "ClearArea")),
            typeof(Vector3)
                });
        }


        private static void RefreshLocations()
        {
            CachedLocations.Clear();

            Location[] locations = Resources.FindObjectsOfTypeAll<Location>();

            for (int i = 0; i < locations.Length; i++)
            {
                Location location = locations[i];

                if (location == null || location.gameObject == null)
                    continue;

                if (!location.gameObject.scene.IsValid() || !location.gameObject.scene.isLoaded)
                    continue;

                if (!location.m_clearArea)
                    continue;

                CachedLocations.Add(location);
            }

            _lastRefreshTime = Time.time;
        }

        private static void Postfix(
            ref bool __result,
            Vector3 __1)
        {
            if (__result)
                return;

            Vector3 point = __1;

            if (Time.time - _lastRefreshTime > RefreshInterval ||
                CachedLocations.Count == 0)
            {
                RefreshLocations();
            }

            point.y = 0f;

            for (int i = CachedLocations.Count - 1; i >= 0; i--)
            {
                Location location = CachedLocations[i];

                if (location == null || location.gameObject == null)
                {
                    CachedLocations.RemoveAt(i);
                    continue;
                }

                if (!location.m_clearArea)
                    continue;

                float baseRadius =
                    Mathf.Max(
                        location.m_exteriorRadius,
                        location.m_interiorRadius);

                if (baseRadius <= 0f)
                    continue;

                float radius = baseRadius + ExtraClearAreaRadius;

                Vector3 center = location.transform.position;
                center.y = 0f;

                if ((point - center).sqrMagnitude <= radius * radius)
                {
                    __result = true;
                    return;
                }
            }
        }
    }
}