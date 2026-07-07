using UnityEngine;

public static class NpcInstaller
{
    public static bool AttachAgentSystem(GameObject prefab, AgentPrefabDefinition definition)
    {
        if (prefab == null || definition == null)
            return false;

        if (!AgentPrefabRegistry.IsValidAgentBasePrefab(prefab))
            return false;

        EnsureProfile(prefab, definition);
        EnsureTameable(prefab);
        EnsureAgentComponent(prefab, definition);
        return true;
    }

    public static bool AttachAgentSystem(GameObject prefab)
    {
        if (!AgentPrefabRegistry.TryGetDefinition(prefab, out AgentPrefabDefinition definition))
            return false;

        return AttachAgentSystem(prefab, definition);
    }

    private static void EnsureProfile(GameObject prefab, AgentPrefabDefinition definition)
    {
        AgentPrefabProfile profile = prefab.GetComponent<AgentPrefabProfile>();

        if (profile == null)
            profile = prefab.AddComponent<AgentPrefabProfile>();

        profile.AgentId = definition.Id;
        profile.MaxCarryWeight = definition.MaxCarryWeight;
        profile.InventoryWidth = definition.InventoryWidth;
        profile.InventoryHeight = definition.InventoryHeight;
        profile.TombstonePrefabName = definition.TombstonePrefabName;
        profile.IconId = definition.Id;
    }

    private static void EnsureTameable(GameObject prefab)
    {
        Tameable tameable = prefab.GetComponent<Tameable>();

        if (tameable == null)
            tameable = prefab.AddComponent<Tameable>();

        tameable.m_commandable = true;
    }

    private static void EnsureAgentComponent(GameObject prefab, AgentPrefabDefinition definition)
    {
        AgentComponent agent = prefab.GetComponent<AgentComponent>();

        if (agent == null)
            agent = prefab.AddComponent<AgentComponent>();

        agent.MaxCarryWeight = definition.MaxCarryWeight;
    }
}
