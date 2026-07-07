using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class WeaponUpgradeStationRequirementTables
{
    private sealed class StationRequirementTable
    {
        public readonly Dictionary<int, int> Levels = new Dictionary<int, int>();
    }

    private static readonly Dictionary<string, StationRequirementTable> Tables = new Dictionary<string, StationRequirementTable>(StringComparer.OrdinalIgnoreCase);
    private static bool _registered;

    public static T SetUpgradeStationLevelRequirement<T>(this T itemOrPrefab, int currentQuality, int stationLevel) where T : class
    {
        string itemPrefab = ResolveItemPrefabName(itemOrPrefab);
        SetStationLevelRequirement(itemPrefab, currentQuality, stationLevel);
        return itemOrPrefab;
    }

    public static T SetUpgradeStationLevelRequirementRange<T>(this T itemOrPrefab, int firstLevel, int lastLevel, int stationLevel) where T : class
    {
        string itemPrefab = ResolveItemPrefabName(itemOrPrefab);
        SetStationLevelRequirementRange(itemPrefab, firstLevel, lastLevel, stationLevel);
        return itemOrPrefab;
    }

    public static T SetScaledUpgradeStationLevelRequirementRange<T>(
        this T itemOrPrefab,
        int firstLevel,
        int lastLevel,
        int firstStationLevel,
        int stationLevelStep,
        float multiplierPerLevel = 1f) where T : class
    {
        string itemPrefab = ResolveItemPrefabName(itemOrPrefab);
        SetScaledStationLevelRequirementRange(itemPrefab, firstLevel, lastLevel, firstStationLevel, stationLevelStep, multiplierPerLevel);
        return itemOrPrefab;
    }

    public static void SetStationLevelRequirement(string itemPrefab, int currentQuality, int stationLevel)
    {
        if (string.IsNullOrWhiteSpace(itemPrefab))
            return;

        if (currentQuality <= 0)
            return;

        stationLevel = Mathf.Max(1, stationLevel);

        string cleanName = CleanPrefabName(itemPrefab);

        if (!Tables.TryGetValue(cleanName, out StationRequirementTable table))
        {
            table = new StationRequirementTable();
            Tables[cleanName] = table;
        }

        table.Levels[currentQuality] = stationLevel;
        _registered = true;
    }

    public static void SetStationLevelRequirementRange(string itemPrefab, int firstLevel, int lastLevel, int stationLevel)
    {
        if (firstLevel > lastLevel)
        {
            int temp = firstLevel;
            firstLevel = lastLevel;
            lastLevel = temp;
        }

        for (int level = firstLevel; level <= lastLevel; level++)
            SetStationLevelRequirement(itemPrefab, level, stationLevel);
    }

    public static void SetScaledStationLevelRequirementRange(
        string itemPrefab,
        int firstLevel,
        int lastLevel,
        int firstStationLevel,
        int stationLevelStep,
        float multiplierPerLevel = 1f)
    {
        if (firstLevel > lastLevel)
        {
            int temp = firstLevel;
            firstLevel = lastLevel;
            lastLevel = temp;
        }

        for (int level = firstLevel; level <= lastLevel; level++)
        {
            int index = level - firstLevel;
            float raw = firstStationLevel + stationLevelStep * index;

            if (Math.Abs(multiplierPerLevel - 1f) > 0.0001f)
                raw *= Mathf.Pow(multiplierPerLevel, index);

            int stationLevel = Mathf.Max(1, Mathf.RoundToInt(raw));
            SetStationLevelRequirement(itemPrefab, level, stationLevel);
        }
    }

    public static bool TryGetStationLevel(string itemPrefab, int currentQuality, out int stationLevel)
    {
        stationLevel = 0;

        if (!_registered)
            return false;

        string cleanName = CleanPrefabName(itemPrefab);

        if (!Tables.TryGetValue(cleanName, out StationRequirementTable table))
            return false;

        if (!table.Levels.TryGetValue(currentQuality, out stationLevel))
            return false;

        stationLevel = Mathf.Max(1, stationLevel);
        return true;
    }

    private static string ResolveItemPrefabName(object itemOrPrefab)
    {
        if (itemOrPrefab == null)
            throw new ArgumentNullException(nameof(itemOrPrefab));

        if (itemOrPrefab is string text)
            return CleanPrefabName(text);

        if (itemOrPrefab is GameObject gameObject)
            return CleanPrefabName(gameObject.name);

        if (itemOrPrefab is Component component)
            return CleanPrefabName(component.gameObject.name);

        object value = TryGetMemberValue(itemOrPrefab, "Prefab");

        if (TryResolveNameFromValue(value, out string prefabName))
            return prefabName;

        value = TryGetMemberValue(itemOrPrefab, "prefab");

        if (TryResolveNameFromValue(value, out prefabName))
            return prefabName;

        value = TryGetMemberValue(itemOrPrefab, "PrefabName");

        if (TryResolveNameFromValue(value, out prefabName))
            return prefabName;

        value = TryGetMemberValue(itemOrPrefab, "m_prefab");

        if (TryResolveNameFromValue(value, out prefabName))
            return prefabName;

        value = TryGetMemberValue(itemOrPrefab, "m_itemPrefab");

        if (TryResolveNameFromValue(value, out prefabName))
            return prefabName;

        throw new InvalidOperationException("Could not resolve prefab name from " + itemOrPrefab.GetType().FullName + ". Use WeaponUpgradeStationRequirementTables.SetStationLevelRequirement(\"PrefabName\", level, stationLevel) instead.");
    }

    private static bool TryResolveNameFromValue(object value, out string name)
    {
        name = string.Empty;

        if (value == null)
            return false;

        if (value is string text)
        {
            name = CleanPrefabName(text);
            return !string.IsNullOrWhiteSpace(name);
        }

        if (value is GameObject gameObject)
        {
            name = CleanPrefabName(gameObject.name);
            return !string.IsNullOrWhiteSpace(name);
        }

        if (value is Component component)
        {
            name = CleanPrefabName(component.gameObject.name);
            return !string.IsNullOrWhiteSpace(name);
        }

        object nested = TryGetMemberValue(value, "gameObject");

        if (nested is GameObject nestedGameObject)
        {
            name = CleanPrefabName(nestedGameObject.name);
            return !string.IsNullOrWhiteSpace(name);
        }

        return false;
    }

    private static object TryGetMemberValue(object instance, string memberName)
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
                    return property.GetValue(instance);
                }
                catch
                {
                    return null;
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
                    return null;
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

        return name.Trim();
    }
}

[HarmonyPatch]
public static class WeaponUpgradeStationRequirementPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        MethodInfo[] methods = typeof(Recipe).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];

            if (method == null)
                continue;

            if (method.Name == "GetRequiredStationLevel" && method.ReturnType == typeof(int))
                yield return method;
        }
    }

    private static void Postfix(Recipe __instance, object[] __args, ref int __result)
    {
        if (__instance == null || __instance.m_item == null)
            return;

        int currentQuality = 1;

        if (__args != null && __args.Length > 0 && __args[0] is int quality)
            currentQuality = quality;

        string itemName = __instance.m_item.name;

        if (WeaponUpgradeStationRequirementTables.TryGetStationLevel(itemName, currentQuality, out int stationLevel))
            __result = stationLevel;
    }
}