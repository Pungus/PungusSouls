using Modules.HomeZone;
using Systems.Inventory;
using UnityEngine;

namespace Core.Agent
{
    public enum AgentBehaviourState
    {
        Idle,
        Follow,
        StayHome,
        Hunt,
        Cook,
        Gather,
        Lumbering,
        Mining,
        Quarrying,
        Repair
    }

    public class AgentContext
    {
        public enum AgentBehaviourMode
        {
            Passive,
            Defensive,
            Aggressive
        }

        public enum AgentStateMode
        {
            Idle,
            Follow,
            StayHome
        }

        public enum AgentTaskMode
        {
            None,
            Hunt,
            Patrol,
            Cook,
            Gather,
            Lumbering,
            Mining,
            Quarrying,
            Repair,
            Fishing,
            Smelting,
            Farming
        }

        public AgentInventory Inventory;
        public Vector3 Position;
        public HomeZoneController HomeZone;
        public Vector3 IdleOrigin;
        public float IdleRadius = 18f;
        public bool HasHomeZone;
        public float FollowStopDistance = 3f;
        public float FollowResumeDistance = 4f;
        public float FollowTeleportDistance = 50f;
        public float RoamTimer;
        public Vector3 RoamTarget;
        public float AttackCooldown;

        public AgentBehaviourMode BehaviourMode = AgentBehaviourMode.Defensive;
        public AgentStateMode StateMode = AgentStateMode.Idle;
        public AgentTaskMode TaskMode = AgentTaskMode.None;

        public AgentBehaviourState BehaviourState = AgentBehaviourState.Idle;
        public HomeZoneMode StayHomeMode = HomeZoneMode.Defensive;

        public bool IsFollowing
        {
            get { return StateMode == AgentStateMode.Follow; }
        }

        public bool IsStayingHome
        {
            get { return StateMode == AgentStateMode.StayHome; }
        }

        public bool IsIdle
        {
            get { return StateMode == AgentStateMode.Idle; }
        }

        public bool IsHunting
        {
            get { return TaskMode == AgentTaskMode.Hunt; }
        }

        public HomeZoneMode HomeZoneMode
        {
            get { return StayHomeMode; }
            set
            {
                StayHomeMode = value;

                if (value == HomeZoneMode.Passive)
                    BehaviourMode = AgentBehaviourMode.Passive;
                else if (value == HomeZoneMode.Aggressive)
                    BehaviourMode = AgentBehaviourMode.Aggressive;
                else
                    BehaviourMode = AgentBehaviourMode.Defensive;

                if (value == HomeZoneMode.Patrol)
                    TaskMode = AgentTaskMode.Patrol;
            }
        }

        public bool FollowEnabled
        {
            get { return StateMode == AgentStateMode.Follow; }
            set
            {
                if (value)
                    StateMode = AgentStateMode.Follow;
                else if (StateMode == AgentStateMode.Follow)
                    StateMode = AgentStateMode.Idle;

                SyncLegacyFields();
            }
        }

        public bool StayHomeEnabled
        {
            get { return StateMode == AgentStateMode.StayHome; }
            set
            {
                if (value)
                    StateMode = AgentStateMode.StayHome;
                else if (StateMode == AgentStateMode.StayHome)
                    StateMode = AgentStateMode.Idle;

                SyncLegacyFields();
            }
        }

        public bool HuntEnabled
        {
            get { return TaskMode == AgentTaskMode.Hunt; }
            set
            {
                TaskMode = value ? AgentTaskMode.Hunt : AgentTaskMode.None;
                SyncLegacyFields();
            }
        }

        public bool WanderEnabled
        {
            get { return StateMode == AgentStateMode.Idle; }
            set
            {
                if (value)
                    StateMode = AgentStateMode.Idle;

                SyncLegacyFields();
            }
        }

        public void SyncLegacyFields()
        {
            if (StateMode == AgentStateMode.Follow)
                BehaviourState = AgentBehaviourState.Follow;
            else if (StateMode == AgentStateMode.StayHome)
                BehaviourState = AgentBehaviourState.StayHome;
            else
                BehaviourState = AgentBehaviourState.Idle;

            if (BehaviourMode == AgentBehaviourMode.Passive)
                StayHomeMode = HomeZoneMode.Passive;
            else if (BehaviourMode == AgentBehaviourMode.Aggressive)
                StayHomeMode = HomeZoneMode.Aggressive;
            else if (TaskMode == AgentTaskMode.Patrol)
                StayHomeMode = HomeZoneMode.Patrol;
            else
                StayHomeMode = HomeZoneMode.Defensive;
        }
    }
}
