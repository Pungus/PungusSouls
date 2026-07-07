using HarmonyLib;
using System.Reflection;
using UnityEngine;

public class AgentSkillActivityTracker : MonoBehaviour
{
    private AgentSkills _skills;
    private Character _character;
    private MonsterAI _monsterAI;
    private ZNetView _zNetView;
    private Vector3 _lastPosition;
    private float _movementTimer;
    private float _combatTimer;
    private FieldInfo _targetCreatureField;

    private const float MovementSampleInterval = 1f;
    private const float CombatSampleInterval = 2f;
    private const float MinimumMovementDistance = 0.35f;
    private const float MovementXpPerMeter = 0.08f;
    private const float RunXpPerMeter = 0.05f;
    private const float SwimXpPerMeter = 0.15f;
    private const float CombatPresenceXp = 0.25f;

    private void Awake()
    {
        _skills = GetComponent<AgentSkills>();
        _character = GetComponent<Character>();
        _monsterAI = GetComponent<MonsterAI>();
        _zNetView = GetComponent<ZNetView>();
        _lastPosition = transform.position;

        if (_monsterAI != null)
            _targetCreatureField = FindField(_monsterAI.GetType(), "m_targetCreature");
    }

    private void Update()
    {
        if (_skills == null)
            _skills = GetComponent<AgentSkills>();

        if (_character == null)
            _character = GetComponent<Character>();

        if (_monsterAI == null)
            _monsterAI = GetComponent<MonsterAI>();

        if (_zNetView == null)
            _zNetView = GetComponent<ZNetView>();

        if (_skills == null || _character == null || _character.IsDead())
            return;

        if (_zNetView != null && _zNetView.IsValid() && !_zNetView.IsOwner())
            return;

        TrackMovement();
        TrackCombatPresence();
    }

    private void TrackMovement()
    {
        _movementTimer += Time.deltaTime;

        if (_movementTimer < MovementSampleInterval)
            return;

        _movementTimer = 0f;
        Vector3 current = transform.position;
        float distance = Vector3.Distance(current, _lastPosition);
        _lastPosition = current;

        if (distance < MinimumMovementDistance)
            return;

        _skills.RaiseSkill("movement", distance * MovementXpPerMeter);

        if (_character.IsSwimming())
        {
            _skills.RaiseSkill("swim", distance * SwimXpPerMeter);
            return;
        }

        if (_character.IsRunning())
            _skills.RaiseSkill("run", distance * RunXpPerMeter);
    }

    private void TrackCombatPresence()
    {
        _combatTimer += Time.deltaTime;

        if (_combatTimer < CombatSampleInterval)
            return;

        _combatTimer = 0f;

        Character target = GetTargetCreature();

        if (target == null || target.IsDead())
            return;

        _skills.RaiseSkill("combat", CombatPresenceXp);
    }

    private Character GetTargetCreature()
    {
        if (_monsterAI == null)
            return null;

        if (_targetCreatureField == null)
            _targetCreatureField = FindField(_monsterAI.GetType(), "m_targetCreature");

        return _targetCreatureField != null ? _targetCreatureField.GetValue(_monsterAI) as Character : null;
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

[HarmonyPatch(typeof(Character), "Jump")]
public static class AgentSkillsJumpPatch
{
    private static void Postfix(Character __instance)
    {
        if (__instance == null)
            return;

        AgentSkills skills = __instance.GetComponent<AgentSkills>();

        if (skills != null)
            skills.RaiseSkill("jump", 1f);
    }
}
