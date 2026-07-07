using System.Collections.Generic;

public static class AgentInventoryRegistry
{
    private static readonly Dictionary<Inventory, AgentComponent> ByInventory = new Dictionary<Inventory, AgentComponent>();
    private static readonly Dictionary<Container, AgentComponent> ByContainer = new Dictionary<Container, AgentComponent>();

    public static void Register(AgentComponent agent)
    {
        if (agent == null || agent.ValheimContainer == null)
            return;

        ByContainer[agent.ValheimContainer] = agent;

        if (agent.ValheimContainer.m_inventory != null)
            ByInventory[agent.ValheimContainer.m_inventory] = agent;
    }

    public static void Unregister(AgentComponent agent)
    {
        if (agent == null || agent.ValheimContainer == null)
            return;

        ByContainer.Remove(agent.ValheimContainer);

        if (agent.ValheimContainer.m_inventory != null)
            ByInventory.Remove(agent.ValheimContainer.m_inventory);
    }

    public static AgentComponent GetAgent(Container container)
    {
        if (container == null)
            return null;

        if (ByContainer.TryGetValue(container, out AgentComponent agent))
            return agent;

        agent = container.GetComponent<AgentComponent>();

        if (agent != null)
        {
            Register(agent);
            return agent;
        }

        agent = container.GetComponentInParent<AgentComponent>();

        if (agent != null)
        {
            Register(agent);
            return agent;
        }

        return null;
    }

    public static AgentComponent GetAgent(Inventory inventory)
    {
        if (inventory == null)
            return null;

        if (ByInventory.TryGetValue(inventory, out AgentComponent agent))
            return agent;

        return null;
    }

    public static void Clear()
    {
        ByInventory.Clear();
        ByContainer.Clear();
    }
}