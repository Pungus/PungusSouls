using Core.Agent;
using UnityEngine;

namespace Modules.Follow
{
    public class FollowBehaviour : IAgentBehaviour
    {
        public BehaviourResult Tick(AgentContext ctx, GameObject agent)
        {
            if (ctx == null || agent == null)
                return null;

            if (!ctx.FollowEnabled)
                return null;

            if (ctx.StayHomeEnabled)
                return null;

            Player player = Player.m_localPlayer;
            if (player == null)
                return null;

            Vector3 target = player.transform.position;
            float distance = Vector3.Distance(agent.transform.position, target);

            if (distance >= ctx.FollowTeleportDistance)
            {
                Vector3 teleportPos = FindTeleportPosition(player.transform.position);
                agent.transform.position = teleportPos;

                return new BehaviourResult
                {
                    HasMove = true,
                    MoveTarget = agent.transform.position,
                    Priority = 100
                };
            }

            if (distance <= ctx.FollowStopDistance)
            {
                return new BehaviourResult
                {
                    HasMove = true,
                    MoveTarget = agent.transform.position,
                    Priority = 100
                };
            }

            if (distance >= ctx.FollowResumeDistance)
            {
                return new BehaviourResult
                {
                    HasMove = true,
                    MoveTarget = target,
                    Priority = 100
                };
            }

            return null;
        }

        private static Vector3 FindTeleportPosition(Vector3 origin)
        {
            for (int i = 0; i < 12; i++)
            {
                Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(2f, 4f);
                Vector3 pos = origin + new Vector3(offset.x, 0f, offset.y);

                if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(pos, out float height))
                {
                    pos.y = height;
                    return pos;
                }
            }

            return origin + Vector3.right * 2f;
        }
    }
}