using Core.Agent;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static Core.Agent.AgentContext;

public class AgentBehaviourController : MonoBehaviour
{
    private enum AgentHuntState
    {
        Searching,
        TravellingToTarget,
        Fighting,
        CollectingLoot,
        ReturningToAnchor,
        Depositing
    }

    private AgentComponent _agent;
    private MonsterAI _monsterAI;
    private Character _character;
    private Humanoid _humanoid;
    private ZNetView _zNetView;
    private Animator _animator;
    private MethodInfo _moveToMethod;
    private MethodInfo _stopMovingMethod;

    private Character _huntTarget;
    private Character _lastAssignedNativeTarget;
    private ItemDrop _currentLootTarget;
    private AgentHuntState _huntState = AgentHuntState.Searching;
    private Vector3 _huntTargetLastKnownPosition;
    private Vector3 _huntDeathPosition;
    private Vector3 _patrolTarget;
    private readonly HashSet<int> _ignoredLootInstanceIds = new HashSet<int>();

    private bool _isPlayingBedSleepAnimation;
    private bool _sleepAnimationMissingLogged;
    private bool _bedSleepLogged;
    private bool _isWakingFromBed;
    private bool _hasPendingBedExit;
    private Vector3 _pendingBedExitPosition;
    private float _bedWakeLockTimer;
    private float _savedAnimatorSpeed = 1f;
    private bool _isPlacedAtBed;
    private float _bedWakeGraceTimer;

    private float _huntSearchTimer;
    private float _huntLogTimer;
    private float _moveDebugTimer;
    private float _huntStateTimer;
    private float _lootSearchTimer;
    private float _nativeAssignLogTimer;
    private float _patrolTimer;
    private float _patrolAngle;
    private float _pathStuckTimer;
    private Vector3 _pathStuckCheckPosition;
    private float _pathStuckCheckTimer;
    private Vector3 _homeWanderTarget;
    private float _homeWanderTimer;
    private bool _homeWanderTargetSet;
    private int _patrolDirection = 1;

    private const float FollowTeleportDistance = 70f;
    private const float HomeReturnDistanceGrace = 1.5f;
    private const float HomeReturnStopDistance = 2f;
    private const float PatrolStepDegrees = 35f;
    private const float PatrolRefreshInterval = 4f;
    private const float PatrolArrivalDistance = 3.5f;
    private const float HuntSearchInterval = 1f;
    private const float HuntTargetRange = 45f;
    private const float HuntMaxChaseDistance = 75f;
    private const float HuntNativeEngageRange = 8f;
    private const float HuntLogInterval = 3f;
    private const float MoveDebugInterval = 2f;
    private const float NativeAssignLogInterval = 5f;
    private const float HuntCollectDelay = 1.5f;
    private const float HuntReturnAnchorDistance = 8f;
    private const float LootSearchInterval = 0.75f;
    private const float LootSearchRadius = 25f;
    private const float LootPickupDistance = 3f;
    private const float LootCollectTimeout = 25f;
    private const float DepositChestSearchRadius = 20f;
    private const int MaxDepositChests = 8;
    private const float AssignedChestMatchDistance = 3f;
    private const float DefaultHomeWanderInterval = 5f;
    private const float DefaultHomeWanderRange = 4f;
    private const float HomeWanderArrivalDistance = 2f;
    private const float BedReachDistance = 3.2f;
    private const float BedMoveStopDistance = 2.8f;
    private const float BedForceSnapDistance = 4.2f;
    private const float BedSnapHeightOffset = 0.45f;
    private const float BedSnapForwardOffset = 0.15f;
    private const float BedWakeLockDuration = 2.2f;
    private const float BedWakeGraceDuration = 3f;
    private const float BedExitSideOffset = 1.4f;
    private const float BedExitBackOffset = -0.4f;

    private const float RestPreparationTimeout = 45f;
    private float _restPreparationTimer;

    private static readonly int SleepingHash = Animator.StringToHash("sleeping");
    private static readonly int WakeupHash = Animator.StringToHash("wakeup");

    private static readonly HashSet<string> HuntablePrefabs = new HashSet<string>
    {
        "Neck",
        "Boar",
        "Deer",
        "Hare",
        "Wolf",
        "Greyling",
        "Greydwarf"
    };

    private void Awake()
    {
        _agent = GetComponent<AgentComponent>();
        _monsterAI = GetComponent<MonsterAI>();
        _character = GetComponent<Character>();
        _humanoid = GetComponent<Humanoid>();
        _zNetView = GetComponent<ZNetView>();
        _animator = GetComponentInChildren<Animator>();
        _moveToMethod = FindMethod(typeof(BaseAI), "MoveTo", typeof(float), typeof(Vector3), typeof(float), typeof(bool));
        _stopMovingMethod = FindMethod(typeof(BaseAI), "StopMoving");
        _patrolAngle = Random.Range(0f, 360f);
        _patrolDirection = Random.value > 0.5f ? 1 : -1;
    }

    public bool ControlledAIUpdate(float dt)
    {
        if (_agent == null || _agent.Context == null || _monsterAI == null || _character == null)
            return true;

        if (_zNetView != null && _zNetView.IsValid() && !_zNetView.IsOwner())
            return true;

        if (_character.IsDead())
            return true;

        if (AgentContainerComponent.IsRadialOpen)
        {
            StopMoving();
            return false;
        }

        UpdateTimers(dt);
        ApplyModeSettings();

        if (_isWakingFromBed)
            return UpdateBedWakeLock(dt);

        if (ShouldSleepAtAssignedBed())
        {
            UpdateAssignedBedSleep(dt);
            return false;
        }

        if (_agent.Context.TaskMode == AgentTaskMode.Hunt)
        {
            if (_huntState == AgentHuntState.Fighting)
            {
                HuntFighting();

                if (_huntState == AgentHuntState.Fighting)
                    return true;
            }

            if (_huntState == AgentHuntState.CollectingLoot ||
                _huntState == AgentHuntState.ReturningToAnchor ||
                _huntState == AgentHuntState.Depositing)
            {
                TryHandleTask(dt);
                return false;
            }
        }


        if (TryHandleBehaviourThreats())
            return true;

        if (TryWaitForRestedBeforeActivity(dt))
            return false;

        if (TryHandleTask(dt))
            return false;

        if (TryLetNativeIdleOwnTick())
            return true;

        UpdateStateMovement(dt);
        return false;
    }
    private bool TryWaitForRestedBeforeActivity(float dt)
    {
        if (_agent == null || _agent.Context == null)
            return false;

        if (_agent.Context.StateMode == AgentStateMode.Follow)
        {
            _restPreparationTimer = 0f;
            return false;
        }

        if (_agent.Context.TaskMode == AgentTaskMode.None)
        {
            _restPreparationTimer = 0f;
            return false;
        }

        if (_agent.Context.TaskMode == AgentTaskMode.Hunt && _huntState == AgentHuntState.Fighting)
            return false;

        AgentRested rested = GetComponent<AgentRested>();

        if (rested == null)
            return false;

        if (rested.IsRested)
        {
            _restPreparationTimer = 0f;
            return false;
        }

        if (_agent.Context.HomeZone == null || !_agent.Context.HomeZone.IsSet)
            return false;

        _restPreparationTimer += dt;

        if (_restPreparationTimer >= RestPreparationTimeout)
            return false;

        ClearNativeAggro();

        Vector3 home = _agent.Context.HomeZone.Center;
        float distance = DistanceXZ(transform.position, home);

        if (distance > HomeReturnStopDistance)
        {
            MoveToPoint(dt, home, HomeReturnStopDistance, false, "RestPrepareHome");
            return true;
        }

        StopMoving();

        if (_character != null)
            _character.SetMoveDir(Vector3.zero);

        return true;
    }
    private void UpdateTimers(float dt)
    {
        if (_bedWakeGraceTimer > 0f)
            _bedWakeGraceTimer -= dt;

        _moveDebugTimer = Mathf.Max(0f, _moveDebugTimer - dt);
        _nativeAssignLogTimer = Mathf.Max(0f, _nativeAssignLogTimer - dt);

        if (_agent.Context.TaskMode != AgentTaskMode.Hunt)
            ResetHuntStateIfNeeded();
    }

    private bool UpdateBedWakeLock(float dt)
    {
        StopMoving();

        if (_character != null)
            _character.SetMoveDir(Vector3.zero);

        _bedWakeLockTimer -= dt;

        if (_bedWakeLockTimer <= 0f)
        {
            _isWakingFromBed = false;

            if (_hasPendingBedExit)
            {
                PlaceAtPosition(_pendingBedExitPosition, transform.rotation);
                _hasPendingBedExit = false;
            }
        }

        return false;
    }

    private bool TryHandleTask(float dt)
    {
        switch (_agent.Context.TaskMode)
        {
            case AgentTaskMode.Hunt:
                return UpdateHunt(dt);
            case AgentTaskMode.Patrol:
                if (_agent.Context.StateMode == AgentStateMode.StayHome && _agent.Context.HomeZone != null && _agent.Context.HomeZone.IsSet)
                {
                    UpdatePatrol(dt, _agent.Context.HomeZone.Center, Mathf.Max(1f, _agent.Context.HomeZone.Radius));
                    return true;
                }
                return false;
            case AgentTaskMode.None:
            default:
                return false;
        }
    }

    private void UpdateStateMovement(float dt)
    {
        switch (_agent.Context.StateMode)
        {
            case AgentStateMode.Follow:
                UpdateFollow(dt);
                break;
            case AgentStateMode.StayHome:
                UpdateStayHomeAnchor(dt);
                break;
            case AgentStateMode.Idle:
            default:
                UpdateIdle(dt);
                break;
        }
    }

    private bool TryHandleBehaviourThreats()
    {
        Character existingTarget = GetNativeTargetCreature();

        if (existingTarget != null && existingTarget.IsDead())
        {
            ClearNativeAggro();
            existingTarget = null;
        }

        if (existingTarget != null && !TargetAllowedByCurrentMode(existingTarget))
        {
            ClearNativeAggro();
            existingTarget = null;
        }

        if (existingTarget != null)
            return true;

        Character enemy = null;

        switch (_agent.Context.BehaviourMode)
        {
            case AgentBehaviourMode.Passive:
                ClearNativeAggro();
                return false;
            case AgentBehaviourMode.Defensive:
                enemy = FindDefensiveEnemy();
                break;
            case AgentBehaviourMode.Aggressive:
                enemy = FindAggressiveEnemy();
                break;
        }

        if (enemy == null)
            return false;

        AssignNativeTarget(enemy);
        return true;
    }

    private Character FindDefensiveEnemy()
    {
        if (_agent.Context.StateMode == AgentStateMode.StayHome)
            return FindEnemyInHomeZone();

        return FindNearbyCombatEnemy(12f);
    }

    private Character FindAggressiveEnemy()
    {
        if (_agent.Context.StateMode == AgentStateMode.StayHome)
            return FindEnemyInHomeZone();

        return FindNearbyCombatEnemy(24f);
    }

    private Character FindEnemyInHomeZone()
    {
        if (_agent.Context.HomeZone == null || !_agent.Context.HomeZone.IsSet)
            return null;

        Vector3 home = _agent.Context.HomeZone.Center;
        float radius = _agent.Context.HomeZone.Radius;
        Character best = null;
        float bestDistance = radius;

        foreach (Character candidate in Character.GetAllCharacters())
        {
            if (!IsValidCombatEnemy(candidate))
                continue;

            if (DistanceXZ(candidate.transform.position, home) > radius)
                continue;

            float distanceToNpc = DistanceXZ(candidate.transform.position, transform.position);

            if (distanceToNpc < bestDistance)
            {
                best = candidate;
                bestDistance = distanceToNpc;
            }
        }

        return best;
    }

    private Character FindNearbyCombatEnemy(float range)
    {
        Character best = null;
        float bestDistance = range;

        foreach (Character candidate in Character.GetAllCharacters())
        {
            if (!IsValidCombatEnemy(candidate))
                continue;

            float distance = DistanceXZ(transform.position, candidate.transform.position);

            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }

    private bool IsValidCombatEnemy(Character candidate)
    {
        if (candidate == null || candidate == _character)
            return false;

        if (candidate.IsDead() || candidate.IsPlayer())
            return false;

        return BaseAI.IsEnemy(_character, candidate);
    }

    private void UpdateFollow(float dt)
    {
        Player player = Player.m_localPlayer;

        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance >= FollowTeleportDistance)
        {
            TeleportNear(player.transform.position);
            return;
        }

        if (distance <= _agent.Context.FollowStopDistance)
        {
            StopMoving();
            return;
        }

        float playerSpeed = player.GetVelocity().magnitude;
        bool shouldRun =
            distance > _agent.Context.FollowStopDistance + 4f ||
            playerSpeed > 4f;

        MoveToPoint(dt, player.transform.position, _agent.Context.FollowStopDistance, shouldRun, "Follow");
    }

    private void UpdateIdle(float dt)
    {
        Vector3 origin = _agent.Context.IdleOrigin;

        if (origin == Vector3.zero)
            origin = transform.position;

        UpdateHomeWander(dt, origin, Mathf.Max(2f, _agent.Context.IdleRadius));
    }

    private void UpdateStayHomeAnchor(float dt)
    {
        if (_agent.Context.HomeZone == null || !_agent.Context.HomeZone.IsSet)
            return;

        Vector3 home = _agent.Context.HomeZone.Center;
        float radius = Mathf.Max(1f, _agent.Context.HomeZone.Radius);
        float distanceFromHome = DistanceXZ(transform.position, home);

        if (distanceFromHome > radius + HomeReturnDistanceGrace)
        {
            MoveToPoint(dt, home, HomeReturnStopDistance, distanceFromHome > 8f, "ReturnHome");
            return;
        }

        UpdateHomeWander(dt, home, radius);
    }

    private void UpdateHomeWander(float dt, Vector3 home, float radius)
    {
        _homeWanderTimer -= dt;

        float distanceToTarget = DistanceXZ(transform.position, _homeWanderTarget);
        bool needsNewTarget = !_homeWanderTargetSet || _homeWanderTimer <= 0f || distanceToTarget <= HomeWanderArrivalDistance;
        bool targetOutsideHome = DistanceXZ(_homeWanderTarget, home) > radius;

        if (needsNewTarget || targetOutsideHome)
            PickHomeWanderTarget(home, radius);

        if (!_homeWanderTargetSet)
        {
            StopMoving();
            return;
        }

        bool moved = MoveToPoint(dt, _homeWanderTarget, HomeWanderArrivalDistance, false, "HomeWander");

        if (!moved && _pathStuckTimer >= 2.25f)
        {
            _homeWanderTargetSet = false;
            _homeWanderTimer = 0f;
            _pathStuckTimer = 0f;
        }
    }

    private void PickHomeWanderTarget(Vector3 home, float radius)
    {
        float monsterInterval = GetFloatField(_monsterAI, "m_randomMoveInterval", DefaultHomeWanderInterval);
        float monsterRange = GetFloatField(_monsterAI, "m_randomMoveRange", DefaultHomeWanderRange);
        _homeWanderTimer = Mathf.Max(1f, monsterInterval);
        float range = Mathf.Clamp(monsterRange, 1f, Mathf.Max(1f, radius - 1f));

        for (int i = 0; i < 12; i++)
        {
            Vector2 offset = Random.insideUnitCircle * range;
            Vector3 candidate = home + new Vector3(offset.x, 0f, offset.y);

            if (DistanceXZ(candidate, home) > radius)
                continue;

            if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(candidate, out float height))
                candidate.y = height;

            _homeWanderTarget = candidate;
            _homeWanderTargetSet = true;
            return;
        }

        _homeWanderTarget = home;
        _homeWanderTargetSet = true;
    }

    private bool UpdateHunt(float dt)
    {
        _huntSearchTimer -= dt;
        _huntLogTimer -= dt;
        _lootSearchTimer -= dt;
        _huntStateTimer += dt;

        switch (_huntState)
        {
            case AgentHuntState.Searching:
                return HuntSearching();
            case AgentHuntState.TravellingToTarget:
                HuntTravelling(dt);
                return true;
            case AgentHuntState.Fighting:
                HuntFighting();
                return true;
            case AgentHuntState.CollectingLoot:
                HuntCollectingLoot(dt);
                return true;
            case AgentHuntState.ReturningToAnchor:
                HuntReturningToAnchor(dt);
                return true;
            case AgentHuntState.Depositing:
                HuntDepositing();
                return true;
            default:
                return false;
        }
    }

    private bool HuntSearching()
    {
        if (_huntSearchTimer > 0f)
            return false;

        _huntSearchTimer = HuntSearchInterval;
        _huntTarget = FindHuntTarget();

        if (_huntTarget == null)
            return false;

        _huntTargetLastKnownPosition = _huntTarget.transform.position;
        LogHunt("Target selected " + GetPrefabName(_huntTarget.gameObject) + " distance=" + DistanceXZ(transform.position, _huntTarget.transform.position).ToString("0.0"), true);
        ChangeHuntState(AgentHuntState.TravellingToTarget);
        return true;
    }

    private void HuntTravelling(float dt)
    {
        if (!IsValidHuntTarget(_huntTarget))
        {
            ClearNativeAggro();
            ChangeHuntState(AgentHuntState.Searching);
            return;
        }

        _huntTargetLastKnownPosition = _huntTarget.transform.position;
        float distance = DistanceXZ(transform.position, _huntTargetLastKnownPosition);

        if (distance > HuntMaxChaseDistance)
        {
            ClearNativeAggro();
            ChangeHuntState(AgentHuntState.Searching);
            return;
        }

        if (distance <= HuntNativeEngageRange)
        {
            AssignNativeTarget(_huntTarget);
            ChangeHuntState(AgentHuntState.Fighting);
            return;
        }

        MoveToPoint(Time.deltaTime, _huntTargetLastKnownPosition, HuntNativeEngageRange, true, "HuntTravel");
    }

    private void HuntFighting()
    {
        if (_huntTarget != null)
            _huntTargetLastKnownPosition = _huntTarget.transform.position;


        if (_huntTarget != null && _huntTarget.IsDead())
        {
            _huntDeathPosition = _huntTarget.transform.position;
            _currentLootTarget = null;
            _lootSearchTimer = 0f;
            ClearNativeAggro();
            ChangeHuntState(AgentHuntState.CollectingLoot);
            return;
        }


        if (_huntTarget == null || !IsValidHuntTarget(_huntTarget))
        {
            _huntDeathPosition = _huntTargetLastKnownPosition;
            ClearNativeAggro();
            ChangeHuntState(AgentHuntState.CollectingLoot);
            return;
        }

        if (GetNativeTargetCreature() == null)
            AssignNativeTarget(_huntTarget);
    }

    private void HuntCollectingLoot(float dt)
    {
        if (_huntStateTimer < HuntCollectDelay)
            return;

        if (_huntStateTimer > LootCollectTimeout)
        {
            ChangeHuntState(AgentHuntState.ReturningToAnchor);
            return;
        }

        if (_currentLootTarget == null || _currentLootTarget.gameObject == null)
        {
            if (_lootSearchTimer > 0f)
                return;

            _lootSearchTimer = LootSearchInterval;
            _currentLootTarget = FindNearestLoot();

            if (_currentLootTarget == null)
                return;
        }

        float distance = DistanceXZ(transform.position, _currentLootTarget.transform.position);

        if (distance > LootPickupDistance)
        {
            MoveToPoint(dt, _currentLootTarget.transform.position, LootPickupDistance, false, "HuntLoot");
            return;
        }

        AgentItemPickup pickup = GetComponent<AgentItemPickup>();
        Inventory inventory = _agent != null && _agent.ValheimContainer != null ? _agent.ValheimContainer.m_inventory : null;

        if (pickup != null && inventory != null && pickup.TryPickupDrop(_currentLootTarget, inventory))
        {
            _currentLootTarget = null;
            _lootSearchTimer = 0f;
            return;
        }
    }

    private void HuntReturningToAnchor(float dt)
    {
        Vector3 anchor = GetCurrentAnchorPosition();
        float distance = DistanceXZ(transform.position, anchor);

        if (distance <= HuntReturnAnchorDistance)
        {
            StopMoving();

            if (_agent.Context.StateMode == AgentStateMode.StayHome)
                ChangeHuntState(AgentHuntState.Depositing);
            else
            {
                ClearHuntCycleState();
                ChangeHuntState(AgentHuntState.Searching);
            }

            return;
        }

        MoveToPoint(dt, anchor, HuntReturnAnchorDistance, true, "HuntReturnAnchor");
    }

    private Vector3 GetCurrentAnchorPosition()
    {
        if (_agent.Context.StateMode == AgentStateMode.Follow && Player.m_localPlayer != null)
            return Player.m_localPlayer.transform.position;

        if (_agent.Context.StateMode == AgentStateMode.StayHome && _agent.Context.HomeZone != null && _agent.Context.HomeZone.IsSet)
            return _agent.Context.HomeZone.Center;

        return _agent.Context.IdleOrigin == Vector3.zero ? transform.position : _agent.Context.IdleOrigin;
    }

    private void HuntDepositing()
    {
        int moved = DepositToAssignedChests();

        if (moved == 0)
        {
            Container depositContainer = FindNearestDepositContainer();

            if (depositContainer != null)
                moved += TransferInventoryToDepositContainer(depositContainer);
        }

        if (moved > 0)
            LogHunt("Deposited " + moved + " item stacks", true);

        ClearHuntCycleState();
        ChangeHuntState(AgentHuntState.Searching);
    }

    private Character FindHuntTarget()
    {
        Character best = null;
        float bestDistance = HuntTargetRange;
        Vector3 anchor = GetCurrentAnchorPosition();

        foreach (Character candidate in Character.GetAllCharacters())
        {
            if (!IsValidHuntTarget(candidate))
                continue;

            if (DistanceXZ(candidate.transform.position, anchor) > HuntTargetRange)
                continue;

            float distance = DistanceXZ(transform.position, candidate.transform.position);

            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }

    private bool IsValidHuntTarget(Character candidate)
    {
        if (candidate == null || candidate == _character)
            return false;

        if (candidate.IsDead() || candidate.IsPlayer())
            return false;

        string prefabName = GetPrefabName(candidate.gameObject);

        if (!HuntablePrefabs.Contains(prefabName))
            return false;

        return DistanceXZ(transform.position, candidate.transform.position) <= HuntMaxChaseDistance;
    }

    private ItemDrop FindNearestLoot()
    {
        ItemDrop[] drops = UnityEngine.Object.FindObjectsOfType<ItemDrop>();
        ItemDrop best = null;
        float bestDistance = LootSearchRadius;

        foreach (ItemDrop drop in drops)
        {
            if (drop == null || drop.m_itemData == null || drop.gameObject == null)
                continue;

            if (_ignoredLootInstanceIds.Contains(drop.GetInstanceID()))
                continue;


            float distanceFromDeath = DistanceXZ(drop.transform.position, _huntDeathPosition);
            float distanceFromNpc = DistanceXZ(transform.position, drop.transform.position);

            if (distanceFromDeath > LootSearchRadius && distanceFromNpc > LootSearchRadius)
                continue;


            if (distanceFromNpc < bestDistance)
            {
                best = drop;
                bestDistance = distanceFromNpc;
            }
        }

        return best;
    }

    private bool TryPickupLoot(ItemDrop drop)
    {
        if (drop == null || drop.m_itemData == null)
            return true;

        if (_agent == null || _agent.ValheimContainer == null || _agent.ValheimContainer.m_inventory == null)
            return false;

        Inventory inventory = _agent.ValheimContainer.m_inventory;
        ItemDrop.ItemData item = drop.m_itemData.Clone();

        if (item == null)
            return true;

        if (inventory.GetTotalWeight() + item.GetWeight() > _agent.MaxCarryWeight)
            return false;

        if (!inventory.AddItem(item))
        {
            _ignoredLootInstanceIds.Add(drop.GetInstanceID());
            return true;
        }

        DestroyLootObject(drop);
        return true;
    }

    private void DestroyLootObject(ItemDrop drop)
    {
        if (drop == null || drop.gameObject == null)
            return;

        ZNetView dropView = drop.GetComponent<ZNetView>();

        if (dropView != null && dropView.IsValid() && !dropView.IsOwner())
            dropView.ClaimOwnership();

        if (ZNetScene.instance != null)
            ZNetScene.instance.Destroy(drop.gameObject);
        else
            UnityEngine.Object.Destroy(drop.gameObject);
    }

    private void ChangeHuntState(AgentHuntState state)
    {
        if (_huntState == state)
            return;

        if (state == AgentHuntState.CollectingLoot)
        {
            _currentLootTarget = null;
            _ignoredLootInstanceIds.Clear();
        }

        _huntState = state;
        _huntStateTimer = 0f;
        _lootSearchTimer = 0f;
        //Debug.Log("[Agent Hunt] State=" + _huntState);
    }

    private void ClearHuntCycleState()
    {
        _huntTarget = null;
        _currentLootTarget = null;
        _lastAssignedNativeTarget = null;
        _ignoredLootInstanceIds.Clear();
        ClearNativeAggro();
    }

    private void ResetHuntStateIfNeeded()
    {
        if (_huntState == AgentHuntState.Searching && _huntTarget == null && _currentLootTarget == null)
            return;

        ClearHuntCycleState();
        _huntState = AgentHuntState.Searching;
        _huntStateTimer = 0f;
        _huntSearchTimer = 0f;
        _lootSearchTimer = 0f;
    }

    private bool ShouldSleepAtAssignedBed()
    {
        if (_agent == null || _agent.Context == null)
            return false;

        if (_agent.Context.StateMode != AgentStateMode.StayHome)
            return false;

        if (_agent.Context.TaskMode == AgentTaskMode.Patrol)
            return false;

        if (_zNetView == null || !_zNetView.IsValid())
            return false;

        ZDO zdo = _zNetView.GetZDO();

        if (zdo == null || !zdo.GetBool("agent_has_bed", false))
            return false;

        if (!EnvMan.IsNight())
            return false;

        if (_bedWakeGraceTimer > 0f)
            return true;

        return _agent.Context.BehaviourMode == AgentBehaviourMode.Passive || FindEnemyInHomeZone() == null;
    }

    private void UpdateAssignedBedSleep(float dt)
    {
        ZDO zdo = _zNetView.GetZDO();

        if (zdo == null)
            return;

        Vector3 savedBedPosition = zdo.GetVec3("agent_bed_pos", Vector3.zero);
        Bed bed = FindBedNearPosition(savedBedPosition);

        if (bed == null)
        {
            ClearAssignedBedSleepState();
            MoveToPoint(dt, savedBedPosition, BedMoveStopDistance, false, "ReturnToMissingBed");
            return;
        }

        ClearNativeAggro();
        float distanceToBed = Mathf.Min(GetDistanceToBedSurface(bed), DistanceXZ(transform.position, bed.transform.position));

        if (distanceToBed <= BedReachDistance)
        {
            StopMoving();

            if (!_isPlacedAtBed)
            {
                PlaceAtBedSleepPosition(bed);
                _isPlacedAtBed = true;
            }

            EnterAssignedBedSleepState();

            if (!_bedSleepLogged)
            {
                _bedSleepLogged = true;
                //Debug.Log("[Agent Bed] Sleeping at assigned bed " + bed.name);
            }

            return;
        }

        ClearAssignedBedSleepState();
        bool moved = MoveToPoint(dt, bed.transform.position, BedMoveStopDistance, false, "ReturnToBed");

        if (!moved && distanceToBed <= BedForceSnapDistance)
        {
            StopMoving();

            if (!_isPlacedAtBed)
            {
                PlaceAtBedSleepPosition(bed);
                _isPlacedAtBed = true;
            }

            EnterAssignedBedSleepState();
        }
    }

    private Bed FindBedNearPosition(Vector3 position)
    {
        Bed best = null;
        float bestDistance = 10f;
        Bed[] beds = UnityEngine.Object.FindObjectsOfType<Bed>();

        foreach (Bed bed in beds)
        {
            if (bed == null)
                continue;

            float distance = Vector3.Distance(bed.transform.position, position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = bed;
            }
        }

        return best;
    }

    private float GetDistanceToBedSurface(Bed bed)
    {
        if (bed == null)
            return float.MaxValue;

        Collider collider = bed.GetComponentInChildren<Collider>();

        if (collider != null)
        {
            Vector3 closest = collider.ClosestPoint(transform.position);
            return DistanceXZ(transform.position, closest);
        }

        return DistanceXZ(transform.position, bed.transform.position);
    }

    private void PlaceAtBedSleepPosition(Bed bed)
    {
        Vector3 position = bed.transform.position + bed.transform.forward * BedSnapForwardOffset + Vector3.up * BedSnapHeightOffset;
        Quaternion rotation = bed.transform.rotation;
        Vector3 exit = bed.transform.position + bed.transform.right * BedExitSideOffset + bed.transform.forward * BedExitBackOffset;

        if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(exit, out float exitHeight))
            exit.y = exitHeight;

        _pendingBedExitPosition = exit;
        _hasPendingBedExit = true;
        PlaceAtPosition(position, rotation);
    }

    private void PlaceAtPosition(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;

        Rigidbody body = GetComponent<Rigidbody>();

        if (body != null)
        {
            body.position = position;
            body.velocity = Vector3.zero;
            body.rotation = rotation;
        }

        if (_zNetView != null && _zNetView.IsValid())
        {
            ZDO zdo = _zNetView.GetZDO();

            if (zdo != null)
            {
                zdo.SetPosition(position);
                zdo.SetRotation(rotation);
            }
        }
    }

    private void EnterAssignedBedSleepState()
    {
        StopMoving();

        if (_character != null)
            _character.SetMoveDir(Vector3.zero);

        if (_humanoid != null)
        {
            _humanoid.SetRun(false);
            _humanoid.SetWalk(false);
        }

        SetBoolField(_monsterAI, "m_sleeping", true);
        SetBoolField(_monsterAI, "m_alerted", false);

        if (!_isPlayingBedSleepAnimation)
            _bedWakeGraceTimer = BedWakeGraceDuration;

        PlaySleepAnimation();
    }

    private void ClearAssignedBedSleepState()
    {
        SetBoolField(_monsterAI, "m_sleeping", false);

        if (_isPlayingBedSleepAnimation)
            StartBedWakeup();

        _bedSleepLogged = false;
        _isPlacedAtBed = false;
    }

    private void StartBedWakeup()
    {
        StopMoving();

        if (_character != null)
            _character.SetMoveDir(Vector3.zero);

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_animator != null)
        {
            _animator.speed = _savedAnimatorSpeed;

            if (HasAnimatorParameter(_animator, "sleeping", AnimatorControllerParameterType.Bool))
                _animator.SetBool(SleepingHash, false);

            if (HasAnimatorParameter(_animator, "wakeup", AnimatorControllerParameterType.Trigger))
                _animator.SetTrigger(WakeupHash);
            else if (HasAnimatorState(_animator, "WakeUp"))
                _animator.CrossFadeInFixedTime("WakeUp", 0.15f);
        }

        _isPlayingBedSleepAnimation = false;
        _isWakingFromBed = true;
        _bedWakeLockTimer = BedWakeLockDuration;
    }

    private void PlaySleepAnimation()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
            return;

        if (HasAnimatorParameter(_animator, "sleeping", AnimatorControllerParameterType.Bool))
            _animator.SetBool(SleepingHash, true);

        if (_isPlayingBedSleepAnimation)
            return;

        _savedAnimatorSpeed = _animator.speed;
        _animator.speed = 1f;

        if (HasAnimatorState(_animator, "SleepEnter"))
        {
            _animator.CrossFadeInFixedTime("SleepEnter", 0.2f);
            _isPlayingBedSleepAnimation = true;
            return;
        }

        if (HasAnimatorState(_animator, "Sleeping"))
        {
            _animator.CrossFadeInFixedTime("Sleeping", 0.25f);
            _isPlayingBedSleepAnimation = true;
            return;
        }

        if (!_sleepAnimationMissingLogged)
        {
            _sleepAnimationMissingLogged = true;
            Debug.LogWarning("[Agent Bed] Animator does not contain state: SleepEnter or Sleeping");
        }
    }

    private bool HasAnimatorParameter(Animator animator, string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null)
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];

            if (parameter.name == parameterName && parameter.type == type)
                return true;
        }

        return false;
    }

    private bool HasAnimatorState(Animator animator, string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        int hash = Animator.StringToHash(stateName);

        for (int i = 0; i < animator.layerCount; i++)
        {
            if (animator.HasState(i, hash))
                return true;
        }

        return false;
    }

    private void UpdatePatrol(float dt, Vector3 home, float radius)
    {
        _patrolTimer -= dt;

        if (_patrolTimer <= 0f || DistanceXZ(transform.position, _patrolTarget) <= PatrolArrivalDistance || DistanceXZ(_patrolTarget, home) > radius)
            AdvancePatrolTarget(home, radius);

        bool moved = MoveToPoint(dt, _patrolTarget, PatrolArrivalDistance * 0.75f, false, "Patrol");

        if (!moved && _pathStuckTimer >= 2.25f)
        {
            _patrolTimer = 0f;
            _pathStuckTimer = 0f;
        }
    }

    private void AdvancePatrolTarget(Vector3 home, float radius)
    {
        _patrolTimer = PatrolRefreshInterval;
        _patrolAngle += PatrolStepDegrees * _patrolDirection;

        if (_patrolAngle > 360f)
            _patrolAngle -= 360f;

        if (_patrolAngle < 0f)
            _patrolAngle += 360f;

        float angleRad = _patrolAngle * Mathf.Deg2Rad;
        float patrolRadius = Mathf.Max(2f, radius * 0.75f);
        _patrolTarget = home + new Vector3(Mathf.Cos(angleRad) * patrolRadius, 0f, Mathf.Sin(angleRad) * patrolRadius);
    }

    private bool MoveToPoint(float dt, Vector3 point, float stopDistance, bool run, string reason)
    {
        float distance = DistanceXZ(transform.position, point);

        if (distance <= stopDistance)
        {
            StopMoving();
            _pathStuckTimer = 0f;
            _pathStuckCheckTimer = 0f;
            _pathStuckCheckPosition = transform.position;
            LogMove(reason, point, stopDistance, run, distance, "arrived");
            return true;
        }

        AgentStamina stamina = GetComponent<AgentStamina>();

        if (run && stamina != null && stamina.Stamina <= 1f)
            run = false;

        if (_moveToMethod == null)
        {
            LogMove(reason, point, stopDistance, run, distance, "MoveTo method missing");
            MoveDirectlyToward(point, run);
            return false;
        }

        object result = _moveToMethod.Invoke(_monsterAI, new object[] { dt, point, stopDistance, run });

        if (_humanoid != null)
            _humanoid.SetRun(run);

        bool moveFinished = result is bool finished && finished;

        if (!moveFinished && IsPathStuck(dt))
        {
            MoveDirectlyToward(point, run);
            LogMove(reason, point, stopDistance, run, distance, "direct fallback");
            return false;
        }

        LogMove(reason, point, stopDistance, run, distance, "result=" + result);
        return moveFinished;
    }

    private bool IsPathStuck(float dt)
    {
        if (_character == null)
            return false;

        _pathStuckCheckTimer += dt;

        if (_pathStuckCheckTimer < 0.75f)
            return false;

        float moved = Vector3.Distance(transform.position, _pathStuckCheckPosition);
        float velocity = _character.GetVelocity().magnitude;
        _pathStuckCheckTimer = 0f;
        _pathStuckCheckPosition = transform.position;

        if (velocity > 0.35f || moved > 0.35f)
        {
            _pathStuckTimer = 0f;
            return false;
        }

        _pathStuckTimer += 0.75f;
        return _pathStuckTimer >= 1.5f;
    }

    private void MoveDirectlyToward(Vector3 point, bool run)
    {
        if (_character == null)
            return;

        Vector3 direction = point - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        direction.Normalize();

        _character.SetMoveDir(direction);

        if (_humanoid != null)
            _humanoid.SetRun(run);
    }

    private void StopMoving()
    {
        if (_humanoid != null)
            _humanoid.SetRun(false);

        if (_stopMovingMethod != null)
            _stopMovingMethod.Invoke(_monsterAI, null);
    }

    private void ApplyModeSettings()
    {
        if (_monsterAI == null || _agent == null || _agent.Context == null)
            return;

        switch (_agent.Context.BehaviourMode)
        {
            case AgentBehaviourMode.Passive:
                SetFloatField(_monsterAI, "m_viewRange", 0f);
                SetFloatField(_monsterAI, "m_viewAngle", 0f);
                break;
            case AgentBehaviourMode.Defensive:
                SetFloatField(_monsterAI, "m_viewRange", 22f);
                SetFloatField(_monsterAI, "m_viewAngle", 120f);
                break;
            case AgentBehaviourMode.Aggressive:
                SetFloatField(_monsterAI, "m_viewRange", 30f);
                SetFloatField(_monsterAI, "m_viewAngle", 180f);
                break;
        }

        SetBoolField(_monsterAI, "m_enableHuntPlayer", false);
    }

    private bool TargetAllowedByCurrentMode(Character target)
    {
        if (target == null || target.IsDead())
            return false;

        if (_agent.Context.TaskMode == AgentTaskMode.Hunt && _huntState == AgentHuntState.Fighting && target == _huntTarget)
            return DistanceXZ(transform.position, target.transform.position) <= HuntMaxChaseDistance;

        if (_agent.Context.BehaviourMode == AgentBehaviourMode.Passive)
            return false;

        if (_agent.Context.StateMode == AgentStateMode.StayHome && _agent.Context.HomeZone != null && _agent.Context.HomeZone.IsSet)
            return DistanceXZ(target.transform.position, _agent.Context.HomeZone.Center) <= _agent.Context.HomeZone.Radius;

        float limit = _agent.Context.BehaviourMode == AgentBehaviourMode.Aggressive ? 30f : 14f;
        return DistanceXZ(transform.position, target.transform.position) <= limit;
    }

    private Character GetNativeTargetCreature()
    {
        return GetFieldValue(_monsterAI, "m_targetCreature") as Character;
    }

    private void AssignNativeTarget(Character target)
    {
        if (target == null || target.IsDead())
            return;

        Character current = GetNativeTargetCreature();

        if (current == target)
            return;

        SetObjectField(_monsterAI, "m_targetCreature", target);
        SetObjectField(_monsterAI, "m_targetStatic", null);
        SetBoolField(_monsterAI, "m_alerted", true);

        if (_lastAssignedNativeTarget != target || _nativeAssignLogTimer <= 0f)
        {
            _lastAssignedNativeTarget = target;
            _nativeAssignLogTimer = NativeAssignLogInterval;
            LogHunt("Native target assigned " + GetPrefabName(target.gameObject), true);
        }
    }

    private void ClearNativeAggro()
    {
        if (_monsterAI == null)
            return;

        SetObjectField(_monsterAI, "m_targetCreature", null);
        SetObjectField(_monsterAI, "m_targetStatic", null);
        SetBoolField(_monsterAI, "m_alerted", false);
    }

    private int DepositToAssignedChests()
    {
        if (_zNetView == null || !_zNetView.IsValid())
            return 0;

        ZDO zdo = _zNetView.GetZDO();

        if (zdo == null)
            return 0;

        int count = Mathf.Clamp(zdo.GetInt("agent_deposit_chest_count", 0), 0, MaxDepositChests);
        int moved = 0;

        for (int i = 0; i < count; i++)
        {
            Vector3 chestPosition = zdo.GetVec3("agent_deposit_chest_" + i + "_pos", Vector3.zero);
            Container container = FindContainerNearPosition(chestPosition);

            if (container == null)
                continue;

            moved += TransferInventoryToDepositContainer(container);

            if (!NpcHasItemsToDeposit())
                break;
        }

        return moved;
    }

    private Container FindNearestDepositContainer()
    {
        if (_agent.Context.HomeZone == null || !_agent.Context.HomeZone.IsSet)
            return null;

        Vector3 home = _agent.Context.HomeZone.Center;
        Container best = null;
        float bestDistance = DepositChestSearchRadius;

        foreach (Container container in UnityEngine.Object.FindObjectsOfType<Container>())
        {
            if (!IsValidDepositContainer(container))
                continue;

            float distance = DistanceXZ(home, container.transform.position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = container;
            }
        }

        return best;
    }

    private Container FindContainerNearPosition(Vector3 position)
    {
        Container best = null;
        float bestDistance = AssignedChestMatchDistance;

        foreach (Container container in UnityEngine.Object.FindObjectsOfType<Container>())
        {
            if (!IsValidDepositContainer(container))
                continue;

            float distance = Vector3.Distance(container.transform.position, position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = container;
            }
        }

        return best;
    }

    private bool IsValidDepositContainer(Container container)
    {
        if (container == null || container.m_inventory == null)
            return false;

        if (_agent != null && _agent.ValheimContainer != null && container == _agent.ValheimContainer)
            return false;

        if (container.GetComponent<AgentComponent>() != null || container.GetComponentInParent<AgentComponent>() != null)
            return false;

        return true;
    }

    private bool NpcHasItemsToDeposit()
    {
        return _agent != null && _agent.ValheimContainer != null && _agent.ValheimContainer.m_inventory != null && _agent.ValheimContainer.m_inventory.GetAllItems().Count > 0;
    }

    private int TransferInventoryToDepositContainer(Container depositContainer)
    {
        if (_agent == null || _agent.ValheimContainer == null || _agent.ValheimContainer.m_inventory == null || depositContainer == null || depositContainer.m_inventory == null)
            return 0;

        Inventory source = _agent.ValheimContainer.m_inventory;
        Inventory target = depositContainer.m_inventory;
        List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>(source.GetAllItems());
        int moved = 0;

        foreach (ItemDrop.ItemData item in items)
        {
            if (item == null)
                continue;

            ItemDrop.ItemData clone = item.Clone();

            if (clone == null || !target.AddItem(clone))
                continue;

            source.RemoveItem(item);
            moved++;
        }

        return moved;
    }

    private void TeleportNear(Vector3 target)
    {
        Vector3 position = target;

        for (int i = 0; i < 20; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 3f;
            Vector3 candidate = target + new Vector3(offset.x, 0f, offset.y);

            if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(candidate, out float height))
            {
                candidate.y = height;
                position = candidate;
                break;
            }
        }

        PlaceAtPosition(position, transform.rotation);
    }

    private void LogMove(string reason, Vector3 point, float stopDistance, bool run, float distance, string result)
    {
        return;
    }

    private void LogHunt(string message, bool force)
    {
        if (!force && _huntLogTimer > 0f)
            return;

        _huntLogTimer = HuntLogInterval;
        //Debug.Log("[Agent Hunt] " + message);
    }

    private static string GetPrefabName(GameObject go)
    {
        if (go == null)
            return string.Empty;

        string name = go.name;
        int cloneIndex = name.IndexOf("(Clone)", System.StringComparison.OrdinalIgnoreCase);

        if (cloneIndex >= 0)
            name = name.Substring(0, cloneIndex);

        return name.Trim();
    }

    private static string FormatVector(Vector3 value)
    {
        return value.x.ToString("0.0") + "," + value.y.ToString("0.0") + "," + value.z.ToString("0.0");
    }

    private static float DistanceXZ(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return Mathf.Sqrt(x * x + z * z);
    }

    private static float GetFloatField(object instance, string name, float fallback)
    {
        FieldInfo field = FindField(instance?.GetType(), name);
        return field != null && field.FieldType == typeof(float) ? (float)field.GetValue(instance) : fallback;
    }

    private static object GetFieldValue(object instance, string name)
    {
        FieldInfo field = FindField(instance?.GetType(), name);
        return field != null ? field.GetValue(instance) : null;
    }

    private static void SetFloatField(object instance, string name, float value)
    {
        FieldInfo field = FindField(instance?.GetType(), name);

        if (field != null && field.FieldType == typeof(float))
            field.SetValue(instance, value);
    }

    private static void SetBoolField(object instance, string name, bool value)
    {
        FieldInfo field = FindField(instance?.GetType(), name);

        if (field != null && field.FieldType == typeof(bool))
            field.SetValue(instance, value);
    }

    private static void SetObjectField(object instance, string name, object value)
    {
        FieldInfo field = FindField(instance?.GetType(), name);

        if (field != null)
            field.SetValue(instance, value);
    }

    private static MethodInfo FindMethod(System.Type type, string name, params System.Type[] parameterTypes)
    {
        while (type != null)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameterTypes, null);

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

    [HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
    private static class MonsterAI_UpdateAI_Patch
    {
        private static bool Prefix(MonsterAI __instance, float dt, ref bool __result)
        {
            AgentBehaviourController controller = __instance.GetComponent<AgentBehaviourController>();

            if (controller == null)
                return true;

            bool letNativeRun = controller.ControlledAIUpdate(dt);

            if (letNativeRun)
                return true;

            __result = true;
            return false;
        }
    }
    private static void SetVector3Field(object instance, string name, Vector3 value)
    {
        FieldInfo field = FindField(instance?.GetType(), name);

        if (field == null || field.FieldType != typeof(Vector3))
            return;

        field.SetValue(instance, value);
    }
    private bool TryLetNativeIdleOwnTick()
    {
        if (_agent == null || _agent.Context == null || _monsterAI == null)
            return false;

        if (_agent.Context.TaskMode != AgentTaskMode.None)
            return false;

        if (_isPlayingBedSleepAnimation || _isWakingFromBed)
            return false;

        if (GetNativeTargetCreature() != null)
            return false;

        if (_agent.Context.StateMode == AgentStateMode.Follow)
            return false;

        Vector3 anchor;
        float radius;

        if (_agent.Context.StateMode == AgentStateMode.StayHome)
        {
            if (_agent.Context.HomeZone == null || !_agent.Context.HomeZone.IsSet)
                return false;

            anchor = _agent.Context.HomeZone.Center;
            radius = Mathf.Max(1f, _agent.Context.HomeZone.Radius);

            float distanceFromHome = DistanceXZ(transform.position, anchor);

            if (distanceFromHome > radius + HomeReturnDistanceGrace)
                return false;
        }
        else
        {
            anchor = _agent.Context.IdleOrigin;

            if (anchor == Vector3.zero)
                anchor = transform.position;

            radius = Mathf.Max(2f, _agent.Context.IdleRadius);
        }

        SetVector3Field(_monsterAI, "m_spawnPoint", anchor);
        SetFloatField(_monsterAI, "m_randomMoveRange", Mathf.Max(1f, radius * 0.75f));
        SetBoolField(_monsterAI, "m_alerted", false);

        return true;
    }
}
