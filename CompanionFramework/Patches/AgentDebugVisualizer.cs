using HarmonyLib;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static Core.Agent.AgentContext;

public class AgentDebugVisualizer : MonoBehaviour
{
    private static AgentDebugVisualizer _instance;
    private static bool _enabled;

    private readonly List<LineRenderer> _lines = new List<LineRenderer>();
    private Material _material;
    private int _lineIndex;

    private const int CircleSegments = 96;
    private const float HeightOffset = 0.12f;
    private const float RefreshInterval = 0.1f;

    private float _refreshTimer;

    public static void Toggle()
    {
        _enabled = !_enabled;

        if (_instance != null)
            _instance.SetVisible(_enabled);

        MessageHud.instance?.ShowMessage(
            MessageHud.MessageType.Center,
            _enabled ? "NPC debug visuals enabled" : "NPC debug visuals disabled"
        );
    }

    private void Awake()
    {
        _instance = this;
        _material = new Material(Shader.Find("Sprites/Default"));
        _material.color = Color.white;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
            Toggle();

        if (!_enabled)
        {
            SetVisible(false);
            return;
        }

        _refreshTimer -= Time.deltaTime;

        if (_refreshTimer > 0f)
            return;

        _refreshTimer = RefreshInterval;
        Draw();
    }

    private void Draw()
    {
        _lineIndex = 0;

        List<AgentComponent> agents = AgentHudRegistry.GetSnapshot();

        if (agents == null || agents.Count == 0)
        {
            agents = new List<AgentComponent>();

            AgentComponent[] found = UnityEngine.Object.FindObjectsOfType<AgentComponent>();

            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null)
                    agents.Add(found[i]);
            }
        }


        for (int i = 0; i < agents.Count; i++)
        {
            AgentComponent agent = agents[i];

            if (agent == null || agent.Context == null)
                continue;

            Character character = agent.GetComponent<Character>();

            if (character != null && character.IsDead())
                continue;

            DrawAgent(agent);
        }

        HideUnusedLines();
    }

    private void DrawAgent(AgentComponent agent)
    {
        Vector3 npcPosition = agent.transform.position + Vector3.up * HeightOffset;

        if (agent.Context.HomeZone != null && agent.Context.HomeZone.IsSet)
        {
            Vector3 home = agent.Context.HomeZone.Center;
            float radius = Mathf.Max(1f, agent.Context.HomeZone.Radius);

            DrawCircle(home, radius, new Color(0.1f, 1f, 0.1f, 0.9f));
            DrawLine(npcPosition, home + Vector3.up * HeightOffset, Color.green);
        }

        if (agent.Context.StateMode == AgentStateMode.Idle)
        {
            Vector3 origin = agent.Context.IdleOrigin;

            if (origin != Vector3.zero)
            {
                DrawCircle(origin, Mathf.Max(2f, agent.Context.IdleRadius), new Color(0.1f, 0.8f, 1f, 0.85f));
                DrawLine(npcPosition, origin + Vector3.up * HeightOffset, Color.cyan);
            }
        }

        if (agent.Context.StateMode == AgentStateMode.Follow && Player.m_localPlayer != null)
        {
            Vector3 player = Player.m_localPlayer.transform.position;
            DrawCircle(player, agent.Context.FollowStopDistance, new Color(1f, 0.9f, 0.1f, 0.65f));
            DrawLine(npcPosition, player + Vector3.up * HeightOffset, Color.yellow);
        }

        if (agent.Context.TaskMode == AgentTaskMode.Patrol)
        {
            Vector3 patrolTarget;

            if (TryGetVector3Field(agent.GetComponent<AgentBehaviourController>(), "_patrolTarget", out patrolTarget))
            {
                DrawCircle(patrolTarget, 1.25f, new Color(1f, 0.5f, 0.1f, 0.9f));
                DrawLine(npcPosition, patrolTarget + Vector3.up * HeightOffset, new Color(1f, 0.5f, 0.1f, 1f));
            }
        }

        Character target = GetNativeTarget(agent);

        if (target != null && !target.IsDead())
            DrawLine(npcPosition, target.transform.position + Vector3.up * HeightOffset, Color.red);

        DrawCircle(agent.transform.position, 0.65f, GetAgentColor(agent));
    }

    private Color GetAgentColor(AgentComponent agent)
    {
        if (agent.Context.TaskMode == AgentTaskMode.Hunt)
            return new Color(1f, 0.25f, 0.1f, 1f);

        if (agent.Context.TaskMode == AgentTaskMode.Patrol)
            return new Color(1f, 0.6f, 0.1f, 1f);

        if (agent.Context.StateMode == AgentStateMode.Follow)
            return Color.yellow;

        if (agent.Context.StateMode == AgentStateMode.StayHome)
            return Color.green;

        return Color.cyan;
    }

    private Character GetNativeTarget(AgentComponent agent)
    {
        MonsterAI ai = agent.GetComponent<MonsterAI>();

        if (ai == null)
            return null;

        FieldInfo field = FindField(ai.GetType(), "m_targetCreature");

        if (field == null)
            return null;

        return field.GetValue(ai) as Character;
    }

    private bool TryGetVector3Field(object instance, string name, out Vector3 value)
    {
        value = Vector3.zero;

        if (instance == null)
            return false;

        FieldInfo field = FindField(instance.GetType(), name);

        if (field == null || field.FieldType != typeof(Vector3))
            return false;

        value = (Vector3)field.GetValue(instance);
        return true;
    }

    private void DrawCircle(Vector3 center, float radius, Color color)
    {
        LineRenderer line = GetLine();
        line.positionCount = CircleSegments + 1;
        line.startColor = color;
        line.endColor = color;
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;

        for (int i = 0; i <= CircleSegments; i++)
        {
            float angle = i / (float)CircleSegments * Mathf.PI * 2f;
            Vector3 point = center;
            point.x += Mathf.Cos(angle) * radius;
            point.z += Mathf.Sin(angle) * radius;
            point.y += HeightOffset;
            line.SetPosition(i, point);
        }
    }

    private void DrawLine(Vector3 from, Vector3 to, Color color)
    {
        LineRenderer line = GetLine();
        line.positionCount = 2;
        line.startColor = color;
        line.endColor = color;
        line.startWidth = 0.06f;
        line.endWidth = 0.06f;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
    }

    private LineRenderer GetLine()
    {
        while (_lineIndex >= _lines.Count)
        {
            GameObject go = new GameObject("AgentDebugLine");
            go.transform.SetParent(transform, false);

            LineRenderer line = go.AddComponent<LineRenderer>();
            line.material = _material;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = _enabled;

            _lines.Add(line);
        }

        LineRenderer result = _lines[_lineIndex];
        result.enabled = true;
        _lineIndex++;
        return result;
    }

    private void HideUnusedLines()
    {
        for (int i = _lineIndex; i < _lines.Count; i++)
            _lines[i].enabled = false;
    }

    private void SetVisible(bool visible)
    {
        for (int i = 0; i < _lines.Count; i++)
            _lines[i].enabled = visible;
    }

    private static FieldInfo FindField(System.Type type, string name)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (field != null)
                return field;

            type = type.BaseType;
        }

        return null;
    }
}

[HarmonyPatch(typeof(Player), "Awake")]
public static class AgentDebugVisualizerInstallerPatch
{
    private static void Postfix(Player __instance)
    {
        if (__instance == null)
            return;

        if (__instance.GetComponent<AgentDebugVisualizer>() == null)
            __instance.gameObject.AddComponent<AgentDebugVisualizer>();
    }
}

[HarmonyPatch(typeof(Terminal), "Awake")]
public static class AgentDebugVisualizerCommandPatch
{
    private static bool _registered;

    private static void Postfix()
    {
        if (_registered)
            return;

        _registered = true;

        new Terminal.ConsoleCommand(
            "ps_agentdebug",
            "Toggle NPC debug visuals",
            args => AgentDebugVisualizer.Toggle()
        );
    }
}