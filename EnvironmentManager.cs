using HarmonyLib;
using UnityEngine;

public class EnvironmentManager
{
    [HarmonyPatch(typeof(EnvMan), "Awake")]
    private static class EnvMan_Awake_Patches
    {
        public static void Postfix(EnvMan __instance)
        {
            __instance.m_environments.Add(newEnv);
            if (clearCloudsCopy == null)
            {
                foreach (EnvSetup environment in __instance.m_environments)
                {
                    if (environment.m_name == "Clear")
                    {
                        clearCloudsCopy = Object.Instantiate(environment.m_psystems[0]);
                        newEnv.m_psystems = new GameObject[1] { clearCloudsCopy };
                        newEnv.m_sunColorMorning = environment.m_sunColorDay;
                        newEnv.m_sunColorDay = environment.m_sunColorDay;
                        newEnv.m_sunColorEvening = environment.m_sunColorDay;
                        newEnv.m_sunColorEvening = environment.m_sunColorDay;
                        newEnv.m_sunColorNight = environment.m_sunColorDay;
                        newEnv.m_lightIntensityDay = environment.m_lightIntensityDay;
                        newEnv.m_lightIntensityNight = environment.m_lightIntensityDay;
                        newEnv.m_sunAngle = environment.m_sunAngle;
                        newEnv.m_ambColorDay = environment.m_ambColorDay;
                        newEnv.m_ambColorNight = environment.m_ambColorDay;
                    }
                }
            }
            foreach (EnvSetup environment2 in __instance.m_environments)
            {
                if (environment2.m_name == "Crypt")
                {
                    newEnv.m_ambientLoop = environment2.m_ambientLoop;
                    newEnv.m_musicDay = environment2.m_musicDay;
                    newEnv.m_musicEvening = environment2.m_musicEvening;
                    newEnv.m_musicMorning = environment2.m_musicMorning;
                    newEnv.m_musicNight = environment2.m_musicNight;
                    newEnv.m_ambientLoop = environment2.m_ambientLoop;
                }
            }
            __instance.InitializeEnvironment(newEnv);
            InitEnvSetup();
        }
    }

    public static EnvSetup newEnv = new EnvSetup();

    public static GameObject clearCloudsCopy;

    public static AudioClip musicCopy;

    public static void InitEnvSetup()
    {
        newEnv.m_name = "FirelinkShrine";
        newEnv.m_fogDensityMorning = 0f;
        newEnv.m_fogDensityDay = 0f;
        newEnv.m_fogDensityEvening = 0f;
        newEnv.m_fogDensityNight = 0f;
    }
}