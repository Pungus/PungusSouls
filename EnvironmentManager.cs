using HarmonyLib;
using UnityEngine;

namespace PungusSouls
{
    public class EnvironmentManager
    {
        public static AudioClip firelinkClip;
        private static bool initialized;

        [HarmonyPatch(typeof(EnvMan), "Awake")]
        private static class EnvMan_Awake_Patch
        {
            static void Postfix(EnvMan __instance)
            {
                if (initialized)
                    return;

                initialized = true;

                if (firelinkClip == null)
                {
                    firelinkClip = PungusSoulsPlugin.assetBundle.LoadAsset<AudioClip>("FirelinkShrine");
                }

                EnvSetup newEnv = new EnvSetup
                {
                    m_name = "FirelinkShrine",
                    m_ambientLoop = firelinkClip
                };

                for (int i = 0; i < __instance.m_environments.Count; i++)
                {
                    var env = __instance.m_environments[i];

                    if (env.m_name == "Crypt")
                    {
                        newEnv.m_musicDay = env.m_musicDay;
                        newEnv.m_musicEvening = env.m_musicEvening;
                        newEnv.m_musicMorning = env.m_musicMorning;
                        newEnv.m_musicNight = env.m_musicNight;
                        break;
                    }
                }

                __instance.m_environments.Add(newEnv);
                __instance.InitializeEnvironment(newEnv);
            }
        }
    }
}