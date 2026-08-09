using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace PungusSouls
{
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
    public static class BonfireInjector
    {
        private const bool DebugBonfireInjector = false;
        private const float InitialDelay = 3f;
        private const float ScanInterval = 2f;

        private static bool started;

        private static void Postfix()
        {
            if (started)
            {
                return;
            }

            if (ZoneSystem.instance == null)
            {
                return;
            }

            started = true;
            ZoneSystem.instance.StartCoroutine(InjectLoop());
        }

        private static IEnumerator InjectLoop()
        {
            while (Player.m_localPlayer == null)
            {
                yield return null;
            }

            yield return new WaitForSeconds(InitialDelay);

            while (true)
            {
                InjectBonfiresInLoadedScenes();
                yield return new WaitForSeconds(ScanInterval);
            }
        }

        private static void InjectBonfiresInLoadedScenes()
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            int injected = 0;
            int gated = 0;

            foreach (Transform transform in transforms)
            {
                if (transform == null)
                {
                    continue;
                }

                string cleanName =
                    transform.name.Replace("(Clone)", string.Empty).Trim();

                if (cleanName == "PS_Bonfire")
                {
                    if (transform.GetComponent<BonfireController>() == null)
                    {
                        transform.gameObject.AddComponent<BonfireController>();
                        injected++;

                        if (DebugBonfireInjector)
                        {
                            Debug.Log(
                                $"[BonfireInjector] Injected BonfireController into {transform.name} at {transform.position}");
                        }
                    }

                    GameObject gateRoot =
                        FindFirelinkGateRoot(transform);

                    if (gateRoot != null)
                    {
                        InstallFirelinkGate(gateRoot);
                        gated++;
                    }

                    continue;
                }

                if (cleanName == "FirelinkBonfireVegvisirGate")
                {
                    GameObject gateRoot =
                        transform.parent != null
                            ? transform.parent.gameObject
                            : transform.gameObject;

                    InstallFirelinkGate(gateRoot);
                    gated++;
                }
            }

            if (DebugBonfireInjector && injected > 0)
            {
                Debug.Log($"[BonfireInjector] Injected {injected} bonfire controllers");
            }

            if (DebugBonfireInjector && gated > 0)
            {
                Debug.Log($"[BonfireInjector] Installed {gated} firelink bonfire gates");
            }
        }

        private static GameObject FindFirelinkGateRoot(Transform bonfire)
        {
            if (bonfire == null)
            {
                return null;
            }

            if (HasFirelinkMarker(bonfire))
            {
                return bonfire.gameObject;
            }

            Transform current = bonfire.parent;

            while (current != null)
            {
                if (HasFirelinkMarker(current))
                {
                    return current.gameObject;
                }

                current = current.parent;
            }

            return null;
        }

        private static bool HasFirelinkMarker(Transform root)
        {
            if (root == null)
            {
                return false;
            }

            Transform[] children =
                root.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child == null)
                {
                    continue;
                }

                if (child.name == "FirelinkBonfireVegvisirGate")
                {
                    return true;
                }
            }

            return false;
        }

        private static void InstallFirelinkGate(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            BonfireVegvisirGate gate =
                root.GetComponent<BonfireVegvisirGate>();

            if (gate == null)
            {
                gate = root.AddComponent<BonfireVegvisirGate>();
            }

            gate.PlayerKey =
                BonfirePlayerKeys.FirelinkVegvisirRegistered;
        }
    }

    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
    public static class BonfireWorldStartPatch
    {
        private static void Postfix()
        {
            if (ZoneSystem.instance == null)
            {
                return;
            }

            ZoneSystem.instance.StartCoroutine(LoadBonfiresWhenReady());
        }

        private static IEnumerator LoadBonfiresWhenReady()
        {
            while (Player.m_localPlayer == null)
            {
                yield return null;
            }

            yield return new WaitForSeconds(2f);

            BonfireManager.ResetForWorld();
            BonfireManager.Load();
            BonfireManager.RefreshMapPins();
        }
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Awake))]
    public static class BonfireMinimapAwakePatch
    {
        private static void Postfix()
        {
            if (ZoneSystem.instance == null)
            {
                return;
            }

            ZoneSystem.instance.StartCoroutine(RefreshPinsWhenReady());
        }

        private static IEnumerator RefreshPinsWhenReady()
        {
            while (Player.m_localPlayer == null)
            {
                yield return null;
            }

            yield return new WaitForSeconds(2f);
            BonfireManager.RefreshMapPins();
        }
    }
}
