using Core.Agent;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class AgentFood : MonoBehaviour
{
    public struct FoodEffect
    {
        public string ItemName;
        public string ItemPrefabName;
        public float HealthBonus;
        public float StaminaBonus;
        public float EitrBonus;
        public float FoodRegen;
        public float RemainingTime;
        public float TotalTime;

        public bool IsActive
        {
            get { return RemainingTime > 0f && !string.IsNullOrEmpty(ItemName); }
        }
    }

    private enum MeadKind
    {
        Unknown,
        Health,
        Stamina
    }

    private const int MaxFoodSlotsCapacity = 5;
    private const int DefaultMaxFoodSlots = 3;
    private const float BaseStamina = 50f;
    private const float SaveInterval = 5f;
    private const float RemoteSyncInterval = 0.5f;
    private const float ConsumeCheckInterval = 1f;
    private const float FoodRegenInterval = 10f;
    private const float MeadCheckInterval = 2f;
    private const float MeadCooldown = 10f;
    private const float HealthMeadThreshold = 0.5f;
    private const float StaminaMeadThreshold = 0.25f;

    private static readonly string[] FoodKeys =
    {
        "agent_food_0",
        "agent_food_1",
        "agent_food_2",
        "agent_food_3",
        "agent_food_4"
    };

    private readonly FoodEffect[] _foods = new FoodEffect[MaxFoodSlotsCapacity];

    private AgentComponent _agent;
    private AgentFoodInventory _foodInventory;
    private ZNetView _zNetView;
    private Humanoid _humanoid;
    private Character _character;
    private SEMan _seMan;
    private ZSyncAnimation _zSyncAnimation;
    private AgentStamina _stamina;
    private bool _initialized;
    private float _saveTimer;
    private float _consumeTimer;
    private float _remoteSyncTimer;
    private float _foodRegenTimer;
    private float _meadCheckTimer;
    private float _meadCooldownTimer;

    public static int MaxFoodSlots
    {
        get { return DefaultMaxFoodSlots; }
    }

    public static float BaseStaminaValue
    {
        get { return BaseStamina; }
    }

    public float TotalHealthBonus
    {
        get
        {
            float total = 0f;

            for (int i = 0; i < MaxFoodSlots; i++)
            {
                if (_foods[i].IsActive)
                    total += GetScaledBonus(_foods[i].HealthBonus, _foods[i]);
            }

            return total;
        }
    }

    public float TotalStaminaBonus
    {
        get
        {
            float total = 0f;

            for (int i = 0; i < MaxFoodSlots; i++)
            {
                if (_foods[i].IsActive)
                    total += GetScaledBonus(_foods[i].StaminaBonus, _foods[i]);
            }

            return total;
        }
    }

    public float TotalEitrBonus
    {
        get
        {
            float total = 0f;

            for (int i = 0; i < MaxFoodSlots; i++)
            {
                if (_foods[i].IsActive)
                    total += GetScaledBonus(_foods[i].EitrBonus, _foods[i]);
            }

            return total;
        }
    }

    private void Awake()
    {
        RefreshReferences();
    }

    private void Start()
    {
        TryInit();
    }

    private void Update()
    {
        RefreshReferences();

        if (!_initialized)
        {
            TryInit();
            return;
        }

        if (_zNetView == null || !_zNetView.IsValid())
            return;

        if (!_zNetView.IsOwner())
        {
            _remoteSyncTimer -= Time.deltaTime;

            if (_remoteSyncTimer <= 0f)
            {
                _remoteSyncTimer = RemoteSyncInterval;
                LoadFromZDO();
            }

            return;
        }

        float dt = Time.deltaTime;
        bool changed = TickFoodTimers(dt);

        _foodRegenTimer += dt;

        if (_foodRegenTimer >= FoodRegenInterval)
        {
            _foodRegenTimer = 0f;
            ApplyFoodRegen();
        }

        _consumeTimer -= dt;

        if (_consumeTimer <= 0f)
        {
            _consumeTimer = ConsumeCheckInterval;

            if (TryAutoConsume())
                changed = true;
        }

        if (_meadCooldownTimer > 0f)
            _meadCooldownTimer -= dt;

        _meadCheckTimer -= dt;

        if (_meadCheckTimer <= 0f)
        {
            _meadCheckTimer = MeadCheckInterval;
            TryConsumeMeadFromNpcInventory();
        }

        _saveTimer += dt;

        if (_saveTimer >= SaveInterval || changed)
        {
            _saveTimer = 0f;
            SaveToZDO();
        }
    }

    private void OnDestroy()
    {
        if (_initialized)
            SaveToZDO();
    }

    public FoodEffect GetFood(int slot)
    {
        if (slot < 0 || slot >= MaxFoodSlots)
            return default(FoodEffect);

        return _foods[slot];
    }

    public bool HasAnyFood()
    {
        for (int i = 0; i < MaxFoodSlots; i++)
        {
            if (_foods[i].IsActive)
                return true;
        }

        return false;
    }

    public bool NeedsFood()
    {
        return FindEmptyFoodSlot() >= 0 || FindMostDepletedRefreshableSlot() >= 0;
    }

    public bool TryConsumeItem(ItemDrop.ItemData item)
    {
        TryInit();

        if (_zNetView == null || !_zNetView.IsOwner())
            return false;

        Inventory inventory = GetFoodInventory();

        if (inventory == null || item == null || !inventory.ContainsItem(item))
            return false;

        int slot = FindFoodSlotForItem(item);

        if (slot < 0)
            return false;

        bool consumed = ConsumeIntoSlot(inventory, item, slot);

        if (consumed)
            SaveToZDO();

        return consumed;
    }

    private void RefreshReferences()
    {
        if (_agent == null)
            _agent = GetComponent<AgentComponent>();

        if (_foodInventory == null)
            _foodInventory = GetComponent<AgentFoodInventory>();

        if (_zNetView == null)
            _zNetView = GetComponent<ZNetView>();

        if (_humanoid == null)
            _humanoid = GetComponent<Humanoid>();

        if (_character == null)
            _character = GetComponent<Character>();

        if (_character != null && _seMan == null)
            _seMan = _character.GetSEMan();

        if (_zSyncAnimation == null)
            _zSyncAnimation = GetComponent<ZSyncAnimation>();

        if (_stamina == null)
            _stamina = GetComponent<AgentStamina>();
    }

    private void TryInit()
    {
        RefreshReferences();

        if (_initialized)
            return;

        if (_zNetView == null || !_zNetView.IsValid())
            return;

        LoadFromZDO();
        _initialized = true;
        //Debug.Log("[Agent Food] Initialized activeFood=" + CountActiveFood() + "/" + MaxFoodSlots + " dedicatedInventory=" + (GetFoodInventory() != null));
    }

    private bool TickFoodTimers(float dt)
    {
        bool changed = false;

        for (int i = 0; i < MaxFoodSlots; i++)
        {
            FoodEffect food = _foods[i];

            if (!food.IsActive)
                continue;

            food.RemainingTime -= dt;

            if (food.RemainingTime <= 0f)
            {
                //Debug.Log("[Agent Food] Slot expired " + food.ItemName);
                _foods[i] = default(FoodEffect);
                changed = true;
            }
            else
            {
                _foods[i] = food;
            }
        }

        if (changed && _stamina != null)
            _stamina.ClampToMax();

        return changed;
    }

    private int CountActiveFood()
    {
        int count = 0;

        for (int i = 0; i < MaxFoodSlots; i++)
        {
            if (_foods[i].IsActive)
                count++;
        }

        return count;
    }

    private void ApplyFoodRegen()
    {
        if (_character == null)
            return;

        float regen = 0f;

        for (int i = 0; i < MaxFoodSlots; i++)
        {
            if (_foods[i].IsActive)
                regen += _foods[i].FoodRegen;
        }

        if (regen > 0f)
            _character.Heal(regen);
    }

    private bool TryAutoConsume()
    {
        Inventory inventory = GetFoodInventory();

        if (inventory == null)
            return false;

        bool changed = false;

        for (int i = 0; i < MaxFoodSlots; i++)
        {
            if (_foods[i].IsActive)
                continue;

            ItemDrop.ItemData item = FindFoodForSlot(inventory);

            if (item != null && ConsumeIntoSlot(inventory, item, i))
                changed = true;
        }

        return changed;
    }

    private bool ConsumeIntoSlot(Inventory inventory, ItemDrop.ItemData item, int slot)
    {
        if (inventory == null || item == null || item.m_shared == null || slot < 0 || slot >= MaxFoodSlots)
            return false;

        float duration = Mathf.Max(1f, item.m_shared.m_foodBurnTime);

        _foods[slot] = new FoodEffect
        {
            ItemName = item.m_shared.m_name,
            ItemPrefabName = GetItemPrefabName(item),
            HealthBonus = item.m_shared.m_food,
            StaminaBonus = item.m_shared.m_foodStamina,
            EitrBonus = item.m_shared.m_foodEitr,
            FoodRegen = item.m_shared.m_foodRegen,
            TotalTime = duration,
            RemainingTime = duration
        };

        inventory.RemoveOneItem(item);
        inventory.m_onChanged?.Invoke();

        if (_seMan != null && item.m_shared.m_consumeStatusEffect != null)
            _seMan.AddStatusEffect(item.m_shared.m_consumeStatusEffect, true);

        if (_zSyncAnimation != null)
            _zSyncAnimation.SetTrigger("eat");

        if (_humanoid != null && _humanoid.m_consumeItemEffects != null)
            _humanoid.m_consumeItemEffects.Create(transform.position, Quaternion.identity);

        if (_character != null && item.m_shared.m_food > 0f)
            _character.Heal(item.m_shared.m_food);

        if (_stamina != null)
        {
            _stamina.ClampToMax();
            _stamina.Restore(item.m_shared.m_foodStamina * 0.5f);
        }

        //Debug.Log("[Agent Food] Consumed " + item.m_shared.m_name + " slot=" + slot);
        return true;
    }

    private Inventory GetFoodInventory()
    {
        RefreshReferences();

        if (_foodInventory != null && _foodInventory.Inventory != null)
            return _foodInventory.Inventory;

        return null;
    }

    private Inventory GetNpcInventory()
    {
        if (_agent != null && _agent.ValheimContainer != null && _agent.ValheimContainer.m_inventory != null)
            return _agent.ValheimContainer.m_inventory;

        if (_humanoid != null)
            return _humanoid.GetInventory();

        return null;
    }

    private ItemDrop.ItemData FindFoodForSlot(Inventory inventory)
    {
        if (inventory == null)
            return null;

        List<ItemDrop.ItemData> items = inventory.GetAllItemsInGridOrder();

        for (int i = 0; i < items.Count; i++)
        {
            ItemDrop.ItemData item = items[i];

            if (CanConsumeFoodItem(item))
                return item;
        }

        return null;
    }

    private int FindFoodSlotForItem(ItemDrop.ItemData item)
    {
        int slot = FindEmptyFoodSlot();

        if (slot >= 0)
            return CanConsumeFoodItem(item) ? slot : -1;

        slot = FindRefreshableSlotForFood(item != null && item.m_shared != null ? item.m_shared.m_name : null);

        if (slot >= 0 && CanConsumeFoodItem(item, true))
            return slot;

        slot = FindMostDepletedRefreshableSlot();
        return slot >= 0 && CanConsumeFoodItem(item, true) ? slot : -1;
    }

    private bool CanConsumeFoodItem(ItemDrop.ItemData item, bool isRefresh = false)
    {
        if (!IsFoodItem(item))
            return false;

        if (!isRefresh && item.m_shared != null && IsFoodAlreadyActive(item.m_shared.m_name))
            return false;

        return CanApplyConsumeStatus(item);
    }

    private static bool IsFoodItem(ItemDrop.ItemData item)
    {
        if (item == null || item.m_shared == null)
            return false;

        if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
            return false;

        return item.m_shared.m_food > 0f || item.m_shared.m_foodStamina > 0f || item.m_shared.m_foodEitr > 0f;
    }

    private bool IsFoodAlreadyActive(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return false;

        for (int i = 0; i < MaxFoodSlots; i++)
        {
            if (_foods[i].IsActive && _foods[i].ItemName == itemName)
                return true;
        }

        return false;
    }

    private bool CanApplyConsumeStatus(ItemDrop.ItemData item)
    {
        if (_seMan == null || item == null || item.m_shared == null)
            return true;

        StatusEffect status = item.m_shared.m_consumeStatusEffect;

        if (status == null)
            return true;

        if (_seMan.HaveStatusEffect(status.NameHash()))
            return false;

        if (!string.IsNullOrEmpty(status.m_category) && _seMan.HaveStatusEffectCategory(status.m_category))
            return false;

        return true;
    }

    private int FindEmptyFoodSlot()
    {
        for (int i = 0; i < MaxFoodSlots; i++)
        {
            if (!_foods[i].IsActive)
                return i;
        }

        return -1;
    }

    private int FindRefreshableSlotForFood(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return -1;

        for (int i = 0; i < MaxFoodSlots; i++)
        {
            if (_foods[i].IsActive && _foods[i].ItemName == itemName && CanEatAgain(i))
                return i;
        }

        return -1;
    }

    private int FindMostDepletedRefreshableSlot()
    {
        int slot = -1;
        float bestRemaining = float.MaxValue;

        for (int i = 0; i < MaxFoodSlots; i++)
        {
            if (!CanEatAgain(i))
                continue;

            if (_foods[i].RemainingTime < bestRemaining)
            {
                bestRemaining = _foods[i].RemainingTime;
                slot = i;
            }
        }

        return slot;
    }

    private bool CanEatAgain(int slot)
    {
        if (slot < 0 || slot >= MaxFoodSlots)
            return false;

        FoodEffect food = _foods[slot];

        if (!food.IsActive)
            return false;

        return food.RemainingTime < Mathf.Max(1f, food.TotalTime) * 0.5f;
    }

    private void TryConsumeMeadFromNpcInventory()
    {
        if (_humanoid == null || _character == null || _meadCooldownTimer > 0f || _humanoid.InAttack() || _humanoid.InDodge() || _character.IsStaggering())
            return;

        Inventory inventory = GetNpcInventory();

        if (inventory == null)
            return;

        if (_seMan == null)
            _seMan = _character.GetSEMan();

        float healthPercent = _character.GetMaxHealth() > 0f ? _character.GetHealth() / _character.GetMaxHealth() : 1f;
        float staminaPercent = _stamina != null ? _stamina.GetStaminaPercentage() : 1f;

        if (healthPercent < HealthMeadThreshold)
        {
            ItemDrop.ItemData healthMead = FindMead(inventory, MeadKind.Health);

            if (healthMead != null && ConsumeMead(inventory, healthMead, MeadKind.Health))
                return;
        }

        if (staminaPercent < StaminaMeadThreshold)
        {
            ItemDrop.ItemData staminaMead = FindMead(inventory, MeadKind.Stamina);

            if (staminaMead != null)
                ConsumeMead(inventory, staminaMead, MeadKind.Stamina);
        }
    }

    private ItemDrop.ItemData FindMead(Inventory inventory, MeadKind wanted)
    {
        List<ItemDrop.ItemData> items = inventory.GetAllItemsInGridOrder();

        for (int i = 0; i < items.Count; i++)
        {
            ItemDrop.ItemData item = items[i];

            if (IsPotionItem(item) && ClassifyMead(item) == wanted && CanApplyConsumeStatus(item))
                return item;
        }

        return null;
    }

    private bool ConsumeMead(Inventory inventory, ItemDrop.ItemData item, MeadKind kind)
    {
        if (inventory == null || item == null || item.m_shared == null)
            return false;

        StatusEffect status = item.m_shared.m_consumeStatusEffect;

        if (status != null && _seMan != null)
            _seMan.AddStatusEffect(status, true);

        if (kind == MeadKind.Stamina && _stamina != null && status is SE_Stats stats && stats.m_staminaUpFront > 0f)
            _stamina.Restore(stats.m_staminaUpFront);

        if (_zSyncAnimation != null)
            _zSyncAnimation.SetTrigger("eat");

        if (_humanoid != null && _humanoid.m_consumeItemEffects != null)
            _humanoid.m_consumeItemEffects.Create(transform.position, Quaternion.identity);

        inventory.RemoveOneItem(item);
        inventory.m_onChanged?.Invoke();
        _meadCooldownTimer = MeadCooldown;
        //Debug.Log("[Agent Food] Mead consumed " + item.m_shared.m_name + " kind=" + kind);
        return true;
    }

    private static bool IsPotionItem(ItemDrop.ItemData item)
    {
        if (item == null || item.m_shared == null)
            return false;

        if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
            return false;

        if (item.m_shared.m_consumeStatusEffect == null)
            return false;

        return item.m_shared.m_food <= 0f && item.m_shared.m_foodStamina <= 0f && item.m_shared.m_foodEitr <= 0f;
    }

    private static MeadKind ClassifyMead(ItemDrop.ItemData item)
    {
        StatusEffect status = item != null && item.m_shared != null ? item.m_shared.m_consumeStatusEffect : null;

        if (status is SE_Stats stats)
        {
            if (stats.m_healthOverTime > 0f || stats.m_healthOverTimeDuration > 0f)
                return MeadKind.Health;

            if (stats.m_staminaOverTime > 0f || stats.m_staminaOverTimeDuration > 0f || stats.m_staminaUpFront > 0f)
                return MeadKind.Stamina;
        }

        string prefabName = item != null && item.m_dropPrefab != null ? item.m_dropPrefab.name : string.Empty;

        if (prefabName.IndexOf("Health", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return MeadKind.Health;

        if (prefabName.IndexOf("Stamina", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return MeadKind.Stamina;

        return MeadKind.Unknown;
    }

    private string SerializeFood(int slot)
    {
        FoodEffect food = _foods[slot];

        if (!food.IsActive)
            return string.Empty;

        return string.Join("|",
            food.ItemName,
            food.HealthBonus.ToString("F1", CultureInfo.InvariantCulture),
            food.StaminaBonus.ToString("F1", CultureInfo.InvariantCulture),
            food.EitrBonus.ToString("F1", CultureInfo.InvariantCulture),
            food.RemainingTime.ToString("F1", CultureInfo.InvariantCulture),
            food.TotalTime.ToString("F1", CultureInfo.InvariantCulture),
            food.ItemPrefabName ?? string.Empty,
            food.FoodRegen.ToString("F1", CultureInfo.InvariantCulture)
        );
    }

    private static FoodEffect DeserializeFood(string data)
    {
        if (string.IsNullOrEmpty(data))
            return default(FoodEffect);

        string[] parts = data.Split('|');

        if (parts.Length < 6)
            return default(FoodEffect);

        if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float health))
            return default(FoodEffect);

        if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float stamina))
            return default(FoodEffect);

        if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float eitr))
            return default(FoodEffect);

        if (!float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float remaining))
            return default(FoodEffect);

        if (!float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float total))
            return default(FoodEffect);

        float regen = 0f;

        if (parts.Length >= 8)
            float.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out regen);

        return new FoodEffect
        {
            ItemName = parts[0],
            HealthBonus = health,
            StaminaBonus = stamina,
            EitrBonus = eitr,
            RemainingTime = remaining,
            TotalTime = total,
            ItemPrefabName = parts.Length >= 7 ? parts[6] : string.Empty,
            FoodRegen = regen
        };
    }

    private void SaveToZDO()
    {
        if (_zNetView == null || !_zNetView.IsValid() || !_zNetView.IsOwner())
            return;

        ZDO zdo = _zNetView.GetZDO();

        if (zdo == null)
            return;

        for (int i = 0; i < MaxFoodSlotsCapacity; i++)
            zdo.Set(FoodKeys[i], SerializeFood(i));
    }

    private void LoadFromZDO()
    {
        if (_zNetView == null || !_zNetView.IsValid())
            return;

        ZDO zdo = _zNetView.GetZDO();

        if (zdo == null)
            return;

        for (int i = 0; i < MaxFoodSlotsCapacity; i++)
            _foods[i] = DeserializeFood(zdo.GetString(FoodKeys[i], string.Empty));
    }

    private static float GetScaledBonus(float baseBonus, FoodEffect food)
    {
        if (!food.IsActive)
            return 0f;

        float total = Mathf.Max(1f, food.TotalTime);
        float fraction = Mathf.Clamp01(food.RemainingTime / total);
        return baseBonus * Mathf.Pow(fraction, 0.3f);
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
            ItemDrop itemDrop = items[i] != null ? items[i].GetComponent<ItemDrop>() : null;

            if (itemDrop != null && itemDrop.m_itemData != null && itemDrop.m_itemData.m_shared == item.m_shared)
                return items[i].name;
        }

        return string.Empty;
    }
}
