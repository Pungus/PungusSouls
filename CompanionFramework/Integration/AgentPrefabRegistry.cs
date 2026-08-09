using System.Collections.Generic;
using UnityEngine;

public sealed class AgentPrefabDefinition
{
    public readonly string Id;
    public readonly string[] PrefabNames;
    public readonly float MaxCarryWeight;
    public readonly int InventoryWidth;
    public readonly int InventoryHeight;
    public readonly string TombstonePrefabName;
    public readonly bool CanFly;
    public readonly bool RequiresInteractionToRegister;
    public readonly string[] HiddenRendererNameContains;
    public readonly BodypartSystem.bodyPart[] HiddenBodyParts;
    public readonly bool AmputateBodyParts;
    public readonly string[] BodyMeshNameContains;
    public readonly string[] ExcludedMeshNameContains;
    public readonly bool EnableJumpMotionAssist;
    public readonly float JumpAssistForwardVelocity;
    public readonly float JumpAssistUpVelocity;
    public readonly float JumpAssistDuration;
    public readonly float JumpAssistMaxTargetDistance;
    public readonly string[] JumpAssistClipNameContains;

    public AgentPrefabDefinition(
        string id,
        string[] prefabNames,
        float maxCarryWeight,
        int inventoryWidth,
        int inventoryHeight,
        bool canFly = false,
        bool requiresInteractionToRegister = false,
        string tombstonePrefabName = "",
        string[] hiddenRendererNameContains = null,
        string[] bodyMeshNameContains = null,
        string[] excludedMeshNameContains = null,
        BodypartSystem.bodyPart[] hiddenBodyParts = null,
        bool enableJumpMotionAssist = false,
        float jumpAssistForwardVelocity = 8f,
        float jumpAssistUpVelocity = 2.5f,
        float jumpAssistDuration = 0.45f,
        float jumpAssistMaxTargetDistance = 18f,
        string[] jumpAssistClipNameContains = null,
        bool amputateBodyParts = false)

    {
        Id = string.IsNullOrEmpty(id) ? string.Empty : id.Trim();
        PrefabNames = prefabNames ?? new string[0];
        MaxCarryWeight = maxCarryWeight > 0f ? maxCarryWeight : 300f;
        InventoryWidth = inventoryWidth > 0 ? inventoryWidth : 8;
        InventoryHeight = inventoryHeight > 0 ? inventoryHeight : 6;
        CanFly = canFly;
        TombstonePrefabName = string.IsNullOrEmpty(tombstonePrefabName) ? string.Empty : tombstonePrefabName.Trim();
        RequiresInteractionToRegister = requiresInteractionToRegister;
        HiddenRendererNameContains = hiddenRendererNameContains ?? new string[0];
        HiddenBodyParts = hiddenBodyParts ?? new BodypartSystem.bodyPart[0];
        AmputateBodyParts = amputateBodyParts;
        BodyMeshNameContains = bodyMeshNameContains ?? new string[0];
        ExcludedMeshNameContains = excludedMeshNameContains ?? new string[0];
        EnableJumpMotionAssist = enableJumpMotionAssist;
        JumpAssistForwardVelocity = jumpAssistForwardVelocity;
        JumpAssistUpVelocity = jumpAssistUpVelocity;
        JumpAssistDuration = jumpAssistDuration;
        JumpAssistMaxTargetDistance = jumpAssistMaxTargetDistance;
        JumpAssistClipNameContains = jumpAssistClipNameContains ?? new[]
        {
            "jump",
            "leap",
            "pounce"
        };
    }
}

public static class AgentPrefabRegistry
{
    private static readonly List<AgentPrefabDefinition> Definitions = new List<AgentPrefabDefinition>();
    private static readonly Dictionary<string, AgentPrefabDefinition> DefinitionsByPrefabName = new Dictionary<string, AgentPrefabDefinition>();

    static AgentPrefabRegistry()
    {
        Register(new AgentPrefabDefinition("sif", new[] { "Sif", "$ps_sif" }, 300f, 8, 6, false, true, "sif_tombstone"));

        Register(new AgentPrefabDefinition("sweet_shalquoir", new[] { "SweetShalquoir", "$ps_sweetshalquoir" }, 300f, 8, 6, false, false,
            enableJumpMotionAssist: true,
            jumpAssistForwardVelocity: 9f,
            jumpAssistUpVelocity: 1.2f,
            jumpAssistDuration: 0.35f,
            jumpAssistMaxTargetDistance: 18f,
            jumpAssistClipNameContains: new[]
            {
                "jump",
                "pounce",
                "attack_f",
                "attack_down"
            }
            ));

        Register(new AgentPrefabDefinition(
            "queen_marika",
            new[] { "queenmarikacompanion", "$ps_queenmarika" },
            300f,
            8,
            6,
            false,
            true,
            ""
           ));
        Register(new AgentPrefabDefinition(
            "FireKeeper",
            new[] { "FireKeeper", "$ps_firekeeper" },
            300f,
            8,
            6,
            false,
            true,
            ""


           ));
        Register(new AgentPrefabDefinition(
            "Ciarin",
            new[] { "Ciarin", "$ps_ciarin" },
            300f,
            8,
            6,
            false,
            true,
            ""
           ));
        Register(new AgentPrefabDefinition(
            "Bria",
            new[] { "Bria", "$ps_bria" },
            300f,
            8,
            6,
            false,
            true,
            ""
           ));

        Register(new AgentPrefabDefinition("HellkiteDrake", new[] { "HellkiteDrake", "$ps_hkdrake" }, 300f, 8, 6, true, false));

        Register(new AgentPrefabDefinition("FangBoarCompanion", new[] { "FangBoarCompanion", "$ps_fangboarcompaion" }, 300f, 8, 6, false, false));

        Register(new AgentPrefabDefinition("BabyMushroom", new[] { "BabyMushroom", "$ps_BabyMushroom" }, 300f, 8, 6, false, true));

        Register(new AgentPrefabDefinition(
        "Solaire",
        new[] { "Solaire", "$ps_solaire" },
        300f,
        8,
        6,
        false,
        true,
        "",
        hiddenRendererNameContains: null,
        hiddenBodyParts: new[]
        {
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.FootLeft,
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmLowerRight,
            BodypartSystem.bodyPart.ArmUpperRight
        },
        amputateBodyParts: true,
        bodyMeshNameContains: new[]
        {
            "Body"
        },
        excludedMeshNameContains: new[]
        {
            "40_03",
            "40_05",
            "Armor",
            "Helmet",
            "Helmet"
        }));

        Register(new AgentPrefabDefinition(
        "Oscar",
        new[] { "Oscar", "$ps_oscar" },
        300f,
        8,
        6,
        false,
        true,
        "",
        hiddenRendererNameContains: null,
        hiddenBodyParts: new[]
        {
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.FootLeft,
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmLowerRight,
            BodypartSystem.bodyPart.ArmUpperRight
        },
        amputateBodyParts: true,
        bodyMeshNameContains: new[]
        {
            "Body"
        },
        excludedMeshNameContains: new[]
        {
            "40_03",
            "40_05",
            "Armor",
            "Helmet",
            "Helmet"
        }));
        Register(new AgentPrefabDefinition(
        "Havel",
        new[] { "Havel", "$ps_havel" },
        300f,
        8,
        6,
        false,
        true,
        "",
        hiddenRendererNameContains: null,
        hiddenBodyParts: new[]
        {
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.FootLeft,
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmLowerRight,
            BodypartSystem.bodyPart.ArmUpperRight
        },
        amputateBodyParts: true,
        bodyMeshNameContains: new[]
        {
            "Body"
        },
        excludedMeshNameContains: new[]
        {
            "40_03",
            "40_05",
            "Armor",
            "Helmet",
            "Helmet"
        }));
    }

    public static void ApplyDefinitionToProfile(AgentPrefabDefinition definition, AgentPrefabProfile profile)
    {
        if (definition == null || profile == null)
            return;

        profile.AgentId = definition.Id;
        profile.MaxCarryWeight = definition.MaxCarryWeight;
        profile.InventoryWidth = definition.InventoryWidth;
        profile.InventoryHeight = definition.InventoryHeight;
        profile.CanFly = definition.CanFly;
        profile.RequiresInteractionToRegister = definition.RequiresInteractionToRegister;
        profile.TombstonePrefabName = definition.TombstonePrefabName;
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
        profile.JumpAssistClipNameContains = definition.JumpAssistClipNameContains ?? new[] { "jump", "leap", "pounce" };
    }

    public static IReadOnlyList<AgentPrefabDefinition> All
    {
        get { return Definitions; }
    }

    public static bool Register(AgentPrefabDefinition definition)
    {
        if (definition == null || string.IsNullOrEmpty(definition.Id) || definition.PrefabNames == null || definition.PrefabNames.Length == 0)
            return false;

        for (int i = Definitions.Count - 1; i >= 0; i--)
        {
            if (Definitions[i] != null && string.Equals(Definitions[i].Id, definition.Id, System.StringComparison.OrdinalIgnoreCase))
                Definitions.RemoveAt(i);
        }

        Definitions.Add(definition);

        for (int i = 0; i < definition.PrefabNames.Length; i++)
        {
            string key = CleanPrefabName(definition.PrefabNames[i]);

            if (!string.IsNullOrEmpty(key))
                DefinitionsByPrefabName[key] = definition;
        }

        return true;
    }

    public static bool Register(string id, string[] prefabNames, float maxCarryWeight = 300f, int inventoryWidth = 8, int inventoryHeight = 6)
    {
        return Register(new AgentPrefabDefinition(id, prefabNames, maxCarryWeight, inventoryWidth, inventoryHeight));
    }

    public static bool TryGetDefinition(GameObject prefab, out AgentPrefabDefinition definition)
    {
        definition = null;

        if (prefab == null)
            return false;

        string prefabName = CleanPrefabName(prefab.name);

        if (string.IsNullOrEmpty(prefabName))
            return false;

        return DefinitionsByPrefabName.TryGetValue(prefabName, out definition);
    }

    public static bool IsValidAgentBasePrefab(GameObject prefab)
    {
        if (prefab == null)
            return false;

        return prefab.GetComponent<ZNetView>() != null &&
               prefab.GetComponent<Character>() != null &&
               prefab.GetComponent<Humanoid>() != null &&
               prefab.GetComponent<MonsterAI>() != null;
    }

    public static void ApplyNpcBodyHider(GameObject prefab, AgentPrefabProfile profile)
    {
        if (prefab == null || profile == null)
            return;

        bool hasRendererRules =
            profile.HiddenRendererNameContains != null &&
            profile.HiddenRendererNameContains.Length > 0;

        bool hasBodyPartRules =
            profile.HiddenBodyParts != null &&
            profile.HiddenBodyParts.Length > 0;

        if (!hasRendererRules && !hasBodyPartRules)
            return;

        AgentNpcBodyPartHider hider = prefab.GetComponent<AgentNpcBodyPartHider>();

        if (hider == null)
            hider = prefab.AddComponent<AgentNpcBodyPartHider>();

        hider.BodyMeshNameContains = profile.BodyMeshNameContains ?? new string[0];
        hider.ExcludedMeshNameContains = profile.ExcludedMeshNameContains ?? new string[0];

        hider.ApplyOnStart = true;
        hider.AffectInactiveRenderers = true;
        hider.HideWholeRenderersByName = hasRendererRules;
        hider.AmputateMeshesByBodyPart = profile.AmputateBodyParts && hasBodyPartRules;

        if (hasRendererRules)
        {
            AgentNpcBodyPartHider.RendererNameRule[] rules =
                new AgentNpcBodyPartHider.RendererNameRule[profile.HiddenRendererNameContains.Length];

            for (int i = 0; i < profile.HiddenRendererNameContains.Length; i++)
            {
                rules[i] = new AgentNpcBodyPartHider.RendererNameRule
                {
                    NameContains = profile.HiddenRendererNameContains[i],
                    Hide = true
                };
            }

            hider.RendererRules = rules;
        }

        if (hasBodyPartRules)
            hider.HideBodyParts = profile.HiddenBodyParts;
    }

    public static string CleanPrefabName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        int cloneIndex = name.IndexOf("(Clone)", System.StringComparison.OrdinalIgnoreCase);

        if (cloneIndex >= 0)
            name = name.Substring(0, cloneIndex);

        return name.Trim();
    }
}