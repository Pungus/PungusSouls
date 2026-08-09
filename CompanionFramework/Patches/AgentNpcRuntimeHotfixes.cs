using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

public static class AgentStaminaRuntimeTuning
{
    private const float AttackMinimumStamina = 4f;

    public static bool HasEnoughAttackStamina(Component component)
    {
        if (component == null)
            return true;

        AgentStamina stamina = component.GetComponent<AgentStamina>();

        if (stamina == null)
            return true;

        return stamina.CanAttack(AttackMinimumStamina);
    }

    public static bool HasEnoughRunStamina(Component component)
    {
        if (component == null)
            return true;

        AgentStamina stamina = component.GetComponent<AgentStamina>();

        if (stamina == null)
            return true;

        return stamina.CanRun();
    }
}

[HarmonyPatch(typeof(AgentBehaviourController), "HuntCollectingLoot")]
public static class AgentHuntLootAggressiveSearchPatch
{
    private static void Prefix(AgentBehaviourController __instance)
    {
        if (__instance == null)
            return;

        ItemDrop nearest = FindNearestDrop(__instance.transform.position, 18f);

        if (nearest == null)
            return;

        FieldInfo field = FindField(typeof(AgentBehaviourController), "_currentLootTarget");

        if (field != null)
            field.SetValue(__instance, nearest);
    }

    private static ItemDrop FindNearestDrop(Vector3 position, float range)
    {
        ItemDrop[] drops = UnityEngine.Object.FindObjectsByType<ItemDrop>(FindObjectsSortMode.None);
        ItemDrop best = null;
        float bestDistance = range;

        for (int i = 0; i < drops.Length; i++)
        {
            ItemDrop drop = drops[i];

            if (drop == null || drop.m_itemData == null || drop.gameObject == null)
                continue;

            if (!drop.m_autoPickup)
                continue;

            float distance = Vector3.Distance(position, drop.transform.position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = drop;
            }
        }

        return best;
    }

    private static FieldInfo FindField(Type type, string name)
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

[HarmonyPatch(typeof(AgentContainerComponent), "TryOpenRadialDirectHotkey")]
public static class AgentRadialRangePatch
{
    private const float MaxRadialRange = 1.8f;

    private static bool Prefix(AgentContainerComponent __instance, ref bool __result)
    {
        if (__instance == null)
            return true;

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (!shift || !Input.GetKeyDown(KeyCode.E))
            return true;

        Player player = Player.m_localPlayer;

        if (player == null)
            return true;

        if (Vector3.Distance(player.transform.position, __instance.transform.position) > MaxRadialRange)
        {
            __result = false;
            return false;
        }

        return true;
    }
}
