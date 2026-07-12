using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PungusSouls
{
    public static class BonfireManager
    {
        private const string PinName = "Bonfire";
        private const string GlobalKeyPrefix = "ps_bonfire_";
        private const float DuplicateDistance = 2f;
        private const float PositionScale = 10f;

        private static readonly List<Vector3> BonfirePositions = new();
        private static readonly List<Minimap.PinData> BonfirePins = new();

        private static bool loaded;
        private static Sprite bonfirePinSprite;

        public static bool TravelMode;
        public static Vector3? CurrentBonfirePosition;

        public static void Load()
        {
            if (loaded)
            {
                return;
            }

            BonfirePositions.Clear();
            BonfirePins.Clear();

            if (ZoneSystem.instance == null)
            {
                return;
            }

            IEnumerable keys = GetGlobalKeys();

            if (keys != null)
            {
                foreach (object keyObject in keys)
                {
                    if (keyObject is not string key)
                    {
                        continue;
                    }

                    if (!TryDecodeBonfireKey(key, out Vector3 position))
                    {
                        continue;
                    }

                    if (!HasBonfireNear(position, DuplicateDistance))
                    {
                        BonfirePositions.Add(position);
                    }
                }
            }

            loaded = true;
            Debug.Log($"[Bonfire] Loaded {BonfirePositions.Count} bonfires from world global keys");
        }

        public static void RegisterBonfire(Vector3 position)
        {
            Load();

            Vector3 storedPosition = QuantizePosition(position);

            if (!HasBonfireNear(storedPosition, DuplicateDistance))
            {
                BonfirePositions.Add(storedPosition);
            }

            if (ZoneSystem.instance != null)
            {
                ZoneSystem.instance.SetGlobalKey(EncodeBonfireKey(storedPosition));
            }

            EnsureMapPin(storedPosition);

            Debug.Log($"[Bonfire] Registered bonfire position {storedPosition}");
            Debug.Log($"[Bonfire] Bonfire count = {BonfirePositions.Count}");
        }

        public static bool IsActivated(Vector3 position)
        {
            Load();
            return HasBonfireNear(position, DuplicateDistance);
        }

        public static void RefreshMapPins()
        {
            if (Minimap.instance == null)
            {
                return;
            }

            Load();

            foreach (Vector3 position in BonfirePositions)
            {
                EnsureMapPin(position);
            }
        }

        public static void EnsureMapPin(Vector3 position)
        {
            if (Minimap.instance == null)
            {
                return;
            }

            Vector3 storedPosition = QuantizePosition(position);

            if (HasExistingBonfireMapPin(storedPosition))
            {
                return;
            }

            Minimap.PinData pin = Minimap.instance.AddPin(
                storedPosition,
                Minimap.PinType.Icon3,
                PinName,
                false,
                false);

            BonfirePins.Add(pin);
            ApplyBonfireIcon(pin);
        }

        public static bool IsBonfirePin(Minimap.PinData pin)
        {
            if (pin == null)
            {
                return false;
            }

            if (pin.m_name != PinName)
            {
                return false;
            }

            Load();

            foreach (Vector3 position in BonfirePositions)
            {
                if (Utils.DistanceXZ(pin.m_pos, position) < DuplicateDistance)
                {
                    return true;
                }
            }

            return false;
        }

        public static void ApplyBonfireIcon(Minimap.PinData pin)
        {
            if (pin == null)
            {
                return;
            }

            Sprite sprite = GetBonfirePinSprite();

            if (sprite == null)
            {
                return;
            }

            pin.m_icon = sprite;

            if (pin.m_iconElement != null)
            {
                pin.m_iconElement.sprite = sprite;
            }
        }

        private static IEnumerable GetGlobalKeys()
        {
            object keys = Traverse.Create(ZoneSystem.instance)
                .Field("m_globalKeys")
                .GetValue();

            return keys as IEnumerable;
        }

        private static bool HasBonfireNear(Vector3 position, float radius)
        {
            foreach (Vector3 existing in BonfirePositions)
            {
                if (Utils.DistanceXZ(existing, position) < radius)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasExistingBonfireMapPin(Vector3 position)
        {
            if (Minimap.instance == null)
            {
                return false;
            }

            List<Minimap.PinData> pins = Traverse.Create(Minimap.instance)
                .Field("m_pins")
                .GetValue<List<Minimap.PinData>>();

            if (pins == null)
            {
                return false;
            }

            foreach (Minimap.PinData pin in pins)
            {
                if (pin == null)
                {
                    continue;
                }

                if (pin.m_name != PinName)
                {
                    continue;
                }

                if (Utils.DistanceXZ(pin.m_pos, position) < DuplicateDistance)
                {
                    if (!BonfirePins.Contains(pin))
                    {
                        BonfirePins.Add(pin);
                    }

                    ApplyBonfireIcon(pin);
                    return true;
                }
            }

            return false;
        }

        private static Vector3 QuantizePosition(Vector3 position)
        {
            return new Vector3(
                Mathf.Round(position.x * PositionScale) / PositionScale,
                Mathf.Round(position.y * PositionScale) / PositionScale,
                Mathf.Round(position.z * PositionScale) / PositionScale);
        }

        private static string EncodeBonfireKey(Vector3 position)
        {
            int x = Mathf.RoundToInt(position.x * PositionScale);
            int y = Mathf.RoundToInt(position.y * PositionScale);
            int z = Mathf.RoundToInt(position.z * PositionScale);

            return GlobalKeyPrefix + x + "_" + y + "_" + z;
        }

        private static bool TryDecodeBonfireKey(string key, out Vector3 position)
        {
            position = Vector3.zero;

            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            if (!key.StartsWith(GlobalKeyPrefix))
            {
                return false;
            }

            string payload = key.Substring(GlobalKeyPrefix.Length);
            string[] parts = payload.Split('_');

            if (parts.Length != 3)
            {
                return false;
            }

            if (!int.TryParse(parts[0], out int x))
            {
                return false;
            }

            if (!int.TryParse(parts[1], out int y))
            {
                return false;
            }

            if (!int.TryParse(parts[2], out int z))
            {
                return false;
            }

            position = new Vector3(
                x / PositionScale,
                y / PositionScale,
                z / PositionScale);

            return true;
        }
        private static Sprite GetBonfirePinSprite()
        {
            if (bonfirePinSprite != null)
            {
                return bonfirePinSprite;
            }

            System.Reflection.Assembly assembly =
                System.Reflection.Assembly.GetExecutingAssembly();

            string resourceName = null;

            foreach (string name in assembly.GetManifestResourceNames())
            {
                Debug.Log("[PungusSouls] Embedded resource: " + name);

                if (name.EndsWith("bonfireicon.png", System.StringComparison.OrdinalIgnoreCase))
                {
                    resourceName = name;
                }
            }

            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[Bonfire] Could not find embedded resource ending with bonfireicon.png");
                return null;
            }

            bonfirePinSprite =
                AgentNpcIconRegistry.LoadSpriteFromEmbeddedResource(
                    assembly,
                    resourceName);

            if (bonfirePinSprite == null)
            {
                Debug.LogError("[Bonfire] Failed to load embedded bonfire icon from " + resourceName);
            }
            else
            {
                Debug.Log("[Bonfire] Loaded embedded bonfire icon from " + resourceName);
            }

            return bonfirePinSprite;
        }
    }
}
