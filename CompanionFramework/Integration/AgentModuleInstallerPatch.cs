using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[HarmonyPatch(typeof(AgentComponent), "Awake")]
public static class AgentModuleInstallerPatch
{
    private static readonly string[] ImmediateModuleTypeNames =
    {
        "AgentFoodInventory",
        "AgentFood",
        "AgentStamina"
    };

    private static readonly string[] DeferredModuleTypeNames =
    {
        "AgentHealthRegen",
        "AgentItemPickup",
        "AgentRested",
        "AgentSkills",
        "AgentSkillActivityTracker",
        "AgentAssignedBedRestController",
        "AgentAmbientActivityController",
        "AgentBedExitAssist",
        "AgentDoorHandler",
        "AgentHudRegistryEntry",
        "AgentMountAndCartController",
        "AgentCookController",
        "AgentResourceWorkController"
    };

    private static void Postfix(AgentComponent __instance)
    {
        if (__instance == null)
            return;

        RegisterInventory(__instance);

        for (int i = 0; i < ImmediateModuleTypeNames.Length; i++)
            EnsureModule(__instance, ImmediateModuleTypeNames[i]);

        AgentDeferredModuleInstaller installer = __instance.GetComponent<AgentDeferredModuleInstaller>();

        if (installer == null)
            installer = __instance.gameObject.AddComponent<AgentDeferredModuleInstaller>();

        installer.Begin(__instance, DeferredModuleTypeNames);
    }

    private static void EnsureModule(AgentComponent agent, string typeName)
    {
        if (agent == null || string.IsNullOrEmpty(typeName))
            return;

        Type type = AccessTools.TypeByName(typeName);

        if (type == null || !typeof(Component).IsAssignableFrom(type))
            return;

        if (agent.GetComponent(type) != null)
            return;

        agent.gameObject.AddComponent(type);
        if (typeName == "AgentMountAndCartController")
            Debug.LogWarning("[AgentModuleInstaller] installed AgentMountAndCartController on " + agent.gameObject.name);
    }

    private static void RegisterInventory(AgentComponent agent)
    {
        Type registryType = AccessTools.TypeByName("AgentInventoryRegistry");

        if (registryType == null)
            return;

        MethodInfo register = registryType.GetMethod("Register", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (register == null)
            return;

        ParameterInfo[] parameters = register.GetParameters();

        if (parameters.Length != 1 || parameters[0].ParameterType != typeof(AgentComponent))
            return;

        register.Invoke(null, new object[] { agent });
    }
}

public class AgentDeferredModuleInstaller : MonoBehaviour
{
    private AgentComponent _agent;
    private string[] _modules;
    private int _index;
    private float _delay;
    private bool _running;

    public void Begin(AgentComponent agent, string[] modules)
    {
        _agent = agent;
        _modules = modules;
        _index = 0;
        _delay = UnityEngine.Random.Range(0.15f, 0.75f);
        _running = true;
    }

    private void Update()
    {
        if (!_running)
            return;

        if (_agent == null || _modules == null)
        {
            _running = false;
            Destroy(this);
            return;
        }

        _delay -= Time.deltaTime;

        if (_delay > 0f)
            return;

        if (_index >= _modules.Length)
        {
            _running = false;
            Destroy(this);
            return;
        }

        EnsureModule(_agent, _modules[_index]);
        _index++;
        _delay = 0.08f;
    }

    private static void EnsureModule(AgentComponent agent, string typeName)
    {
        if (agent == null || string.IsNullOrEmpty(typeName))
            return;

        Type type = AccessTools.TypeByName(typeName);

        if (type == null || !typeof(Component).IsAssignableFrom(type))
            return;

        if (agent.GetComponent(type) != null)
            return;

        agent.gameObject.AddComponent(type);
        if (typeName == "AgentMountAndCartController")
            Debug.LogWarning("[AgentModuleInstaller] installed AgentMountAndCartController on " + agent.gameObject.name);
    }


    private static void RegisterInventory(AgentComponent agent)
    {
        Type registryType = AccessTools.TypeByName("AgentInventoryRegistry");

        if (registryType == null)
            return;

        MethodInfo register = registryType.GetMethod("Register", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (register == null)
            return;

        ParameterInfo[] parameters = register.GetParameters();

        if (parameters.Length != 1 || parameters[0].ParameterType != typeof(AgentComponent))
            return;

        register.Invoke(null, new object[] { agent });
    }
}

public class AgentTerrainStepAssist : MonoBehaviour
{
    private Character _character;
    private Humanoid _humanoid;
    private ZNetView _zNetView;
    private Vector3 _moveTarget;
    private Vector3 _lastPosition;
    private string _reason;
    private float _targetTimer;
    private float _checkTimer;
    private float _stuckTimer;
    private float _jumpCooldown;
    private bool _hasTarget;
    private bool _run;

    private const float TargetMemoryTime = 0.45f;
    private const float CheckInterval = 0.18f;
    private const float StuckVelocityThreshold = 0.25f;
    private const float StuckMoveThreshold = 0.04f;
    private const float StuckJumpTime = 0.85f;
    private const float JumpCooldown = 1.2f;
    private const float ForwardProbeDistance = 0.75f;
    private const float LowProbeHeight = 0.28f;
    private const float HighProbeHeight = 1.15f;
    private const float StepUpNudge = 0.08f;
    private const float ForwardNudge = 0.33f;

    private bool HasCombatTarget()
    {
        MonsterAI ai = GetComponent<MonsterAI>();

        if (ai == null)
            return false;

        FieldInfo field = FindField(ai.GetType(), "m_targetCreature");

        if (field == null)
            return false;

        Character target = field.GetValue(ai) as Character;
        return target != null && !target.IsDead();
    }
    private void Awake()
    {
        _character = GetComponent<Character>();
        _humanoid = GetComponent<Humanoid>();
        _zNetView = GetComponent<ZNetView>();
        _lastPosition = transform.position;
    }

    private void Update()
    {
        if (_character == null || _character.IsDead())
            return;

        if (_zNetView != null && _zNetView.IsValid() && !_zNetView.IsOwner())
            return;

        if (HasCombatTarget())
        {
            _hasTarget = false;
            _stuckTimer = 0f;
            return;
        }

        if (_jumpCooldown > 0f)
            _jumpCooldown -= Time.deltaTime;

        if (_hasTarget)
        {
            _targetTimer -= Time.deltaTime;

            if (_targetTimer <= 0f)
                _hasTarget = false;
        }

        if (!_hasTarget)
        {
            _lastPosition = transform.position;
            _stuckTimer = 0f;
            return;
        }

        _checkTimer -= Time.deltaTime;

        if (_checkTimer > 0f)
            return;

        _checkTimer = CheckInterval;
        UpdateStepAssist(CheckInterval);
    }

    public void SetMoveTarget(Vector3 target, bool run, string reason)
    {
        if (!ShouldAssistReason(reason))
        {
            _hasTarget = false;
            _stuckTimer = 0f;
            return;
        }

        _moveTarget = target;
        _run = run;
        _reason = reason;
        _hasTarget = true;
        _targetTimer = TargetMemoryTime;
    }

    private static bool ShouldAssistReason(string reason)
    {
        if (string.IsNullOrEmpty(reason))
            return false;

        if (reason == "HomeWander" || reason == "Patrol")
            return false;

        return reason == "ReturnHome" ||
               reason == "ReturnHomeInterior" ||
               reason == "DepositInventory" ||
               reason == "ResourceWork" ||
               reason == "ReturnToBed" ||
               reason == "ReturnToMissingBed" ||
               reason == "HuntLoot" ||
               reason == "HuntReturnAnchor";
    }

    private void UpdateStepAssist(float dt)
    {
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null && body.isKinematic)
            return;
        Vector3 current = transform.position;
        float moved = Vector3.Distance(current, _lastPosition);
        float velocity = _character.GetVelocity().magnitude;
        _lastPosition = current;

        Vector3 direction = _moveTarget - current;
        direction.y = 0f;

        if (direction.sqrMagnitude < 9f)
        {
            _stuckTimer = 0f;
            return;
        }

        if (direction.sqrMagnitude < 1.2f)
        {
            _stuckTimer = 0f;
            return;
        }

        direction.Normalize();

        StepProbeResult probe = ProbeStep(current, direction);

        if (!probe.HasLowObstacle || probe.HasHighObstacle || probe.IsOwnCollider)
        {
            _stuckTimer = 0f;
            return;
        }

        bool barelyMoving = velocity < StuckVelocityThreshold && moved < StuckMoveThreshold;

        if (!barelyMoving)
        {
            _stuckTimer = 0f;
            return;
        }

        _stuckTimer += dt;

        if (_stuckTimer < StuckJumpTime || _jumpCooldown > 0f)
            return;

        TryStepJump(direction, probe.HitPoint);
    }

    private StepProbeResult ProbeStep(Vector3 current, Vector3 direction)
    {
        StepProbeResult result = new StepProbeResult();
        Vector3 lowOrigin = current + Vector3.up * LowProbeHeight;
        Vector3 highOrigin = current + Vector3.up * HighProbeHeight;

        if (Physics.Raycast(lowOrigin, direction, out RaycastHit lowHit, ForwardProbeDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            result.HasLowObstacle = true;
            result.HitPoint = lowHit.point;
            result.IsOwnCollider = IsOwnCollider(lowHit.collider);
        }

        if (Physics.Raycast(highOrigin, direction, ForwardProbeDistance, ~0, QueryTriggerInteraction.Ignore))
            result.HasHighObstacle = true;

        return result;
    }

    private void TryStepJump(Vector3 direction, Vector3 hitPoint)
    {
        AgentPrefabProfile profile = GetComponent<AgentPrefabProfile>();

        if (profile != null && !profile.CanJump)
        {
            return;
        }
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null && body.isKinematic)
            return;
        if (!_character.IsOnGround())
            return;

        _character.SetMoveDir(direction);

        if (_humanoid != null)
            _humanoid.SetRun(_run);

        transform.position += direction * ForwardNudge + Vector3.up * StepUpNudge;
        _character.Jump();

        if (body != null)
        {
            body.position = transform.position;

            if (!body.isKinematic)
            {
                body.linearVelocity = new Vector3(
                    direction.x * 3.25f,
                    Mathf.Max(body.linearVelocity.y, 2.2f),
                    direction.z * 3.25f
                );
            }
        }

        _jumpCooldown = JumpCooldown;
        _stuckTimer = 0f;
    }

    private bool IsOwnCollider(Collider collider)
    {
        return collider != null && collider.GetComponentInParent<AgentTerrainStepAssist>() == this;
    }

    private struct StepProbeResult
    {
        public bool HasLowObstacle;
        public bool HasHighObstacle;
        public bool IsOwnCollider;
        public Vector3 HitPoint;
    }
    private static FieldInfo FindField(System.Type type, string name)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (field != null)
                return field;

            type = type.BaseType;
        }

        return null;
    }
}

[HarmonyPatch(typeof(AgentBehaviourController), "MoveToPoint")]
public static class AgentTerrainStepAssistMovePatch
{

    private static void Prefix(AgentBehaviourController __instance, Vector3 point, bool run, string reason)
    {
        if (__instance == null)
            return;

        AgentTerrainStepAssist assist = __instance.GetComponent<AgentTerrainStepAssist>();

        if (assist != null)
            assist.SetMoveTarget(point, run, reason);
    }
}

public class AgentBedExitAssist : MonoBehaviour
{
    private const string ZDO_HasBed = "agent_has_bed";
    private const string ZDO_BedPosition = "agent_bed_pos";
    private const float BedSearchRadius = 10f;
    private const float BedStillNearDistance = 4.5f;
    private const float WakeExitDelay = 2.8f;
    private const float ExitCheckInterval = 0.35f;
    private const float ExitHeightOffset = 0.1f;
    private const float CapsuleRadius = 0.35f;
    private const float CapsuleHeight = 1.8f;

    private AgentComponent _agent;
    private ZNetView _zNetView;
    private Character _character;
    private Humanoid _humanoid;
    private MonsterAI _monsterAI;
    private MethodInfo _stopMovingMethod;
    private bool _handledThisMorning;
    private float _dayWakeTimer;
    private float _checkTimer;

    private void Awake()
    {
        _agent = GetComponent<AgentComponent>();
        _zNetView = GetComponent<ZNetView>();
        _character = GetComponent<Character>();
        _humanoid = GetComponent<Humanoid>();
        _monsterAI = GetComponent<MonsterAI>();
        _stopMovingMethod = FindMethod(typeof(BaseAI), "StopMoving");
    }

    private void Update()
    {
        if (_agent == null || _agent.Context == null)
            return;

        if (_zNetView == null || !_zNetView.IsValid() || !_zNetView.IsOwner())
            return;

        if (_character == null || _character.IsDead())
            return;

        if (_agent.Context.StateMode != Core.Agent.AgentContext.AgentStateMode.StayHome)
            return;

        if (EnvMan.IsNight())
        {
            _handledThisMorning = false;
            _dayWakeTimer = 0f;
            _checkTimer = 0f;
            return;
        }

        if (_handledThisMorning)
            return;

        ZDO zdo = _zNetView.GetZDO();

        if (zdo == null || !zdo.GetBool(ZDO_HasBed, false))
            return;

        _dayWakeTimer += Time.deltaTime;

        if (_dayWakeTimer < WakeExitDelay)
            return;

        _checkTimer -= Time.deltaTime;

        if (_checkTimer > 0f)
            return;

        _checkTimer = ExitCheckInterval;
        TryExitBed(zdo);
    }

    private void TryExitBed(ZDO zdo)
    {
        Vector3 savedBedPosition = zdo.GetVec3(ZDO_BedPosition, Vector3.zero);
        Bed bed = FindBedNearPosition(savedBedPosition);

        if (bed == null)
        {
            _handledThisMorning = true;
            return;
        }

        float distanceToBed = DistanceXZ(transform.position, bed.transform.position);

        if (distanceToBed > BedStillNearDistance)
        {
            _handledThisMorning = true;
            return;
        }

        if (!TryFindExitPosition(bed, out Vector3 exitPosition))
        {
            Debug.LogWarning("[Agent BedExit] Could not find clear bed exit position for " + gameObject.name);
            _handledThisMorning = true;
            return;
        }

        StopMotion();
        Vector3 lookDirection = bed.transform.position - exitPosition;
        lookDirection.y = 0f;

        Quaternion rotation = lookDirection.sqrMagnitude > 0.01f ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up) : transform.rotation;
        PlaceAtPosition(exitPosition, rotation);
        _handledThisMorning = true;
    }

    private bool TryFindExitPosition(Bed bed, out Vector3 exitPosition)
    {
        exitPosition = Vector3.zero;
        Vector3 origin = bed.transform.position;
        Vector3 right = bed.transform.right;
        Vector3 forward = bed.transform.forward;

        List<Vector3> offsets = new List<Vector3>
        {
            right * 1.8f,
            -right * 1.8f,
            -forward * 1.8f,
            forward * 1.8f,
            right * 2.4f,
            -right * 2.4f,
            -forward * 2.4f,
            forward * 2.4f,
            (right - forward).normalized * 2.4f,
            (-right - forward).normalized * 2.4f,
            (right + forward).normalized * 2.4f,
            (-right + forward).normalized * 2.4f,
            right * 3.2f,
            -right * 3.2f,
            -forward * 3.2f,
            forward * 3.2f
        };

        for (int i = 0; i < offsets.Count; i++)
        {
            Vector3 candidate = origin + offsets[i];

            if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(candidate, out float height))
                candidate.y = height + ExitHeightOffset;
            else
                candidate.y = transform.position.y;

            if (!IsClear(candidate))
                continue;

            exitPosition = candidate;
            return true;
        }

        return false;
    }

    private bool IsClear(Vector3 position)
    {
        Vector3 bottom = position + Vector3.up * CapsuleRadius;
        Vector3 top = position + Vector3.up * CapsuleHeight;
        Collider[] colliders = Physics.OverlapCapsule(bottom, top, CapsuleRadius, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            if (collider == null)
                continue;

            if (collider.GetComponentInParent<AgentComponent>() == _agent)
                continue;

            if (collider.GetComponentInParent<Bed>() != null)
                continue;

            if (collider.isTrigger)
                continue;

            return false;
        }

        return true;
    }

    private Bed FindBedNearPosition(Vector3 position)
    {
        Bed best = null;
        float bestDistance = BedSearchRadius;
        Bed[] beds = UnityEngine.Object.FindObjectsByType<Bed>(FindObjectsSortMode.None);

        foreach (Bed bed in beds)
        {
            if (bed == null)
                continue;

            float distance = Vector3.Distance(bed.transform.position, position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = bed;
            }
        }

        return best;
    }

    private void StopMotion()
    {
        if (_character != null)
            _character.SetMoveDir(Vector3.zero);

        if (_humanoid != null)
            _humanoid.SetRun(false);

        if (_monsterAI != null && _stopMovingMethod != null)
            _stopMovingMethod.Invoke(_monsterAI, null);

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null && !body.isKinematic)
            body.linearVelocity = Vector3.zero;
    }

    private void PlaceAtPosition(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = position;
            body.rotation = rotation;

            if (!body.isKinematic)
                body.linearVelocity = Vector3.zero;
        }

        if (_zNetView != null && _zNetView.IsValid())
        {
            ZDO zdo = _zNetView.GetZDO();

            if (zdo != null)
            {
                zdo.SetPosition(position);
                zdo.SetRotation(rotation);
            }
        }
    }

    private static float DistanceXZ(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return Mathf.Sqrt(x * x + z * z);
    }

    private static MethodInfo FindMethod(Type type, string name)
    {
        while (type != null)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method != null)
                return method;

            type = type.BaseType;
        }

        return null;
    }
}
