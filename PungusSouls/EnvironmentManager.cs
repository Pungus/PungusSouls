using HarmonyLib;
using UnityEngine;

public static class EnvironmentManager
{
    public static EnvSetup FirelinkEnv;

    [HarmonyPatch(typeof(EnvMan), "Awake")]
    private static class EnvMan_Awake_Patch
    {
        private static void Postfix(EnvMan __instance)
        {
            foreach (var env in __instance.m_environments)
            {
                if (env.m_name == "FirelinkShrine")
                    return;
            }

            EnvSetup clearEnv = null;
            EnvSetup cryptEnv = null;

            foreach (var env in __instance.m_environments)
            {
                if (env.m_name == "Clear")
                    clearEnv = env;

                if (env.m_name == "Crypt")
                    cryptEnv = env;
            }

            if (clearEnv == null)
            {
                Debug.LogError("[Firelink] Could not find Clear environment");
                return;
            }

            FirelinkEnv = clearEnv.Clone();

            //
            // Basic identity
            //
            FirelinkEnv.m_name = "FirelinkShrine";
            FirelinkEnv.m_default = false;

            //
            // Fog
            //
            FirelinkEnv.m_fogDensityMorning = 0f;
            FirelinkEnv.m_fogDensityDay = 0f;
            FirelinkEnv.m_fogDensityEvening = 0f;
            FirelinkEnv.m_fogDensityNight = 0f;

            //
            // Lighting
            //
            FirelinkEnv.m_sunColorMorning = clearEnv.m_sunColorDay;
            FirelinkEnv.m_sunColorDay = clearEnv.m_sunColorDay;
            FirelinkEnv.m_sunColorEvening = clearEnv.m_sunColorDay;
            FirelinkEnv.m_sunColorNight = clearEnv.m_sunColorDay;

            FirelinkEnv.m_ambColorDay = clearEnv.m_ambColorDay;
            FirelinkEnv.m_ambColorNight = clearEnv.m_ambColorDay;

            FirelinkEnv.m_lightIntensityDay = clearEnv.m_lightIntensityDay;
            FirelinkEnv.m_lightIntensityNight = clearEnv.m_lightIntensityDay;

            FirelinkEnv.m_sunAngle = clearEnv.m_sunAngle;

            //
            // Optional custom particle system
            //
            if (clearEnv.m_psystems != null &&
                clearEnv.m_psystems.Length > 0 &&
                clearEnv.m_psystems[0] != null)
            {
                var cloudCopy = Object.Instantiate(clearEnv.m_psystems[0]);
                cloudCopy.SetActive(false);

                FirelinkEnv.m_psystems = new[]
                {
                    cloudCopy
                };
            }

            //
            // Copy music from Crypt
            //
            if (cryptEnv != null)
            {
                FirelinkEnv.m_ambientLoop = cryptEnv.m_ambientLoop;
                FirelinkEnv.m_musicMorning = cryptEnv.m_musicMorning;
                FirelinkEnv.m_musicDay = cryptEnv.m_musicDay;
                FirelinkEnv.m_musicEvening = cryptEnv.m_musicEvening;
                FirelinkEnv.m_musicNight = cryptEnv.m_musicNight;
            }

            __instance.AppendEnvironment(FirelinkEnv);
            foreach (var env in __instance.m_environments)
            {
                if (env.m_name == "FirelinkShrine")
                {
                    Debug.Log("[Firelink] Successfully registered");
                    break;
                }
            }

            Debug.Log("[Firelink] Environment registered");
        }
    }
}