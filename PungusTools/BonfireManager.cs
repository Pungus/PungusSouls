using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace PungusSouls
{
    public static class BonfireManager
    {
        private const string PinName = "Bonfire";
        private const float DuplicateDistance = 2f;

        private static readonly List<Vector3> BonfirePositions = new();
        private static readonly List<Minimap.PinData> BonfirePins = new();

        private static string loadedWorldName;
        private static Sprite bonfirePinSprite;
        public static Vector3? CurrentBonfirePosition;
        public static bool TravelMode;

        public static void Load()
        {
            string worldName = GetWorldName();

            if (string.IsNullOrEmpty(worldName))
            {
                return;
            }

            if (loadedWorldName == worldName)
            {
                EnsureMapPins();
                return;
            }

            BonfirePositions.Clear();
            BonfirePins.Clear();

            string path = GetSavePath(worldName);

            if (File.Exists(path))
            {
                try
                {
                    foreach (string line in File.ReadAllLines(path))
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        string[] parts = line.Split(',');

                        if (parts.Length != 3)
                        {
                            continue;
                        }

                        if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
                        {
                            continue;
                        }

                        if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                        {
                            continue;
                        }

                        if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                        {
                            continue;
                        }

                        Vector3 position = new Vector3(x, y, z);

                        if (!HasBonfireNear(position, DuplicateDistance))
                        {
                            BonfirePositions.Add(position);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("[Bonfire] Failed to load bonfire save data");
                    Debug.LogError(ex);
                }
            }

            loadedWorldName = worldName;

            Debug.Log($"[Bonfire] Loaded {BonfirePositions.Count} saved bonfires for world {worldName}");

            EnsureMapPins();
        }

        public static void Save()
        {
            string worldName = GetWorldName();

            if (string.IsNullOrEmpty(worldName))
            {
                return;
            }

            try
            {
                string folder = GetSaveFolder();

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                List<string> lines = new List<string>();

                foreach (Vector3 position in BonfirePositions)
                {
                    lines.Add(
                        position.x.ToString(CultureInfo.InvariantCulture) + "," +
                        position.y.ToString(CultureInfo.InvariantCulture) + "," +
                        position.z.ToString(CultureInfo.InvariantCulture));
                }

                File.WriteAllLines(GetSavePath(worldName), lines);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Bonfire] Failed to save bonfire data");
                Debug.LogError(ex);
            }
        }

        public static void RegisterBonfire(Vector3 position)
        {
            Load();

            if (!HasBonfireNear(position, DuplicateDistance))
            {
                BonfirePositions.Add(position);
                Save();
            }

            EnsureMapPin(position);

            Debug.Log($"[Bonfire] Registered bonfire position {position}");
            Debug.Log($"[Bonfire] Bonfire position count = {BonfirePositions.Count}");
        }

        public static bool IsActivated(Vector3 position)
        {
            Load();

            return HasBonfireNear(position, DuplicateDistance);
        }

        public static void EnsureMapPins()
        {
            if (Minimap.instance == null)
            {
                return;
            }

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

            if (HasExistingBonfireMapPin(position))
            {
                return;
            }

            Minimap.PinData pin = Minimap.instance.AddPin(
                position,
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

            List<Minimap.PinData> pins =
                Traverse.Create(Minimap.instance)
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

        private static Sprite GetBonfirePinSprite()
        {
            if (bonfirePinSprite != null)
            {
                return bonfirePinSprite;
            }

            bonfirePinSprite =
                AgentNpcIconRegistry.LoadSpriteFromEmbeddedResource(
                    System.Reflection.Assembly.GetExecutingAssembly(),
                    "PungusSouls.assets.icons.bonfireicon.png");

            if (bonfirePinSprite == null)
            {
                Debug.LogError("[Bonfire] Failed to load embedded bonfire pin sprite");
            }

            return bonfirePinSprite;
        }

        private static string GetWorldName()
        {
            if (ZNet.World == null)
            {
                return string.Empty;
            }

            return MakeSafeFileName(ZNet.World.m_name);
        }

        private static string GetSaveFolder()
        {
            return Path.Combine(
                Paths.ConfigPath,
                "PungusSouls",
                "Bonfires");
        }

        private static string GetSavePath(string worldName)
        {
            return Path.Combine(
                GetSaveFolder(),
                worldName + ".txt");
        }

        private static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unknown_world";
            }

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidChar, '_');
            }

            return value.Trim();
        }
    }
}