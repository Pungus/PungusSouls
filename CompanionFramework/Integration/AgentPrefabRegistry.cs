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


    public AgentPrefabDefinition(
        string id,
        string[] prefabNames,
        float maxCarryWeight,
        int inventoryWidth,
        int inventoryHeight,
        string tombstonePrefabName = "")
    {
        Id = string.IsNullOrEmpty(id) ? string.Empty : id.Trim();
        PrefabNames = prefabNames ?? new string[0];
        MaxCarryWeight = maxCarryWeight > 0f ? maxCarryWeight : 300f;
        InventoryWidth = inventoryWidth > 0 ? inventoryWidth : 8;
        InventoryHeight = inventoryHeight > 0 ? inventoryHeight : 6;
        TombstonePrefabName = string.IsNullOrEmpty(tombstonePrefabName) ? string.Empty : tombstonePrefabName.Trim();
    }

}

public static class AgentPrefabRegistry
{
    private static readonly List<AgentPrefabDefinition> Definitions = new List<AgentPrefabDefinition>();
    private static readonly Dictionary<string, AgentPrefabDefinition> DefinitionsByPrefabName = new Dictionary<string, AgentPrefabDefinition>();

    static AgentPrefabRegistry()
    {
        Register(new AgentPrefabDefinition("sif", new[] { "Sif", "$ps_sif" }, 300f, 8, 6, "sif_tombstone"));
        Register(new AgentPrefabDefinition("sweet_shalquoir", new[] { "SweetShalquoir", "$ps_sweetshalquoir" }, 300f, 8, 6));
        Register(new AgentPrefabDefinition("queen_marika", new[] { "queenmarikacompanion", "$ps_queenmarika" }, 300f, 8, 6));
        Register(new AgentPrefabDefinition("HellkiteDrake", new[] { "HellkiteDrake", "$ps_hkdrake" }, 300f, 8, 6));
        Register(new AgentPrefabDefinition("FangBoarCompanion", new[] { "FangBoarCompanion", "$ps_fangboarcompaion" }, 300f, 8, 6));
        Register(new AgentPrefabDefinition("BabyMushroom", new[] { "BabyMushroom", "$ps_BabyMushroom" }, 300f, 8, 6));
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
