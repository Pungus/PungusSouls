using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class AgentDoorHandler : MonoBehaviour
{
    private enum Phase
    {
        Idle,
        Approaching,
        WaitingForOpen,
        PassingThrough,
        Closing
    }

    private sealed class OpenedDoor
    {
        public Door Door;
        public float Timer;
    }

    private readonly List<OpenedDoor> _openedDoors = new List<OpenedDoor>();
    private readonly Collider[] _proximityBuffer = new Collider[16];

    private AgentComponent _agent;
    private Character _character;
    private Humanoid _humanoid;
    private ZNetView _zNetView;

    private Phase _phase;
    private Door _targetDoor;
    private Vector3 _doorPosition;
    private Vector3 _passDirection;
    private Vector3 _currentMoveTarget;
    private bool _currentRun;
    private string _currentReason;

    private float _approachTimer;
    private float _waitTimer;
    private float _passTimer;
    private float _cooldownTimer;
    private float _closeTimer;
    private float _scanTimer;
    private float _stuckTimer;
    private float _stuckCheckTimer;
    private Vector3 _lastStuckPosition;

    private static Door[] _doorCache;
    private static float _doorCacheExpiry;

    private const float DoorCacheInterval = 5f;
    private const float ScanInterval = 0.25f;
    private const float CloseInterval = 0.75f;
    private const float ScanRadius = 5.0f;
    private const float ProactiveScanRadius = 7.5f;
    private const float InteractDistance = 2.2f;
    private const float CloseDistance = 4.5f;
    private const float OpenWaitTime = 1.2f;
    private const float PassTimeout = 4.0f;
    private const float ApproachTimeout = 10f;
    private const float CloseDelay = 7.0f;
    private const float PostCloseCooldown = 2.5f;
    private const float StuckCheckInterval = 0.5f;
    private const float StuckMoveDistance = 0.2f;
    private const float StuckThreshold = 1.0f;
    private const float ForwardDot = 0.1f;

    public bool IsActive
    {
        get { return _phase != Phase.Idle; }
    }

    private void Awake()
    {
        _agent = GetComponent<AgentComponent>();
        _character = GetComponent<Character>();
        _humanoid = GetComponent<Humanoid>();
        _zNetView = GetComponent<ZNetView>();
        _lastStuckPosition = transform.position;
    }

    private void Update()
    {
        RefreshReferences();

        if (!CanRun())
            return;

        float dt = Time.deltaTime;

        _closeTimer -= dt;

        if (_closeTimer <= 0f)
        {
            _closeTimer = CloseInterval;
            UpdateOpenedDoors();
        }

        if (_phase == Phase.Idle)
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= dt;

            return;
        }

        UpdateActiveDoor(dt);
    }

    public bool TryHandleDoorForMove(float dt, Vector3 moveTarget, bool run, string reason)
    {
        RefreshReferences();

        if (!CanRun())
            return false;

        _currentMoveTarget = moveTarget;
        _currentRun = run;
        _currentReason = reason ?? string.Empty;

        if (_phase != Phase.Idle)
        {
            UpdateActiveDoor(dt);
            return true;
        }

        if (_cooldownTimer > 0f)
            return false;

        if (!ReasonAllowsDoor(_currentReason))
            return false;

        _scanTimer -= dt;
        UpdateStuck(dt, moveTarget);

        if (_scanTimer > 0f && _stuckTimer < StuckThreshold)
            return false;

        _scanTimer = ScanInterval;

        Door door = FindBestDoorForMove(moveTarget, _stuckTimer >= StuckThreshold);

        if (door == null)
            return false;

        BeginDoorHandling(door, moveTarget);
        UpdateActiveDoor(dt);
        return true;
    }

    private void UpdateActiveDoor(float dt)
    {
        switch (_phase)
        {
            case Phase.Approaching:
                UpdateApproaching(dt);
                break;
            case Phase.WaitingForOpen:
                UpdateWaitingForOpen(dt);
                break;
            case Phase.PassingThrough:
                UpdatePassingThrough(dt);
                break;
            case Phase.Closing:
                UpdateClosing();
                break;
        }
    }

    private void BeginDoorHandling(Door door, Vector3 moveTarget)
    {
        if (door == null)
            return;

        _targetDoor = door;
        _doorPosition = door.transform.position;

        _passDirection = moveTarget - transform.position;
        _passDirection.y = 0f;

        if (_passDirection.sqrMagnitude <= 0.01f)
        {
            _passDirection = _doorPosition - transform.position;
            _passDirection.y = 0f;
        }

        if (_passDirection.sqrMagnitude <= 0.01f)
            _passDirection = transform.forward;

        _passDirection.Normalize();

        _phase = Phase.Approaching;
        _approachTimer = 0f;
        _waitTimer = 0f;
        _passTimer = 0f;
        _stuckTimer = 0f;
        _stuckCheckTimer = 0f;
        _lastStuckPosition = transform.position;
    }

    private void UpdateApproaching(float dt)
    {
        if (!ValidateDoor())
            return;

        _approachTimer += dt;

        if (_approachTimer > ApproachTimeout)
        {
            ResetToIdle(true);
            return;
        }

        float distance = DistanceXZ(transform.position, _doorPosition);

        if (distance <= InteractDistance)
        {
            StopMoving();
            FacePoint(_doorPosition);
            TryInteractDoor(_targetDoor);
            _phase = Phase.WaitingForOpen;
            _waitTimer = OpenWaitTime;
            return;
        }

        Vector3 direction = _doorPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
            MoveDirect(direction.normalized, true);
    }

    private void UpdateWaitingForOpen(float dt)
    {
        if (!ValidateDoor())
            return;

        FacePoint(_doorPosition);
        StopMoving();

        _waitTimer -= dt;

        if (_waitTimer > 0f)
            return;

        if (IsDoorOpen(_targetDoor))
        {
            _phase = Phase.PassingThrough;
            _passTimer = 0f;
            return;
        }

        if (TryInteractDoor(_targetDoor))
        {
            _waitTimer = OpenWaitTime;
            return;
        }

        ResetToIdle(true);
    }

    private void UpdatePassingThrough(float dt)
    {
        if (!ValidateDoor())
            return;

        _passTimer += dt;

        if (!IsDoorOpen(_targetDoor))
            TryInteractDoor(_targetDoor);

        float distanceFromDoor = DistanceXZ(transform.position, _doorPosition);

        if (distanceFromDoor > CloseDistance || _passTimer > PassTimeout)
        {
            TrackOpenedDoor(_targetDoor);
            _phase = Phase.Closing;
            return;
        }

        Vector3 direction = _currentMoveTarget - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
            direction = _passDirection;

        if (direction.sqrMagnitude > 0.01f)
            MoveDirect(direction.normalized, true);
    }

    private void UpdateClosing()
    {
        if (_targetDoor != null)
        {
            TrackOpenedDoor(_targetDoor);
        }

        ResetToIdle(true);
    }

    private Door FindBestDoorForMove(Vector3 moveTarget, bool proactive)
    {
        Vector3 moveDirection = moveTarget - transform.position;
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude <= 0.01f)
        {
            moveDirection = _character != null ? _character.GetVelocity() : Vector3.zero;
            moveDirection.y = 0f;
        }

        if (moveDirection.sqrMagnitude <= 0.01f)
            moveDirection = transform.forward;

        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude <= 0.01f)
            return null;

        moveDirection.Normalize();

        float radius = proactive ? ProactiveScanRadius : ScanRadius;
        Door[] doors = GetDoorCache();

        Door best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < doors.Length; i++)
        {
            Door door = doors[i];

            if (!IsValidClosedDoor(door))
                continue;

            Vector3 toDoor = door.transform.position - transform.position;
            toDoor.y = 0f;

            float distance = toDoor.magnitude;

            if (distance <= 0.01f || distance > radius)
                continue;

            float dot = Vector3.Dot(moveDirection, toDoor.normalized);

            if (!proactive && dot < ForwardDot)
                continue;

            float targetDistance = DistanceXZ(door.transform.position, moveTarget);
            float score = distance - dot + targetDistance * 0.15f;

            if (score < bestScore)
            {
                bestScore = score;
                best = door;
            }
        }

        return best;
    }

    private void UpdateStuck(float dt, Vector3 moveTarget)
    {
        _stuckCheckTimer += dt;

        if (_stuckCheckTimer < StuckCheckInterval)
            return;

        float moved = Vector3.Distance(transform.position, _lastStuckPosition);
        _lastStuckPosition = transform.position;
        _stuckCheckTimer = 0f;

        if (DistanceXZ(transform.position, moveTarget) < InteractDistance)
        {
            _stuckTimer = 0f;
            return;
        }

        if (moved < StuckMoveDistance)
            _stuckTimer += StuckCheckInterval;
        else
            _stuckTimer = 0f;
    }

    private static Door[] GetDoorCache()
    {
        if (_doorCache == null || Time.time >= _doorCacheExpiry)
        {
            _doorCache = Object.FindObjectsByType<Door>(FindObjectsSortMode.None);
            _doorCacheExpiry = Time.time + DoorCacheInterval;
        }

        return _doorCache;
    }

    private bool TryInteractDoor(Door door)
    {
        if (door == null || _humanoid == null)
            return false;

        ZNetView view = door.GetComponent<ZNetView>();

        if (view != null && view.IsValid() && !view.IsOwner())
            view.ClaimOwnership();

        MethodInfo interact = FindMethod(door.GetType(), "Interact", typeof(Humanoid), typeof(bool), typeof(bool));

        if (interact == null)
            return false;

        try
        {
            object result = interact.Invoke(door, new object[] { _humanoid, false, false });
            return !(result is bool value) || value;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidClosedDoor(Door door)
    {
        if (door == null || !door)
            return false;

        ZNetView view = door.GetComponent<ZNetView>();

        if (view == null || !view.IsValid() || view.GetZDO() == null)
            return false;

        if (view.GetZDO().GetInt(ZDOVars.s_state) != 0)
            return false;

        if (door.m_keyItem != null)
            return false;

        if (door.m_checkGuardStone && !PrivateArea.CheckAccess(door.transform.position, 0f, false))
            return false;

        return true;
    }

    private bool IsDoorOpen(Door door)
    {
        if (door == null)
            return false;

        ZNetView view = door.GetComponent<ZNetView>();

        if (view != null && view.IsValid())
        {
            ZDO zdo = view.GetZDO();

            if (zdo != null && zdo.GetInt(ZDOVars.s_state) != 0)
                return true;
        }

        string[] fields = { "m_open", "m_isOpen", "m_opened" };

        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo field = FindField(door.GetType(), fields[i]);

            if (field != null && field.FieldType == typeof(bool))
                return (bool)field.GetValue(door);
        }

        return false;
    }

    private void TrackOpenedDoor(Door door)
    {
        if (door == null)
            return;

        for (int i = 0; i < _openedDoors.Count; i++)
        {
            if (_openedDoors[i].Door == door)
            {
                _openedDoors[i].Timer = CloseDelay;
                return;
            }
        }

        _openedDoors.Add(new OpenedDoor { Door = door, Timer = CloseDelay });
    }

    private void UpdateOpenedDoors()
    {
        for (int i = _openedDoors.Count - 1; i >= 0; i--)
        {
            OpenedDoor opened = _openedDoors[i];

            if (opened == null || opened.Door == null || !opened.Door)
            {
                _openedDoors.RemoveAt(i);
                continue;
            }

            opened.Timer -= CloseInterval;

            if (opened.Timer > 0f)
                continue;

            if (IsAnyoneNearDoor(opened.Door))
            {
                opened.Timer = CloseDelay;
                continue;
            }

            if (IsDoorOpen(opened.Door))
                TryInteractDoor(opened.Door);

            _openedDoors.RemoveAt(i);
        }
    }

    private bool IsAnyoneNearDoor(Door door)
    {
        if (door == null)
            return false;

        Vector3 position = door.transform.position;

        if (Player.m_localPlayer != null && Vector3.Distance(Player.m_localPlayer.transform.position, position) < 2.5f)
            return true;

        int count = Physics.OverlapSphereNonAlloc(position, 2.5f, _proximityBuffer, ~0, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider collider = _proximityBuffer[i];
            _proximityBuffer[i] = null;

            if (collider == null || collider.gameObject == gameObject)
                continue;

            if (collider.GetComponentInParent<AgentComponent>() != null)
                return true;
        }

        return false;
    }

    private bool ValidateDoor()
    {
        if (_targetDoor == null || !_targetDoor)
        {
            ResetToIdle(true);
            return false;
        }

        return true;
    }

    private void ResetToIdle(bool withCooldown)
    {
        _phase = Phase.Idle;
        _targetDoor = null;
        _approachTimer = 0f;
        _waitTimer = 0f;
        _passTimer = 0f;
        _stuckTimer = 0f;
        _stuckCheckTimer = 0f;
        _lastStuckPosition = transform.position;

        if (withCooldown)
            _cooldownTimer = PostCloseCooldown;
    }

    private bool CanRun()
    {
        if (_agent == null || _agent.Context == null)
            return false;

        if (_character == null || _character.IsDead())
            return false;

        if (_zNetView != null && _zNetView.IsValid() && !_zNetView.IsOwner())
            return false;

        if (AgentContainerComponent.IsRadialOpen)
            return false;

        return true;
    }

    private void RefreshReferences()
    {
        if (_agent == null)
            _agent = GetComponent<AgentComponent>();

        if (_character == null)
            _character = GetComponent<Character>();

        if (_humanoid == null)
            _humanoid = GetComponent<Humanoid>();

        if (_zNetView == null)
            _zNetView = GetComponent<ZNetView>();
    }

    private void MoveDirect(Vector3 direction, bool run)
    {
        if (_character == null)
            return;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
            return;

        direction.Normalize();

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(direction, Vector3.up),
            720f * Time.deltaTime);

        _character.SetMoveDir(direction);

        if (_humanoid != null)
            _humanoid.SetRun(run);
    }

    private void StopMoving()
    {
        if (_character != null)
            _character.SetMoveDir(Vector3.zero);

        if (_humanoid != null)
        {
            _humanoid.SetRun(false);
            _humanoid.SetWalk(false);
        }
    }

    private void FacePoint(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
            return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(direction.normalized, Vector3.up),
            720f * Time.deltaTime);
    }

    public static bool ReasonAllowsDoor(string reason)
    {
        if (string.IsNullOrEmpty(reason))
            return true;

        return reason == "Follow" ||
               reason == "ReturnHome" ||
               reason == "ReturnToBed" ||
               reason == "ReturnToMissingBed" ||
               reason == "BedNavigate" ||
               reason == "DepositInventory" ||
               reason == "RestPrepareComfort" ||
               reason == "IdleComfort" ||
               reason == "ResourceWork" ||
               reason == "FarmingHarvest" ||
               reason == "FarmingPlant" ||
               reason == "Fishing" ||
               reason == "Lumbering" ||
               reason == "Mining" ||
               reason == "Quarrying" ||
               reason == "Gather" ||
               reason == "AmbientActivity" ||
               reason == "HuntTravel" ||
               reason == "HuntLoot" ||
               reason == "HuntReturnAnchor" ||
               reason == "HomeWander" ||
               reason == "Patrol" ||
               reason == "RepairWork" ||
               reason == "SmeltingWork" ||
               reason == "FarmingWork" ||
               reason == "FishingWork" ||
               reason == "CookWork" ||
               reason == "ReturnToHomeObject" ||
               reason == "Velocity" ||
               reason == "Forward";
    }

    private static float DistanceXZ(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return Mathf.Sqrt(x * x + z * z);
    }

    private static MethodInfo FindMethod(System.Type type, string name, params System.Type[] parameters)
    {
        while (type != null)
        {
            MethodInfo method = parameters == null || parameters.Length == 0
                ? type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                : type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameters, null);

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
public static class AgentDoorHandlerInstallerPatch
{
    private static void Postfix(AgentComponent __instance)
    {
        if (__instance == null)
            return;

        if (__instance.GetComponent<AgentDoorHandler>() == null)
            __instance.gameObject.AddComponent<AgentDoorHandler>();
    }
}