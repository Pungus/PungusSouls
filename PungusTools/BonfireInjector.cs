using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace PungusSouls
{
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
    public static class BonfireInjector
    {
        private static bool started;

        private static void Postfix()
        {
            if (started)
            {
                return;
            }

            started = true;

            ZoneSystem.instance.StartCoroutine(InjectLoop());
        }

        private static IEnumerator InjectLoop()
        {
            while (true)
            {
                InjectBonfires();

                yield return new WaitForSeconds(5f);
            }
        }

        private static void InjectBonfires()
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Transform transform in transforms)
            {
                if (transform.name != "PS_Bonfire")
                {
                    continue;
                }

                if (transform.GetComponent<BonfireController>() != null)
                {
                    continue;
                }

                transform.gameObject.AddComponent<BonfireController>();

                Debug.Log(
                    $"[Bonfire] Injected BonfireController into {transform.name} at {transform.position}");
            }
        }
    }
}