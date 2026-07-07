using System.Collections.Generic;
using UnityEngine;

namespace Core.Agent
{

    public interface IAgentBehaviour
    {
        BehaviourResult Tick(AgentContext ctx, GameObject agent);
    }

    public class BehaviourResult
    {
        public Vector3 MoveTarget;
        public Character AttackTarget;

        public bool HasMove;
        public bool HasAttack;
        public int Priority;
    }

    public class AgentBrain
    {
        private readonly List<IAgentBehaviour> _behaviours = new();

        public void AddBehaviour(IAgentBehaviour behaviour)
        {
            _behaviours.Add(behaviour);
        }
        private float _attackCooldown;
        public void Tick(AgentContext ctx, GameObject agent)
        {

            if (_attackCooldown > 0f)
            _attackCooldown -= Time.deltaTime;
            BehaviourResult best = null;

            foreach (var b in _behaviours)
            {
                var result = b.Tick(ctx, agent);
                if (result == null)
                    continue;

                if (best == null || result.Priority > best.Priority)
                {
                    best = result;
                }

            }

            if (best != null)
            {
                Execute(agent, best);
            }
        }

        private void Execute(GameObject agent, BehaviourResult result)
        {
            var humanoid = agent.GetComponent<Humanoid>();

            if (humanoid == null)
                return;

            if (result.HasAttack && result.AttackTarget != null)
            {
                Vector3 dir = result.AttackTarget.transform.position - agent.transform.position;
                dir.y = 0f;

                if (dir.sqrMagnitude > 0.01f)
                    agent.transform.rotation = Quaternion.LookRotation(dir);

                humanoid.SetMoveDir(Vector3.zero);
                humanoid.SetRun(false);

                if (result.AttackTarget.IsDead())
                    return;

                humanoid.StartAttack(null, false);
                return;
            }

            if (result.HasMove)
            {
                Vector3 dir = result.MoveTarget - agent.transform.position;
                dir.y = 0f;

                if (dir.sqrMagnitude <= 0.25f)
                {
                    humanoid.SetMoveDir(Vector3.zero);
                    humanoid.SetRun(false);
                    return;
                }

                humanoid.SetMoveDir(dir.normalized);
                humanoid.SetRun(true);
            }
        }
    }
}