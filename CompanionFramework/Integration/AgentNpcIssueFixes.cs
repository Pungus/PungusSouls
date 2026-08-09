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