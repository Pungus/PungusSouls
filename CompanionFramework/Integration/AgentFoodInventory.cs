using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

public class AgentFoodInventory : MonoBehaviour
{
    public const int Width = 3;
    public const int Height = 1;

    private const float SaveInterval = 2f;
    private const string SlotPrefabPrefix = "agent_food_inv_slot_";
    private const string SlotStackSuffix = "_stack";
    private const string SlotQualitySuffix = "_quality";
    private const string SlotDurabilitySuffix = "_durability";
    private const string LoadedKey = "agent_food_inv_loaded";

    private AgentComponent _agent;
    private ZNetView _zNetView;
    private float _saveTimer;
    private bool _loaded;

    public Inventory Inventory { get; private set; }

    private void Awake()
    {
        _agent = GetComponent<AgentComponent>();
        _zNetView = GetComponent<ZNetView>();
        EnsureInventory();
    }

    private void Start()
    {
        TryLoadFromZDO();
    }

    private void Update()
    {
        EnsureInventory();
        TryLoadFromZDO();
        MoveInvalidItemsToNpcInventory();

        if (_zNetView == null || !_zNetView.IsValid() || !_zNetView.IsOwner())
            return;

        _saveTimer += Time.deltaTime;

        if (_saveTimer >= SaveInterval)
        {
            _saveTimer = 0f;
            SaveToZDO();
        }
    }

    private void OnDestroy()
    {
        SaveToZDO();
    }

    public bool IsFoodInventory(Inventory inventory)
    {
        return inventory != null && Inventory != null && inventory == Inventory;
    }

    public bool Contains(ItemDrop.ItemData item)
    {
        return Inventory != null && item != null && Inventory.ContainsItem(item);
    }

    public static bool IsFoodItem(ItemDrop.ItemData item)
    {
        if (item == null || item.m_shared == null)
            return false;

        if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
            return false;

        return item.m_shared.m_food > 0f || item.m_shared.m_foodStamina > 0f || item.m_shared.m_foodEitr > 0f;
    }

    public static AgentFoodInventory FindForInventory(Inventory inventory)
    {
        if (inventory == null)
            return null;

        AgentFoodInventory[] all = UnityEngine.Object.FindObjectsByType<AgentFoodInventory>(FindObjectsSortMode.None);

        foreach (AgentFoodInventory foodInventory in all)
        {
            if (foodInventory != null && foodInventory.Inventory == inventory)
                return foodInventory;
        }

        return null;
    }

    private void EnsureInventory()
    {
        if (Inventory != null)
            return;

        Inventory = new Inventory("NPC Food", null, Width, Height);
        Inventory.m_name = "NPC Food";
    }

    private void MoveInvalidItemsToNpcInventory()
    {
        if (Inventory == null)
            return;

        List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>(Inventory.GetAllItems());

        foreach (ItemDrop.ItemData item in items)
        {
            if (item == null || IsFoodItem(item))
                continue;

            Inventory.RemoveItem(item);

            if (_agent != null && _agent.ValheimContainer != null && _agent.ValheimContainer.m_inventory != null)
            {
                if (!_agent.ValheimContainer.m_inventory.AddItem(item))
                    Inventory.AddItem(item);
            }
            else
            {
                Inventory.AddItem(item);
            }
        }
    }

    private void TryLoadFromZDO()
    {
        if (_loaded)
            return;

        if (_zNetView == null || !_zNetView.IsValid())
            return;

        if (ObjectDB.instance == null)
            return;

        ZDO zdo = _zNetView.GetZDO();

        if (zdo == null)
            return;

        EnsureInventory();
        Inventory.RemoveAll();

        bool hadSavedSlots = false;

        for (int slot = 0; slot < Width; slot++)
        {
            string prefabName = zdo.GetString(SlotPrefabPrefix + slot, string.Empty);

            if (string.IsNullOrEmpty(prefabName))
                continue;

            hadSavedSlots = true;
            GameObject prefab = ObjectDB.instance.GetItemPrefab(prefabName);

            if (prefab == null)
            {
                Debug.LogWarning("[Agent FoodInventory] Could not find saved food prefab " + prefabName);
                continue;
            }

            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();

            if (itemDrop == null || itemDrop.m_itemData == null)
                continue;

            ItemDrop.ItemData item = itemDrop.m_itemData.Clone();
            item.m_stack = Mathf.Max(1, zdo.GetInt(SlotPrefabPrefix + slot + SlotStackSuffix, 1));
            item.m_quality = Mathf.Max(1, zdo.GetInt(SlotPrefabPrefix + slot + SlotQualitySuffix, item.m_quality));
            item.m_durability = zdo.GetFloat(SlotPrefabPrefix + slot + SlotDurabilitySuffix, item.m_durability);
            item.m_gridPos = new Vector2i(slot, 0);

            if (IsFoodItem(item))
                Inventory.AddItem(item);
        }

        _loaded = true;
        zdo.Set(LoadedKey, true);

        if (hadSavedSlots) ;
            //Debug.Log("[Agent FoodInventory] Loaded dedicated food inventory for " + gameObject.name);
    }

    private void SaveToZDO()
    {
        if (_zNetView == null || !_zNetView.IsValid() || !_zNetView.IsOwner())
            return;

        ZDO zdo = _zNetView.GetZDO();

        if (zdo == null || Inventory == null)
            return;

        for (int slot = 0; slot < Width; slot++)
        {
            ItemDrop.ItemData item = GetItemInSlot(slot);

            if (item == null || !IsFoodItem(item))
            {
                zdo.Set(SlotPrefabPrefix + slot, string.Empty);
                zdo.Set(SlotPrefabPrefix + slot + SlotStackSuffix, 0);
                zdo.Set(SlotPrefabPrefix + slot + SlotQualitySuffix, 0);
                zdo.Set(SlotPrefabPrefix + slot + SlotDurabilitySuffix, 0f);
                continue;
            }

            zdo.Set(SlotPrefabPrefix + slot, GetItemPrefabName(item));
            zdo.Set(SlotPrefabPrefix + slot + SlotStackSuffix, item.m_stack);
            zdo.Set(SlotPrefabPrefix + slot + SlotQualitySuffix, item.m_quality);
            zdo.Set(SlotPrefabPrefix + slot + SlotDurabilitySuffix, item.m_durability);
        }
    }

    private ItemDrop.ItemData GetItemInSlot(int slot)
    {
        if (Inventory == null)
            return null;

        foreach (ItemDrop.ItemData item in Inventory.GetAllItems())
        {
            if (item != null && item.m_gridPos.x == slot && item.m_gridPos.y == 0)
                return item;
        }

        return null;
    }

    private static string GetItemPrefabName(ItemDrop.ItemData item)
    {
        if (item == null)
            return string.Empty;

        if (item.m_dropPrefab != null)
            return item.m_dropPrefab.name;

        List<GameObject> items = ObjectDB.instance != null ? ObjectDB.instance.m_items : null;

        if (items == null || item.m_shared == null)
            return string.Empty;

        for (int i = 0; i < items.Count; i++)
        {
            ItemDrop drop = items[i] != null ? items[i].GetComponent<ItemDrop>() : null;

            if (drop != null && drop.m_itemData != null && drop.m_itemData.m_shared == item.m_shared)
                return items[i].name;
        }

        return string.Empty;
    }
}
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
