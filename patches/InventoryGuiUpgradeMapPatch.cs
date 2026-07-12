using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PungusSouls
{
    [HarmonyPatch]
    public static class InventoryGuiUpgradeMapPatch
    {
        private sealed class RecipeResourceState
        {
            public Recipe Recipe;
            public Piece.Requirement[] OriginalResources;
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo[] methods = typeof(InventoryGui).GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];

                if (method != null && method.Name == "SetupUpgradeItem")
                    yield return method;
            }
        }

        private static void Prefix(
            InventoryGui __instance,
            object[] __args,
            ref RecipeResourceState __state)
        {
            Recipe recipe = FindArgument<Recipe>(__args);

            if (recipe == null)
                recipe = FindFieldValue<Recipe>(__instance);

            ItemDrop.ItemData itemData = FindArgument<ItemDrop.ItemData>(__args);

            if (itemData == null)
                itemData = FindFieldValue<ItemDrop.ItemData>(__instance);

            if (recipe == null || itemData == null || ObjectDB.instance == null)
                return;

            string itemPrefab = ResolveItemPrefabName(recipe, itemData);

            if (string.IsNullOrWhiteSpace(itemPrefab))
                return;

            int currentQuality = Mathf.Max(1, itemData.m_quality);

            if (!WeaponUpgradeRequirementTables.TryBuildRequirementsForLevel(
                    ObjectDB.instance,
                    itemPrefab,
                    currentQuality,
                    out Piece.Requirement[] mappedResources))
            {
                return;
            }

            __state = new RecipeResourceState
            {
                Recipe = recipe,
                OriginalResources = recipe.m_resources
            };

            recipe.m_resources = mappedResources;

            Debug.Log(
                "[UpgradeMap] Swapped upgrade requirements for " +
                itemPrefab +
                " quality " +
                currentQuality);
        }

        private static void Finalizer(RecipeResourceState __state)
        {
            if (__state == null || __state.Recipe == null)
                return;

            __state.Recipe.m_resources = __state.OriginalResources;
        }

        private static T FindArgument<T>(object[] args) where T : class
        {
            if (args == null)
                return null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is T value)
                    return value;
            }

            return null;
        }

        private static T FindFieldValue<T>(object instance) where T : class
        {
            if (instance == null)
                return null;

            FieldInfo[] fields = instance.GetType().GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            for (int i = 0; i < fields.Length; i++)
            {
                try
                {
                    object value = fields[i].GetValue(instance);

                    if (value is T typed)
                        return typed;
                }
                catch
                {
                }
            }

            return null;
        }

        private static string ResolveItemPrefabName(
            Recipe recipe,
            ItemDrop.ItemData itemData)
        {
            if (itemData != null && itemData.m_dropPrefab != null)
                return CleanPrefabName(itemData.m_dropPrefab.name);

            if (recipe != null && recipe.m_item != null)
                return CleanPrefabName(recipe.m_item.name);

            return string.Empty;
        }

        private static string CleanPrefabName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            int cloneIndex = name.IndexOf(
                "(Clone)",
                StringComparison.OrdinalIgnoreCase);

            if (cloneIndex >= 0)
                name = name.Substring(0, cloneIndex);

            return name.Trim();
        }
    }
}