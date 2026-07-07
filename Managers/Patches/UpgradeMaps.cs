using System;
using System.Collections.Generic;
using UnityEngine;

namespace PungusSouls
{
    public sealed class UpgradeTier
    {
        public int StartLevel;
        public int EndLevel;

        public int StationLevel;

        public float Multiplier = 1f;

        public WeaponUpgradeRequirementLevel[] Requirements =
            Array.Empty<WeaponUpgradeRequirementLevel>();
    }

    public sealed class UpgradeMap
    {
        public string Name;

        public readonly List<UpgradeTier> Tiers =
            new List<UpgradeTier>();
    }

    public static class UpgradeMapRegistry
    {
        private static readonly Dictionary<string, UpgradeMap> Maps =
            new Dictionary<string, UpgradeMap>(
                StringComparer.OrdinalIgnoreCase);

        public static void Register(UpgradeMap map)
        {
            if (map == null)
            {
                Debug.LogWarning("[UpgradeMap] Null map registration");
                return;
            }

            if (string.IsNullOrWhiteSpace(map.Name))
            {
                Debug.LogWarning("[UpgradeMap] Map has no name");
                return;
            }

            Debug.Log($"[UpgradeMap] Registering {map.Name}");

            Maps[map.Name] = map;
        }

        public static UpgradeMap Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            Maps.TryGetValue(name, out UpgradeMap map);

            return map;
        }
    }

    public static class UpgradeMapExtensions
    {
        public static T ApplyUpgradeMap<T>(
            this T item,
            string mapName) where T : class
        {
            Debug.Log($"[UpgradeMap] Applying {mapName}");
            UpgradeMap map =
                UpgradeMapRegistry.Get(mapName);

            if (map == null)
            {
                Debug.LogWarning(
                    $"[UpgradeMap] Map '{mapName}' was not found.");

                return item;
            }

            foreach (UpgradeTier tier in map.Tiers)
            {
                ApplyTier(item, tier);
            }

            return item;
        }

        private static void ApplyTier<T>(
            T item,
            UpgradeTier tier) where T : class
        {
            if (tier == null)
                return;

            for (int level = tier.StartLevel;
                 level <= tier.EndLevel;
                 level++)
            {
                int levelOffset =
                    level - tier.StartLevel;

                WeaponUpgradeRequirementLevel[]
                    scaledRequirements =
                    BuildScaledRequirements(
                        tier.Requirements,
                        tier.Multiplier,
                        levelOffset);

                Debug.Log(
                    $"[UpgradeMap] Level {level} " +
                    $"{scaledRequirements[0].ItemPrefab} x{scaledRequirements[0].Amount}");

                item.SetUpgradeLevelRequirements(
                    level,
                    scaledRequirements);

                item.SetUpgradeStationLevelRequirement(
                    level,
                    tier.StationLevel);
            }
        }

        private static WeaponUpgradeRequirementLevel[]
            BuildScaledRequirements(
                WeaponUpgradeRequirementLevel[] requirements,
                float multiplier,
                int levelOffset)
        {
            if (requirements == null)
                return Array.Empty<WeaponUpgradeRequirementLevel>();

            WeaponUpgradeRequirementLevel[] result =
                new WeaponUpgradeRequirementLevel[
                    requirements.Length];

            for (int i = 0; i < requirements.Length; i++)
            {
                WeaponUpgradeRequirementLevel source =
                    requirements[i];

                int amount =
                    Mathf.Max(
                        1,
                        Mathf.RoundToInt(
                            source.Amount *
                            Mathf.Pow(
                                multiplier,
                                levelOffset)));

                result[i] =
                    new WeaponUpgradeRequirementLevel(
                        source.ItemPrefab,
                        amount);
            }

            return result;
        }
    }
}
