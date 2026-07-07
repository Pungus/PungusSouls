using HarmonyLib;
using System.Reflection;
using UnityEngine;

public static class AgentFoodSlotRules
{
    public const int SlotCount = 3;
    public const int FoodRow = 0;

    public static bool IsDedicatedFoodPosition(Vector2i position)
    {
        return position.y == FoodRow && position.x >= 0 && position.x < SlotCount;
    }

    public static bool IsFoodItem(ItemDrop.ItemData item)
    {
        if (item == null || item.m_shared == null)
            return false;

        if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
            return false;

        return item.m_shared.m_food > 0f || item.m_shared.m_foodStamina > 0f || item.m_shared.m_foodEitr > 0f;
    }

    public static ItemDrop.ItemData GetFoodInSlot(Inventory inventory, int slot)
    {
        if (inventory == null || slot < 0 || slot >= SlotCount)
            return null;

        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (item == null)
                continue;

            if (item.m_gridPos.x == slot && item.m_gridPos.y == FoodRow)
                return item;
        }

        return null;
    }

    public static ItemDrop.ItemData FindDedicatedFoodForEating(AgentFood food, Inventory inventory)
    {
        if (inventory == null)
            return null;

        for (int slot = 0; slot < SlotCount; slot++)
        {
            ItemDrop.ItemData item = GetFoodInSlot(inventory, slot);

            if (!IsFoodItem(item))
                continue;

            if (IsFoodAlreadyActive(food, item))
                continue;

            return item;
        }

        return null;
    }

    public static ItemDrop.ItemData FindFoodOutsideDedicatedSlots(Inventory inventory)
    {
        if (inventory == null)
            return null;

        foreach (ItemDrop.ItemData item in inventory.GetAllItemsInGridOrder())
        {
            if (!IsFoodItem(item))
                continue;

            if (IsDedicatedFoodPosition(item.m_gridPos))
                continue;

            return item;
        }

        return null;
    }

    public static bool TryMoveFoodIntoSlot(Inventory inventory, int slot)
    {
        if (inventory == null || slot < 0 || slot >= SlotCount)
            return false;

        if (GetFoodInSlot(inventory, slot) != null)
            return false;

        ItemDrop.ItemData item = FindFoodOutsideDedicatedSlots(inventory);

        if (item == null)
            return false;

        item.m_gridPos = new Vector2i(slot, FoodRow);
        inventory.m_onChanged?.Invoke();
        return true;
    }

    public static bool TryMoveSlotItemOut(Inventory inventory, int slot)
    {
        if (inventory == null || slot < 0 || slot >= SlotCount)
            return false;

        ItemDrop.ItemData item = GetFoodInSlot(inventory, slot);

        if (item == null)
            return false;

        if (!TryFindFreeNonFoodSlot(inventory, out Vector2i freePosition))
            return false;

        item.m_gridPos = freePosition;
        inventory.m_onChanged?.Invoke();
        return true;
    }

    public static bool TryFindFreeNonFoodSlot(Inventory inventory, out Vector2i position)
    {
        position = new Vector2i(0, 0);

        if (inventory == null)
            return false;

        for (int y = 0; y < inventory.GetHeight(); y++)
        {
            for (int x = 0; x < inventory.GetWidth(); x++)
            {
                Vector2i candidate = new Vector2i(x, y);

                if (IsDedicatedFoodPosition(candidate))
                    continue;

                if (!IsOccupied(inventory, candidate))
                {
                    position = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsOccupied(Inventory inventory, Vector2i position)
    {
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (item != null && item.m_gridPos.x == position.x && item.m_gridPos.y == position.y)
                return true;
        }

        return false;
    }

    private static bool IsFoodAlreadyActive(AgentFood food, ItemDrop.ItemData item)
    {
        if (food == null || item == null || item.m_shared == null)
            return false;

        string name = item.m_shared.m_name;

        if (string.IsNullOrEmpty(name))
            return false;

        for (int i = 0; i < AgentFood.MaxFoodSlots; i++)
        {
            AgentFood.FoodEffect active = food.GetFood(i);

            if (active.IsActive && active.ItemName == name)
                return true;
        }

        return false;
    }
}

[HarmonyPatch]
public static class AgentFoodDedicatedSlotConsumePatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(AgentFood), "FindFoodForSlot");
    }

    private static bool Prepare()
    {
        return TargetMethod() != null;
    }

    private static void Postfix(AgentFood __instance, Inventory inventory, ref ItemDrop.ItemData __result)
    {
        __result = AgentFoodSlotRules.FindDedicatedFoodForEating(__instance, inventory);
    }
}
