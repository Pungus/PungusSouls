using HarmonyLib;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace PungusSouls
{
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
    public static class BonfireSpawner
    {
        private static bool _started;
        private static Vector3? _starterBonfirePosition;

/*        private static void Postfix()
        {
            if (_started)
            {
                return;
            }

            _started = true;

            ZoneSystem.instance.StartCoroutine(StarterBonfireLoop());
        }
*/
        private static IEnumerator StarterBonfireLoop()
        {
            while (Player.m_localPlayer == null)
            {
                yield return null;
            }
            yield return new WaitForSeconds(5f);

            BonfireManager.Load();
            BonfireManager.RefreshMapPins();


            while (true)
            {
                yield return EnsureStarterBonfire();

                yield return new WaitForSeconds(10f);
            }
        }

        private static IEnumerator EnsureStarterBonfire()
        {
            while (Player.m_localPlayer == null)
            {
                yield return null;
            }

            if (_starterBonfirePosition.HasValue)
            {
                Vector3 knownPosition = _starterBonfirePosition.Value;

                if (Vector3.Distance(
                        Player.m_localPlayer.transform.position,
                        knownPosition) < 150f &&
                    !StarterBonfireExists(knownPosition))
                {
                    SpawnAt(knownPosition);
                }

                yield break;
            }

            Vegvisir[] vegvisirs =
                Object.FindObjectsByType<Vegvisir>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (vegvisirs == null || vegvisirs.Length == 0)
            {
                yield break;
            }

            Vegvisir vegvisir = vegvisirs
                .Where(v => v != null)
                .OrderBy(v =>
                    Vector3.Distance(
                        v.transform.position,
                        Player.m_localPlayer.transform.position))
                .FirstOrDefault();

            if (vegvisir == null)
            {
                yield break;
            }

            Vector3 bonfirePosition =
                vegvisir.transform.position
                - vegvisir.transform.right * 1f
                - vegvisir.transform.forward * 2f;

            if (Physics.Raycast(
                    bonfirePosition + Vector3.up * 2f,
                    Vector3.down,
                    out RaycastHit hit,
                    10f))
            {
                bonfirePosition = hit.point;
            }

            _starterBonfirePosition = bonfirePosition;

            Debug.Log($"[Bonfire] Starter position resolved: {bonfirePosition}");

            if (StarterBonfireExists(bonfirePosition))
            {
                Debug.Log("[Bonfire] Starter bonfire already exists");
                yield break;
            }

            SpawnAt(bonfirePosition);
        }

        private static bool StarterBonfireExists(Vector3 position)
        {
            BonfireController[] bonfires =
                Object.FindObjectsByType<BonfireController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (BonfireController bonfire in bonfires)
            {
                if (bonfire == null)
                {
                    continue;
                }

                if (Vector3.Distance(
                        bonfire.transform.position,
                        position) < 10f)
                {
                    return true;
                }
            }

            return false;
        }


        private static void SpawnAt(Vector3 position)
        {
            GameObject prefab =
                PungusSoulsPlugin.GetBonfirePrefab();

            if (prefab == null)
            {
                Debug.LogError("[Bonfire] GetBonfirePrefab returned null");
                return;
            }

            if (StarterBonfireExists(position))
            {
                Debug.Log("[Bonfire] Starter bonfire already exists");
                return;
            }

            GameObject spawned = Object.Instantiate(
                prefab,
                position,
                Quaternion.identity);

            spawned.name = "PS_Bonfire";

            if (spawned.GetComponent<BonfireController>() == null)
            {
                spawned.AddComponent<BonfireController>();
            }

            GameObject znPrefab =
                ZNetScene.instance?.GetPrefab("PS_Bonfire");

            Debug.Log($"[Bonfire] ZNetScene prefab found = {znPrefab != null}");
            Debug.Log($"[Bonfire] Spawned starter bonfire at {position}");
        }

    }
}