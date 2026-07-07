using HarmonyLib;
using System.Reflection;
using UnityEngine;

public class AgentSleepWakeExitFix : MonoBehaviour
{
    private AgentComponent _agent;
    private MonsterAI _monsterAI;
    private Character _character;
    private Humanoid _humanoid;
    private Animator _animator;
    private ZNetView _zNetView;
    private float _wakeEnforceTimer;
    private bool _wasNight;

    private const float WakeEnforceDuration = 4.0f;
    private const float WakeMoveLock = 2.2f;

    private static readonly string[] SleepingBoolNames =
    {
        "sleeping",
        "Sleeping",
        "sleep",
        "Sleep",
        "isSleeping",
        "IsSleeping"
    };

    private static readonly string[] WakeTriggerNames =
    {
        "wakeup",
        "Wakeup",
        "WakeUp",
        "wake_up",
        "Wake_Up",
        "wake",
        "Wake"
    };

    private void Awake()
    {
        _agent = GetComponent<AgentComponent>();
        _monsterAI = GetComponent<MonsterAI>();
        _character = GetComponent<Character>();
        _humanoid = GetComponent<Humanoid>();
        _animator = GetComponentInChildren<Animator>();
        _zNetView = GetComponent<ZNetView>();
        _wasNight = EnvMan.IsNight();
    }

    private void Update()
    {
        if (_agent == null)
            _agent = GetComponent<AgentComponent>();

        if (_monsterAI == null)
            _monsterAI = GetComponent<MonsterAI>();

        if (_character == null)
            _character = GetComponent<Character>();

        if (_humanoid == null)
            _humanoid = GetComponent<Humanoid>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_zNetView == null)
            _zNetView = GetComponent<ZNetView>();

        if (_agent == null || _character == null || _character.IsDead())
            return;

        if (_zNetView != null && _zNetView.IsValid() && !_zNetView.IsOwner())
            return;

        bool isNight = EnvMan.IsNight();

        if (_wasNight && !isNight)
            BeginWakeEnforce();

        _wasNight = isNight;

        if (!isNight && IsMonsterSleeping())
            BeginWakeEnforce();

        if (_wakeEnforceTimer <= 0f)
            return;

        _wakeEnforceTimer -= Time.deltaTime;
        EnforceAwakeAnimatorState();
    }

    public void BeginWakeEnforce()
    {
        _wakeEnforceTimer = WakeEnforceDuration;
        EnforceAwakeAnimatorState();
    }

    private void EnforceAwakeAnimatorState()
    {
        if (_monsterAI != null)
        {
            SetBoolField(_monsterAI, "m_sleeping", false);
            SetBoolField(_monsterAI, "m_alerted", false);
        }

        if (_character != null)
            _character.SetMoveDir(Vector3.zero);

        if (_humanoid != null)
        {
            _humanoid.SetRun(false);
            _humanoid.SetWalk(false);
        }

        if (_animator == null)
            return;

        SetAnyBool(_animator, SleepingBoolNames, false);
        SetAnyTrigger(_animator, WakeTriggerNames);
    }

    private bool IsMonsterSleeping()
    {
        if (_monsterAI == null)
            return false;

        FieldInfo field = FindField(_monsterAI.GetType(), "m_sleeping");

        if (field == null || field.FieldType != typeof(bool))
            return false;

        return (bool)field.GetValue(_monsterAI);
    }

    private static void SetAnyBool(Animator animator, string[] names, bool value)
    {
        if (animator == null || names == null)
            return;

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];

            if (parameter.type != AnimatorControllerParameterType.Bool)
                continue;

            for (int j = 0; j < names.Length; j++)
            {
                if (parameter.name != names[j])
                    continue;

                animator.SetBool(parameter.nameHash, value);
                break;
            }
        }
    }

    private static void SetAnyTrigger(Animator animator, string[] names)
    {
        if (animator == null || names == null)
            return;

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];

            if (parameter.type != AnimatorControllerParameterType.Trigger)
                continue;

            for (int j = 0; j < names.Length; j++)
            {
                if (parameter.name != names[j])
                    continue;

                animator.ResetTrigger(parameter.nameHash);
                animator.SetTrigger(parameter.nameHash);
                return;
            }
        }
    }

    private static void SetBoolField(object instance, string name, bool value)
    {
        if (instance == null)
            return;

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

[HarmonyPatch(typeof(AgentComponent), "Awake")]
public static class AgentSleepWakeExitFixInstallerPatch
{
    private static void Postfix(AgentComponent __instance)
    {
        if (__instance == null)
            return;

        if (__instance.GetComponent<AgentSleepWakeExitFix>() == null)
            __instance.gameObject.AddComponent<AgentSleepWakeExitFix>();
    }
}

[HarmonyPatch(typeof(AgentBehaviourController), "StartBedWakeup")]
public static class AgentSleepWakeExitFixWakePatch
{
    private static void Postfix(AgentBehaviourController __instance)
    {
        if (__instance == null)
            return;

        AgentSleepWakeExitFix fix = __instance.GetComponent<AgentSleepWakeExitFix>();

        if (fix != null)
            fix.BeginWakeEnforce();
    }
}
