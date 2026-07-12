using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace PungusSouls
{
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    public static class RuneStoneTextOverridePatch
    {
        private sealed class RuneStoneTextOverride
        {
            public string Name;
            public string Topic;
            public string Label;
            public string Text;

            public RuneStoneTextOverride(string name, string topic, string label, string text)
            {
                Name = name;
                Topic = topic;
                Label = label;
                Text = text;
            }
        }

        private static readonly Dictionary<string, RuneStoneTextOverride> Overrides =
            new Dictionary<string, RuneStoneTextOverride>
            {
                {
                    "RuneStone_Andre1",
                    new RuneStoneTextOverride(
                        "Blacksmith Forge",
                        "Andre",
                        "Talk",
                        "Bring me the Dull Ember, and I will teach the forge to remember its first flame."
                    )
                },
                {
                    "RuneStone_Andre_LargeEmber",
                    new RuneStoneTextOverride(
                        "Blacksmith Forge",
                        "Andre",
                        "Talk",
                        "A larger flame wakes stronger metal. Bring me the Large Ember when you find it."
                    )
                },
                {
                    "RuneStone_Andre_DivineEmber",
                    new RuneStoneTextOverride(
                        "Blacksmith Forge",
                        "Andre",
                        "Talk",
                        "Divine fire is not kind to ordinary steel. Bring the Divine Ember only when you are ready."
                    )
                }
            };

        private static void Postfix(ZNetScene __instance)
        {
            if (__instance == null || __instance.m_prefabs == null)
                return;

            foreach (GameObject prefab in __instance.m_prefabs)
            {
                if (prefab == null)
                    continue;

                string prefabName = prefab.name.Replace("(Clone)", "");

                if (!prefabName.StartsWith("RuneStone_"))
                    continue;

                if (!Overrides.TryGetValue(prefabName, out RuneStoneTextOverride textOverride))
                {
                    Debug.Log("[RuneStoneTextOverridePatch] Found RuneStone prefab without override: " + prefabName);
                    continue;
                }

                Apply(prefab, prefabName, textOverride);
            }
        }

        private static void Apply(GameObject prefab, string prefabName, RuneStoneTextOverride textOverride)
        {
            RuneStone runeStone = prefab.GetComponent<RuneStone>();

            if (runeStone == null)
                runeStone = prefab.GetComponentInChildren<RuneStone>(true);

            if (runeStone == null)
            {
                Debug.Log("[RuneStoneTextOverridePatch] No RuneStone component found on " + prefabName);
                return;
            }

            runeStone.m_name = textOverride.Name;
            runeStone.m_topic = textOverride.Topic;
            runeStone.m_label = textOverride.Label;
            runeStone.m_text = textOverride.Text;

            Debug.Log("[RuneStoneTextOverridePatch] Applied text override to " + prefabName);
        }
    }
}