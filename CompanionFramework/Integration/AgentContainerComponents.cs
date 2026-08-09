using Core.Agent;
using HarmonyLib;
using Modules.HomeZone;
using System.Collections.Generic;
using System.Reflection;
using Systems.Interaction;
using Systems.Inventory;
using UnityEngine;
using static Core.Agent.AgentContext;

public class AgentContainerComponent : MonoBehaviour, Hoverable
{
    public static bool IsRadialOpen;
    public static float BlockInputUntil;

    private AgentContainer _container;
    private LineRenderer _homeZoneRing;
    private float _currentRadius = 10f;
    private RadialMenu _activeMenu;
    private bool _radiusPreviewDirty;

    private const float DirectRadialRange = 1.8f;
    private const float DirectRadialMaxAngle = 35f;

    public void Init(AgentContainer container)
    {
        _container = container;
    }

    private void Awake()
    {
        ////Debug.Log("[AgentContainerComponent] AWAKE live on " + gameObject.name);
    }

    [HarmonyPatch(typeof(Player), "TakeInput")]
    public static class Player_TakeInput_Patch
    {
        private static bool Prefix()
        {
            return !IsRadialOpen;
        }
    }

    [HarmonyPatch(typeof(GameCamera), "UpdateCamera")]
    public static class GameCamera_UpdateCamera_Patch
    {
        private static bool Prefix()
        {
            return !IsRadialOpen;
        }
    }

    [HarmonyPatch(typeof(Hud), "InRadial")]
    public static class Hud_InRadial_Patch
    {
        private static void Postfix(ref bool __result)
        {
            if (!__result && IsRadialOpen)
                __result = true;
        }
    }

    [HarmonyPatch(typeof(Player), "SetControls")]
    public static class Player_SetControls_Patch
    {
        private static void Prefix(ref Vector3 movedir, ref bool attack, ref bool attackHold, ref bool secondaryAttack, ref bool secondaryAttackHold, ref bool block, ref bool blockHold, ref bool jump, ref bool crouch, ref bool run, ref bool autoRun, ref bool dodge)
        {
            if (!IsRadialOpen && Time.time > BlockInputUntil)
                return;

            movedir = Vector3.zero;
            attack = false;
            attackHold = false;
            secondaryAttack = false;
            secondaryAttackHold = false;
            block = false;
            blockHold = false;
            jump = false;
            crouch = false;
            run = false;
            autoRun = false;
            dodge = false;
        }
    }

    public string GetHoverText()
    {
        return $"{GetHoverName()}\n[E] Open Inventory\n[Shift + E] Mode";
    }

    public string GetHoverName()
    {
        return gameObject.name;
    }

    private void WakeAssignedBedRest(string reason)
    {
        AgentAssignedBedRestController rest = GetComponent<AgentAssignedBedRestController>();

        if (rest != null)
            rest.WakeFromAssignedBed(reason);
    }

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        ZNetView nview = GetComponent<ZNetView>();

        if (nview != null && nview.IsValid() && !nview.IsOwner())
        {
            nview.ClaimOwnership();
            ////Debug.Log("[Agent] Claimed ownership for interaction");
        }

        WakeAssignedBedRest(alt ? "player radial" : "player interaction");

        if (alt)
        {
            OpenRadialMenu();
            return true;
        }

        return _container != null && _container.Interact();
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    private void OpenRadialMenu()
    {
        WakeAssignedBedRest("player radial");

        if (_activeMenu != null)
            return;

        if (Hud.instance == null || Hud.instance.m_rootObject == null)
        {
            //Debug.LogError("[Agent] Hud root is unavailable");
            return;
        }

        GameObject go = new GameObject("RadialMenu");
        go.transform.SetParent(Hud.instance.m_rootObject.transform, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(760f, 760f);

        _activeMenu = go.AddComponent<RadialMenu>();
        _activeMenu.Init(BuildRadialEntries());
        LockPlayerControl();
        IsRadialOpen = true;
    }

    private List<RadialMenuEntry> BuildRadialEntries()
    {
        AgentComponent agent = GetComponent<AgentComponent>();
        List<RadialMenuEntry> entries = new List<RadialMenuEntry>();

        if (agent == null || agent.Context == null)
        {
            entries.Add(new RadialMenuEntry("Close", CloseRadialMenu));
            return entries;
        }

        RadialMenuEntry tactics = new RadialMenuEntry("Tactics");
        tactics.Add(agent.Context.BehaviourMode == AgentBehaviourMode.Passive ? "Passive ✓" : "Passive", () => SetBehaviour(agent, AgentBehaviourMode.Passive));
        tactics.Add(agent.Context.BehaviourMode == AgentBehaviourMode.Defensive ? "Defensive ✓" : "Defensive", () => SetBehaviour(agent, AgentBehaviourMode.Defensive));
        tactics.Add(agent.Context.BehaviourMode == AgentBehaviourMode.Aggressive ? "Aggressive ✓" : "Aggressive", () => SetBehaviour(agent, AgentBehaviourMode.Aggressive));
        entries.Add(tactics);

        RadialMenuEntry state = new RadialMenuEntry("State");
        state.Add(agent.Context.StateMode == AgentStateMode.Idle ? "Idle ✓" : "Idle", () => SetState(agent, AgentStateMode.Idle));
        state.Add(agent.Context.StateMode == AgentStateMode.Follow ? "Follow ✓" : "Follow", () => SetState(agent, AgentStateMode.Follow));
        state.Add(agent.Context.StateMode == AgentStateMode.StayHome ? "Stay Home ✓" : "Stay Home", () => SetStayHomeState(agent));
        entries.Add(state);

        RadialMenuEntry followDistance = new RadialMenuEntry($"Follow {agent.Context.FollowStopDistance:0.0}m");
        followDistance.Add("Follow -", () => ChangeFollowDistance(agent, -0.5f));
        followDistance.Add("Follow +", () => ChangeFollowDistance(agent, 0.5f));
        entries.Add(followDistance);

        if (!_radiusPreviewDirty)
            SyncCurrentRadiusFromHome(agent);

        RadialMenuEntry area = new RadialMenuEntry($"Area {_currentRadius:0}m");
        area.Add("Radius -", () => ChangeRadius(agent, -5f));
        area.Add("Radius +", () => ChangeRadius(agent, 5f));

        if (HasHome(agent))
        {
            area.Add("Update Radius", () => SetHomeRadiusOnly(agent));
            area.Add("Move Home Here", () => SetHomeAreaHere(agent));
        }
        else
        {
            area.Add("Set Home Area Here", () => SetHomeAreaHere(agent));
        }

        entries.Add(area);

        RadialMenuEntry tasks = new RadialMenuEntry("Tasks");
        tasks.Add(agent.Context.TaskMode == AgentTaskMode.None ? "No Task ✓" : "No Task", () => SetTask(agent, AgentTaskMode.None));
        tasks.Add(agent.Context.TaskMode == AgentTaskMode.Hunt ? "Hunt ✓" : "Hunt", () => SetTask(agent, AgentTaskMode.Hunt));
        tasks.Add(agent.Context.TaskMode == AgentTaskMode.Patrol ? "Patrol ✓" : "Patrol", () => SetPatrolTask(agent));
        tasks.Add(agent.Context.TaskMode == AgentTaskMode.Cook ? "Cooking ✓" : "Cooking", () => SetCookingTask(agent));
        tasks.Add("Configure Cooking", () => OpenCookingConfiguration(agent));
        entries.Add(tasks);

        RadialMenuEntry work = new RadialMenuEntry("Work");
        work.Add(agent.Context.TaskMode == AgentTaskMode.Gather ? "Gathering ✓" : "Gathering", () => SetResourceTask(agent, AgentTaskMode.Gather));
        work.Add(agent.Context.TaskMode == AgentTaskMode.Lumbering ? "Lumbering ✓" : "Lumbering", () => SetResourceTask(agent, AgentTaskMode.Lumbering));
        work.Add(agent.Context.TaskMode == AgentTaskMode.Mining ? "Mining ✓" : "Mining", () => SetResourceTask(agent, AgentTaskMode.Mining));
        work.Add(agent.Context.TaskMode == AgentTaskMode.Quarrying ? "Quarrying ✓" : "Quarrying", () => SetResourceTask(agent, AgentTaskMode.Quarrying));
        work.Add(agent.Context.TaskMode == AgentTaskMode.Repair ? "Repair ✓" : "Repair", () => SetRepairTask(agent));
        work.Add(agent.Context.TaskMode == AgentTaskMode.Fishing ? "Fishing ✓" : "Fishing", () => SetFishingTask(agent));
        work.Add(agent.Context.TaskMode == AgentTaskMode.Smelting ? "Smelting ✓" : "Smelting", () => SetSmeltingTask(agent));
        work.Add(agent.Context.TaskMode == AgentTaskMode.Farming ? "Farming ✓" : "Farming", () => SetFarmingTask(agent));
        entries.Add(work);

        RadialMenuEntry clear = new RadialMenuEntry("Clear");
        clear.Add("Clear Task", () => SetTask(agent, AgentTaskMode.None));
        clear.Add("Clear Work Orders", () => ClearAllWorkOrders(agent));
        entries.Add(clear);

        entries.Add(new RadialMenuEntry("Close", CloseRadialMenu));

        return entries;
    }
    private void ClearAllWorkOrders(AgentComponent agent)
    {
        if (agent == null)
            return;

        ZNetView view = agent.GetComponent<ZNetView>();

        if (view != null && view.IsValid() && !view.IsOwner())
            view.ClaimOwnership();

        if (view == null || !view.IsValid())
            return;

        ZDO zdo = view.GetZDO();

        if (zdo == null)
            return;

        AgentCookingOrderStore.Clear(zdo);

        if (agent.Context != null)
            agent.SetTaskMode(AgentTaskMode.None);

        ShowMessage("Work orders cleared");
        RefreshRadial();
    }
    private void SetFarmingTask(AgentComponent agent)
    {
        if (agent == null || agent.Context == null)
            return;

        if (!HasHome(agent))
        {
            ShowMessage("Set home radius first");
            return;
        }

        ClearTameableFollow();
        agent.SetStateMode(AgentStateMode.StayHome);
        agent.SetTaskMode(AgentTaskMode.Farming);
        ShowMessage("Task: Farming");
        RefreshRadial();
    }
    private void SetSmeltingTask(AgentComponent agent)
    {
        if (agent == null || agent.Context == null)
            return;

        if (!HasHome(agent))
        {
            ShowMessage("Set home radius first");
            return;
        }

        ClearTameableFollow();
        agent.SetStateMode(AgentStateMode.StayHome);
        agent.SetTaskMode(AgentTaskMode.Smelting);
        ShowMessage("Task: Smelting");
        RefreshRadial();
    }
    private void SetFishingTask(AgentComponent agent)
    {
        if (agent == null || agent.Context == null)
            return;

        ClearTameableFollow();
        agent.SetTaskMode(AgentTaskMode.Fishing);
        ShowMessage("Task: Fishing");
        RefreshRadial();
    }
    private void SetRepairTask(AgentComponent agent)
    {
        if (agent == null || agent.Context == null)
            return;

        if (!HasHome(agent))
        {
            ShowMessage("Set home radius first");
            return;
        }

        ClearTameableFollow();
        agent.SetStateMode(AgentStateMode.StayHome);
        agent.SetTaskMode(AgentTaskMode.Repair);
        ShowMessage("Task: Repair");
        RefreshRadial();
    }

    private void SetResourceTask(AgentComponent agent, AgentTaskMode mode)
    {
        if (agent == null || agent.Context == null)
            return;

        if (mode != AgentTaskMode.Lumbering &&
            mode != AgentTaskMode.Mining &&
            mode != AgentTaskMode.Quarrying &&
            mode != AgentTaskMode.Gather)
        {
            return;
        }

        agent.SetTaskMode(mode);

        ShowMessage("Task: " + mode);
        RefreshRadial();
    }
    private void OpenCookingConfiguration(AgentComponent agent)
    {
        if (agent == null || agent.Context == null)
            return;

        if (!HasHome(agent))
        {
            ShowMessage("Set home radius first");
            return;
        }

        CloseRadialMenu();
        AgentCookingAssignmentPanel.Open(agent);
    }
    private void SetCookingTask(AgentComponent agent)
    {
        if (agent == null || agent.Context == null)
            return;

        if (!HasHome(agent))
        {
            ShowMessage("Set home radius first");
            return;
        }

        ClearTameableFollow();
        agent.SetStateMode(AgentStateMode.StayHome);
        agent.SetTaskMode(AgentTaskMode.Cook);
        ShowMessage("Task: Cooking");
        RefreshRadial();
    }

    private void PauseCookingTask(AgentComponent agent)
    {
        if (agent == null || agent.Context == null)
            return;

        if (agent.Context.TaskMode == AgentTaskMode.Cook)
            agent.SetTaskMode(AgentTaskMode.None);

        ShowMessage("Cooking paused");
        RefreshRadial();
    }

    private void ClearCookingOrders(AgentComponent agent)
    {
        if (agent == null)
            return;

        ZNetView view = agent.GetComponent<ZNetView>();

        if (view != null && view.IsValid() && !view.IsOwner())
            view.ClaimOwnership();

        if (view == null || !view.IsValid())
            return;

        ZDO zdo = view.GetZDO();

        if (zdo == null)
            return;

        AgentCookingOrderStore.Clear(zdo);

        if (agent.Context != null && agent.Context.TaskMode == AgentTaskMode.Cook)
            agent.SetTaskMode(AgentTaskMode.None);

        ShowMessage("Cooking orders cleared");
        RefreshRadial();
    }

    private void SetGatherTask(AgentComponent agent)
    {
        if (agent == null || agent.Context == null)
            return;

        if (!HasHome(agent))
        {
            ShowMessage("Set home radius first");
            return;
        }

        ClearTameableFollow();
        agent.SetStateMode(AgentStateMode.StayHome);
        agent.SetTaskMode(AgentTaskMode.Gather);
        ShowMessage("Task: Gather");
        RefreshRadial();
    }

    private void ClearGatherOrders(AgentComponent agent)
    {
        if (agent == null)
            return;

        if (agent.Context != null && agent.Context.TaskMode == AgentTaskMode.Gather)
            agent.SetTaskMode(AgentTaskMode.None);

        ShowMessage("Gathering cleared");
        RefreshRadial();
    }
    private void SetCookingTask()
    {
        AgentComponent agent = GetComponent<AgentComponent>();

        if (agent == null || agent.Context == null)
            return;

        agent.Context.TaskMode = Core.Agent.AgentContext.AgentTaskMode.Cook;
        agent.Context.SyncLegacyFields();
        agent.SaveBehaviourStateToZDO();

        MessageHud.instance?.ShowMessage(
            MessageHud.MessageType.Center,
            "Cooking resumed");
    }
    private void AddGatherItemIfDiscovered(RadialMenuEntry stayHome, AgentComponent agent, string prefabName, string label, int targetAmount)
    {
        if (!PlayerHasDiscoveredItem(prefabName))
            return;

        stayHome.Add("Gather " + label, () => SetGatherItemTask(agent, prefabName, label, targetAmount));
    }

    private void SetGatherItemTask(AgentComponent agent, string prefabName, string label, int targetAmount)
    {
        if (agent == null || agent.Context == null)
            return;

        if (!HasHome(agent))
        {
            ShowMessage("Set home radius first");
            return;
        }

        ClearTameableFollow();
        agent.SetStateMode(AgentStateMode.StayHome);
        agent.SetTaskMode(AgentTaskMode.Gather);

        ShowMessage("Task: Gathering");
        RefreshRadial();
    }

    private void PauseCookingTask()
    {
        AgentComponent agent = GetComponent<AgentComponent>();

        if (agent == null || agent.Context == null)
            return;

        if (agent.Context.TaskMode == Core.Agent.AgentContext.AgentTaskMode.Cook)
        {
            agent.Context.TaskMode = Core.Agent.AgentContext.AgentTaskMode.None;
            agent.Context.SyncLegacyFields();
            agent.SaveBehaviourStateToZDO();
        }

        MessageHud.instance?.ShowMessage(
            MessageHud.MessageType.Center,
            "Cooking paused");
    }

    private void ClearCookingOrders()
    {
        AgentComponent agent = GetComponent<AgentComponent>();

        if (agent == null)
            return;

        ZNetView view = agent.GetComponent<ZNetView>();

        if (view != null && view.IsValid() && !view.IsOwner())
            view.ClaimOwnership();

        if (view == null || !view.IsValid())
            return;

        ZDO zdo = view.GetZDO();

        if (zdo == null)
            return;

        AgentCookingOrderStore.Clear(zdo);

        if (agent.Context != null && agent.Context.TaskMode == Core.Agent.AgentContext.AgentTaskMode.Cook)
        {
            agent.Context.TaskMode = Core.Agent.AgentContext.AgentTaskMode.None;
            agent.Context.SyncLegacyFields();
            agent.SaveBehaviourStateToZDO();
        }

        MessageHud.instance?.ShowMessage(
            MessageHud.MessageType.Center,
            "Cooking orders cleared");
    }

    private void AddGatherRadialOptions(RadialMenuEntry stayHome, AgentComponent agent)
    {
        List<GatherRadialOption> options = GetDiscoveredGatherOptions();

        if (options.Count == 0)
        {
            stayHome.Add("No gather items", () => ShowMessage("No discovered gatherable items"));
            return;
        }

        for (int i = 0; i < options.Count; i++)
        {
            GatherRadialOption option = options[i];
            stayHome.Add("Gather " + option.Label, () => AssignGatherOrder(agent, option));
        }

        stayHome.Add(agent.Context.TaskMode == AgentTaskMode.Gather ? "Gathering ✓" : "Resume Gathering", () => ResumeGathering(agent));
        stayHome.Add("Clear Gathering", () => ClearGatherOrders(agent));
    }

    private sealed class GatherRadialOption
    {
        public string PrefabName;
        public string Label;
        public int TargetAmount;

        public GatherRadialOption(string prefabName, string label, int targetAmount)
        {
            PrefabName = prefabName;
            Label = label;
            TargetAmount = targetAmount;
        }
    }

    private List<GatherRadialOption> GetDiscoveredGatherOptions()
    {
        List<GatherRadialOption> options = new List<GatherRadialOption>();

        AddIfDiscovered(options, "Wood", "Wood", 100);
        AddIfDiscovered(options, "FineWood", "Fine Wood", 50);
        AddIfDiscovered(options, "RoundLog", "Core Wood", 50);
        AddIfDiscovered(options, "Stone", "Stone", 100);
        AddIfDiscovered(options, "Flint", "Flint", 50);
        AddIfDiscovered(options, "CopperOre", "Copper Ore", 30);
        AddIfDiscovered(options, "TinOre", "Tin Ore", 30);
        AddIfDiscovered(options, "IronScrap", "Scrap Iron", 30);
        AddIfDiscovered(options, "SilverOre", "Silver Ore", 30);
        AddIfDiscovered(options, "Raspberry", "Raspberries", 50);
        AddIfDiscovered(options, "Blueberries", "Blueberries", 50);
        AddIfDiscovered(options, "Mushroom", "Mushrooms", 50);
        AddIfDiscovered(options, "Thistle", "Thistle", 50);
        AddIfDiscovered(options, "Dandelion", "Dandelion", 50);

        return options;
    }

    private void AddIfDiscovered(List<GatherRadialOption> options, string prefabName, string label, int targetAmount)
    {
        if (!PlayerHasDiscoveredItem(prefabName))
            return;

        options.Add(new GatherRadialOption(prefabName, label, targetAmount));
    }
    private bool PlayerHasDiscoveredItem(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
            return false;

        Player player = Player.m_localPlayer;

        if (player == null)
            return false;

        ItemDrop itemDrop = GetItemDrop(prefabName);
        string sharedName = itemDrop != null && itemDrop.m_itemData != null && itemDrop.m_itemData.m_shared != null
            ? itemDrop.m_itemData.m_shared.m_name
            : prefabName;

        if (CallPlayerKnownMaterial(player, sharedName))
            return true;

        if (CallPlayerKnownMaterial(player, prefabName))
            return true;

        Inventory inventory = player.GetInventory();

        if (inventory != null && InventoryContainsPrefab(inventory, prefabName))
            return true;

        return false;
    }

    private bool CallPlayerKnownMaterial(Player player, string itemName)
    {
        if (player == null || string.IsNullOrEmpty(itemName))
            return false;

        MethodInfo method = player.GetType().GetMethod(
            "IsKnownMaterial",
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        if (method == null || method.ReturnType != typeof(bool))
            return false;

        ParameterInfo[] parameters = method.GetParameters();

        if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
            return false;

        try
        {
            object value = method.Invoke(player, new object[] { itemName });
            return value is bool result && result;
        }
        catch
        {
            return false;
        }
    }

    private bool KnownMaterialFieldContains(Player player, string itemName)
    {
        if (player == null || string.IsNullOrEmpty(itemName))
            return false;

        FieldInfo field = player.GetType().GetField(
            "m_knownMaterial",
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        if (field == null)
            return false;

        object value = field.GetValue(player);

        if (value is System.Collections.IEnumerable enumerable)
        {
            foreach (object entry in enumerable)
            {
                string text = entry as string;

                if (string.IsNullOrEmpty(text))
                    continue;

                if (text == itemName)
                    return true;
            }
        }

        return false;
    }

    private bool InventoryContainsPrefab(Inventory inventory, string prefabName)
    {
        if (inventory == null || string.IsNullOrEmpty(prefabName))
            return false;

        List<ItemDrop.ItemData> items = inventory.GetAllItems();

        for (int i = 0; i < items.Count; i++)
        {
            ItemDrop.ItemData item = items[i];

            if (item == null)
                continue;

            if (item.m_dropPrefab != null && item.m_dropPrefab.name == prefabName)
                return true;

            ItemDrop itemDrop = GetItemDrop(prefabName);

            if (itemDrop != null &&
                itemDrop.m_itemData != null &&
                itemDrop.m_itemData.m_shared != null &&
                item.m_shared == itemDrop.m_itemData.m_shared)
                return true;
        }

        return false;
    }

    private string GetItemDisplayName(string prefabName, string fallback)
    {
        ItemDrop itemDrop = GetItemDrop(prefabName);

        if (itemDrop != null &&
            itemDrop.m_itemData != null &&
            itemDrop.m_itemData.m_shared != null &&
            !string.IsNullOrEmpty(itemDrop.m_itemData.m_shared.m_name))
        {
            string token = itemDrop.m_itemData.m_shared.m_name;
            return Localization.instance != null ? Localization.instance.Localize(token) : token;
        }

        return fallback;
    }

    private ItemDrop GetItemDrop(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
            return null;

        GameObject prefab = null;

        if (ObjectDB.instance != null)
            prefab = ObjectDB.instance.GetItemPrefab(prefabName);

        if (prefab == null && ZNetScene.instance != null)
            prefab = ZNetScene.instance.GetPrefab(prefabName);

        return prefab != null ? prefab.GetComponent<ItemDrop>() : null;
    }

    private void AssignGatherOrder(AgentComponent agent, GatherRadialOption option)
    {
        if (agent == null || agent.Context == null || option == null)
            return;

        if (!HasHome(agent))
        {
            ShowMessage("Set home radius first");
            return;
        }

        ClearTameableFollow();
        agent.SetStateMode(AgentStateMode.StayHome);
        agent.SetTaskMode(AgentTaskMode.Gather);

        ShowMessage("Task: Gathering");
        RefreshRadial();
    }

    private void ResumeGathering(AgentComponent agent)
    {
        if (agent == null || agent.Context == null)
            return;

        if (!HasHome(agent))
        {
            ShowMessage("Set home radius first");
            return;
        }

        ClearTameableFollow();
        agent.SetStateMode(AgentStateMode.StayHome);
        agent.SetTaskMode(AgentTaskMode.Gather);
        ShowMessage("Gathering resumed");
        RefreshRadial();
    }

    private void SetBehaviour(AgentComponent agent, AgentBehaviourMode mode)
    {
        agent.SetBehaviourMode(mode);
        ShowMessage("Tactics: " + mode);
        RefreshRadial();
    }

    private void SetState(AgentComponent agent, AgentStateMode mode)
    {
        ClearTameableFollow();
        agent.SetStateMode(mode);
        ShowMessage("State: " + mode);
        RefreshRadial();
    }

    private void SetStayHomeState(AgentComponent agent)
    {
        if (!HasHome(agent))
        {
            ShowMessage("Set home radius first");
            return;
        }

        ClearTameableFollow();
        agent.SetStateMode(AgentStateMode.StayHome);
        ShowMessage("State: Stay Home");
        RefreshRadial();
    }

    private void SetTask(AgentComponent agent, AgentTaskMode mode)
    {
        agent.SetTaskMode(mode);
        ShowMessage("Task: " + mode);
        RefreshRadial();
    }

    private void SetPatrolTask(AgentComponent agent)
    {
        if (!HasHome(agent))
        {
            ShowMessage("Set home radius first");
            return;
        }

        agent.SetStateMode(AgentStateMode.StayHome);
        agent.SetTaskMode(AgentTaskMode.Patrol);
        ShowMessage("Task: Patrol");
        RefreshRadial();
    }

    private void ChangeFollowDistance(AgentComponent agent, float delta)
    {
        if (agent == null || agent.Context == null)
            return;

        float stopDistance = Mathf.Clamp(agent.Context.FollowStopDistance + delta, 1f, 20f);
        float resumeDistance = Mathf.Clamp(stopDistance + 1f, stopDistance + 0.5f, 25f);
        agent.SaveFollowDistanceToZDO(stopDistance, resumeDistance);
        ShowMessage($"Follow distance: {stopDistance:0.0}m");
        RefreshRadial();
    }

    private bool HasHome(AgentComponent agent)
    {
        return agent != null && agent.Context != null && agent.Context.HomeZone != null && agent.Context.HomeZone.IsSet;
    }

    private void ClearTameableFollow()
    {
        Tameable tameable = GetComponent<Tameable>();

        if (tameable == null)
            return;

        if (!IsTameableFollowing(tameable))
            return;

        Player player = Player.m_localPlayer;

        if (player != null)
            InvokeTameableCommand(tameable, player);
    }

    private bool IsTameableFollowing(Tameable tameable)
    {
        if (tameable == null)
            return false;

        MethodInfo method = typeof(Tameable).GetMethod("IsFollowing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (method != null && method.ReturnType == typeof(bool))
        {
            object value = method.Invoke(tameable, null);

            if (value is bool result)
                return result;
        }

        FieldInfo followPlayerField = typeof(Tameable).GetField("m_followPlayer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (followPlayerField != null)
        {
            object value = followPlayerField.GetValue(tameable);

            if (value is long longValue)
                return longValue != 0L;

            if (value is ZDOID zdoidValue)
                return !zdoidValue.IsNone();
        }

        return false;
    }

    private bool InvokeTameableCommand(Tameable tameable, Player player)
    {
        MethodInfo command = typeof(Tameable).GetMethod("Command", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return command != null && InvokeReflectedMethod(command, tameable, player);
    }

    private bool InvokeReflectedMethod(MethodInfo method, object instance, Player player)
    {
        ParameterInfo[] parameters = method.GetParameters();
        object[] args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            System.Type type = parameters[i].ParameterType;

            if (type == typeof(Player) || type == typeof(Humanoid) || type == typeof(Character))
                args[i] = player;
            else if (type == typeof(bool))
                args[i] = false;
            else if (type == typeof(string))
                args[i] = string.Empty;
            else if (type.IsValueType)
                args[i] = System.Activator.CreateInstance(type);
            else
                args[i] = null;
        }

        try
        {
            method.Invoke(instance, args);
            return true;
        }
        catch (System.Exception ex)
        {
            //Debug.LogWarning("[Agent] Tameable command reflection failed: " + ex.GetType().Name + " " + ex.Message);
            return false;
        }
    }

    private bool TryOpenRadialDirectHotkey()
    {
        if (_activeMenu != null || IsRadialOpen || Time.time <= BlockInputUntil)
            return false;

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (!shift || !Input.GetKeyDown(KeyCode.E))
            return false;

        Player player = Player.m_localPlayer;

        if (player == null)
            return false;

        float distance = Vector3.Distance(player.transform.position, transform.position);

        if (distance > DirectRadialRange)
            return false;

        Camera camera = Camera.main;

        if (camera != null)
        {
            Vector3 toAgent = transform.position - camera.transform.position;
            float angle = Vector3.Angle(camera.transform.forward, toAgent);

            if (angle > DirectRadialMaxAngle)
                return false;
        }

        WakeAssignedBedRest("player radial hotkey");
        OpenRadialMenu();
        BlockInputUntil = Time.time + 0.25f;
        return true;
    }

    private void ChangeRadius(AgentComponent agent, float delta)
    {
        _currentRadius = Mathf.Clamp(_currentRadius + delta, 5f, 200f);
        _radiusPreviewDirty = true;
        Vector3 center = GetHomeZonePreviewCenter(agent);
        ShowHomeZoneRing();
        UpdateHomeZoneRing(center, _currentRadius);
        ShowMessage($"Home radius: {_currentRadius:0}m");
        RefreshRadial();
    }

    private void SetHomeRadiusOnly(AgentComponent agent)
    {
        if (agent == null || agent.Context == null)
            return;

        if (agent.Context.HomeZone == null || !agent.Context.HomeZone.IsSet)
        {
            SetHomeAreaHere(agent);
            return;
        }

        agent.SetHomeRadius(_currentRadius);
        _radiusPreviewDirty = false;
        HideHomeZoneRing();
        ShowMessage($"Home radius updated: {_currentRadius:0}m");
        CloseRadialMenu();
    }

    private void SetHomeAreaHere(AgentComponent agent)
    {
        if (agent == null || agent.Context == null)
            return;

        Vector3 center = transform.position;
        agent.SetHomeZone(center, _currentRadius);
        _radiusPreviewDirty = false;
        HideHomeZoneRing();
        ShowMessage($"Home area set: {_currentRadius:0}m");
        CloseRadialMenu();
    }

    private Vector3 GetHomeZonePreviewCenter(AgentComponent agent)
    {
        if (agent != null && agent.Context != null && agent.Context.HomeZone != null && agent.Context.HomeZone.IsSet)
            return agent.Context.HomeZone.Center;

        return transform.position;
    }

    private void SyncCurrentRadiusFromHome(AgentComponent agent)
    {
        if (agent == null || agent.Context == null || agent.Context.HomeZone == null || !agent.Context.HomeZone.IsSet)
            return;

        _currentRadius = Mathf.Clamp(agent.Context.HomeZone.Radius, 5f, 200f);
    }

    private void RefreshRadial()
    {
        if (_activeMenu == null)
            return;

        _activeMenu.SetOptions(BuildRadialEntries());
    }

    private void LockPlayerControl()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RestorePlayerControl()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void CloseRadialMenu()
    {
        if (_activeMenu != null)
        {
            _activeMenu.Cancel();
            _activeMenu = null;
        }

        RestorePlayerControl();
        IsRadialOpen = false;
        BlockInputUntil = Time.time + 0.25f;
        HideHomeZoneRing();
    }

    private void Update()
    {
        if (TryOpenRadialDirectHotkey())
            return;

        if (_activeMenu != null && Input.GetKeyDown(KeyCode.Escape))
            CloseRadialMenu();
    }

    private void LateUpdate()
    {
        if (!IsRadialOpen)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ShowMessage(string message)
    {
        MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, message);
    }

    public void CreateHomeZoneRing()
    {
        if (_homeZoneRing != null)
            return;

        GameObject go = new GameObject("HomeZoneRing");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.widthMultiplier = 0.05f;
        lr.loop = true;
        lr.positionCount = 64;
        lr.startColor = Color.cyan;
        lr.endColor = Color.cyan;
        _homeZoneRing = lr;
        go.SetActive(false);
    }

    public void ShowHomeZoneRing()
    {
        if (_homeZoneRing == null)
            CreateHomeZoneRing();

        _homeZoneRing.gameObject.SetActive(true);
    }

    public void HideHomeZoneRing()
    {
        if (_homeZoneRing != null)
            _homeZoneRing.gameObject.SetActive(false);
    }

    public void UpdateHomeZoneRing(Vector3 center, float radius)
    {
        if (_homeZoneRing == null)
            return;

        int segments = _homeZoneRing.positionCount;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            Vector3 pos = new Vector3(center.x + x, center.y + 0.1f, center.z + z);
            _homeZoneRing.SetPosition(i, pos);
        }
    }
}
