using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AgentStaminaRuntimeTuning : MonoBehaviour
{
    private AgentStamina _stamina;
    private Character _character;
    private Humanoid _humanoid;
    private MonsterAI _monsterAI;
    private ZNetView _zNetView;

    private const float WalkRegenPerSecond = 8f;
    private const float IdleRegenPerSecond = 16f;
    private const float CombatRegenPerSecond = 3f;
    private const float RunDrainScale = 0.35f;
    private const float RunAllowedStamina = 0.15f;
    private const float AttackMinimumStamina = 4f;

    private float _lastStamina;

    private void Awake()
    {
        _stamina = GetComponent<AgentStamina>();
        _character = GetComponent<Character>();
        _humanoid = GetComponent<Humanoid>();
        _monsterAI = GetComponent<MonsterAI>();
        _zNetView = GetComponent<ZNetView>();
        _lastStamina = GetStamina();
    }

    private void LateUpdate()
    {
        if (_stamina == null)
            _stamina = GetComponent<AgentStamina>();

        if (_character == null)
            _character = GetComponent<Character>();

        if (_humanoid == null)
            _humanoid = GetComponent<Humanoid>();

        if (_monsterAI == null)
            _monsterAI = GetComponent<MonsterAI>();

        if (_zNetView == null)
            _zNetView = GetComponent<ZNetView>();

        if (_stamina == null || _character == null || _character.IsDead())
            return;

        if (_zNetView != null && _zNetView.IsValid() && !_zNetView.IsOwner())
            return;

        float current = GetStamina();
        float max = Mathf.Max(1f, GetMaxStamina());
        bool running = _character.IsRunning();
        bool hasTarget = GetNativeTarget() != null;
        float velocity = _character.GetVelocity().magnitude;

        if (running && current < _lastStamina)
        {
            float drained = _lastStamina - current;
            current += drained * (1f - RunDrainScale);
        }

        if (!running || velocity < 0.35f)
        {
            float regen = hasTarget ? CombatRegenPerSecond : (velocity > 0.2f ? WalkRegenPerSecond : IdleRegenPerSecond);
            current += regen * Time.deltaTime;
        }

        current = Mathf.Clamp(current, 0f, max);
        SetStamina(current);
        _lastStamina = current;
    }

    public static bool HasEnoughAttackStamina(Component component)
    {
        if (component == null)
            return true;

        AgentStamina stamina = component.GetComponent<AgentStamina>();

        if (stamina == null)
            return true;

        float value = GetFloatMember(stamina, "Stamina", 999f);
        return value >= AttackMinimumStamina;
    }

    public static bool HasEnoughRunStamina(Component component)
    {
        if (component == null)
            return true;

        AgentStamina stamina = component.GetComponent<AgentStamina>();

        if (stamina == null)
            return true;

        float value = GetFloatMember(stamina, "Stamina", 999f);
        return value > RunAllowedStamina;
    }

    private Character GetNativeTarget()
    {
        if (_monsterAI == null)
            return null;

        FieldInfo field = FindField(_monsterAI.GetType(), "m_targetCreature");
        return field != null ? field.GetValue(_monsterAI) as Character : null;
    }

    private float GetStamina()
    {
        return GetFloatMember(_stamina, "Stamina", 0f);
    }

    private float GetMaxStamina()
    {
        return GetFloatMember(_stamina, "MaxStamina", 100f);
    }

    private void SetStamina(float value)
    {
        SetFloatMember(_stamina, "Stamina", value);
    }

    private static float GetFloatMember(object instance, string name, float fallback)
    {
        if (instance == null)
            return fallback;

        PropertyInfo property = FindProperty(instance.GetType(), name);

        if (property != null && property.PropertyType == typeof(float))
            return (float)property.GetValue(instance, null);

        FieldInfo field = FindField(instance.GetType(), name);

        if (field != null && field.FieldType == typeof(float))
            return (float)field.GetValue(instance);

        return fallback;
    }

    private static void SetFloatMember(object instance, string name, float value)
    {
        if (instance == null)
            return;

        PropertyInfo property = FindProperty(instance.GetType(), name);

        if (property != null && property.PropertyType == typeof(float) && property.CanWrite)
        {
            property.SetValue(instance, value, null);
            return;
        }

        FieldInfo field = FindField(instance.GetType(), name);

        if (field != null && field.FieldType == typeof(float))
            field.SetValue(instance, value);
    }

    private static PropertyInfo FindProperty(Type type, string name)
    {
        while (type != null)
        {
            PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (property != null)
                return property;

            type = type.BaseType;
        }

        return null;
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

[HarmonyPatch(typeof(AgentComponent), "Awake")]
public static class AgentNpcRuntimeHotfixInstallerPatch
{
    private static void Postfix(AgentComponent __instance)
    {
        if (__instance == null)
            return;

        if (__instance.GetComponent<AgentStaminaRuntimeTuning>() == null)
            __instance.gameObject.AddComponent<AgentStaminaRuntimeTuning>();
    }
}

[HarmonyPatch(typeof(AgentBehaviourController), "MoveToPoint")]
public static class AgentFollowRunTuningPatch
{
    private static void Prefix(AgentBehaviourController __instance, Vector3 point, float stopDistance, ref bool run, string reason)
    {
        if (__instance == null)
            return;

        if (!run)
            return;

        AgentComponent agent = __instance.GetComponent<AgentComponent>();

        if (agent == null)
            return;

        if (!AgentStaminaRuntimeTuning.HasEnoughRunStamina(__instance))
        {
            run = false;
            return;
        }

        if (reason == "Follow" && Player.m_localPlayer != null && agent.Context != null)
        {
            float distance = Vector3.Distance(__instance.transform.position, Player.m_localPlayer.transform.position);
            float catchUpDistance = Mathf.Max(agent.Context.FollowResumeDistance, agent.Context.FollowStopDistance + 2.25f);

            if (distance < catchUpDistance)
                run = false;
        }
    }
}

[HarmonyPatch(typeof(AgentTerrainStepAssist), "Update")]
public static class AgentNoJumpDuringCombatTerrainPatch
{
    private static bool Prefix(AgentTerrainStepAssist __instance)
    {
        return !HasValidTarget(__instance);
    }

    private static bool HasValidTarget(Component component)
    {
        MonsterAI monsterAI = component != null ? component.GetComponent<MonsterAI>() : null;

        if (monsterAI == null)
            return false;

        FieldInfo field = FindField(monsterAI.GetType(), "m_targetCreature");
        Character target = field != null ? field.GetValue(monsterAI) as Character : null;
        return target != null && !target.IsDead();
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
        ItemDrop[] drops = UnityEngine.Object.FindObjectsOfType<ItemDrop>();
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
    private const float MaxRadialRange = 2.8f;

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
