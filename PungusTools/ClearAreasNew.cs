using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace PungusSouls
{
    [HarmonyPatch(typeof(ZoneSystem), "PlaceVegetation")]
    public static class ClearAreasFromOverlappingLocations
    {
        private const float ZoneHalfSize = 32f;
        private const float MinimumCustomClearAreaRadius = 23f;
        private const float ExtraClearAreaRadius = 20f;
        private const bool DebugClearAreaPatch = false;

        private static Type ClearAreaType;
        private static ConstructorInfo ClearAreaConstructor;

        [HarmonyPrepare]
        private static bool Prepare()
        {
            ClearAreaType = AccessTools.Inner(typeof(ZoneSystem), "ClearArea");

            if (ClearAreaType == null)
            {
                Debug.LogError("[PungusSouls ClearArea] Failed to find ZoneSystem.ClearArea");
                return false;
            }

            ClearAreaConstructor = AccessTools.Constructor(
                ClearAreaType,
                new[]
                {
                    typeof(Vector3),
                    typeof(float)
                });

            if (ClearAreaConstructor == null)
            {
                Debug.LogError("[PungusSouls ClearArea] Failed to find ZoneSystem.ClearArea constructor");
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        private static void Prefix(
            ZoneSystem __instance,
            Vector2i zoneID,
            Vector3 zoneCenterPos,
            object clearAreas)
        {
            if (__instance == null || clearAreas == null)
                return;

            IList clearAreaList = clearAreas as IList;

            if (clearAreaList == null)
                return;

            int added = 0;

            float zoneMinX = zoneCenterPos.x - ZoneHalfSize;
            float zoneMaxX = zoneCenterPos.x + ZoneHalfSize;
            float zoneMinZ = zoneCenterPos.z - ZoneHalfSize;
            float zoneMaxZ = zoneCenterPos.z + ZoneHalfSize;

            foreach (ZoneSystem.LocationInstance instance in __instance.m_locationInstances.Values)
            {
                ZoneSystem.ZoneLocation location = instance.m_location;

                if (location == null)
                    continue;

                if (!location.m_clearArea)
                    continue;

                float baseRadius =
                    Mathf.Max(
                        location.m_exteriorRadius,
                        location.m_interiorRadius);

                if (baseRadius <= MinimumCustomClearAreaRadius)
                    continue;

                float expandedRadius =
                    baseRadius +
                    Mathf.Max(
                        ExtraClearAreaRadius,
                        baseRadius * 0.15f);

                Vector3 center = instance.m_position;

                if (!OverlapsZone(
                        center,
                        expandedRadius,
                        zoneMinX,
                        zoneMaxX,
                        zoneMinZ,
                        zoneMaxZ))
                {
                    continue;
                }

                object clearArea =
                    ClearAreaConstructor.Invoke(
                        new object[]
                        {
                            center,
                            expandedRadius
                        });

                clearAreaList.Add(clearArea);
                added++;

                if (DebugClearAreaPatch)
                {
                    Debug.Log(
                        "[PungusSouls ClearArea] Added overlapping clear area " +
                        location.m_name +
                        " zone=" +
                        zoneID +
                        " center=" +
                        center +
                        " baseRadius=" +
                        baseRadius.ToString("F1") +
                        " expandedRadius=" +
                        expandedRadius.ToString("F1"));
                }
            }

            if (DebugClearAreaPatch && added > 0)
            {
                Debug.Log(
                    "[PungusSouls ClearArea] Added " +
                    added +
                    " overlapping clear areas for vegetation zone " +
                    zoneID);
            }
        }

        private static bool OverlapsZone(
            Vector3 center,
            float radius,
            float zoneMinX,
            float zoneMaxX,
            float zoneMinZ,
            float zoneMaxZ)
        {
            float clearMinX = center.x - radius;
            float clearMaxX = center.x + radius;
            float clearMinZ = center.z - radius;
            float clearMaxZ = center.z + radius;

            if (clearMaxX < zoneMinX)
                return false;

            if (clearMinX > zoneMaxX)
                return false;

            if (clearMaxZ < zoneMinZ)
                return false;

            if (clearMinZ > zoneMaxZ)
                return false;

            return true;
        }
    }
}