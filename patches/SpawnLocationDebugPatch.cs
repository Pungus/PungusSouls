using HarmonyLib;
using System;
using UnityEngine;

namespace PungusSouls
{
    [HarmonyPatch(typeof(ZoneSystem), "SpawnLocation")]
    public static class SpawnLocationDebugPatch
    {
        [HarmonyPrefix]
        private static void Prefix(
            ZoneSystem.ZoneLocation location,
            int seed,
            Vector3 pos,
            Quaternion rot,
            ZoneSystem.SpawnMode mode)
        {
            string name = location == null
                ? "null"
                : location.m_name + " / " + location.m_prefabName;

            Debug.Log(
                "[PungusSouls LocationDebug] Spawning location " +
                name +
                " at " +
                pos +
                " mode=" +
                mode);
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(
            Exception __exception,
            ZoneSystem.ZoneLocation location,
            Vector3 pos,
            ZoneSystem.SpawnMode mode)
        {
            if (__exception == null)
                return null;

            string name = location == null
                ? "null"
                : location.m_name + " / " + location.m_prefabName;

            Debug.LogError(
                "[PungusSouls LocationDebug] Failed spawning location " +
                name +
                " at " +
                pos +
                " mode=" +
                mode +
                "\n" +
                __exception);

            return __exception;
        }
    }
}