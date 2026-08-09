using Core.Agent;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static Core.Agent.AgentContext;

public sealed class AgentPortalFollowRuntime : MonoBehaviour
{
    private sealed class PendingTeleport
    {
        public Player Player;
        public Vector3 PlayerStartPosition;
        public Vector3 RequestedDestination;
        public readonly List<AgentComponent> Agents = new List<AgentComponent>();
        public float Age;
    }

    private readonly List<PendingTeleport> _pending = new List<PendingTeleport>();

    private const float ExecuteDelay = 0.75f;
    private const float AbortDelay = 5f;
    private const float PlayerMovedThreshold = 2f;
    private const float LandingRadius = 3.2f;
    private const float FloorSearchHeightOffset = 0.4f;

    public void Schedule(Player player, Vector3 playerStartPosition, Vector3 requestedDestination, List<AgentComponent> agents)
    {
        if (player == null || agents == null || agents.Count == 0)
            return;

        PendingTeleport pending = new PendingTeleport
        {
            Player = player,
            PlayerStartPosition = playerStartPosition,
            RequestedDestination = requestedDestination
        };

        pending.Agents.AddRange(agents);
        _pending.Add(pending);

    }

    private void Update()
    {
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            PendingTeleport pending = _pending[i];

            if (pending == null || pending.Player == null)
            {
                _pending.RemoveAt(i);
                continue;
            }

            pending.Age += Time.deltaTime;

            bool playerHasMoved = Vector3.Distance(pending.Player.transform.position, pending.PlayerStartPosition) >= PlayerMovedThreshold;
            bool canExecute = pending.Age >= ExecuteDelay && playerHasMoved;
            bool timedOut = pending.Age >= AbortDelay;

            if (!canExecute && !timedOut)
                continue;

            if (canExecute)
                TeleportAgents(pending);

            _pending.RemoveAt(i);
        }
    }

    private void TeleportAgents(PendingTeleport pending)
    {
        Vector3 playerPosition = pending.Player.transform.position;
        int teleported = 0;

        for (int i = 0; i < pending.Agents.Count; i++)
        {
            AgentComponent agent = pending.Agents[i];

            if (!IsValidFollower(agent))
                continue;

            Vector3 landing = FindLandingPosition(playerPosition, i);
            PlaceAgent(agent, landing, pending.Player.transform.rotation);
            teleported++;
        }

    }

    private Vector3 FindLandingPosition(Vector3 center, int index)
    {
        float angle = index * 137.5f * Mathf.Deg2Rad;
        float radius = 1.6f + (index % 3) * 0.75f;
        Vector3 candidate = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

        for (int attempt = 0; attempt < 12; attempt++)
        {
            float attemptAngle = (index * 137.5f + attempt * 45f) * Mathf.Deg2Rad;
            float attemptRadius = 1.4f + (attempt % 4) * 0.75f;
            Vector3 test = center + new Vector3(Mathf.Cos(attemptAngle) * attemptRadius, FloorSearchHeightOffset, Mathf.Sin(attemptAngle) * attemptRadius);

            if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(test, out float height))
            {
                test.y = height;
                return test;
            }
        }

        if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(candidate, out float fallbackHeight))
            candidate.y = fallbackHeight;
        else
            candidate.y = center.y;

        return candidate;
    }

    private void PlaceAgent(AgentComponent agent, Vector3 position, Quaternion rotation)
    {
        if (agent == null)
            return;

        ZNetView view = agent.GetComponent<ZNetView>();

        if (view != null && view.IsValid() && !view.IsOwner())
            view.ClaimOwnership();

        Character character = agent.GetComponent<Character>();

        if (character != null)
            character.SetMoveDir(Vector3.zero);

        Humanoid humanoid = agent.GetComponent<Humanoid>();

        if (humanoid != null)
        {
            humanoid.SetRun(false);
            humanoid.SetWalk(false);
        }

        Rigidbody body = agent.GetComponent<Rigidbody>();

        if (body != null)
        {
            body.position = position;
            body.rotation = rotation;

            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        agent.transform.position = position;
        agent.transform.rotation = rotation;

        if (view != null && view.IsValid())
        {
            ZDO zdo = view.GetZDO();

            if (zdo != null)
            {
                zdo.SetPosition(position);
                zdo.SetRotation(rotation);
            }
        }
    }

    private static bool IsValidFollower(AgentComponent agent)
    {
        if (agent == null || agent.Context == null)
            return false;

        if (agent.Context.StateMode != AgentStateMode.Follow)
            return false;

        Character character = agent.GetComponent<Character>();

        if (character != null && character.IsDead())
            return false;

        return true;
    }
}

[HarmonyPatch]
public static class AgentPortalFollowPatch
{
    private sealed class TeleportState
    {
        public Vector3 PlayerStartPosition;
        public Vector3 RequestedDestination;
        public List<AgentComponent> Followers;
    }

    private const float FollowTeleportPortalRange = 35f;

    private static IEnumerable<MethodBase> TargetMethods()
    {
        MethodInfo[] methods = typeof(Player).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (MethodInfo method in methods)
        {
            if (method == null || method.Name != "TeleportTo")
                continue;

            ParameterInfo[] parameters = method.GetParameters();

            if (parameters.Length == 0)
                continue;

            bool hasVectorDestination = false;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType == typeof(Vector3))
                {
                    hasVectorDestination = true;
                    break;
                }
            }

            if (hasVectorDestination)
                yield return method;
        }
    }

    private static void Prefix(Player __instance, object[] __args, out TeleportState __state)
    {
        __state = null;

        if (__instance == null || __args == null)
            return;

        Vector3 destination;

        if (!TryGetDestination(__args, out destination))
            return;

        List<AgentComponent> followers = CollectNearbyFollowers(__instance.transform.position);

        if (followers.Count == 0)
            return;

        __state = new TeleportState
        {
            PlayerStartPosition = __instance.transform.position,
            RequestedDestination = destination,
            Followers = followers
        };
    }

    private static void Postfix(Player __instance, TeleportState __state)
    {
        if (__instance == null || __state == null || __state.Followers == null || __state.Followers.Count == 0)
            return;

        AgentPortalFollowRuntime runtime = __instance.GetComponent<AgentPortalFollowRuntime>();

        if (runtime == null)
            runtime = __instance.gameObject.AddComponent<AgentPortalFollowRuntime>();

        runtime.Schedule(__instance, __state.PlayerStartPosition, __state.RequestedDestination, __state.Followers);
    }

    private static bool TryGetDestination(object[] args, out Vector3 destination)
    {
        destination = Vector3.zero;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is Vector3 vector)
            {
                destination = vector;
                return true;
            }
        }

        return false;
    }

    private static List<AgentComponent> CollectNearbyFollowers(Vector3 playerPosition)
    {
        List<AgentComponent> followers = new List<AgentComponent>();
        AgentComponent[] agents = UnityEngine.Object.FindObjectsByType<AgentComponent>(FindObjectsSortMode.None);

        foreach (AgentComponent agent in agents)
        {
            if (agent == null || agent.Context == null)
                continue;

            if (agent.Context.StateMode != AgentStateMode.Follow)
                continue;

            Character character = agent.GetComponent<Character>();

            if (character != null && character.IsDead())
                continue;

            float distance = Vector3.Distance(agent.transform.position, playerPosition);

            if (distance > FollowTeleportPortalRange)
                continue;

            followers.Add(agent);
        }

        return followers;
    }
}