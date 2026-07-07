using System.Collections.Generic;
using UnityEngine;

namespace PungusSouls
{
    public static class BonfireManager
    {
        private const string StarterActivatedKey = "ps_starter_bonfire_activated";
        private static bool _starterActivated;
        private static readonly HashSet<Vector3> Bonfires = new();

        public static bool TravelMode;

        public static bool StarterActivated
        {
            get => _starterActivated;
            set
            {
                _starterActivated = value;
                PlayerPrefs.SetInt(
                    StarterActivatedKey,
                    value ? 1 : 0);
            }
        }

        public static void Load()
        {
            _starterActivated =
                PlayerPrefs.GetInt(
                    StarterActivatedKey,
                    0) == 1;
        }

        public static void RegisterBonfire(Vector3 position)
        {
            Bonfires.Add(position);

            Debug.Log(
                $"[Bonfire] Registered position {position}");

            Debug.Log(
                $"[Bonfire] Bonfire count = {Bonfires.Count}");
        }

        public static void UnregisterBonfire(Vector3 position)
        {
            Bonfires.Remove(position);

            Debug.Log(
                $"[Bonfire] Unregistered position {position}");

            Debug.Log(
                $"[Bonfire] Bonfire count = {Bonfires.Count}");
        }

        public static IReadOnlyCollection<Vector3> GetBonfires()
        {
            return Bonfires;
        }
    }
}