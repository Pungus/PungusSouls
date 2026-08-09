using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PungusSouls
{
    public sealed class UpgradeTier
    {
        public int StartLevel;
        public int EndLevel;
        public int StationLevel;
        public float Multiplier = 1f;
        public WeaponUpgradeRequirementLevel[] Requirements = new WeaponUpgradeRequirementLevel[0];
    }

    public sealed class UpgradeMap
    {
        public string Name;
        public readonly List<UpgradeTier> Tiers = new List<UpgradeTier>();
    }

    public static class UpgradeMapRegistry
    {
        private static readonly Dictionary<string, UpgradeMap> Maps = new Dictionary<string, UpgradeMap>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, UpgradeMap> ItemMaps = new Dictionary<string, UpgradeMap>(StringComparer.OrdinalIgnoreCase);

        public static void Register(UpgradeMap map)
        {
            if (map == null)
                return;

            if (string.IsNullOrWhiteSpace(map.Name))
                return;

            Maps[map.Name] = map;
        }

        public static UpgradeMap Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            Maps.TryGetValue(name, out UpgradeMap map);
            return map;
        }

        public static void AssignMapToItem(string itemPrefab, UpgradeMap map)
        {
            if (string.IsNullOrWhiteSpace(itemPrefab) || map == null)
                return;

            ItemMaps[CleanPrefabName(itemPrefab)] = map;
        }

        public static bool TryGetStationLevelForItem(string itemPrefab, int upgradeLevel, out int stationLevel)
        {
            stationLevel = 0;

            if (string.IsNullOrWhiteSpace(itemPrefab) || upgradeLevel <= 0)
                return false;

            if (!ItemMaps.TryGetValue(CleanPrefabName(itemPrefab), out UpgradeMap map) || map == null)
                return false;

            for (int i = 0; i < map.Tiers.Count; i++)
            {
                UpgradeTier tier = map.Tiers[i];

                if (tier == null)
                    continue;

                if (upgradeLevel < tier.StartLevel || upgradeLevel > tier.EndLevel)
                    continue;

                stationLevel = Mathf.Max(1, tier.StationLevel);
                return true;
            }

            return false;
        }

        private static string CleanPrefabName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            int cloneIndex = name.IndexOf("(Clone)", StringComparison.OrdinalIgnoreCase);

            if (cloneIndex >= 0)
                name = name.Substring(0, cloneIndex);

            int bracketIndex = name.IndexOf('(');

            if (bracketIndex >= 0)
                name = name.Substring(0, bracketIndex);

            int spaceIndex = name.IndexOf(' ');

            if (spaceIndex >= 0)
                name = name.Substring(0, spaceIndex);

            return name.Trim();
        }
    }

    public static class UpgradeMapExtensions
    {
        public static T ApplyUpgradeMap<T>(this T item, string mapName) where T : class
        {
            UpgradeMap map = UpgradeMapRegistry.Get(mapName);

            if (map == null)
            {
                Debug.LogWarning("[UpgradeMap] Map '" + mapName + "' was not found.");
                return item;
            }

            string[] itemNames = ResolveItemPrefabNames(item);

            if (itemNames.Length == 0)
            {
                Debug.LogWarning("[UpgradeMap] Could not resolve prefab name for item using map '" + mapName + "'.");
                return item;
            }

            for (int i = 0; i < itemNames.Length; i++)
            {
                UpgradeMapRegistry.AssignMapToItem(itemNames[i], map);
            }

            ApplyLevelOneUpgradeItems(item, map);

            for (int i = 0; i < map.Tiers.Count; i++)
            {
                ApplyTier(item, itemNames, map.Tiers[i]);
            }

            return item;
        }

        private static void ApplyTier<T>(T item, string[] itemNames, UpgradeTier tier) where T : class
        {
            if (item == null || tier == null || itemNames == null || itemNames.Length == 0)
                return;

            int firstLevel = Mathf.Max(1, tier.StartLevel);
            int lastLevel = Mathf.Max(firstLevel, tier.EndLevel);
            int stationLevel = Mathf.Max(1, tier.StationLevel);

            for (int upgradeLevel = firstLevel; upgradeLevel <= lastLevel; upgradeLevel++)
            {
                for (int i = 0; i < itemNames.Length; i++)
                {
                    WeaponUpgradeStationRequirementTables.SetStationLevelRequirement(itemNames[i], upgradeLevel, stationLevel);
                }

                if (upgradeLevel <= 1)
                    continue;

                int levelOffset = upgradeLevel - firstLevel;

                WeaponUpgradeRequirementLevel[] scaledRequirements = BuildScaledRequirements(tier.Requirements, tier.Multiplier, levelOffset);

                item.SetUpgradeLevelRequirements(upgradeLevel, scaledRequirements);
            }
        }

        private static void ApplyLevelOneUpgradeItems<T>(T item, UpgradeMap map) where T : class
        {
            if (item == null || map == null)
                return;

            for (int i = 0; i < map.Tiers.Count; i++)
            {
                UpgradeTier tier = map.Tiers[i];

                if (tier == null)
                    continue;

                if (tier.StartLevel > 1 || tier.EndLevel < 1)
                    continue;

                WeaponUpgradeRequirementLevel[] requirements = BuildScaledRequirements(tier.Requirements, tier.Multiplier, 1 - tier.StartLevel);

                AddRequiredUpgradeItems(item, requirements);
                return;
            }
        }

        private static WeaponUpgradeRequirementLevel[] BuildScaledRequirements(WeaponUpgradeRequirementLevel[] requirements, float multiplier, int levelOffset)
        {
            if (requirements == null)
                return new WeaponUpgradeRequirementLevel[0];

            WeaponUpgradeRequirementLevel[] result = new WeaponUpgradeRequirementLevel[requirements.Length];

            for (int i = 0; i < requirements.Length; i++)
            {
                WeaponUpgradeRequirementLevel source = requirements[i];
                int amount = Mathf.Max(1, Mathf.RoundToInt(source.Amount * Mathf.Pow(multiplier, levelOffset)));
                result[i] = new WeaponUpgradeRequirementLevel(source.ItemPrefab, amount);
            }

            return result;
        }

        private static void AddRequiredUpgradeItems<T>(T item, WeaponUpgradeRequirementLevel[] requirements) where T : class
        {
            if (item == null || requirements == null)
                return;

            object requiredUpgradeItems = GetMemberValue(item, "RequiredUpgradeItems");

            if (requiredUpgradeItems == null)
            {
                Debug.LogWarning("[UpgradeMap] Could not find RequiredUpgradeItems on " + FirstResolvedName(item));
                return;
            }

            MethodInfo addMethod = FindAddMethod(requiredUpgradeItems.GetType());

            if (addMethod == null)
            {
                Debug.LogWarning("[UpgradeMap] Could not find usable Add method on RequiredUpgradeItems for " + FirstResolvedName(item));
                return;
            }

            for (int i = 0; i < requirements.Length; i++)
            {
                WeaponUpgradeRequirementLevel requirement = requirements[i];

                if (string.IsNullOrWhiteSpace(requirement.ItemPrefab) || requirement.Amount <= 0)
                    continue;

                object[] args = BuildArguments(addMethod, requirement.ItemPrefab, requirement.Amount);
                addMethod.Invoke(requiredUpgradeItems, args);
            }
        }

        private static MethodInfo FindAddMethod(Type type)
        {
            if (type == null)
                return null;

            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];

                if (method == null || method.Name != "Add")
                    continue;

                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length >= 2 && parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType == typeof(int))
                    return method;
            }

            return null;
        }

        private static object[] BuildArguments(MethodInfo method, string itemPrefab, int amount)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object[] args = new object[parameters.Length];
            args[0] = itemPrefab;
            args[1] = amount;

            for (int i = 2; i < parameters.Length; i++)
            {
                if (parameters[i].HasDefaultValue)
                {
                    args[i] = parameters[i].DefaultValue;
                    continue;
                }

                Type type = parameters[i].ParameterType;

                if (type == typeof(bool))
                    args[i] = false;
                else if (type == typeof(int))
                    args[i] = 0;
                else if (type == typeof(float))
                    args[i] = 0f;
                else if (type == typeof(string))
                    args[i] = string.Empty;
                else if (type.IsValueType)
                    args[i] = Activator.CreateInstance(type);
                else
                    args[i] = null;
            }

            return args;
        }

        private static string[] ResolveItemPrefabNames(object itemOrPrefab)
        {
            List<string> names = new List<string>();
            AddName(names, ResolveSingleName(itemOrPrefab));
            AddName(names, ResolveNameFromMember(itemOrPrefab, "PrefabName"));
            AddName(names, ResolveNameFromMember(itemOrPrefab, "Prefab"));
            AddName(names, ResolveNameFromMember(itemOrPrefab, "prefab"));
            AddName(names, ResolveNameFromMember(itemOrPrefab, "m_prefab"));
            AddName(names, ResolveNameFromMember(itemOrPrefab, "m_itemPrefab"));
            return names.ToArray();
        }

        private static string FirstResolvedName(object itemOrPrefab)
        {
            string[] names = ResolveItemPrefabNames(itemOrPrefab);
            return names.Length > 0 ? names[0] : "unknown";
        }

        private static void AddName(List<string> names, string name)
        {
            name = CleanPrefabName(name);

            if (string.IsNullOrWhiteSpace(name))
                return;

            for (int i = 0; i < names.Count; i++)
            {
                if (string.Equals(names[i], name, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            names.Add(name);
        }

        private static string ResolveSingleName(object itemOrPrefab)
        {
            if (itemOrPrefab == null)
                return string.Empty;

            if (itemOrPrefab is string text)
                return text;

            if (itemOrPrefab is GameObject gameObject)
                return gameObject.name;

            if (itemOrPrefab is Component component)
                return component.gameObject.name;

            return string.Empty;
        }

        private static string ResolveNameFromMember(object itemOrPrefab, string memberName)
        {
            object value = GetMemberValue(itemOrPrefab, memberName);
            return ResolveNameFromValue(value);
        }

        private static string ResolveNameFromValue(object value)
        {
            if (value == null)
                return string.Empty;

            if (value is string text)
                return text;

            if (value is GameObject gameObject)
                return gameObject.name;

            if (value is Component component)
                return component.gameObject.name;

            object nested = GetMemberValue(value, "gameObject");

            if (nested is GameObject nestedGameObject)
                return nestedGameObject.name;

            return string.Empty;
        }

        private static object GetMemberValue(object instance, string memberName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(memberName))
                return null;

            Type type = instance.GetType();

            while (type != null)
            {
                PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        return property.GetValue(instance, null);
                    }
                    catch
                    {
                    }
                }

                FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (field != null)
                {
                    try
                    {
                        return field.GetValue(instance);
                    }
                    catch
                    {
                    }
                }

                type = type.BaseType;
            }

            return null;
        }

        private static string CleanPrefabName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            int cloneIndex = name.IndexOf("(Clone)", StringComparison.OrdinalIgnoreCase);

            if (cloneIndex >= 0)
                name = name.Substring(0, cloneIndex);

            int bracketIndex = name.IndexOf('(');

            if (bracketIndex >= 0)
                name = name.Substring(0, bracketIndex);

            int spaceIndex = name.IndexOf(' ');

            if (spaceIndex >= 0)
                name = name.Substring(0, spaceIndex);

            return name.Trim();
        }
    }
}
