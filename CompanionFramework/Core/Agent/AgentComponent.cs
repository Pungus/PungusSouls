using Core.Agent;
using Modules.Death;
using Modules.HomeZone;
using System;
using System.Collections.Generic;
using Systems.Inventory;
using UnityEngine;
using static Core.Agent.AgentContext;

public class AgentComponent : MonoBehaviour
{
    private ZNetView _zNetView;
    private bool _deathHandled;
    private bool _deathHooked;

    public AgentContext Context { get; private set; }
    public Container ValheimContainer { get; private set; }
    public BaseAI VanillaAI { get; private set; }
    public MonsterAI MonsterAI { get; private set; }
    public AgentBehaviourController BehaviourController { get; private set; }
    public float MaxCarryWeight = 300f;

    private const string ZDO_HomeX = "agent_home_x";
    private const string ZDO_HomeY = "agent_home_y";
    private const string ZDO_HomeZ = "agent_home_z";
    private const string ZDO_HomeRadius = "agent_home_radius";
    private const string ZDO_BehaviourState = "agent_behaviour_state";
    private const string ZDO_StayHomeMode = "agent_stay_home_mode";
    private const string ZDO_IdleX = "agent_idle_x";
    private const string ZDO_IdleY = "agent_idle_y";
    private const string ZDO_IdleZ = "agent_idle_z";
    private const string ZDO_IdleRadius = "agent_idle_radius";
    private const string ZDO_StayHome = "agent_stay_home";
    private const string ZDO_Follow = "agent_follow";
    private const string ZDO_DepositChestUser = "agent_deposit_chest_user";
    private const string ZDO_DepositChestId = "agent_deposit_chest_id";
    private const string ZDO_DepositChestCount = "agent_deposit_chest_count";
    private const string ZDO_DepositChestPosPrefix = "agent_deposit_chest_";
    private const string ZDO_BehaviourMode = "agent_behaviour_mode_v2";
    private const string ZDO_StateMode = "agent_state_mode_v2";
    private const string ZDO_TaskMode = "agent_task_mode_v2";
    private const string ZDO_ContextSchema = "agent_context_schema";
    private const string ZDO_FollowStopDistance = "agent_follow_stop_distance";
    private const string ZDO_FollowResumeDistance = "agent_follow_resume_distance";

    private void Awake()
    {
        AgentComponent[] components = GetComponents<AgentComponent>();

        if (components.Length > 1)
        {
            //Debug.LogError("[Agent] Duplicate AgentComponent on " + gameObject.name + " id=" + gameObject.GetInstanceID());
            Destroy(this);
            return;
        }

        _zNetView = GetComponent<ZNetView>();
        CacheVanillaAI();
        SetupContainer();
        AgentInventoryRegistry.Register(this);
        SetupContext();
        SetupBehaviourController();
        HookDeath();
        SetupInteraction();
        ////Debug.Log("[Agent] AgentComponent ACTIVE on " + gameObject.name);
    }

    private void OnDestroy()
    {
        AgentInventoryRegistry.Unregister(this);
    }

    private void Start()
    {
        LoadStateFromZDO();
    }

    private void Update()
    {
        if (Context == null)
            return;

        Context.Position = transform.position;
        HomeZoneVisuals.Draw(Context);
    }

    public void SetBehaviourMode(AgentBehaviourMode mode)
    {
        if (Context == null)
            return;

        Context.BehaviourMode = mode;
        Context.SyncLegacyFields();
        SaveBehaviourStateToZDO();
    }

    public void SetStateMode(AgentStateMode mode)
    {
        if (Context == null)
            return;

        Context.StateMode = mode;

        if (mode == AgentStateMode.Idle)
            Context.IdleOrigin = transform.position;

        Context.SyncLegacyFields();
        SaveBehaviourStateToZDO();
    }

    public void SetTaskMode(AgentTaskMode mode)
    {
        if (Context == null)
            return;

        bool requiresHome = mode == AgentTaskMode.Patrol ||
                            mode == AgentTaskMode.Cook ||
                            mode == AgentTaskMode.Farming ||
                            mode == AgentTaskMode.Smelting ||
                            mode == AgentTaskMode.Repair;

        if (requiresHome && Context.StateMode != AgentStateMode.StayHome)
            Context.StateMode = AgentStateMode.StayHome;

        AgentAssignedBedRestController bedRest = GetComponent<AgentAssignedBedRestController>();
        if (mode != AgentTaskMode.None && bedRest != null)
            bedRest.WakeFromAssignedBed("task-changed");

        AgentFishingController fishing = GetComponent<AgentFishingController>();
        if (fishing != null)
            fishing.NotifyTaskChanged();

        Context.TaskMode = mode;
        Context.SyncLegacyFields();
        SaveBehaviourStateToZDO();
    }
    public void SetBehaviourState(AgentBehaviourState state)
    {
        if (Context == null)
            return;

        if (state == AgentBehaviourState.Follow)
            Context.StateMode = AgentStateMode.Follow;
        else if (state == AgentBehaviourState.StayHome)
            Context.StateMode = AgentStateMode.StayHome;
        else if (state == AgentBehaviourState.Hunt)
            Context.TaskMode = AgentTaskMode.Hunt;
        else
            Context.StateMode = AgentStateMode.Idle;

        if (Context.StateMode == AgentStateMode.Idle)
            Context.IdleOrigin = transform.position;

        Context.SyncLegacyFields();
        SaveBehaviourStateToZDO();
    }

    public void SaveFollowDistanceToZDO(float stopDistance, float resumeDistance)
    {
        if (Context == null)
            return;

        Context.FollowStopDistance = Mathf.Clamp(stopDistance, 1f, 20f);
        Context.FollowResumeDistance = Mathf.Clamp(resumeDistance, Context.FollowStopDistance + 0.5f, 25f);

        if (_zNetView != null && _zNetView.IsValid())
        {
            ZDO zdo = _zNetView.GetZDO();

            if (zdo != null)
            {
                zdo.Set(ZDO_FollowStopDistance, Context.FollowStopDistance);
                zdo.Set(ZDO_FollowResumeDistance, Context.FollowResumeDistance);
            }
        }

        SaveBehaviourStateToZDO();
    }

    private void CacheVanillaAI()
    {
        VanillaAI = GetComponent<BaseAI>();
        MonsterAI = GetComponent<MonsterAI>();

        foreach (BaseAI ai in GetComponents<BaseAI>())
        {
            if (ai != null) ;
            ////Debug.Log("[Agent] Runtime AI component: " + ai.GetType().Name + " enabled=" + ai.enabled);
        }

        if (MonsterAI == null) ;
        //Debug.LogWarning("[Agent] MonsterAI missing on " + gameObject.name + ". Native combat will not work correctly.");
    }

    private void SetupContainer()
    {
        ValheimContainer = GetComponent<Container>();

        if (ValheimContainer == null)
            ValheimContainer = gameObject.AddComponent<Container>();

        AgentPrefabProfile profile = GetComponent<AgentPrefabProfile>();
        int width = profile != null && profile.InventoryWidth > 0 ? profile.InventoryWidth : 8;
        int height = profile != null && profile.InventoryHeight > 0 ? profile.InventoryHeight : 6;

        ValheimContainer.m_name = "NPC Inventory";
        ValheimContainer.m_width = width;
        ValheimContainer.m_height = height;
        ValheimContainer.m_checkGuardStone = false;
        ValheimContainer.m_privacy = Container.PrivacySetting.Public;

        if (ValheimContainer.m_inventory == null)
        {
            ValheimContainer.m_inventory = new Inventory(ValheimContainer.m_name, null, ValheimContainer.m_width, ValheimContainer.m_height);
            ValheimContainer.m_inventory.m_name = ValheimContainer.m_name;
            return;
        }

        if (ValheimContainer.m_inventory.GetWidth() == ValheimContainer.m_width && ValheimContainer.m_inventory.GetHeight() == ValheimContainer.m_height)
        {
            ValheimContainer.m_inventory.m_name = ValheimContainer.m_name;
            return;
        }

        Inventory oldInventory = ValheimContainer.m_inventory;
        List<ItemDrop.ItemData> oldItems = new List<ItemDrop.ItemData>(oldInventory.GetAllItems());
        Inventory resizedInventory = new Inventory(ValheimContainer.m_name, null, ValheimContainer.m_width, ValheimContainer.m_height);
        resizedInventory.m_name = ValheimContainer.m_name;

        foreach (ItemDrop.ItemData oldItem in oldItems)
        {
            if (oldItem == null)
                continue;

            ItemDrop.ItemData clone = oldItem.Clone();

            if (clone != null)
                resizedInventory.AddItem(clone);
        }

        ValheimContainer.m_inventory = resizedInventory;
        ValheimContainer.m_inventory.m_onChanged?.Invoke();
        ////Debug.Log("[Agent] Resized NPC inventory to " + ValheimContainer.m_width + "x" + ValheimContainer.m_height);
    }

    private void SetupContext()
    {
        Context = new AgentContext
        {
            Inventory = new AgentInventory(),
            Position = transform.position,
            HomeZone = new HomeZoneController(),
            BehaviourMode = AgentBehaviourMode.Defensive,
            StateMode = AgentStateMode.Idle,
            TaskMode = AgentTaskMode.None,
            IdleOrigin = transform.position,
            IdleRadius = 18f,
            FollowStopDistance = 3f,
            FollowResumeDistance = 4f
        };

        Context.SyncLegacyFields();
    }

    private void SetupBehaviourController()
    {
        BehaviourController = GetComponent<AgentBehaviourController>();

        if (BehaviourController == null)
        {
            BehaviourController = gameObject.AddComponent<AgentBehaviourController>();
            ////Debug.Log("[Agent] Added AgentBehaviourController to " + gameObject.name);
        }
    }

    private void HookDeath()
    {
        Humanoid humanoid = GetComponent<Humanoid>();

        if (humanoid == null || _deathHooked)
            return;

        humanoid.m_onDeath = (Action)Delegate.Combine(humanoid.m_onDeath, new Action(OnAgentDeath));
        _deathHooked = true;
    }

    private void SetupInteraction()
    {
        AgentContainer agentContainer = new AgentContainer(ValheimContainer);
        AgentContainerComponent containerComponent = GetComponent<AgentContainerComponent>();

        if (containerComponent == null)
        {
            containerComponent = gameObject.AddComponent<AgentContainerComponent>();
            ////Debug.Log("[Agent] Added AgentContainerComponent to " + gameObject.name);
        }
        else
        {
            ////Debug.Log("[Agent] Found existing AgentContainerComponent on " + gameObject.name);
        }

        containerComponent.Init(agentContainer);
    }

    private void OnAgentDeath()
    {
        if (_deathHandled)
            return;

        _deathHandled = true;
        AgentDeathTombstoneController.CreateTombstoneForAgent(this);
    }

    public void SaveHomeToZDO(Vector3 pos, float radius)
    {
        ApplyHomeZone(pos, radius, AgentStateMode.StayHome);
    }

    public void SetHomeZone(Vector3 pos, float radius)
    {
        ApplyHomeZone(pos, radius, AgentStateMode.StayHome);
    }

    public void SetHomeRadius(float radius)
    {
        Vector3 center = transform.position;

        if (Context != null && Context.HomeZone != null && Context.HomeZone.IsSet)
            center = Context.HomeZone.Center;

        ApplyHomeZone(center, radius, Context != null ? Context.StateMode : AgentStateMode.StayHome);
    }

    private void ApplyHomeZone(Vector3 pos, float radius, AgentStateMode stateMode)
    {
        if (radius <= 0f)
            return;

        if (Context == null)
            SetupContext();

        if (Context.HomeZone == null)
            Context.HomeZone = new HomeZoneController();

        Context.HomeZone.SetHomeZone(pos, radius);
        Context.HasHomeZone = true;
        Context.IdleOrigin = pos;
        Context.IdleRadius = Mathf.Max(Context.IdleRadius, radius);

        if (stateMode == AgentStateMode.StayHome)
            Context.StateMode = AgentStateMode.StayHome;

        Context.SyncLegacyFields();
        SaveHomeZoneValuesToZDO(pos, radius);
        SaveBehaviourStateToZDO();
    }

    private void SaveHomeZoneValuesToZDO(Vector3 pos, float radius)
    {
        if (_zNetView == null || !_zNetView.IsValid())
            return;

        ZDO zdo = _zNetView.GetZDO();
        if (zdo == null)
            return;

        zdo.Set(ZDO_HomeX, pos.x);
        zdo.Set(ZDO_HomeY, pos.y);
        zdo.Set(ZDO_HomeZ, pos.z);
        zdo.Set(ZDO_HomeRadius, radius);
    }

    public void SaveBehaviourStateToZDO()
    {
        if (_zNetView == null || !_zNetView.IsValid() || Context == null)
            return;

        Context.SyncLegacyFields();
        ZDO zdo = _zNetView.GetZDO();
        zdo.Set(ZDO_ContextSchema, 2);
        zdo.Set(ZDO_BehaviourMode, (int)Context.BehaviourMode);
        zdo.Set(ZDO_StateMode, (int)Context.StateMode);
        zdo.Set(ZDO_TaskMode, (int)Context.TaskMode);
        zdo.Set(ZDO_BehaviourState, (int)Context.BehaviourState);
        zdo.Set(ZDO_StayHomeMode, (int)Context.StayHomeMode);
        zdo.Set(ZDO_IdleX, Context.IdleOrigin.x);
        zdo.Set(ZDO_IdleY, Context.IdleOrigin.y);
        zdo.Set(ZDO_IdleZ, Context.IdleOrigin.z);
        zdo.Set(ZDO_IdleRadius, Context.IdleRadius);
        zdo.Set(ZDO_Follow, Context.StateMode == AgentStateMode.Follow);
        zdo.Set(ZDO_StayHome, Context.StateMode == AgentStateMode.StayHome);
        zdo.Set(ZDO_FollowStopDistance, Context.FollowStopDistance);
        zdo.Set(ZDO_FollowResumeDistance, Context.FollowResumeDistance);

        if (Context.HomeZone != null && Context.HomeZone.IsSet)
            SaveHomeZoneValuesToZDO(Context.HomeZone.Center, Context.HomeZone.Radius);
    }

    public void SaveFollowToZDO(bool follow)
    {
        SetStateMode(follow ? AgentStateMode.Follow : AgentStateMode.Idle);
    }

    private void LoadStateFromZDO()
    {
        if (_zNetView == null || !_zNetView.IsValid() || Context == null)
            return;

        ZDO zdo = _zNetView.GetZDO();
        float homeRadius = zdo.GetFloat(ZDO_HomeRadius, 0f);

        if (homeRadius > 0f)
        {
            Vector3 homePos = new Vector3(
                zdo.GetFloat(ZDO_HomeX, transform.position.x),
                zdo.GetFloat(ZDO_HomeY, transform.position.y),
                zdo.GetFloat(ZDO_HomeZ, transform.position.z)
            );

            Context.HomeZone.SetHomeZone(homePos, homeRadius);
            Context.HasHomeZone = true;
            ////Debug.Log("[Agent] HomeZone loaded");
        }

        Context.IdleOrigin = new Vector3(
            zdo.GetFloat(ZDO_IdleX, transform.position.x),
            zdo.GetFloat(ZDO_IdleY, transform.position.y),
            zdo.GetFloat(ZDO_IdleZ, transform.position.z)
        );

        Context.IdleRadius = zdo.GetFloat(ZDO_IdleRadius, Context.IdleRadius);
        Context.FollowStopDistance = Mathf.Clamp(zdo.GetFloat(ZDO_FollowStopDistance, Context.FollowStopDistance), 1f, 20f);
        Context.FollowResumeDistance = Mathf.Clamp(zdo.GetFloat(ZDO_FollowResumeDistance, Context.FollowResumeDistance), Context.FollowStopDistance + 0.5f, 25f);

        int schema = zdo.GetInt(ZDO_ContextSchema, 0);

        if (schema >= 2)
        {
            Context.BehaviourMode = (AgentBehaviourMode)zdo.GetInt(ZDO_BehaviourMode, (int)AgentBehaviourMode.Defensive);
            Context.StateMode = (AgentStateMode)zdo.GetInt(ZDO_StateMode, (int)AgentStateMode.Idle);
            Context.TaskMode = (AgentTaskMode)zdo.GetInt(ZDO_TaskMode, (int)AgentTaskMode.None);
            Context.SyncLegacyFields();
        }
        else
        {
            MigrateLegacyContext(zdo);
        }

        ////Debug.Log("[Agent] Loaded context behaviour=" + Context.BehaviourMode + " state=" + Context.StateMode + " task=" + Context.TaskMode);
    }

    private void MigrateLegacyContext(ZDO zdo)
    {
        HomeZoneMode oldHomeMode = (HomeZoneMode)zdo.GetInt(ZDO_StayHomeMode, (int)HomeZoneMode.Defensive);

        if (oldHomeMode == HomeZoneMode.Passive)
            Context.BehaviourMode = AgentBehaviourMode.Passive;
        else if (oldHomeMode == HomeZoneMode.Aggressive)
            Context.BehaviourMode = AgentBehaviourMode.Aggressive;
        else
            Context.BehaviourMode = AgentBehaviourMode.Defensive;

        Context.TaskMode = oldHomeMode == HomeZoneMode.Patrol ? AgentTaskMode.Patrol : AgentTaskMode.None;
        int savedState = zdo.GetInt(ZDO_BehaviourState, -1);

        if (savedState >= 0)
        {
            AgentBehaviourState legacyState = (AgentBehaviourState)savedState;

            if (legacyState == AgentBehaviourState.Follow)
                Context.StateMode = AgentStateMode.Follow;
            else if (legacyState == AgentBehaviourState.StayHome)
                Context.StateMode = AgentStateMode.StayHome;
            else if (legacyState == AgentBehaviourState.Hunt)
            {
                Context.StateMode = AgentStateMode.Idle;
                Context.TaskMode = AgentTaskMode.Hunt;
            }
            else
                Context.StateMode = AgentStateMode.Idle;
        }
        else
        {
            bool follow = zdo.GetBool(ZDO_Follow, false);
            bool stayHome = zdo.GetBool(ZDO_StayHome, false);

            if (stayHome && Context.HasHomeZone)
                Context.StateMode = AgentStateMode.StayHome;
            else if (follow)
                Context.StateMode = AgentStateMode.Follow;
            else
                Context.StateMode = AgentStateMode.Idle;
        }

        Context.SyncLegacyFields();
        SaveBehaviourStateToZDO();
    }

    public void SaveDepositChestToZDO(ZDOID chestId)
    {
        SaveDepositChestToZDO(chestId, Vector3.zero);
    }

    public void SaveDepositChestToZDO(ZDOID chestId, Vector3 chestPosition)
    {
        if (_zNetView == null || !_zNetView.IsValid())
            return;

        ZDO zdo = _zNetView.GetZDO();
        if (zdo == null)
            return;

        zdo.Set(ZDO_DepositChestUser, chestId.UserID);
        zdo.Set(ZDO_DepositChestId, chestId.ID);

        if (chestPosition != Vector3.zero)
        {
            zdo.Set(ZDO_DepositChestCount, 1);
            zdo.Set(ZDO_DepositChestPosPrefix + "0_pos", chestPosition);
        }
        ////Debug.Log("[Agent] Deposit chest saved: " + chestId + " pos=" + chestPosition);
    }

    private void FixAllTMPFonts()
    {
        TMPro.TMP_FontAsset[] fontAssets = Resources.FindObjectsOfTypeAll<TMPro.TMP_FontAsset>();

        if (fontAssets == null || fontAssets.Length == 0)
        {
            //Debug.LogError("[Agent] No TMP fonts found");
            return;
        }

        TMPro.TMP_FontAsset font = fontAssets[0];
        TMPro.TextMeshProUGUI[] texts = Resources.FindObjectsOfTypeAll<TMPro.TextMeshProUGUI>();

        foreach (TMPro.TextMeshProUGUI text in texts)
        {
            if (text != null && text.font == null)
                text.font = font;
        }
    }
}
