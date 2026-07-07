using Core.Agent;
using UnityEngine;

namespace Integration
{

    public static class GameBootstrap
    {
        public static AgentBrain BuildAgent(AgentContext ctx)
        {
            return new AgentBrain();
        }
    }
}