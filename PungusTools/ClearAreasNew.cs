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

        private const float MinimumCustomClearAreaRadius = 0f;
        private const float ExtraClearAreaRadius = 25f;

        private const bool IncludeLocationsWithoutClearArea = false;
        private const bool AddOffsetClearAreas = true;

        private const float OffsetAreaDistanceFactor = 0.55f;
        private const float OffsetAreaRadiusFactor = 0.65f;

        private const bool DebugClearAreaPatch = false;

        private static Type ClearAreaType;
        private static ConstructorInfo ClearAreaConstructor;

        [HarmonyPrepare]
        private static bool Prepare()
        {
            ClearAreaType = AccessTools.Inner(
                typeof(ZoneSystem),
                "ClearArea");

            if (ClearAreaType == null)
            {
                Debug.LogError(
                    "[PungusSouls ClearArea] Failed to find ZoneSystem.ClearArea");

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
                Debug.LogError(
                    "[PungusSouls ClearArea] Failed to find ZoneSystem.ClearArea constructor");

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
            {
                return;
            }

            IList clearAreaList = clearAreas as IList;

            if (clearAreaList == null)
            {
                return;
            }

            int added = 0;

            float zoneMinX = zoneCenterPos.x - ZoneHalfSize;
            float zoneMaxX = zoneCenterPos.x + ZoneHalfSize;
            float zoneMinZ = zoneCenterPos.z - ZoneHalfSize;
            float zoneMaxZ = zoneCenterPos.z + ZoneHalfSize;

            foreach (ZoneSystem.LocationInstance instance in __instance.m_locationInstances.Values)
            {
                ZoneSystem.ZoneLocation location = instance.m_location;

                if (location == null)
                {
                    continue;
                }

                if (!location.m_clearArea && !IncludeLocationsWithoutClearArea)
                {
                    continue;
                }

                float baseRadius = Mathf.Max(
                    location.m_exteriorRadius,
                    location.m_interiorRadius);

                if (baseRadius <= MinimumCustomClearAreaRadius)
                {
                    continue;
                }

                float expandedRadius =
                    baseRadius +
                    Mathf.Max(
                        ExtraClearAreaRadius,
                        baseRadius * 0.25f);

                Vector3 center = instance.m_position;
                center.y = 0f;

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

                added += AddClearArea(
                    clearAreaList,
                    center,
                    expandedRadius,
                    location,
                    zoneID,
                    "main");

                if (AddOffsetClearAreas)
                {
                    float offsetDistance =
                        expandedRadius * OffsetAreaDistanceFactor;

                    float offsetRadius =
                        expandedRadius * OffsetAreaRadiusFactor;

                    added += AddOffsetClearAreaIfOverlapping(
                        clearAreaList,
                        center + new Vector3(offsetDistance, 0f, 0f),
                        offsetRadius,
                        zoneMinX,
                        zoneMaxX,
                        zoneMinZ,
                        zoneMaxZ,
                        location,
                        zoneID,
                        "east");

                    added += AddOffsetClearAreaIfOverlapping(
                        clearAreaList,
                        center + new Vector3(-offsetDistance, 0f, 0f),
                        offsetRadius,
                        zoneMinX,
                        zoneMaxX,
                        zoneMinZ,
                        zoneMaxZ,
                        location,
                        zoneID,
                        "west");

                    added += AddOffsetClearAreaIfOverlapping(
                        clearAreaList,
                        center + new Vector3(0f, 0f, offsetDistance),
                        offsetRadius,
                        zoneMinX,
                        zoneMaxX,
                        zoneMinZ,
                        zoneMaxZ,
                        location,
                        zoneID,
                        "north");

                    added += AddOffsetClearAreaIfOverlapping(
                        clearAreaList,
                        center + new Vector3(0f, 0f, -offsetDistance),
                        offsetRadius,
                        zoneMinX,
                        zoneMaxX,
                        zoneMinZ,
                        zoneMaxZ,
                        location,
                        zoneID,
                        "south");
                }
            }

            if (DebugClearAreaPatch && added > 0)
            {
                Debug.Log(
                    "[PungusSouls ClearArea] Added " +
                    added +
                    " expanded clear areas for vegetation zone " +
                    zoneID);
            }
        }

        private static int AddOffsetClearAreaIfOverlapping(
            IList clearAreaList,
            Vector3 center,
            float radius,
            float zoneMinX,
            float zoneMaxX,
            float zoneMinZ,
            float zoneMaxZ,
            ZoneSystem.ZoneLocation location,
            Vector2i zoneID,
            string label)
        {
            center.y = 0f;

            if (!OverlapsZone(
                    center,
                    radius,
                    zoneMinX,
                    zoneMaxX,
                    zoneMinZ,
                    zoneMaxZ))
            {
                return 0;
            }

            return AddClearArea(
                clearAreaList,
                center,
                radius,
                location,
                zoneID,
                label);
        }

        private static int AddClearArea(
            IList clearAreaList,
            Vector3 center,
            float radius,
            ZoneSystem.ZoneLocation location,
            Vector2i zoneID,
            string label)
        {
            object clearArea = ClearAreaConstructor.Invoke(
                new object[]
                {
                    center,
                    radius
                });

            clearAreaList.Add(clearArea);

            if (DebugClearAreaPatch)
            {
                Debug.Log(
                    "[PungusSouls ClearArea] Added " +
                    label +
                    " clear area " +
                    location.m_name +
                    " zone=" +
                    zoneID +
                    " center=" +
                    center +
                    " radius=" +
                    radius.ToString("F1"));
            }

            return 1;
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
            {
                return false;
            }

            if (clearMinX > zoneMaxX)
            {
                return false;
            }

            if (clearMaxZ < zoneMinZ)
            {
                return false;
            }

            if (clearMinZ > zoneMaxZ)
            {
                return false;
            }

            return true;
        }
    }
}