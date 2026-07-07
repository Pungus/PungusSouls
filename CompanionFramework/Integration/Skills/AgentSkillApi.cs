using UnityEngine;

public static class AgentSkillApi
{
    public static AgentSkillDefinition RegisterSkill(string id, string displayName, float gainMultiplier = 1f, float maxLevel = 100f)
    {
        return AgentSkillRegistry.Register(id, displayName, gainMultiplier, maxLevel);
    }

    public static bool RaiseSkill(GameObject agentObject, string id, float amount)
    {
        if (agentObject == null)
            return false;

        AgentSkills skills = agentObject.GetComponent<AgentSkills>();
        return skills != null && skills.RaiseSkill(id, amount);
    }

    public static float GetSkillLevel(GameObject agentObject, string id)
    {
        if (agentObject == null)
            return 0f;

        AgentSkills skills = agentObject.GetComponent<AgentSkills>();
        return skills != null ? skills.GetLevel(id) : 0f;
    }

    public static float ApplySkillFactor(GameObject agentObject, string id, float baseValue, float maxBonusMultiplier)
    {
        if (agentObject == null)
            return baseValue;

        AgentSkills skills = agentObject.GetComponent<AgentSkills>();
        return skills != null ? skills.ApplySkillFactor(id, baseValue, maxBonusMultiplier) : baseValue;
    }
}
