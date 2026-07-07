using Core.Agent;
using UnityEngine;

namespace Modules.HomeZone
{
    public enum HomeZoneMode
    {
        Passive,
        Defensive,
        Aggressive,
        Patrol
    }

    public class HomeZoneBehaviour : IAgentBehaviour
    {
        private const float AttackRange = 2.25f;
        private const float PassiveFleeRange = 12f;
        private const float DefensiveRange = 10f;
        private const float AggressiveSightRange = 25f;
        private const float PatrolCombatRange = 12f;
        private const float PatrolPursuitRange = 8f;

        public BehaviourResult Tick(AgentContext ctx, GameObject agent)
        {
            if (ctx == null || agent == null)
                return null;

            if (!ctx.StayHomeEnabled)
                return null;

            HomeZoneController zone = ctx.HomeZone;

            if (zone == null || !zone.IsSet)
                return null;

            float distanceFromHome = Vector3.Distance(agent.transform.position, zone.Center);

            if (distanceFromHome > zone.Radius)
            {
                return new BehaviourResult
                {
                    HasMove = true,
                    MoveTarget = zone.Center,
                    Priority = 130
                };
            }

            switch (ctx.HomeZoneMode)
            {
                case HomeZoneMode.Passive:
                    return UpdatePassive(ctx, agent, zone);

                case HomeZoneMode.Defensive:
                    return UpdateDefensive(ctx, agent, zone);

                case HomeZoneMode.Aggressive:
                    return UpdateAggressive(ctx, agent, zone);

                case HomeZoneMode.Patrol:
                    return UpdatePatrol(ctx, agent, zone);

                default:
                    return UpdatePassive(ctx, agent, zone);
            }
        }

        private BehaviourResult UpdatePassive(AgentContext ctx, GameObject agent, HomeZoneController zone)
        {
            Character enemy = FindClosestEnemy(agent, PassiveFleeRange);

            if (enemy != null)
            {
                Vector3 fleeTarget = GetFleeTarget(agent.transform.position, enemy.transform.position, zone);

                return new BehaviourResult
                {
                    HasMove = true,
                    MoveTarget = fleeTarget,
                    Priority = 80
                };
            }

            return Roam(ctx, agent, zone, 40);
        }

        private BehaviourResult UpdateDefensive(AgentContext ctx, GameObject agent, HomeZoneController zone)
        {
            Character enemy = FindClosestEnemy(agent, DefensiveRange);

            if (enemy == null)
                return Roam(ctx, agent, zone, 40);

            float enemyDistanceFromHome = Vector3.Distance(enemy.transform.position, zone.Center);

            if (enemyDistanceFromHome > zone.Radius)
                return Roam(ctx, agent, zone, 40);

            float distanceToEnemy = Vector3.Distance(agent.transform.position, enemy.transform.position);

            if (distanceToEnemy <= AttackRange)
            {
                return new BehaviourResult
                {
                    HasAttack = true,
                    AttackTarget = enemy,
                    Priority = 90
                };
            }

            return new BehaviourResult
            {
                HasMove = true,
                MoveTarget = ClampToZone(enemy.transform.position, zone),
                Priority = 85
            };
        }

        private BehaviourResult UpdateAggressive(AgentContext ctx, GameObject agent, HomeZoneController zone)
        {
            Character enemy = FindClosestEnemyInsideZone(agent, zone, AggressiveSightRange);

            if (enemy == null)
                return Roam(ctx, agent, zone, 40);

            float distanceToEnemy = Vector3.Distance(agent.transform.position, enemy.transform.position);

            if (distanceToEnemy <= AttackRange)
            {
                return new BehaviourResult
                {
                    HasAttack = true,
                    AttackTarget = enemy,
                    Priority = 100
                };
            }

            return new BehaviourResult
            {
                HasMove = true,
                MoveTarget = ClampToZone(enemy.transform.position, zone),
                Priority = 95
            };
        }

        private BehaviourResult UpdatePatrol(AgentContext ctx, GameObject agent, HomeZoneController zone)
        {
            Character enemy = FindClosestEnemyInsideZone(agent, zone, PatrolCombatRange);

            if (enemy != null)
            {
                float distanceToEnemy = Vector3.Distance(agent.transform.position, enemy.transform.position);
                float distanceFromPatrolTarget = Vector3.Distance(enemy.transform.position, ctx.RoamTarget);

                if (distanceToEnemy <= AttackRange)
                {
                    return new BehaviourResult
                    {
                        HasAttack = true,
                        AttackTarget = enemy,
                        Priority = 90
                    };
                }

                if (distanceFromPatrolTarget <= PatrolPursuitRange)
                {
                    return new BehaviourResult
                    {
                        HasMove = true,
                        MoveTarget = ClampToZone(enemy.transform.position, zone),
                        Priority = 85
                    };
                }
            }

            return PatrolPerimeter(ctx, agent, zone);
        }

        private BehaviourResult Roam(AgentContext ctx, GameObject agent, HomeZoneController zone, int priority)
        {
            ctx.RoamTimer -= Time.deltaTime;

            bool needTarget = ctx.RoamTimer <= 0f;
            bool closeToTarget = Vector3.Distance(agent.transform.position, ctx.RoamTarget) <= 0.75f;
            bool targetOutsideZone = Vector3.Distance(ctx.RoamTarget, zone.Center) > zone.Radius;

            if (needTarget || closeToTarget || targetOutsideZone)
            {
                ctx.RoamTimer = Random.Range(3f, 7f);

                Vector3 random = Random.insideUnitSphere;
                random.y = 0f;

                if (random.sqrMagnitude < 0.01f)
                    random = Vector3.forward;

                float distance = Random.Range(1f, zone.Radius * 0.75f);
                ctx.RoamTarget = zone.Center + random.normalized * distance;
            }

            if (Vector3.Distance(agent.transform.position, ctx.RoamTarget) <= 0.75f)
            {
                return new BehaviourResult
                {
                    HasMove = true,
                    MoveTarget = agent.transform.position,
                    Priority = priority
                };
            }

            return new BehaviourResult
            {
                HasMove = true,
                MoveTarget = ctx.RoamTarget,
                Priority = priority
            };
        }

        private BehaviourResult PatrolPerimeter(AgentContext ctx, GameObject agent, HomeZoneController zone)
        {
            ctx.RoamTimer -= Time.deltaTime;

            bool needTarget = ctx.RoamTimer <= 0f;
            bool closeToTarget = Vector3.Distance(agent.transform.position, ctx.RoamTarget) <= 1f;
            bool targetOutsideZone = Vector3.Distance(ctx.RoamTarget, zone.Center) > zone.Radius;

            if (needTarget || closeToTarget || targetOutsideZone)
            {
                ctx.RoamTimer = Random.Range(4f, 8f);

                float angle = Random.Range(0f, Mathf.PI * 2f);

                Vector3 edge = new Vector3(
                    Mathf.Cos(angle) * zone.Radius * 0.9f,
                    0f,
                    Mathf.Sin(angle) * zone.Radius * 0.9f
                );

                ctx.RoamTarget = zone.Center + edge;
            }

            return new BehaviourResult
            {
                HasMove = true,
                MoveTarget = ctx.RoamTarget,
                Priority = 60
            };
        }

        private static bool IsEnemy(Character self, Character other)
        {
            if (self == null || other == null)
                return false;

            if (other == self)
                return false;

            if (other.IsDead())
                return false;

            if (other.IsPlayer())
                return false;

            return BaseAI.IsEnemy(self, other);
        }

        private static Character FindClosestEnemy(GameObject agent, float range)
        {
            Character self = agent.GetComponent<Character>();

            if (self == null)
                return null;

            Character closest = null;
            float closestDistance = range;

            foreach (Character character in Character.GetAllCharacters())
            {
                if (character == null || character == self)
                    continue;

                if (!IsEnemy(self, character))
                    continue;

                float distance = Vector3.Distance(agent.transform.position, character.transform.position);

                if (distance < closestDistance)
                {
                    closest = character;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        private static Character FindClosestEnemyInsideZone(GameObject agent, HomeZoneController zone, float range)
        {
            Character self = agent.GetComponent<Character>();

            if (self == null)
                return null;

            Character closest = null;
            float closestDistance = range;

            foreach (Character character in Character.GetAllCharacters())
            {
                if (character == null || character == self)
                    continue;

                if (!IsEnemy(self, character))
                    continue;

                float distanceToAgent = Vector3.Distance(agent.transform.position, character.transform.position);
                float distanceToHome = Vector3.Distance(character.transform.position, zone.Center);

                if (distanceToAgent < closestDistance && distanceToHome <= zone.Radius)
                {
                    closest = character;
                    closestDistance = distanceToAgent;
                }
            }

            return closest;
        }

        private static Vector3 GetFleeTarget(Vector3 agentPosition, Vector3 enemyPosition, HomeZoneController zone)
        {
            Vector3 direction = agentPosition - enemyPosition;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.01f)
                direction = Random.insideUnitSphere;

            direction.y = 0f;

            Vector3 target = agentPosition + direction.normalized * 8f;

            return ClampToZone(target, zone);
        }

        private static Vector3 ClampToZone(Vector3 position, HomeZoneController zone)
        {
            Vector3 offset = position - zone.Center;
            offset.y = 0f;

            float max = zone.Radius * 0.9f;

            if (offset.magnitude > max)
                offset = offset.normalized * max;

            return zone.Center + offset;
        }
    }
}