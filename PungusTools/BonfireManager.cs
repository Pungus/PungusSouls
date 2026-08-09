using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace PungusSouls
{
    public static class BonfireManager
    {
        private const string PinName = "Bonfire";
        private const string GlobalKeyPrefix = "ps_bonfire_";
        private const float DuplicateDistance = 2f;
        private const float PositionScale = 10f;

        private static readonly List<Vector3> BonfirePositions = new List<Vector3>();
        private static readonly List<Minimap.PinData> BonfirePins = new List<Minimap.PinData>();

        private static bool loaded;
        private static Sprite bonfirePinSprite;

        public static bool TravelMode;
        public static Vector3? CurrentBonfirePosition;

        public static void ResetForWorld()
        {
            loaded = false;
            TravelMode = false;
            CurrentBonfirePosition = null;
            BonfirePositions.Clear();
            BonfirePins.Clear();
        }

        public static void Load()
        {
            if (loaded)
                return;

            BonfirePositions.Clear();
            BonfirePins.Clear();

            if (ZoneSystem.instance == null)
                return;

            IEnumerable keys = GetGlobalKeys();

            if (keys != null)
            {
                foreach (object keyObject in keys)
                {
                    string key = keyObject as string;

                    if (string.IsNullOrEmpty(key))
                        continue;

                    if (!TryDecodeBonfireKey(key, out Vector3 position))
                        continue;

                    if (!HasBonfireNear(position, DuplicateDistance))
                        BonfirePositions.Add(position);
                }
            }

            loaded = true;
        }

        public static void RegisterBonfire(Vector3 position)
        {
            Load();

            Vector3 storedPosition = QuantizePosition(position);

            if (!HasBonfireNear(storedPosition, DuplicateDistance))
                BonfirePositions.Add(storedPosition);

            if (ZoneSystem.instance != null)
                ZoneSystem.instance.SetGlobalKey(EncodeBonfireKey(storedPosition));

            EnsureMapPin(storedPosition);
        }

        public static bool IsActivated(Vector3 position)
        {
            Load();
            return HasBonfireNear(QuantizePosition(position), DuplicateDistance);
        }

        public static void RefreshMapPins()
        {
            if (Minimap.instance == null)
                return;

            Load();

            for (int i = 0; i < BonfirePositions.Count; i++)
                EnsureMapPin(BonfirePositions[i]);
        }

        public static void EnsureMapPin(Vector3 position)
        {
            if (Minimap.instance == null)
                return;

            Vector3 storedPosition = QuantizePosition(position);

            if (HasExistingBonfireMapPin(storedPosition))
                return;

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
                return false;

            if (pin.m_name != PinName)
                return false;

            Load();

            for (int i = 0; i < BonfirePositions.Count; i++)
            {
                if (Utils.DistanceXZ(pin.m_pos, BonfirePositions[i]) < DuplicateDistance)
                    return true;
            }

            return false;
        }

        public static void ApplyBonfireIcon(Minimap.PinData pin)
        {
            if (pin == null)
                return;

            Sprite sprite = GetBonfirePinSprite();

            if (sprite == null)
                return;

            pin.m_icon = sprite;

            if (pin.m_iconElement != null)
                pin.m_iconElement.sprite = sprite;
        }

        public static void CancelTravel()
        {
            TravelMode = false;
            CurrentBonfirePosition = null;
        }

        private static IEnumerable GetGlobalKeys()
        {
            if (ZoneSystem.instance == null)
                return null;

            object keys = Traverse.Create(ZoneSystem.instance)
                .Field("m_globalKeys")
                .GetValue();

            return keys as IEnumerable;
        }

        private static bool HasBonfireNear(Vector3 position, float radius)
        {
            for (int i = 0; i < BonfirePositions.Count; i++)
            {
                if (Utils.DistanceXZ(BonfirePositions[i], position) < radius)
                    return true;
            }

            return false;
        }

        private static bool HasExistingBonfireMapPin(Vector3 position)
        {
            if (Minimap.instance == null)
                return false;

            List<Minimap.PinData> pins = Traverse.Create(Minimap.instance)
                .Field("m_pins")
                .GetValue<List<Minimap.PinData>>();

            if (pins == null)
                return false;

            for (int i = 0; i < pins.Count; i++)
            {
                Minimap.PinData pin = pins[i];

                if (pin == null)
                    continue;

                if (pin.m_name != PinName)
                    continue;

                if (Utils.DistanceXZ(pin.m_pos, position) >= DuplicateDistance)
                    continue;

                if (!BonfirePins.Contains(pin))
                    BonfirePins.Add(pin);

                ApplyBonfireIcon(pin);
                return true;
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
                return false;

            if (!key.StartsWith(GlobalKeyPrefix))
                return false;

            string payload = key.Substring(GlobalKeyPrefix.Length);
            string[] parts = payload.Split('_');

            if (parts.Length != 3)
                return false;

            if (!int.TryParse(parts[0], out int x))
                return false;

            if (!int.TryParse(parts[1], out int y))
                return false;

            if (!int.TryParse(parts[2], out int z))
                return false;

            position = new Vector3(
                x / PositionScale,
                y / PositionScale,
                z / PositionScale);

            return true;
        }

        private static Sprite GetBonfirePinSprite()
        {
            if (bonfirePinSprite != null)
                return bonfirePinSprite;

            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = null;
            string[] resourceNames = assembly.GetManifestResourceNames();

            for (int i = 0; i < resourceNames.Length; i++)
            {
                string name = resourceNames[i];

                if (name.EndsWith("bonfireicon.png", System.StringComparison.OrdinalIgnoreCase))
                {
                    resourceName = name;
                    break;
                }
            }

            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogWarning("[Bonfire] Could not find embedded resource ending with bonfireicon.png");
                return null;
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return null;

                byte[] data = new byte[stream.Length];
                int read = stream.Read(data, 0, data.Length);

                if (read <= 0)
                    return null;

                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                if (!texture.LoadImage(data))
                {
                    Object.Destroy(texture);
                    return null;
                }

                texture.name = "bonfireicon";

                bonfirePinSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);

                bonfirePinSprite.name = "bonfireicon";
                return bonfirePinSprite;
            }
        }
    }
}