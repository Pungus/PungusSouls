using Core.Agent;
using System.Reflection;
using UnityEngine;

public class AgentItemPickup : MonoBehaviour
{
    private const string ZDO_AutoPickup = "agent_auto_pickup";
    private const float PickupRange = 3.5f;
    private const float PickupInterval = 0.35f;
    private const float FullInventoryMessageInterval = 3f;
    private const int BufferSize = 96;

    private readonly Collider[] _buffer = new Collider[BufferSize];
    private AgentComponent _agent;
    private ZNetView _zNetView;
    private Character _character;
    private MonsterAI _monsterAI;
    private AgentSkills _skills;
    private float _pickupTimer;
    private float _fullInventoryMessageTimer;
    private int _itemMask;

    public bool AutoPickupEnabled
    {
        get
        {
            if (_zNetView == null || !_zNetView.IsValid())
                return true;

            ZDO zdo = _zNetView.GetZDO();
            return zdo == null || zdo.GetBool(ZDO_AutoPickup, true);
        }
        set
        {
            if (_zNetView == null || !_zNetView.IsValid())
                return;

            ZDO zdo = _zNetView.GetZDO();

            if (zdo != null)
                zdo.Set(ZDO_AutoPickup, value);
        }
    }

    private void Awake()
    {
        RefreshReferences();
        _itemMask = LayerMask.GetMask("item");
    }

    private void Update()
    {
        RefreshReferences();

        if (_agent == null || _agent.Context == null)
            return;

        if (_zNetView == null || !_zNetView.IsValid() || !_zNetView.IsOwner())
            return;

        if (_character == null || _character.IsDead())
            return;

        if (!AutoPickupEnabled || AgentContainerComponent.IsRadialOpen || IsSleeping())
            return;

        Inventory inventory = GetInventory();

        if (inventory == null)
            return;

        _pickupTimer -= Time.deltaTime;
        _fullInventoryMessageTimer -= Time.deltaTime;

        if (_pickupTimer > 0f)
            return;

        _pickupTimer = PickupInterval;
        UpdatePickup(inventory);
    }

    public void SetAutoPickup(bool enabled)
    {
        AutoPickupEnabled = enabled;
    }

    private void RefreshReferences()
    {
        if (_agent == null)
            _agent = GetComponent<AgentComponent>();

        if (_zNetView == null)
            _zNetView = GetComponent<ZNetView>();

        if (_character == null)
            _character = GetComponent<Character>();

        if (_monsterAI == null)
            _monsterAI = GetComponent<MonsterAI>();

        if (_skills == null)
            _skills = GetComponent<AgentSkills>();
    }

    private void UpdatePickup(Inventory inventory)
    {
        Vector3 center = transform.position + Vector3.up * 0.8f;
        int count = _itemMask != 0
            ? Physics.OverlapSphereNonAlloc(center, PickupRange, _buffer, _itemMask, QueryTriggerInteraction.Collide)
            : Physics.OverlapSphereNonAlloc(center, PickupRange, _buffer, ~0, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider collider = _buffer[i];
            _buffer[i] = null;

            if (collider == null)
                continue;

            ItemDrop drop = GetItemDrop(collider);

            if (!IsValidPickup(drop))
                continue;

            if (!drop.CanPickup())
            {
                drop.RequestOwn();
                continue;
            }

            drop.Load();

            if (drop.m_itemData == null || drop.m_itemData.m_shared == null)
                continue;

            TryPickupDrop(drop, inventory);
        }
    }

    private ItemDrop GetItemDrop(Collider collider)
    {
        if (collider == null)
            return null;

        if (collider.attachedRigidbody != null)
        {
            ItemDrop attachedDrop = collider.attachedRigidbody.GetComponent<ItemDrop>();

            if (attachedDrop != null)
                return attachedDrop;
        }

        ItemDrop directDrop = collider.GetComponent<ItemDrop>();

        if (directDrop != null)
            return directDrop;

        return collider.GetComponentInParent<ItemDrop>();
    }

    private bool IsValidPickup(ItemDrop drop)
    {
        if (drop == null || drop.gameObject == null)
            return false;

        if (!drop.m_autoPickup || drop.InTar())
            return false;

        ZNetView view = drop.GetComponent<ZNetView>();
        return view != null && view.IsValid();
    }

    public bool TryPickupDrop(ItemDrop drop, Inventory inventory)
    {
        if (drop == null || inventory == null || drop.m_itemData == null)
            return false;

        ItemDrop.ItemData item = drop.m_itemData.Clone();

        if (item == null)
            return true;

        float maxWeight = _agent != null ? _agent.MaxCarryWeight : 300f;

        if (inventory.GetTotalWeight() + item.GetWeight() > maxWeight)
        {
            ShowFullMessage("Too heavy for NPC");
            return false;
        }

        if (!inventory.AddItem(item))
        {
            ShowFullMessage("NPC inventory full");
            return false;
        }

        DestroyDrop(drop);

        if (_skills != null)
            _skills.RaiseSkill("pickup", Mathf.Max(1, item.m_stack));

        return true;
    }

    private void DestroyDrop(ItemDrop drop)
    {
        if (drop == null || drop.gameObject == null)
            return;

        ZNetView view = drop.GetComponent<ZNetView>();

        if (view != null && view.IsValid() && !view.IsOwner())
            view.ClaimOwnership();

        if (ZNetScene.instance != null)
            ZNetScene.instance.Destroy(drop.gameObject);
        else
            Destroy(drop.gameObject);
    }

    private Inventory GetInventory()
    {
        return _agent != null && _agent.ValheimContainer != null ? _agent.ValheimContainer.m_inventory : null;
    }

    private void ShowFullMessage(string message)
    {
        if (_fullInventoryMessageTimer > 0f)
            return;

        _fullInventoryMessageTimer = FullInventoryMessageInterval;
        MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, message);
    }

    private bool IsSleeping()
    {
        if (_monsterAI == null)
            return false;

        FieldInfo field = FindField(_monsterAI.GetType(), "m_sleeping");

        if (field == null || field.FieldType != typeof(bool))
            return false;

        return (bool)field.GetValue(_monsterAI);
    }

    private static FieldInfo FindField(System.Type type, string name)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field != null)
                return field;

            type = type.BaseType;
        }

        return null;
    }
}
