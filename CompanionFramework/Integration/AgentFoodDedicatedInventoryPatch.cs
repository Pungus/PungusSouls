using HarmonyLib;
using System;
using UnityEngine;

[HarmonyPatch(typeof(Inventory))]
public static class AgentFoodInventoryRestrictionPatch
{
    [HarmonyPatch("AddItem", new Type[] { typeof(ItemDrop.ItemData) })]
    [HarmonyPrefix]
    private static bool AddItemDataPrefix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
    {
        AgentFoodInventory foodInventory = AgentFoodInventory.FindForInventory(__instance);

        if (foodInventory == null)
            return true;

        if (AgentFoodInventory.IsFoodItem(item))
            return true;

        MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, "Only food can go in NPC food slots");
        __result = false;
        return false;
    }

    [HarmonyPatch("AddItem", new Type[] { typeof(GameObject), typeof(int) })]
    [HarmonyPrefix]
    private static bool AddPrefabPrefix(Inventory __instance, GameObject prefab, int amount, ref bool __result)
    {
        AgentFoodInventory foodInventory = AgentFoodInventory.FindForInventory(__instance);

        if (foodInventory == null)
            return true;

        ItemDrop itemDrop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;

        if (itemDrop != null && AgentFoodInventory.IsFoodItem(itemDrop.m_itemData))
            return true;

        MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, "Only food can go in NPC food slots");
        __result = false;
        return false;
    }
}
