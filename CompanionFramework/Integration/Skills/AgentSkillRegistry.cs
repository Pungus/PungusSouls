using System.Collections.Generic;

public sealed class AgentSkillDefinition
{
    public readonly string Id;
    public readonly string DisplayName;
    public readonly float GainMultiplier;
    public readonly float MaxLevel;

    public AgentSkillDefinition(string id, string displayName, float gainMultiplier = 1f, float maxLevel = 100f)
    {
        Id = AgentSkillRegistry.NormalizeId(id);
        DisplayName = string.IsNullOrEmpty(displayName) ? Id : displayName.Trim();
        GainMultiplier = gainMultiplier < 0f ? 0f : gainMultiplier;
        MaxLevel = maxLevel <= 0f ? 100f : maxLevel;
    }
}

public static class AgentSkillRegistry
{
    private static readonly Dictionary<string, AgentSkillDefinition> Definitions = new Dictionary<string, AgentSkillDefinition>();

    static AgentSkillRegistry()
    {
        Register("movement", "Movement");
        Register("run", "Run");
        Register("jump", "Jump");
        Register("swim", "Swim");
        Register("swords", "Swords");
        Register("knives", "Knives");
        Register("clubs", "Clubs");
        Register("polearms", "Polearms");
        Register("spears", "Spears");
        Register("axes", "Axes");
        Register("bows", "Bows");
        Register("crossbows", "Crossbows");
        Register("unarmed", "Unarmed");
        Register("blocking", "Blocking");
        Register("dodge", "Dodge");
        Register("hunting", "Hunting");
        Register("combat", "Combat");
        Register("pickup", "Pickup");
        Register("resting", "Resting");
    }

    public static AgentSkillDefinition Register(string id, string displayName, float gainMultiplier = 1f, float maxLevel = 100f)
    {
        id = NormalizeId(id);

        if (string.IsNullOrEmpty(id))
            return null;

        AgentSkillDefinition definition = new AgentSkillDefinition(id, displayName, gainMultiplier, maxLevel);
        Definitions[id] = definition;
        return definition;
    }

    public static AgentSkillDefinition GetOrCreate(string id)
    {
        id = NormalizeId(id);

        if (string.IsNullOrEmpty(id))
            return null;

        if (Definitions.TryGetValue(id, out AgentSkillDefinition definition))
            return definition;

        return Register(id, id, 1f, 100f);
    }

    public static bool TryGet(string id, out AgentSkillDefinition definition)
    {
        return Definitions.TryGetValue(NormalizeId(id), out definition);
    }

    public static IReadOnlyDictionary<string, AgentSkillDefinition> All
    {
        get { return Definitions; }
    }

    public static string NormalizeId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return string.Empty;

        string value = id.Trim().ToLowerInvariant();
        value = value.Replace(";", string.Empty).Replace("|", string.Empty);
        return value;
    }
}
