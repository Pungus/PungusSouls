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

    private static readonly Dictionary<string, StationRequirementTable> Tables =
        new Dictionary<string, StationRequirementTable>(StringComparer.OrdinalIgnoreCase);

    private static bool _registered;

    public static bool DebugLogging;

    public static T SetUpgradeStationLevelRequirement<T>(
        this T itemOrPrefab,
        int upgradeLevel,
        int stationLevel) where T : class
    {
        string itemPrefab = ResolveItemPrefabName(itemOrPrefab);

        SetStationLevelRequirement(
            itemPrefab,
            upgradeLevel,
            stationLevel);

        return itemOrPrefab;
    }

    public static T SetUpgradeStationLevelRequirementRange<T>(
        this T itemOrPrefab,
        int firstLevel,
        int lastLevel,
        int stationLevel) where T : class
    {
        string itemPrefab = ResolveItemPrefabName(itemOrPrefab);

        SetStationLevelRequirementRange(
            itemPrefab,
            firstLevel,
            lastLevel,
            stationLevel);

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

        SetScaledStationLevelRequirementRange(
            itemPrefab,
            firstLevel,
            lastLevel,
            firstStationLevel,
            stationLevelStep,
            multiplierPerLevel);

        return itemOrPrefab;
    }

    public static void SetStationLevelRequirement(
        string itemPrefab,
        int upgradeLevel,
        int stationLevel)
    {
        if (string.IsNullOrWhiteSpace(itemPrefab) || upgradeLevel <= 0)
            return;

        stationLevel = Mathf.Max(1, stationLevel);

        string cleanName = CleanPrefabName(itemPrefab);

        if (!Tables.TryGetValue(cleanName, out StationRequirementTable table))
        {
            table = new StationRequirementTable();
            Tables[cleanName] = table;
        }

        table.Levels[upgradeLevel] = stationLevel;
        _registered = true;

        if (DebugLogging)
        {
            Debug.Log(
                "[PungusSouls] Registered station level item=" +
                cleanName +
                " upgradeLevel=" +
                upgradeLevel +
                " stationLevel=" +
                stationLevel);
        }
    }

    public static void SetStationLevelRequirementRange(
        string itemPrefab,
        int firstLevel,
        int lastLevel,
        int stationLevel)
    {
        if (firstLevel > lastLevel)
        {
            int temp = firstLevel;
            firstLevel = lastLevel;
            lastLevel = temp;
        }

        firstLevel = Mathf.Max(1, firstLevel);
        lastLevel = Mathf.Max(1, lastLevel);

        for (int level = firstLevel; level <= lastLevel; level++)
        {
            SetStationLevelRequirement(
                itemPrefab,
                level,
                stationLevel);
        }
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

        firstLevel = Mathf.Max(1, firstLevel);
        lastLevel = Mathf.Max(1, lastLevel);

        for (int level = firstLevel; level <= lastLevel; level++)
        {
            int index = level - firstLevel;
            float raw = firstStationLevel + stationLevelStep * index;

            if (Math.Abs(multiplierPerLevel - 1f) > 0.0001f)
                raw *= Mathf.Pow(multiplierPerLevel, index);

            SetStationLevelRequirement(
                itemPrefab,
                level,
                Mathf.Max(1, Mathf.RoundToInt(raw)));
        }
    }

    public static bool TryGetStationLevel(
        string itemPrefab,
        int upgradeLevel,
        out int stationLevel)
    {
        stationLevel = 0;

        if (!_registered || string.IsNullOrWhiteSpace(itemPrefab) || upgradeLevel <= 0)
            return false;

        string cleanName = CleanPrefabName(itemPrefab);

        if (!Tables.TryGetValue(cleanName, out StationRequirementTable table))
            return false;

        if (!table.Levels.TryGetValue(upgradeLevel, out stationLevel))
            return false;

        stationLevel = Mathf.Max(1, stationLevel);
        return true;
    }

    public static bool TryGetStationLevelForTargetQuality(
        Recipe recipe,
        int targetQuality,
        out int stationLevel)
    {
        stationLevel = 0;

        if (recipe == null || recipe.m_item == null || targetQuality <= 1)
            return false;

        int upgradeLevel = Mathf.Max(1, targetQuality);

        foreach (string itemName in GetRecipeItemNames(recipe))
        {
            if (TryGetStationLevel(itemName, upgradeLevel, out stationLevel))
                return true;
        }

        foreach (string itemName in GetRecipeItemNames(recipe))
        {
            if (PungusSouls.UpgradeMapRegistry.TryGetStationLevelForItem(
                    itemName,
                    upgradeLevel,
                    out stationLevel))
            {
                return true;
            }
        }

        return false;
    }

    public static string GetDebugLookupInfo(
        Recipe recipe,
        int targetQuality)
    {
        if (recipe == null || recipe.m_item == null)
            return "recipe=null";

        int upgradeLevel = Mathf.Max(1, targetQuality);
        string[] names = GetRecipeItemNames(recipe).ToArray();

        return
            "targetQuality=" +
            targetQuality +
            " upgradeLevel=" +
            upgradeLevel +
            " names=" +
            string.Join(",", names);
    }

    private static List<string> GetRecipeItemNames(Recipe recipe)
    {
        List<string> names = new List<string>();

        if (recipe == null || recipe.m_item == null)
            return names;

        AddName(names, recipe.m_item.name);

        if (recipe.m_item.gameObject != null)
            AddName(names, recipe.m_item.gameObject.name);

        if (recipe.m_item.m_itemData != null &&
            recipe.m_item.m_itemData.m_dropPrefab != null)
        {
            AddName(names, recipe.m_item.m_itemData.m_dropPrefab.name);
        }

        return names;
    }

    private static void AddName(
        List<string> names,
        string name)
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

        throw new InvalidOperationException(
            "Could not resolve prefab name from " +
            itemOrPrefab.GetType().FullName +
            ". Use WeaponUpgradeStationRequirementTables.SetStationLevelRequirement(\"PrefabName\", level, stationLevel) instead.");
    }

    private static bool TryResolveNameFromValue(
        object value,
        out string name)
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

    private static object TryGetMemberValue(
        object instance,
        string memberName)
    {
        if (instance == null || string.IsNullOrWhiteSpace(memberName))
            return null;

        Type type = instance.GetType();

        while (type != null)
        {
            PropertyInfo property = type.GetProperty(
                memberName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

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

            FieldInfo field = type.GetField(
                memberName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

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

        int bracketIndex = name.IndexOf('(');

        if (bracketIndex >= 0)
            name = name.Substring(0, bracketIndex);

        int spaceIndex = name.IndexOf(' ');

        if (spaceIndex >= 0)
            name = name.Substring(0, spaceIndex);

        return name.Trim();
    }
}

[HarmonyPatch(typeof(Recipe), nameof(Recipe.GetRequiredStationLevel), new[] { typeof(int) })]
[HarmonyAfter(new[] { "org.bepinex.helpers.ItemManager" })]
public static class WeaponUpgradeStationRequirementPatch
{
    [HarmonyPriority(Priority.Last)]
    public static bool Prefix(
        Recipe __instance,
        int quality,
        ref int __result)
    {
        bool found =
            WeaponUpgradeStationRequirementTables.TryGetStationLevelForTargetQuality(
                __instance,
                quality,
                out int stationLevel);

        Debug.Log(
            "[PungusSouls] Station Prefix " +
            WeaponUpgradeStationRequirementTables.GetDebugLookupInfo(__instance, quality) +
            " found=" +
            found +
            " stationLevel=" +
            stationLevel);

        if (found)
        {
            __result = stationLevel;
            return false;
        }

        return true;
    }

    [HarmonyPriority(Priority.Last)]
    [HarmonyAfter(new[] { "org.bepinex.helpers.ItemManager" })]
    public static void Postfix(
        Recipe __instance,
        int quality,
        ref int __result)
    {
        bool found =
            WeaponUpgradeStationRequirementTables.TryGetStationLevelForTargetQuality(
                __instance,
                quality,
                out int stationLevel);

        Debug.Log(
            "[PungusSouls] Station Postfix " +
            WeaponUpgradeStationRequirementTables.GetDebugLookupInfo(__instance, quality) +
            " found=" +
            found +
            " stationLevel=" +
            stationLevel +
            " currentResultBeforeOverride=" +
            __result);

        if (found)
        {
            __result = stationLevel;

            Debug.Log(
                "[PungusSouls] Station Postfix forced result=" +
                __result);
        }
    }
}
