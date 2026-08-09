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
        profile.RequiresInteractionToRegister = definition.RequiresInteractionToRegister;
        profile.CanFly = definition.CanFly;
        profile.HiddenRendererNameContains = definition.HiddenRendererNameContains ?? new string[0];
        profile.HiddenBodyParts = definition.HiddenBodyParts ?? new BodypartSystem.bodyPart[0];
        profile.AmputateBodyParts = definition.AmputateBodyParts;
        profile.BodyMeshNameContains = definition.BodyMeshNameContains ?? new string[0];
        profile.ExcludedMeshNameContains = definition.ExcludedMeshNameContains ?? new string[0];
        profile.EnableJumpMotionAssist = definition.EnableJumpMotionAssist;
        profile.JumpAssistForwardVelocity = definition.JumpAssistForwardVelocity;
        profile.JumpAssistUpVelocity = definition.JumpAssistUpVelocity;
        profile.JumpAssistDuration = definition.JumpAssistDuration;
        profile.JumpAssistMaxTargetDistance = definition.JumpAssistMaxTargetDistance;
        profile.JumpAssistClipNameContains = definition.JumpAssistClipNameContains ?? new[]
        {
            "jump",
            "leap",
            "pounce"
        };
        AgentPrefabRegistry.ApplyNpcBodyHider(prefab, profile);
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
