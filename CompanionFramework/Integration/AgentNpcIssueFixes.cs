using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

[HarmonyPatch(typeof(AgentBehaviourController), "ControlledAIUpdate")]
public static class AgentAggroTooltipSuppressPatch
{
    private static void Postfix(AgentBehaviourController __instance)
    {
        if (__instance == null)
            return;

        MonsterAI monster = __instance.GetComponent<MonsterAI>();

        if (monster == null)
            return;

        FieldInfo targetField = FindField(monster.GetType(), "m_targetCreature");
        Character target = targetField != null ? targetField.GetValue(monster) as Character : null;

        if (target != null && !target.IsDead())
            return;

        SetBoolField(monster, "m_alerted", false);
    }

    private static void SetBoolField(object instance, string name, bool value)
    {
        FieldInfo field = FindField(instance.GetType(), name);

        if (field != null && field.FieldType == typeof(bool))
            field.SetValue(instance, value);
    }

    private static FieldInfo FindField(System.Type type, string name)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field != null)
                return field;

            type = type.BaseType;
        }

        return null;
    }
}

[HarmonyPatch(typeof(Integration.AgentDepositChestGuiPatch_Awake), "RefreshNpcDropdown")]
public static class AgentDepositChestDropdownFixPatch
{
    private const float NpcSearchDistance = 80f;

    private static bool Prefix(Container currentContainer)
    {
        TMP_Dropdown dropdown = Integration.AgentDepositChestGuiPatch_Awake.NpcDropdown;

        if (dropdown == null)
            return false;

        int oldValue = dropdown.value;
        Integration.AgentDepositChestGuiPatch_Awake.DropdownAgents.Clear();
        dropdown.ClearOptions();
        List<string> options = new List<string>();
        List<AgentComponent> agents = AgentHudRegistry.GetSnapshot();

        if (agents.Count == 0)
            agents.AddRange(UnityEngine.Object.FindObjectsOfType<AgentComponent>());

        for (int i = 0; i < agents.Count; i++)
        {
            AgentComponent agent = agents[i];

            if (agent == null || agent.Context == null)
                continue;

            Character character = agent.GetComponent<Character>();

            if (character != null && character.IsDead())
                continue;

            if (currentContainer != null)
            {
                float distanceToChest = Vector3.Distance(agent.transform.position, currentContainer.transform.position);
                float distanceToHome = agent.Context.HomeZone != null && agent.Context.HomeZone.IsSet
                    ? Vector3.Distance(agent.Context.HomeZone.Center, currentContainer.transform.position)
                    : distanceToChest;

                if (Mathf.Min(distanceToChest, distanceToHome) > NpcSearchDistance)
                    continue;
            }

            Integration.AgentDepositChestGuiPatch_Awake.DropdownAgents.Add(agent);
            options.Add(BuildAgentLabel(agent));
        }

        if (options.Count == 0)
        {
            options.Add("No NPC nearby");
            dropdown.AddOptions(options);
            dropdown.value = 0;
            dropdown.interactable = false;

            if (Integration.AgentDepositChestGuiPatch_Awake.AssignButton != null)
                Integration.AgentDepositChestGuiPatch_Awake.AssignButton.interactable = false;

            dropdown.RefreshShownValue();
            return false;
        }

        dropdown.AddOptions(options);
        dropdown.value = Mathf.Clamp(oldValue, 0, options.Count - 1);
        dropdown.interactable = true;

        if (Integration.AgentDepositChestGuiPatch_Awake.AssignButton != null)
            Integration.AgentDepositChestGuiPatch_Awake.AssignButton.interactable = true;

        dropdown.RefreshShownValue();
        return false;
    }

    private static string BuildAgentLabel(AgentComponent agent)
    {
        if (agent == null || agent.gameObject == null)
            return "NPC";

        Character character = agent.GetComponent<Character>();

        if (character != null && !string.IsNullOrEmpty(character.m_name))
            return character.m_name;

        string name = agent.gameObject.name.Replace("(Clone)", string.Empty).Trim();
        return string.IsNullOrEmpty(name) ? "NPC" : name;
    }
}
