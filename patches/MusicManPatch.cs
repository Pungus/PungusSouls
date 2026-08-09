using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PungusSouls
{
    public static class MusicManPatch
    {
        private const string FirelinkMusicName = "FirelinkShrine";
        private const string CustomSourceName = "PungusSouls_FirelinkShrine_Source";
        private const bool DebugMusicPatch = false;
        private const float DefaultMusicVolume = 0.65f;

        private static readonly HashSet<string> BlockedMusicNames = new HashSet<string>
        {
            "meadows",
            "location_meadows",
            "location_forest"
        };

        private static AudioSource firelinkSource;
        private static MusicMan activeMusicMan;
        private static bool firelinkActive;

        public static void AddFirelinkShrineMusic(MusicMan musicMan)
        {
            activeMusicMan = musicMan;
        }

        public static void StartFirelinkShrineMusic(MusicMan musicMan)
        {
            if (musicMan == null)
            {
                return;
            }

            firelinkActive = true;
            activeMusicMan = musicMan;
            StopMusicManSources(musicMan);
            PlayFirelinkSource(musicMan);
        }

        public static void StopFirelinkShrineMusic()
        {
            firelinkActive = false;
            StopFirelinkSource();
        }

        [HarmonyPatch(typeof(MusicMan), "Awake")]
        public static class MusicManAwakePatch
        {
            private static void Postfix(MusicMan __instance)
            {
                activeMusicMan = __instance;
            }
        }

        [HarmonyPatch(typeof(MusicMan), "LocationMusic")]
        public static class MusicManLocationMusicPatch
        {
            private static bool Prefix(MusicMan __instance, string name)
            {
                if (__instance == null)
                {
                    return true;
                }

                if (name == FirelinkMusicName)
                {
                    firelinkActive = true;
                    activeMusicMan = __instance;
                    StopMusicManSources(__instance);
                    PlayFirelinkSource(__instance);

                    if (DebugMusicPatch)
                    {
                        Debug.Log($"[MusicManPatch] Playing '{FirelinkMusicName}' directly and skipping MusicMan lookup");
                    }

                    return false;
                }

                if (firelinkActive && BlockedMusicNames.Contains(name))
                {
                    activeMusicMan = __instance;
                    StopMusicManSources(__instance);
                    PlayFirelinkSource(__instance);

                    if (DebugMusicPatch)
                    {
                        Debug.Log($"[MusicManPatch] Blocked competing music '{name}' while Firelink is active");
                    }

                    return false;
                }

                if (firelinkActive)
                {
                    StopFirelinkShrineMusic();
                }

                return true;
            }
        }

        [HarmonyPatch]
        public static class MusicManStartMusicPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                MethodInfo[] methods = typeof(MusicMan).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (MethodInfo method in methods)
                {
                    if (method.Name == "StartMusic")
                    {
                        yield return method;
                    }
                }
            }

            private static bool Prefix(MusicMan __instance, object[] __args)
            {
                if (!firelinkActive || __instance == null)
                {
                    return true;
                }

                string musicName = GetMusicNameFromArgs(__args);
                if (string.IsNullOrEmpty(musicName))
                {
                    return true;
                }

                if (musicName == FirelinkMusicName || BlockedMusicNames.Contains(musicName))
                {
                    activeMusicMan = __instance;
                    StopMusicManSources(__instance);
                    PlayFirelinkSource(__instance);

                    if (DebugMusicPatch)
                    {
                        Debug.Log($"[MusicManPatch] Blocked StartMusic '{musicName}' while Firelink is active");
                    }

                    return false;
                }

                StopFirelinkShrineMusic();
                return true;
            }
        }

        [HarmonyPatch(typeof(MusicMan), "Update")]
        public static class MusicManUpdatePatch
        {
            private static void Postfix(MusicMan __instance)
            {
                if (!firelinkActive || __instance == null)
                {
                    return;
                }

                AudioClip clip = PungusSoulsPlugin.LocationClip;
                if (clip == null)
                {
                    return;
                }

                if (firelinkSource == null || firelinkSource.clip != clip || !firelinkSource.isPlaying)
                {
                    PlayFirelinkSource(__instance);
                    return;
                }

                firelinkSource.volume = GetFirelinkVolume();
            }
        }

        private static void PlayFirelinkSource(MusicMan musicMan)
        {
            AudioClip clip = PungusSoulsPlugin.LocationClip;
            if (musicMan == null || clip == null)
            {
                return;
            }

            AudioSource source = GetOrCreateFirelinkSource(musicMan);
            if (source == null)
            {
                return;
            }

            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.loop = true;
            source.volume = GetFirelinkVolume();

            if (source.clip == clip && source.isPlaying)
            {
                return;
            }

            source.Stop();
            source.clip = clip;
            source.Play();

            if (DebugMusicPatch)
            {
                Debug.Log($"[MusicManPatch] Started Firelink source clip='{clip.name}' volume={source.volume:0.00}");
            }
        }

        private static AudioSource GetOrCreateFirelinkSource(MusicMan musicMan)
        {
            if (firelinkSource != null)
            {
                return firelinkSource;
            }

            Transform existing = musicMan.transform.Find(CustomSourceName);
            if (existing != null)
            {
                firelinkSource = existing.GetComponent<AudioSource>();
                if (firelinkSource != null)
                {
                    return firelinkSource;
                }
            }

            GameObject sourceObject = new GameObject(CustomSourceName);
            sourceObject.transform.SetParent(musicMan.transform, false);
            firelinkSource = sourceObject.AddComponent<AudioSource>();
            firelinkSource.name = CustomSourceName;
            firelinkSource.spatialBlend = 0f;
            firelinkSource.playOnAwake = false;
            firelinkSource.loop = true;
            firelinkSource.volume = GetFirelinkVolume();
            return firelinkSource;
        }

        private static void StopFirelinkSource()
        {
            if (firelinkSource == null)
            {
                return;
            }

            if (firelinkSource.isPlaying)
            {
                firelinkSource.Stop();
            }

            firelinkSource.clip = null;
        }

        private static void StopMusicManSources(MusicMan musicMan)
        {
            if (musicMan == null)
            {
                return;
            }

            AudioSource[] sources = musicMan.GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource source in sources)
            {
                if (source == null || source == firelinkSource || !source.isPlaying)
                {
                    continue;
                }

                source.Stop();
            }
        }

        private static float GetFirelinkVolume()
        {
            FieldInfo volumeField = typeof(PungusSoulsPlugin).GetField(
                "FirelinkShrineVolume",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (volumeField != null && volumeField.GetValue(null) is float volume)
            {
                return Mathf.Clamp01(volume);
            }

            return DefaultMusicVolume;
        }

        private static string GetMusicNameFromArgs(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] == null)
            {
                return null;
            }

            if (args[0] is string stringName)
            {
                return stringName;
            }

            return GetStringField(args[0], "m_name");
        }

        private static string GetStringField(object target, string fieldName)
        {
            if (target == null)
            {
                return null;
            }

            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return field?.GetValue(target) as string;
        }
    }
}
