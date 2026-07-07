using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class AgentDoorHandler : MonoBehaviour
{
    private sealed class OpenedDoor
    {
        public Door Door;
        public float Timer;
    }

    private readonly List<OpenedDoor> _openedDoors = new List<OpenedDoor>();
    private readonly Collider[] _doorHits = new Collider[24];
    private AgentComponent _agent;
    private Character _character;
    private Humanoid _humanoid;
    private ZNetView _zNetView;
    private float _scanTimer;
    private float _closeTimer;

    private const float ScanInterval = 0.2f;
    private const float CloseInterval = 0.75f;
    private const float ScanRadius = 3.0f;
    private const float ForwardOffset = 1.25f;
    private const float ForwardDot = 0.2f;
    private const float CloseDelay = 4.0f;
    private const float CloseDistance = 4.5f;
    private const float MinimumMoveSpeed = 0.05f;

    private void Awake()
    {
        _agent = GetComponent<AgentComponent>();
        _character = GetComponent<Character>();
        _humanoid = GetComponent<Humanoid>();
        _zNetView = GetComponent<ZNetView>();
    }

    private void Update()
    {
        if (_agent == null || _agent.Context == null)
            return;

        if (_character == null)
            _character = GetComponent<Character>();

        if (_humanoid == null)
            _humanoid = GetComponent<Humanoid>();

        if (_zNetView == null)
            _zNetView = GetComponent<ZNetView>();

        if (_character == null || _character.IsDead())
            return;

        if (_zNetView != null && _zNetView.IsValid() && !_zNetView.IsOwner())
            return;

        _scanTimer -= Time.deltaTime;
        _closeTimer -= Time.deltaTime;

        if (_scanTimer <= 0f)
        {
            _scanTimer = ScanInterval;
            TryOpenDoorAhead();
        }

        if (_closeTimer <= 0f)
        {
            _closeTimer = CloseInterval;
            UpdateOpenedDoors();
        }
    }

    private void TryOpenDoorAhead()
    {
        Vector3 direction = GetMoveDirection();

        if (direction.sqrMagnitude < 0.01f)
            return;

        Vector3 center = transform.position + Vector3.up * 0.8f + direction * ForwardOffset;
        int count = Physics.OverlapSphereNonAlloc(center, ScanRadius, _doorHits, ~0, QueryTriggerInteraction.Collide);
        Door bestDoor = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider hit = _doorHits[i];
            _doorHits[i] = null;

            if (hit == null)
                continue;

            Door door = hit.GetComponentInParent<Door>();

            if (door == null || IsDoorOpen(door))
                continue;

            Vector3 toDoor = door.transform.position - transform.position;
            toDoor.y = 0f;

            float distance = toDoor.magnitude;

            if (distance <= 0.01f || distance > ScanRadius)
                continue;

            float dot = Vector3.Dot(direction, toDoor.normalized);

            if (dot < ForwardDot)
                continue;

            float score = distance - dot;

            if (score < bestScore)
            {
                bestScore = score;
                bestDoor = door;
            }
        }

        if (bestDoor != null && SetDoorOpen(bestDoor, true))
            TrackOpenedDoor(bestDoor);
    }

    private Vector3 GetMoveDirection()
    {
        Vector3 velocity = _character != null ? _character.GetVelocity() : Vector3.zero;
        velocity.y = 0f;

        if (velocity.magnitude > MinimumMoveSpeed)
            return velocity.normalized;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.01f ? forward.normalized : Vector3.zero;
    }

    private bool SetDoorOpen(Door door, bool open)
    {
        if (door == null)
            return false;

        ZNetView view = door.GetComponent<ZNetView>();

        if (view != null && view.IsValid() && !view.IsOwner())
            view.ClaimOwnership();

        if (IsDoorOpen(door) == open)
            return true;

        if (TryInvokeDoorMethod(door, "SetOpen", open))
            return true;

        if (TryInvokeDoorMethod(door, "SetState", open))
            return true;

        if (TryInvokeDoorMethod(door, "SetDoorState", open))
            return true;

        return TrySetZdoDoorState(door, open);
    }

    private bool TryInvokeDoorMethod(Door door, string methodName, bool open)
    {
        MethodInfo method = FindMethod(door.GetType(), methodName, typeof(bool));

        if (method == null)
            return false;

        try
        {
            method.Invoke(door, new object[] { open });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TrySetZdoDoorState(Door door, bool open)
    {
        ZNetView view = door != null ? door.GetComponent<ZNetView>() : null;

        if (view == null || !view.IsValid())
            return false;

        ZDO zdo = view.GetZDO();

        if (zdo == null)
            return false;

        zdo.Set("state", open);
        zdo.Set("open", open);
        SetBoolField(door, "m_open", open);
        SetBoolField(door, "m_isOpen", open);
        SetBoolField(door, "m_opened", open);
        return true;
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

    private void TrackOpenedDoor(Door door)
    {
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

            if (opened == null || opened.Door == null)
            {
                _openedDoors.RemoveAt(i);
                continue;
            }

            opened.Timer -= CloseInterval;

            if (opened.Timer > 0f)
                continue;

            if (Vector3.Distance(transform.position, opened.Door.transform.position) < CloseDistance)
                continue;

            if (IsDoorOpen(opened.Door))
                SetDoorOpen(opened.Door, false);

            _openedDoors.RemoveAt(i);
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
