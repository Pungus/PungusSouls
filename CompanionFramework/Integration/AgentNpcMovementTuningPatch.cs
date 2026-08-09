using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class AgentWaterAvoidance : MonoBehaviour
{
    private AgentComponent _agent;
    private Character _character;
    private Humanoid _humanoid;
    private ZNetView _zNetView;
    private Vector3 _lastSafePosition;
    private float _safeSampleTimer;

    private const float SafeSampleInterval = 0.75f;
    private const float EmergencyWaterDepth = 0.55f;
    private const float ShoreSearchRadius = 7f;
    private const float ShoreCheckStepDegrees = 30f;

    private void Awake()
    {
        _agent = GetComponent<AgentComponent>();
        _character = GetComponent<Character>();
        _humanoid = GetComponent<Humanoid>();
        _zNetView = GetComponent<ZNetView>();
        _lastSafePosition = transform.position;

        if (_humanoid == null)
            enabled = false;
    }

    private void Update()
    {
        if (_agent == null)
            _agent = GetComponent<AgentComponent>();

        if (_character == null)
            _character = GetComponent<Character>();

        if (_humanoid == null)
            _humanoid = GetComponent<Humanoid>();

        if (_zNetView == null)
            _zNetView = GetComponent<ZNetView>();

        if (_humanoid == null)
        {
            enabled = false;
            return;
        }

        if (_agent == null || _character == null || _character.IsDead())
            return;

        if (_zNetView != null && _zNetView.IsValid() && !_zNetView.IsOwner())
            return;

        _safeSampleTimer -= Time.deltaTime;

        if (_safeSampleTimer <= 0f)
        {
            _safeSampleTimer = SafeSampleInterval;

            if (!IsWaterPosition(transform.position, 0.2f))
                _lastSafePosition = transform.position;
        }

        if (IsWaterPosition(transform.position, EmergencyWaterDepth))
            MoveBackToShore();
    }

    public bool TryAdjustDestination(Vector3 requested, out Vector3 adjusted)
    {
        adjusted = requested;

        if (!IsWaterPosition(requested, 0.15f))
            return false;

        Vector3 origin = transform.position;
        Vector3 direction = requested - origin;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            direction = transform.forward;

        direction.Normalize();

        if (TryFindDryPointNear(requested, -direction, out adjusted))
            return true;

        if (TryFindDryPointNear(origin, direction, out adjusted))
            return true;

        adjusted = _lastSafePosition;
        return true;
    }

    private void MoveBackToShore()
    {
        Vector3 target;

        if (!TryFindDryPointNear(transform.position, (_lastSafePosition - transform.position).normalized, out target))
            target = _lastSafePosition;

        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        direction.Normalize();
        _character.SetMoveDir(direction);

        Humanoid humanoid = GetComponent<Humanoid>();

        if (humanoid != null)
            humanoid.SetRun(true);
    }

    private bool TryFindDryPointNear(Vector3 center, Vector3 preferredDirection, out Vector3 point)
    {
        point = center;
        preferredDirection.y = 0f;

        if (preferredDirection.sqrMagnitude < 0.01f)
            preferredDirection = transform.forward;

        preferredDirection.Normalize();

        for (float radius = 1.5f; radius <= ShoreSearchRadius; radius += 1.5f)
        {
            for (float angle = 0f; angle < 360f; angle += ShoreCheckStepDegrees)
            {
                Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
                Vector3 candidate = center + rotation * preferredDirection * radius;

                if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(candidate, out float height))
                    candidate.y = height;

                if (IsWaterPosition(candidate, 0.05f))
                    continue;

                point = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool IsWaterPosition(Vector3 position, float depthTolerance)
    {
        float waterLevel = GetWaterLevel(position);
        return position.y < waterLevel - depthTolerance;
    }

    private static float GetWaterLevel(Vector3 position)
    {
        float waterLevel = 30f;

        if (ZoneSystem.instance != null)
        {
            System.Reflection.FieldInfo field = FindField(ZoneSystem.instance.GetType(), "m_waterLevel");

            if (field != null && field.FieldType == typeof(float))
                return (float)field.GetValue(ZoneSystem.instance);

            System.Reflection.MethodInfo method = FindMethod(ZoneSystem.instance.GetType(), "GetWaterLevel", typeof(Vector3));

            if (method != null && method.ReturnType == typeof(float))
                return (float)method.Invoke(ZoneSystem.instance, new object[] { position });
        }

        return waterLevel;
    }

    private static System.Reflection.FieldInfo FindField(System.Type type, string name)
    {
        while (type != null)
        {
            System.Reflection.FieldInfo field = type.GetField(
                name,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic
            );

            if (field != null)
                return field;

            type = type.BaseType;
        }

        return null;
    }

    private static System.Reflection.MethodInfo FindMethod(System.Type type, string name, params System.Type[] parameters)
    {
        while (type != null)
        {
            System.Reflection.MethodInfo method = type.GetMethod(
                name,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic,
                null,
                parameters,
                null
            );

            if (method != null)
                return method;

            type = type.BaseType;
        }

        return null;
    }
}

[HarmonyPatch(typeof(AgentBehaviourController), "MoveToPoint")]
public static class AgentWaterAvoidanceMovePatch
{
    private static void Prefix(AgentBehaviourController __instance, ref Vector3 point, string reason)
    {
        if (__instance == null)
            return;

        if (reason == "HuntLoot")
            return;

        AgentWaterAvoidance avoidance = __instance.GetComponent<AgentWaterAvoidance>();

        if (avoidance == null)
            return;

        if (avoidance.TryAdjustDestination(point, out Vector3 adjusted))
            point = adjusted;
    }
}

public class AgentSlopeJumpAssist : MonoBehaviour
{
    private AgentTerrainStepAssist _terrainAssist;
    private Character _character;
    private Humanoid _humanoid;
    private ZNetView _zNetView;
    private FieldInfo _hasTargetField;
    private FieldInfo _moveTargetField;
    private FieldInfo _reasonField;
    private float _checkTimer;
    private float _stuckTimer;
    private float _jumpCooldown;
    private Vector3 _lastPosition;

    private const float CheckInterval = 0.2f;
    private const float StuckTime = 1.15f;
    private const float JumpCooldown = 3.2f;
    private const float MinimumMove = 0.04f;
    private const float AheadDistance = 2.0f;
    private const float SmallTerrainIgnore = 0.7f;
    private const float MaxJumpUp = 2.2f;
    private const float MaxJumpDown = -3.5f;
    private const float ObstacleHeightRequired = 0.85f;

    private void Awake()
    {
        _terrainAssist = GetComponent<AgentTerrainStepAssist>();
        _character = GetComponent<Character>();
        _humanoid = GetComponent<Humanoid>();
        _zNetView = GetComponent<ZNetView>();
        _lastPosition = transform.position;

        if (_humanoid == null)
            enabled = false;

        CacheFields();
    }

    private void Update()
    {
        if (_terrainAssist == null)
            _terrainAssist = GetComponent<AgentTerrainStepAssist>();

        if (_character == null)
            _character = GetComponent<Character>();

        if (_humanoid == null)
            _humanoid = GetComponent<Humanoid>();

        if (_zNetView == null)
            _zNetView = GetComponent<ZNetView>();

        if (_humanoid == null)
        {
            enabled = false;
            return;
        }

        if (_terrainAssist == null || _character == null || _character.IsDead())
            return;

        if (_zNetView != null && _zNetView.IsValid() && !_zNetView.IsOwner())
            return;

        if (_jumpCooldown > 0f)
            _jumpCooldown -= Time.deltaTime;

        _checkTimer -= Time.deltaTime;

        if (_checkTimer > 0f)
            return;

        _checkTimer = CheckInterval;
        CheckSlopeJump();
    }

    private void CacheFields()
    {
        if (_terrainAssist == null)
            return;

        _hasTargetField = FindField(_terrainAssist.GetType(), "_hasTarget");
        _moveTargetField = FindField(_terrainAssist.GetType(), "_moveTarget");
        _reasonField = FindField(_terrainAssist.GetType(), "_reason");
    }

    private void CheckSlopeJump()
    {
        if (!TryGetMoveTarget(out Vector3 target, out string reason))
        {
            ResetStuck();
            return;
        }

        if (!ReasonAllowsJump(reason))
        {
            ResetStuck();
            return;
        }

        Vector3 current = transform.position;
        float moved = Vector3.Distance(current, _lastPosition);
        float speed = _character.GetVelocity().magnitude;
        _lastPosition = current;

        if (moved > MinimumMove || speed > 0.22f)
        {
            _stuckTimer = 0f;
            return;
        }

        _stuckTimer += CheckInterval;

        if (_stuckTimer < StuckTime || _jumpCooldown > 0f || !_character.IsOnGround())
            return;

        Vector3 direction = target - current;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.25f)
            return;

        direction.Normalize();

        if (!TryGetAheadHeight(current, direction, out float aheadHeight))
            return;

        float delta = aheadHeight - current.y;
        bool largeUp = delta >= SmallTerrainIgnore && delta <= MaxJumpUp;
        bool largeDown = delta <= -SmallTerrainIgnore && delta >= MaxJumpDown;
        bool obstructed = HasMeaningfulForwardObstacle(current, direction);

        if (!largeUp && !largeDown && !obstructed)
            return;

        DoSlopeJump(direction, largeDown);
    }

    private void ResetStuck()
    {
        _stuckTimer = 0f;
        _lastPosition = transform.position;
    }

    private bool TryGetMoveTarget(out Vector3 target, out string reason)
    {
        target = Vector3.zero;
        reason = string.Empty;

        if (_terrainAssist == null)
            return false;

        if (_hasTargetField == null || _moveTargetField == null)
            CacheFields();

        if (_hasTargetField == null || _moveTargetField == null)
            return false;

        if (!((bool)_hasTargetField.GetValue(_terrainAssist)))
            return false;

        object targetValue = _moveTargetField.GetValue(_terrainAssist);

        if (!(targetValue is Vector3 vector))
            return false;

        target = vector;

        if (_reasonField != null)
            reason = _reasonField.GetValue(_terrainAssist) as string ?? string.Empty;

        return true;
    }

    private bool TryGetAheadHeight(Vector3 current, Vector3 direction, out float height)
    {
        height = current.y;
        Vector3 origin = current + direction * AheadDistance + Vector3.up * 4f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 8f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && hit.collider.GetComponentInParent<AgentComponent>() == GetComponent<AgentComponent>())
                return false;

            height = hit.point.y;
            return true;
        }

        return false;
    }

    private bool HasMeaningfulForwardObstacle(Vector3 current, Vector3 direction)
    {
        Vector3 lowOrigin = current + Vector3.up * 0.45f;
        Vector3 highOrigin = current + Vector3.up * ObstacleHeightRequired;
        bool lowHit = Physics.Raycast(lowOrigin, direction, out RaycastHit low, 1.1f, ~0, QueryTriggerInteraction.Ignore);
        bool highClear = !Physics.Raycast(highOrigin, direction, 1.1f, ~0, QueryTriggerInteraction.Ignore);

        if (!lowHit || !highClear)
            return false;

        if (low.collider != null && low.collider.GetComponentInParent<AgentComponent>() == GetComponent<AgentComponent>())
            return false;

        return true;
    }

    private void DoSlopeJump(Vector3 direction, bool jumpDown)
    {
        _character.SetMoveDir(direction);

        if (_humanoid != null)
            _humanoid.SetRun(true);

        transform.position += direction * 0.55f + Vector3.up * 0.08f;
        _character.Jump();

        Rigidbody body = GetComponent<Rigidbody>();

        if (body != null)
        {
            body.position = transform.position;
            float vertical = jumpDown ? 1.1f : 2.6f;
            body.linearVelocity = new Vector3(direction.x * 4.25f, Mathf.Max(body.linearVelocity.y, vertical), direction.z * 4.25f);
        }

        _jumpCooldown = JumpCooldown;
        _stuckTimer = 0f;
    }

    private static bool ReasonAllowsJump(string reason)
    {
        return reason == "Follow" || reason == "ReturnHome" || reason == "ReturnToBed" || reason == "ReturnToMissingBed" || reason == "HuntTravel" || reason == "HuntLoot" || reason == "HuntReturnAnchor";
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
public static class AgentMovementTuningInstallerPatch
{
    private static void Postfix(AgentComponent __instance)
    {
        if (__instance == null)
            return;

        if (__instance.GetComponent<AgentSlopeJumpAssist>() == null)
            __instance.gameObject.AddComponent<AgentSlopeJumpAssist>();

        if (__instance.GetComponent<AgentWaterAvoidance>() == null)
            __instance.gameObject.AddComponent<AgentWaterAvoidance>();
    }
}

[HarmonyPatch(typeof(AgentBehaviourController), "MoveToPoint")]
public static class AgentSprintThresholdPatch
{
    private static void Prefix(AgentBehaviourController __instance, ref bool run)
    {
        if (!run || __instance == null)
            return;

        AgentStamina stamina = __instance.GetComponent<AgentStamina>();

        if (stamina == null)
            return;

        if (stamina.Stamina <= 0.15f)
            run = false;
    }
}
