using HarmonyLib;
using LocationManager;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace PungusSouls
{
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
    public static class BonfireSpawner
    {
        private static bool _spawned;

        private static void Postfix()
        {
            if (_spawned)
            {
                return;
            }

            _spawned = true;

            ZoneSystem.instance.StartCoroutine(SpawnBonfire());
        }
        private static bool StarterBonfireExists(Vector3 position)
        {
            BonfireController[] bonfires =
                Object.FindObjectsByType<BonfireController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (BonfireController bonfire in bonfires)
            {
                if (Vector3.Distance(
                        bonfire.transform.position,
                        position) < 10f)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerator SpawnBonfire()
        {
            BonfireManager.Load();
            Debug.Log("[Bonfire] Looking for Vegvisir");
            Vegvisir[] vegvisirs = null;

            while (Player.m_localPlayer == null)
            {
                yield return null;
            }

            yield return new WaitForSeconds(5f);

            while (vegvisirs == null || vegvisirs.Length == 0)
            {
                vegvisirs = Object.FindObjectsByType<Vegvisir>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

                yield return new WaitForSeconds(1f);
            }

            while (Player.m_localPlayer == null)
            {
                Debug.LogError("[Bonfire] Player is null");
                yield return new WaitForSeconds(1f);
            }

            Vegvisir vegvisir = vegvisirs
                .Where(v => v != null && v.transform != null)
                .OrderBy(v => Vector3.Distance(
                    v.transform.position,
                    Player.m_localPlayer.transform.position))
                .FirstOrDefault();

            if (vegvisir == null)
            {
                Debug.LogError("[Bonfire] No valid Vegvisir found");
                yield break;
            }

            Debug.Log($"[Bonfire] Vegvisir found at {vegvisir.transform.position}");

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

            Debug.Log($"[Bonfire] Final position: {bonfirePosition}");

            GameObject prefab = PungusSoulsPlugin.GetBonfirePrefab();

            if (prefab == null)
            {
                Debug.LogError("[Bonfire] GetBonfirePrefab returned null");
                yield break;
            }

            if (StarterBonfireExists(bonfirePosition))
            {
                Debug.Log(
                    "[Bonfire] Starter bonfire already exists");

                yield break;
            }

            GameObject spawned = Object.Instantiate(
                prefab,
                bonfirePosition,
                Quaternion.identity);

            spawned.name = "PS_Bonfire";
            GameObject znPrefab = ZNetScene.instance?.GetPrefab("PS_Bonfire");


            Debug.Log($"[Bonfire] ZNetScene prefab found = {znPrefab != null}");

            if (spawned.GetComponent<BonfireController>() == null)
            {
                spawned.AddComponent<BonfireController>();
            }

            Debug.Log("[Bonfire] Spawned successfully");
        }
    }
}