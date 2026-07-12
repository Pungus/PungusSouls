using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VRequirement = Piece.Requirement;

namespace PungusSouls
{
    public readonly struct WeaponUpgradeRequirementLevel
    {
        public readonly string ItemPrefab;
        public readonly int Amount;

        public WeaponUpgradeRequirementLevel(string itemPrefab, int amount)
        {
            ItemPrefab = itemPrefab;
            Amount = amount;
        }
    }

    public enum WeaponUpgradeRequirementRounding
    {
        Floor,
        Round,
        Ceiling
    }

    public static class WeaponUpgradeRequirementRangeExtensions
    {
        public static T SetUpgradeLevelRequirementRange<T>(
            this T itemOrPrefab,
            int firstLevel,
            int lastLevel,
            params WeaponUpgradeRequirementLevel[] requirements) where T : class
        {
            if (itemOrPrefab == null)
                return itemOrPrefab;

            if (requirements == null)
                return itemOrPrefab;

            NormalizeRange(ref firstLevel, ref lastLevel);

            for (int level = firstLevel; level <= lastLevel; level++)
                itemOrPrefab.SetUpgradeLevelRequirements(level, requirements);

            return itemOrPrefab;
        }

        public static T AddUpgradeLevelRequirementRange<T>(
            this T itemOrPrefab,
            int firstLevel,
            int lastLevel,
            string requirementPrefab,
            int amount) where T : class
        {
            if (itemOrPrefab == null)
                return itemOrPrefab;

            NormalizeRange(ref firstLevel, ref lastLevel);

            for (int level = firstLevel; level <= lastLevel; level++)
                itemOrPrefab.AddUpgradeLevelRequirement(level, requirementPrefab, amount);

            return itemOrPrefab;
        }

        public static T SetScaledUpgradeLevelRequirementRange<T>(
            this T itemOrPrefab,
            int firstLevel,
            int lastLevel,
            string primaryMaterial,
            string secondaryMaterial,
            int firstAmount,
            int amountStep) where T : class
        {
            if (itemOrPrefab == null)
                return itemOrPrefab;

            NormalizeRange(ref firstLevel, ref lastLevel);

            for (int level = firstLevel; level <= lastLevel; level++)
            {
                int amount = Mathf.Max(1, firstAmount + (level - firstLevel) * amountStep);

                itemOrPrefab.SetUpgradeLevelRequirements(
                    level,
                    new WeaponUpgradeRequirementLevel(primaryMaterial, amount),
                    new WeaponUpgradeRequirementLevel(secondaryMaterial, amount)
                );
            }

            return itemOrPrefab;
        }
        public static T SetScaledPrimaryUpgradeLevelRequirementRange<T>(
            this T itemOrPrefab,
            int firstLevel,
            int lastLevel,
            string primaryMaterial,
            int firstAmount,
            int amountStep,
            params WeaponUpgradeRequirementLevel[] fixedRequirements) where T : class
        {
            if (itemOrPrefab == null)
                return itemOrPrefab;

            NormalizeRange(ref firstLevel, ref lastLevel);

            for (int level = firstLevel; level <= lastLevel; level++)
            {
                int amount = Mathf.Max(1, firstAmount + (level - firstLevel) * amountStep);
                List<WeaponUpgradeRequirementLevel> requirements = new List<WeaponUpgradeRequirementLevel>
                {
                    new WeaponUpgradeRequirementLevel(primaryMaterial, amount)
                };

                if (fixedRequirements != null)
                    requirements.AddRange(fixedRequirements);

                itemOrPrefab.SetUpgradeLevelRequirements(level, requirements.ToArray());
            }

            return itemOrPrefab;
        }


        public static T SetScaledUpgradeLevelRequirementRange<T>(
            this T itemOrPrefab,
            int firstLevel,
            int lastLevel,
            string primaryMaterial,
            string secondaryMaterial,
            int firstAmount,
            int amountStep,
            float multiplierPerLevel,
            WeaponUpgradeRequirementRounding rounding = WeaponUpgradeRequirementRounding.Round) where T : class
        {
            if (itemOrPrefab == null)
                return itemOrPrefab;

            NormalizeRange(ref firstLevel, ref lastLevel);

            for (int level = firstLevel; level <= lastLevel; level++)
            {
                int levelIndex = level - firstLevel;
                int amount = CalculateScaledAmount(firstAmount, amountStep, multiplierPerLevel, levelIndex, rounding);

                itemOrPrefab.SetUpgradeLevelRequirements(
                    level,
                    new WeaponUpgradeRequirementLevel(primaryMaterial, amount),
                    new WeaponUpgradeRequirementLevel(secondaryMaterial, amount)
                );
            }

            return itemOrPrefab;
        }

        public static T SetScaledPrimaryUpgradeLevelRequirementRange<T>(
            this T itemOrPrefab,
            int firstLevel,
            int lastLevel,
            string primaryMaterial,
            int firstAmount,
            int amountStep,
            float multiplierPerLevel,
            WeaponUpgradeRequirementRounding rounding = WeaponUpgradeRequirementRounding.Round,
            params WeaponUpgradeRequirementLevel[] fixedRequirements) where T : class
        {
            if (itemOrPrefab == null)
                return itemOrPrefab;

            NormalizeRange(ref firstLevel, ref lastLevel);

            for (int level = firstLevel; level <= lastLevel; level++)
            {
                int levelIndex = level - firstLevel;
                int amount = CalculateScaledAmount(firstAmount, amountStep, multiplierPerLevel, levelIndex, rounding);
                List<WeaponUpgradeRequirementLevel> requirements = new List<WeaponUpgradeRequirementLevel>
                {
                    new WeaponUpgradeRequirementLevel(primaryMaterial, amount)
                };

                if (fixedRequirements != null)
                    requirements.AddRange(fixedRequirements);

                itemOrPrefab.SetUpgradeLevelRequirements(level, requirements.ToArray());
            }

            return itemOrPrefab;
        }

        private static int CalculateScaledAmount(
            int firstAmount,
            int amountStep,
            float multiplierPerLevel,
            int levelIndex,
            WeaponUpgradeRequirementRounding rounding)
        {
            float amount = firstAmount + amountStep * levelIndex;

            if (!float.IsNaN(multiplierPerLevel) && multiplierPerLevel > 0f && !Mathf.Approximately(multiplierPerLevel, 1f))
                amount *= Mathf.Pow(multiplierPerLevel, levelIndex);

            int rounded;

            switch (rounding)
            {
                case WeaponUpgradeRequirementRounding.Floor:
                    rounded = Mathf.FloorToInt(amount);
                    break;
                case WeaponUpgradeRequirementRounding.Ceiling:
                    rounded = Mathf.CeilToInt(amount);
                    break;
                default:
                    rounded = Mathf.RoundToInt(amount);
                    break;
            }

            return Mathf.Max(1, rounded);
        }

        public static void SetLevelRequirementRange(
            string itemPrefab,
            int firstLevel,
            int lastLevel,
            params WeaponUpgradeRequirementLevel[] requirements)
        {
            if (string.IsNullOrWhiteSpace(itemPrefab))
                return;

            if (requirements == null)
                return;

            NormalizeRange(ref firstLevel, ref lastLevel);

            for (int level = firstLevel; level <= lastLevel; level++)
                WeaponUpgradeRequirementTables.SetLevelRequirements(itemPrefab, level, requirements);
        }

        private static void NormalizeRange(ref int firstLevel, ref int lastLevel)
        {
            if (firstLevel > lastLevel)
            {
                int temp = firstLevel;
                firstLevel = lastLevel;
                lastLevel = temp;
            }

            firstLevel = Mathf.Max(1, firstLevel);
            lastLevel = Mathf.Max(1, lastLevel);
        }
    }

    public static class WeaponUpgradeRequirementTables
    {
        private sealed class RequirementTable
        {
            public readonly Dictionary<int, List<WeaponUpgradeRequirementLevel>> Levels = new Dictionary<int, List<WeaponUpgradeRequirementLevel>>();
        }

        private static readonly Dictionary<string, RequirementTable> Tables = new Dictionary<string, RequirementTable>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<VRequirement, string> RequirementItemNames = new Dictionary<VRequirement, string>();
        private static readonly Dictionary<VRequirement, string> RequirementRecipeNames = new Dictionary<VRequirement, string>();

        private static bool _registered;

        public static T SetUpgradeLevelRequirements<T>(this T itemOrPrefab, int currentQuality, params WeaponUpgradeRequirementLevel[] requirements) where T : class
        {
            string itemPrefab = ResolveItemPrefabName(itemOrPrefab);
            SetLevelRequirements(itemPrefab, currentQuality, requirements);
            return itemOrPrefab;
        }

        public static T AddUpgradeLevelRequirement<T>(this T itemOrPrefab, int currentQuality, string requirementPrefab, int amount) where T : class
        {
            string itemPrefab = ResolveItemPrefabName(itemOrPrefab);
            AddLevelRequirement(itemPrefab, currentQuality, requirementPrefab, amount);
            return itemOrPrefab;
        }

        public static void SetLevelRequirements(string itemPrefab, int currentQuality, params WeaponUpgradeRequirementLevel[] requirements)
        {
            if (string.IsNullOrWhiteSpace(itemPrefab))
                return;

            if (currentQuality <= 0)
                return;

            RequirementTable table = GetOrCreateTable(itemPrefab);
            List<WeaponUpgradeRequirementLevel> list = new List<WeaponUpgradeRequirementLevel>();

            if (requirements != null)
            {
                for (int i = 0; i < requirements.Length; i++)
                {
                    WeaponUpgradeRequirementLevel requirement = requirements[i];

                    if (string.IsNullOrWhiteSpace(requirement.ItemPrefab))
                        continue;

                    if (requirement.Amount <= 0)
                        continue;

                    list.Add(requirement);
                }
            }

            table.Levels[currentQuality] = list;
            _registered = true;
            ApplyIfObjectDbReady();
        }

        public static void AddLevelRequirement(string itemPrefab, int currentQuality, string requirementPrefab, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemPrefab))
                return;

            if (string.IsNullOrWhiteSpace(requirementPrefab))
                return;

            if (currentQuality <= 0 || amount <= 0)
                return;

            RequirementTable table = GetOrCreateTable(itemPrefab);

            if (!table.Levels.TryGetValue(currentQuality, out List<WeaponUpgradeRequirementLevel> list))
            {
                list = new List<WeaponUpgradeRequirementLevel>();
                table.Levels[currentQuality] = list;
            }

            list.Add(new WeaponUpgradeRequirementLevel(requirementPrefab, amount));
            _registered = true;
            ApplyIfObjectDbReady();
        }

        public static void Clear()
        {
            Tables.Clear();
            RequirementItemNames.Clear();
            RequirementRecipeNames.Clear();
            _registered = false;
        }

        public static void ApplyIfObjectDbReady()
        {
            if (ObjectDB.instance == null)
                return;

            Apply(ObjectDB.instance);
        }


        public static void Apply(ObjectDB objectDb)
        {
            if (!_registered || objectDb == null || objectDb.m_recipes == null)
                return;

            RequirementItemNames.Clear();
            RequirementRecipeNames.Clear();

            for (int i = 0; i < objectDb.m_recipes.Count; i++)
            {
                Recipe recipe = objectDb.m_recipes[i];

                if (recipe == null || recipe.m_item == null)
                    continue;

                string recipeItemName = CleanPrefabName(recipe.m_item.name);

                if (!Tables.TryGetValue(recipeItemName, out RequirementTable table))
                    continue;

                ApplyToRecipe(objectDb, recipe, recipeItemName, table);
            }
        }
        private static bool IsUpgradeContext()
        {
            InventoryGui gui = InventoryGui.instance;

            if (gui == null)
                return false;

            FieldInfo[] fields = typeof(InventoryGui).GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];

                if (!field.Name.ToLowerInvariant().Contains("upgrade"))
                    continue;

                try
                {
                    object value = field.GetValue(gui);

                    if (value is ItemDrop.ItemData)
                        return true;
                }
                catch
                {
                }
            }

            return false;
        }
            public static bool TryGetAmount(
        VRequirement requirement,
        int currentQuality,
        out int amount)
            {
                amount = 0;

                if (currentQuality <= 1)
                    return false;

                if (requirement == null)
                    return false;

                if (!RequirementRecipeNames.TryGetValue(
                        requirement,
                        out string recipeItemName))
                    return false;

                if (!RequirementItemNames.TryGetValue(
                        requirement,
                        out string requirementItemName))
                    return false;

                if (!Tables.TryGetValue(
                        recipeItemName,
                        out RequirementTable table))
                    return false;

                if (!table.Levels.TryGetValue(
                        currentQuality,
                        out List<WeaponUpgradeRequirementLevel> requirements))
                    return false;

                for (int i = 0; i < requirements.Count; i++)
                {
                    WeaponUpgradeRequirementLevel requirementLevel =
                        requirements[i];

                    if (string.Equals(
                            CleanPrefabName(requirementLevel.ItemPrefab),
                            requirementItemName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        amount = Mathf.Max(
                            1,
                            requirementLevel.Amount);

                        return true;
                    }
                }

                amount = 0;
                return true;
            }


        public static bool TryBuildRequirementsForLevel( ObjectDB objectDb, string itemPrefab, int currentQuality, out Piece.Requirement[] resources)
        {
            resources = Array.Empty<Piece.Requirement>();

            if (objectDb == null)
                return false;

            if (string.IsNullOrWhiteSpace(itemPrefab))
                return false;

            string cleanItemName = CleanPrefabName(itemPrefab);

            if (!Tables.TryGetValue(cleanItemName, out RequirementTable table))
                return false;

            if (!table.Levels.TryGetValue(currentQuality, out List<WeaponUpgradeRequirementLevel> requirements))
                return false;

            List<Piece.Requirement> result = new List<Piece.Requirement>();

            for (int i = 0; i < requirements.Count; i++)
            {
                WeaponUpgradeRequirementLevel requirementLevel = requirements[i];

                if (string.IsNullOrWhiteSpace(requirementLevel.ItemPrefab))
                    continue;

                ItemDrop itemDrop = FindItemDrop(
                    objectDb,
                    requirementLevel.ItemPrefab);

                if (itemDrop == null)
                {
                    Debug.LogWarning(
                        "[UpgradeRequirements] Missing requirement item '" +
                        requirementLevel.ItemPrefab +
                        "' for upgrade map '" +
                        cleanItemName +
                        "'");

                    continue;
                }

                Piece.Requirement requirement =
                    new Piece.Requirement
                    {
                        m_resItem = itemDrop,
                        m_amount = Mathf.Max(1, requirementLevel.Amount),
                        m_amountPerLevel = 0,
                        m_recover = true
                    };

                result.Add(requirement);

                RequirementRecipeNames[requirement] = cleanItemName;
                RequirementItemNames[requirement] =
                    CleanPrefabName(requirementLevel.ItemPrefab);
            }

            if (result.Count == 0)
                return false;

            resources = result.ToArray();

            return true;
        }
        private static void ApplyToRecipe(ObjectDB objectDb,Recipe recipe,string recipeItemName,RequirementTable table)
        {
            List<VRequirement> resources =
                new List<VRequirement>();

            if (recipe.m_resources != null)
                resources.AddRange(recipe.m_resources);

            HashSet<string> allRequirementNames =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<int, List<WeaponUpgradeRequirementLevel>> pair in table.Levels)
            {
                List<WeaponUpgradeRequirementLevel> levelRequirements =
                    pair.Value;

                if (levelRequirements == null)
                    continue;

                for (int i = 0; i < levelRequirements.Count; i++)
                {
                    string requirementName =
                        CleanPrefabName(levelRequirements[i].ItemPrefab);

                    if (!string.IsNullOrWhiteSpace(requirementName))
                        allRequirementNames.Add(requirementName);
                }
            }

            foreach (string requirementName in allRequirementNames)
            {
                VRequirement requirement =
                    FindRequirement(resources, requirementName);

                if (requirement == null)
                {
                    ItemDrop itemDrop =
                        FindItemDrop(objectDb, requirementName);

                    if (itemDrop == null)
                    {
                        Debug.LogWarning(
                            "[UpgradeRequirements] Missing requirement item '" +
                            requirementName +
                            "' for recipe '" +
                            recipeItemName +
                            "'");

                        continue;
                    }

                    requirement =
                        new VRequirement
                        {
                            m_resItem = itemDrop,
                            m_amount = 0,
                            m_amountPerLevel = 0,
                            m_recover = true
                        };

                    resources.Add(requirement);
                }
                else
                {
                    requirement.m_amountPerLevel = 0;
                }

                RequirementRecipeNames[requirement] = recipeItemName;
                RequirementItemNames[requirement] = requirementName;
            }

            foreach (VRequirement requirement in resources)
            {
                if (requirement == null || requirement.m_resItem == null)
                    continue;

                if (!RequirementRecipeNames.ContainsKey(requirement))
                {
                    RequirementRecipeNames[requirement] = recipeItemName;
                    RequirementItemNames[requirement] =
                        CleanPrefabName(
                            requirement.m_resItem.gameObject != null
                                ? requirement.m_resItem.gameObject.name
                                : requirement.m_resItem.name);
                }
            }

            recipe.m_resources = resources.ToArray();
        }
        private static RequirementTable GetOrCreateTable(string itemPrefab)
        {
            string cleanName = CleanPrefabName(itemPrefab);

            if (!Tables.TryGetValue(cleanName, out RequirementTable table))
            {
                table = new RequirementTable();
                Tables[cleanName] = table;
            }

            return table;

        }

        private static VRequirement FindRequirement(List<VRequirement> requirements, string requirementPrefab)
        {
            string cleanRequirementName = CleanPrefabName(requirementPrefab);

            for (int i = 0; i < requirements.Count; i++)
            {
                VRequirement requirement = requirements[i];

                if (requirement == null || requirement.m_resItem == null)
                    continue;

                string itemName = CleanPrefabName(requirement.m_resItem.gameObject != null ? requirement.m_resItem.gameObject.name : requirement.m_resItem.name);

                if (string.Equals(itemName, cleanRequirementName, StringComparison.OrdinalIgnoreCase))
                    return requirement;
            }

            return null;
        }

        private static ItemDrop FindItemDrop(ObjectDB objectDb, string prefabName)
        {
            string cleanName = CleanPrefabName(prefabName);

            if (objectDb == null || objectDb.m_items == null)
                return null;

            for (int i = 0; i < objectDb.m_items.Count; i++)
            {
                GameObject item = objectDb.m_items[i];

                if (item == null)
                    continue;

                if (!string.Equals(CleanPrefabName(item.name), cleanName, StringComparison.OrdinalIgnoreCase))
                    continue;

                return item.GetComponent<ItemDrop>();
            }

            return null;
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

            throw new InvalidOperationException("Could not resolve prefab name from " + itemOrPrefab.GetType().FullName + ". Use WeaponUpgradeRequirementTables.SetLevelRequirements(\"PrefabName\", level, ...) instead.");
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

    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    public static class WeaponUpgradeRequirementObjectDbPatch
    {
        private static void Postfix(ObjectDB __instance)
        {
            WeaponUpgradeRequirementTables.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(Piece.Requirement), "GetAmount")]
    public static class WeaponUpgradeRequirementAmountPatch
    {
        private static void Postfix(Piece.Requirement __instance, int qualityLevel, ref int __result)
        {
            if (WeaponUpgradeRequirementTables.TryGetAmount(__instance, qualityLevel, out int amount))
                __result = amount;
        }
    }
}
