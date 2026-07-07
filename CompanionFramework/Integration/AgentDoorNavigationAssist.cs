using HarmonyLib;
using System.Reflection;
using UnityEngine;

public class AgentDoorNavigationAssist : MonoBehaviour
{
    private AgentComponent _agent;
    private Character _character;
    private Humanoid _humanoid;
    private ZNetView _zNetView;
    private AgentTerrainStepAssist _terrainAssist;
    private FieldInfo _hasTargetField;
    private FieldInfo _moveTargetField;
    private FieldInfo _reasonField;
    private Door _activeDoor;
    private Vector3 _pushTarget;
    private float _pushTimer;
    private float _scanTimer;

    private const float ScanInterval = 0.15f;
    private const float ScanRadius = 2.7f;
    private const float DoorForwardDistance = 1.6f;
    private const float PushThroughDuration = 1.6f;
    private const float PushThroughDistance = 2.6f;
    private const float DoorDot = 0.12f;

    private readonly Collider[] _hits = new Collider[16];

    private void Awake()
    {
        _agent = GetComponent<AgentComponent>();
        _character = GetComponent<Character>();
        _humanoid = GetComponent<Humanoid>();
        _zNetView = GetComponent<ZNetView>();
        _terrainAssist = GetComponent<AgentTerrainStepAssist>();
        CacheTerrainFields();
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

        if (_terrainAssist == null)
        {
            _terrainAssist = GetComponent<AgentTerrainStepAssist>();
            CacheTerrainFields();
        }

        if (_agent == null || _character == null || _character.IsDead())
            return;

        if (_zNetView != null && _zNetView.IsValid() && !_zNetView.IsOwner())
            return;

        if (_pushTimer > 0f)
        {
            _pushTimer -= Time.deltaTime;
            PushThroughDoor();
            return;
        }

        _scanTimer -= Time.deltaTime;

        if (_scanTimer > 0f)
            return;

        _scanTimer = ScanInterval;
        TryFindAndUseDoor();
    }

    private void CacheTerrainFields()
    {
        if (_terrainAssist == null)
            return;

        _hasTargetField = FindField(_terrainAssist.GetType(), "_hasTarget");
        _moveTargetField = FindField(_terrainAssist.GetType(), "_moveTarget");
        _reasonField = FindField(_terrainAssist.GetType(), "_reason");
    }

    private void TryFindAndUseDoor()
    {
        if (!TryGetMoveDirection(out Vector3 direction, out string reason))
            return;

        if (!ReasonAllowsDoor(reason))
            return;

        Vector3 center = transform.position + Vector3.up * 0.85f + direction * DoorForwardDistance;
        int count = Physics.OverlapSphereNonAlloc(center, ScanRadius, _hits, ~0, QueryTriggerInteraction.Collide);
        Door best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider hit = _hits[i];
            _hits[i] = null;

            if (hit == null)
                continue;

            Door door = hit.GetComponentInParent<Door>();

            if (door == null)
                continue;

            Vector3 toDoor = door.transform.position - transform.position;
            toDoor.y = 0f;

            if (toDoor.sqrMagnitude < 0.01f)
                continue;

            float dot = Vector3.Dot(direction, toDoor.normalized);

            if (dot < DoorDot)
                continue;

            float score = toDoor.magnitude - dot;

            if (score < bestScore)
            {
                bestScore = score;
                best = door;
            }
        }

        if (best == null)
            return;

        if (!IsDoorOpen(best))
            UseDoor(best);

        StartPushThrough(best, direction);
    }

    private bool TryGetMoveDirection(out Vector3 direction, out string reason)
    {
        direction = Vector3.zero;
        reason = string.Empty;

        if (_terrainAssist != null && _hasTargetField != null && _moveTargetField != null)
        {
            object hasTargetRaw = _hasTargetField.GetValue(_terrainAssist);

            if (hasTargetRaw is bool hasTarget && hasTarget)
            {
                object targetRaw = _moveTargetField.GetValue(_terrainAssist);

                if (targetRaw is Vector3 target)
                {
                    direction = target - transform.position;
                    direction.y = 0f;

                    if (_reasonField != null)
                        reason = _reasonField.GetValue(_terrainAssist) as string ?? string.Empty;

                    if (direction.sqrMagnitude > 0.01f)
                    {
                        direction.Normalize();
                        return true;
                    }
                }
            }
        }

        Vector3 velocity = _character != null ? _character.GetVelocity() : Vector3.zero;
        velocity.y = 0f;

        if (velocity.sqrMagnitude > 0.04f)
        {
            direction = velocity.normalized;
            reason = "Velocity";
            return true;
        }

        direction = transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            direction.Normalize();
            reason = "Forward";
            return true;
        }

        return false;
    }

    private void StartPushThrough(Door door, Vector3 direction)
    {
        _activeDoor = door;
        _pushTarget = door.transform.position + direction * PushThroughDistance;

        if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(_pushTarget, out float height))
            _pushTarget.y = height;

        _pushTimer = PushThroughDuration;
    }

    private void PushThroughDoor()
    {
        if (_activeDoor == null)
            return;

        if (!IsDoorOpen(_activeDoor))
            UseDoor(_activeDoor);

        Vector3 direction = _pushTarget - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.1f)
        {
            _pushTimer = 0f;
            return;
        }

        direction.Normalize();
        _character.SetMoveDir(direction);

        if (_humanoid != null)
            _humanoid.SetRun(false);
    }

    private bool UseDoor(Door door)
    {
        if (door == null)
            return false;

        ZNetView doorView = door.GetComponent<ZNetView>();

        if (doorView != null && doorView.IsValid() && !doorView.IsOwner())
            doorView.ClaimOwnership();

        MethodInfo interact = FindMethod(typeof(Door), "Interact", typeof(Humanoid), typeof(bool), typeof(bool));

        if (interact != null)
        {
            object result = interact.Invoke(door, new object[] { _humanoid, false, false });
            return !(result is bool value) || value;
        }

        return false;
    }

    private bool IsDoorOpen(Door door)
    {
        if (door == null)
            return false;

        string[] fields = { "m_open", "m_isOpen", "m_opened" };

        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo field = FindField(door.GetType(), fields[i]);

            if (field != null && field.FieldType == typeof(bool))
                return (bool)field.GetValue(door);
        }

        ZNetView view = door.GetComponent<ZNetView>();

        if (view != null && view.IsValid())
        {
            ZDO zdo = view.GetZDO();

            if (zdo != null)
                return zdo.GetBool("state", false) || zdo.GetBool("open", false);
        }

        return false;
    }

    private static bool ReasonAllowsDoor(string reason)
    {
        if (string.IsNullOrEmpty(reason))
            return true;

        return reason == "Follow" ||
               reason == "ReturnHome" ||
               reason == "ReturnToBed" ||
               reason == "ReturnToMissingBed" ||
               reason == "HuntTravel" ||
               reason == "HuntLoot" ||
               reason == "HuntReturnAnchor" ||
               reason == "HomeWander" ||
               reason == "Patrol" ||
               reason == "Velocity" ||
               reason == "Forward";
    }

    private static MethodInfo FindMethod(System.Type type, string name, params System.Type[] parameters)
    {
        while (type != null)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameters, null);

            if (method != null)
                return method;

            type = type.BaseType;
        }

        return null;
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
public static class AgentDoorNavigationAssistInstallerPatch
{
    private static void Postfix(AgentComponent __instance)
    {
        if (__instance == null)
            return;

        if (__instance.GetComponent<AgentDoorNavigationAssist>() == null)
            __instance.gameObject.AddComponent<AgentDoorNavigationAssist>();
    }
}
