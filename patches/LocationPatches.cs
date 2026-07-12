using System;
using HarmonyLib;
using UnityEngine;

namespace PungusSouls
{
    [HarmonyPatch(typeof(ZoneSystem), "SpawnLocation")]
    public static class NewLondoSpawnLocationPatch
    {
        private const string TargetLocation = "newlondo";
        private const float SampleRadius = 75;
        private const int SampleCount = 32;
        private const float MinimumHeightDifference = 1.5f;

        private static void Prefix(
            ZoneSystem.ZoneLocation location,
            Vector3 pos,
            ref Quaternion rot)
        {
            if (!IsTargetLocation(location))
                return;

            Vector3 downhill = GetDownhillDirection(pos, SampleRadius, SampleCount, out float heightDifference);

            downhill.y = 0f;

            if (downhill.sqrMagnitude < 0.001f)
                return;

            if (heightDifference < MinimumHeightDifference)
                return;

            rot = Quaternion.LookRotation(downhill.normalized, Vector3.up);
        }

        private static bool IsTargetLocation(ZoneSystem.ZoneLocation location)
        {
            if (location == null)
                return false;

            if (string.Equals(location.m_prefabName, TargetLocation, StringComparison.OrdinalIgnoreCase))
                return true;

            if (location.m_prefab.IsValid && string.Equals(location.m_prefab.Name, TargetLocation, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static Vector3 GetDownhillDirection(Vector3 center, float radius, int samples, out float heightDifference)
        {
            float highest = float.MinValue;
            float lowest = float.MaxValue;

            Vector3 highestPoint = center;
            Vector3 lowestPoint = center;

            for (int i = 0; i < samples; i++)
            {
                float angle = i * 360f / samples;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 sample = center + dir * radius;

                float height = WorldGenerator.instance.GetHeight(sample.x, sample.z);

                if (height > highest)
                {
                    highest = height;
                    highestPoint = sample;
                }

                if (height < lowest)
                {
                    lowest = height;
                    lowestPoint = sample;
                }
            }

            heightDifference = highest - lowest;
            return lowestPoint - highestPoint;
        }
    }
}