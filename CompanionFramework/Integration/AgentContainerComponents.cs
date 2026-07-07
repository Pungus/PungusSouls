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

    private const float DirectRadialRange = 2.5f;
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

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        ZNetView nview = GetComponent<ZNetView>();

        if (nview != null && nview.IsValid() && !nview.IsOwner())
        {
            nview.ClaimOwnership();
            ////Debug.Log("[Agent] Claimed ownership for interaction");
        }

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

        entries.Add(new RadialMenuEntry(agent.Context.StateMode == AgentStateMode.Idle ? "Idle ✓" : "Idle", () => SetState(agent, AgentStateMode.Idle)));
        entries.Add(new RadialMenuEntry(agent.Context.StateMode == AgentStateMode.Follow ? "Follow ✓" : "Follow", () => SetState(agent, AgentStateMode.Follow)));

        RadialMenuEntry followDistance = new RadialMenuEntry($"Follow {agent.Context.FollowStopDistance:0.0}m");
        followDistance.Add("Follow -", () => ChangeFollowDistance(agent, -0.5f));
        followDistance.Add("Follow +", () => ChangeFollowDistance(agent, 0.5f));
        entries.Add(followDistance);

        RadialMenuEntry stayHome = new RadialMenuEntry(agent.Context.StateMode == AgentStateMode.StayHome ? "Stay Home ✓" : "Stay Home", () => SetStayHomeState(agent));
        stayHome.Add(agent.Context.TaskMode == AgentTaskMode.None ? "No Task ✓" : "No Task", () => SetTask(agent, AgentTaskMode.None));
        stayHome.Add(agent.Context.TaskMode == AgentTaskMode.Hunt ? "Hunt ✓" : "Hunt", () => SetTask(agent, AgentTaskMode.Hunt));
        stayHome.Add(agent.Context.TaskMode == AgentTaskMode.Patrol ? "Patrol ✓" : "Patrol", () => SetPatrolTask(agent));
        entries.Add(stayHome);

        RadialMenuEntry radius = new RadialMenuEntry($"Set Radius {_currentRadius:0}m", () => SetHomeRadiusOnly(agent));
        radius.Add("Radius -", () => ChangeRadius(-5f));
        radius.Add("Radius +", () => ChangeRadius(5f));
        entries.Add(radius);

        entries.Add(new RadialMenuEntry("Close", CloseRadialMenu));
        return entries;
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

        OpenRadialMenu();
        BlockInputUntil = Time.time + 0.25f;
        return true;
    }

    private void ChangeRadius(float delta)
    {
        _currentRadius = Mathf.Clamp(_currentRadius + delta, 5f, 200f);
        ShowHomeZoneRing();
        UpdateHomeZoneRing(transform.position, _currentRadius);
        ShowMessage($"Home radius: {_currentRadius:0}m");
        RefreshRadial();
    }

    private void SetHomeRadiusOnly(AgentComponent agent)
    {
        if (agent == null || agent.Context == null)
            return;

        if (agent.Context.HomeZone == null)
            agent.Context.HomeZone = new HomeZoneController();

        agent.Context.HomeZone.SetHomeZone(transform.position, _currentRadius);
        agent.Context.HasHomeZone = true;
        agent.SaveHomeToZDO(transform.position, _currentRadius);
        agent.SaveBehaviourStateToZDO();
        HideHomeZoneRing();
        ShowMessage($"Home radius set: {_currentRadius:0}m");
        RefreshRadial();
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
