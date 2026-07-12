using System.Collections;
using System.Reflection;
using UnityEngine;

namespace PungusSouls
{
    public static class MusicManPatch
    {
        public static void AddFirelinkShrineMusic(MusicMan musicMan)
        {
            if (musicMan == null)
            {

            }

            if (PungusSoulsPlugin.LocationClip == null)
            {

            }

            AddMusic(musicMan, "FirelinkShrine", PungusSoulsPlugin.LocationClip);
        }

        [HarmonyLib.HarmonyPatch(typeof(MusicMan), "Awake")]
        public static class MusicManAwakePatch
        {
            private static void Postfix(MusicMan __instance)
            {
                AddFirelinkShrineMusic(__instance);
            }
        }

        private static void AddMusic(MusicMan musicMan, string musicName, AudioClip clip)
        {
            FieldInfo musicField = typeof(MusicMan).GetField("m_music", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            IList musicList = musicField.GetValue(musicMan) as IList;


            object existing = null;
            object template = null;

            foreach (object music in musicList)
            {
                string name = GetStringField(music, "m_name");

                if (name == musicName)
                {
                    existing = music;
                }

                if (name == "location_forest")
                {
                    template = music;
                }
            }

            if (template == null)
            {
                template = musicList[0];
            }

            object target = existing ?? CloneMusic(template);

            SetField(target, "m_name", musicName);
            SetField(target, "m_clips", new[] { clip });
            SetFieldIfExists(target, "m_loop", true);
            SetFieldIfExists(target, "m_resume", false);
            SetFieldIfExists(target, "m_alwaysFadeout", true);
            SetFieldIfExists(target, "m_interruptible", true);

            if (existing == null)
            {
                musicList.Add(target);

            }

        }

        private static object CloneMusic(object source)
        {
            MethodInfo memberwiseClone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            return memberwiseClone.Invoke(source, null);
        }

        private static string GetStringField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field?.GetValue(target) as string;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        private static void SetFieldIfExists(object target, string fieldName, object value)
        {
            SetField(target, fieldName, value);
        }
    }
}