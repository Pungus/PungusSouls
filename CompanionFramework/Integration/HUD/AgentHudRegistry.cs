using System.Collections.Generic;
using UnityEngine;

public class AgentHudRegistryEntry : MonoBehaviour
{
    private AgentComponent _agent;

    private void Awake()
    {
        _agent = GetComponent<AgentComponent>();
    }

    private void OnEnable()
    {
        if (_agent == null)
            _agent = GetComponent<AgentComponent>();

        if (_agent != null)
            AgentHudRegistry.Register(_agent);
    }

    private void OnDisable()
    {
        if (_agent != null)
            AgentHudRegistry.Unregister(_agent);
    }

    private void OnDestroy()
    {
        if (_agent != null)
            AgentHudRegistry.Unregister(_agent);
    }
}

public static class AgentHudRegistry
{
    private static readonly List<AgentComponent> Agents = new List<AgentComponent>();

    public static IReadOnlyList<AgentComponent> ActiveAgents
    {
        get { return Agents; }
    }

    public static void Register(AgentComponent agent)
    {
        if (agent == null)
            return;

        if (!Agents.Contains(agent))
            Agents.Add(agent);
    }

    public static void Unregister(AgentComponent agent)
    {
        if (agent == null)
            return;

        Agents.Remove(agent);
    }

    public static void RemoveNulls()
    {
        for (int i = Agents.Count - 1; i >= 0; i--)
        {
            AgentComponent agent = Agents[i];

            if (agent == null || agent.gameObject == null)
                Agents.RemoveAt(i);
        }
    }

    public static List<AgentComponent> GetSnapshot(List<AgentComponent> buffer = null)
    {
        RemoveNulls();

        if (buffer == null)
            buffer = new List<AgentComponent>(Agents.Count);
        else
            buffer.Clear();

        for (int i = 0; i < Agents.Count; i++)
            buffer.Add(Agents[i]);

        return buffer;
    }
}
