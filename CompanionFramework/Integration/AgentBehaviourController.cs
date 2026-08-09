using Core.Agent;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static Core.Agent.AgentContext;

public class AgentBehaviourController : MonoBehaviour
{
    private const string AgentBehaviourDepositPatchVersion = "nonhumanoid-native-combat-2026-08-09a";
    private enum AgentHuntState
    {
        Searching,
        TravellingToTarget,
        Fighting,
        CollectingLoot,
        ReturningToAnchor,
        Depositing
    }
    public enum AgentMoveResult
    {
        Arrived,
        Moving,
        Failed,
        Blocked,
        Invalid
    }

    private AgentComponent _agent;
    private MonsterAI _monsterAI;
    private Character _character;
    private Humanoid _humanoid;
    private ZNetView _zNetView;
    private ZSyncAnimation _zanim;
    private Rigidbody _body;
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
    private AgentDoorHandler _doorHandler;
    private bool _isPlayingBedSleepAnimation;
    private bool _sleepAnimationMissingLogged;
    private bool _bedSleepLogged;
    private bool _sleepBodyLocked;
    private bool _bedSleepStateApplied;
    private bool _isWakingFromBed;
    private bool _hasPendingBedExit;
    private Vector3 _pendingBedExitPosition;
    private float _bedWakeLockTimer;
    private float _savedAnimatorSpeed = 1f;
    private bool _isPlacedAtBed;
    private float _bedWakeGraceTimer;

    private float _huntSearchTimer;
    private float _huntScanDebugTimer;
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
    private Vector3 _pathCorrectionTarget;
    private float _pathCorrectionTimer;
    private int _pathCorrectionSide = 1;
    private float _stuckJumpTimer;
    private int _moveFalseDoneCount;
    private float _nativeAggroValidateTimer;
    private float _followDebugTimer;
    private float _huntControlDebugTimer;
    private float _huntStateLogTimer;
    private float _huntNoTargetLogTimer;
    private float _sleepLockLogTimer;
    private Vector3 _homeWanderTarget;
    private float _homeWanderTimer;
    private bool _homeWanderTargetSet;
    private int _patrolDirection = 1;
    private WaterVolume _patrolWaterVolume;

    private const float FollowTeleportDistance = 70f;
    private const float HomeReturnDistanceGrace = 1.5f;
    private const float HomeWorkLeashExtra = 35f;
    private const float HomeCombatLeashExtra = 45f;
    private const float HomeHardReturnExtra = 60f;
    private const float HomeReturnStopDistance = 2f;
    private const float PatrolStepDegrees = 35f;
    private const float PatrolRefreshInterval = 4f;
    private const float PatrolArrivalDistance = 3.5f;
    private const float HuntSearchInterval = 3f;
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
    private const float PathCorrectionDuration = 0.75f;
    private const float PathCorrectionForward = 1.0f;
    private const float PathCorrectionSide = 1.25f;
    private const int MoveFalseDoneThreshold = 8;
    private const float NativeAggroValidateInterval = 0.5f;
    private const float WorkTaskDefensiveInterruptRange = 8f;
    private const float WorkTaskAggressiveInterruptRange = 12f;
    private const bool DebugHuntScan = false;
    private const bool DebugAgentTaskDecisions = false;
    private const float DepositStartWeightRatio = 0.85f;
    private const float DepositUseDistance = 3f;
    private const float DepositFallbackCacheInterval = 12f;
    private const float DepositNoAssignedChestSearchExtraRadius = 35f;

    private float _restPreparationTimer;
    private Container _activeDepositContainer;
    private Container _cachedFallbackDepositContainer;
    private float _depositCacheTimer;
    private Bed _activeSleepBed;

    private static readonly int SleepingHash = Animator.StringToHash("sleeping");
    private static readonly int AttachBedHash = Animator.StringToHash("attach_bed");
    private static readonly int LyingDownHash = Animator.StringToHash("lying_down");
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
        _zanim = GetComponent<ZSyncAnimation>();
        _body = GetComponent<Rigidbody>();
        _animator = GetComponentInChildren<Animator>();
        _doorHandler = GetComponent<AgentDoorHandler>();

        if (_doorHandler == null)
            _doorHandler = gameObject.AddComponent<AgentDoorHandler>();
        _moveToMethod = FindMethod(typeof(BaseAI), "MoveTo", typeof(float), typeof(Vector3), typeof(float), typeof(bool));
        _stopMovingMethod = FindMethod(typeof(BaseAI), "StopMoving");
        _patrolAngle = Random.Range(0f, 360f);
        _patrolDirection = Random.value > 0.5f ? 1 : -1;
        Debug.LogWarning("[AgentBehaviourController] version=" + AgentBehaviourDepositPatchVersion + " name=" + gameObject.name);
    }

    public bool ControlledAIUpdate(float dt)
    {
        if (_agent == null || _agent.Context == null || _monsterAI == null || _character == null)
            return true;

        if (_zNetView != null && _zNetView.IsValid() && !_zNetView.IsOwner())
            return true;

        if (_character.IsDead())
            return true;

        AgentPrefabProfile flyProfile = GetComponent<AgentPrefabProfile>();
        bool isFlyingAgent = flyProfile != null && flyProfile.CanFly;
        Character nativeTarget = GetNativeTargetCreature();


        if (isFlyingAgent)
        {
            if (nativeTarget != null && !nativeTarget.IsDead())
                return true;

            if (_agent.Context.StateMode != AgentStateMode.Follow)
                return true;
        }

        if (AgentContainerComponent.IsRadialOpen)
        {
            StopMoving();
            return false;
        }

        UpdateTimers(dt);
        ApplyModeSettings();
        ValidateNativeAggroState(dt);

        if (_agent.Context.TaskMode != AgentTaskMode.None)
            PrepareForActiveTask();


        if (_isWakingFromBed)
        {
            if (_agent.Context.TaskMode != AgentTaskMode.None && !EnvMan.IsNight())
            {
                _bedWakeLockTimer = 0f;
                _isWakingFromBed = false;
                _hasPendingBedExit = false;
                EnsureDynamicBody();
            }
            else
            {
                return UpdateBedWakeLock(dt);
            }
        }

        AgentAssignedBedRestController assignedBedRest = GetComponent<AgentAssignedBedRestController>();
        if (_agent.Context.TaskMode == AgentTaskMode.None && assignedBedRest != null && assignedBedRest.UpdateAssignedBedRest(dt))
        {
            ClearNativeAggro();
            ClearTaskMotionForSleep();
            LogSleepLockState("assigned-controller");
            return false;
        }

        if (HasActiveAssignedBedSleepState() && !ShouldSleepAtAssignedBed())
            ClearAssignedBedSleepState();

        if (!ShouldTaskBlockNightSleep() && ShouldSleepAtAssignedBed())
        {
            ClearNativeAggro();
            ClearTaskMotionForSleep();
            UpdateAssignedBedSleep(dt);
            LogSleepLockState("behaviour-controller");
            return false;
        }

        if (_agent.Context.StateMode == AgentStateMode.Follow && _agent.Context.TaskMode == AgentTaskMode.None && GetNativeTargetCreature() == null)
        {
            ClearNativeAggro();
            UpdateFollow(dt);
            LogHuntControl("FollowOverrideTaskSuppression");
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
            UpdateHunt(dt);
            return false;
        }
        if (IsControlledNonCombatTask())
        {
            if (ShouldInterruptControlledTaskForThreat())
            {
                if (TryHandleBehaviourThreats())
                    return true;
            }
            else
            {
                ClearNativeAggro();
            }
            if (TryHandleInventoryDeposit(dt))
                return false;
            TryHandleTask(dt);
            return false;
        }

        if (TryHandleBehaviourThreats())
            return true;

        if (TryHandleInventoryDeposit(dt))
            return false;

        if (TryWaitForRestedBeforeActivity(dt))
            return false;

        if (TryHandleTask(dt))
            return false;

        if (TryHandleIdleComfortSeeking(dt))
            return false;
        if (TryLetNativeIdleOwnTick())
            return true;

        UpdateStateMovement(dt);
        return false;
    }

    private void PrepareForActiveTask()
    {
        EnsureDynamicBody();

        AgentAssignedBedRestController assignedBedRest = GetComponent<AgentAssignedBedRestController>();
        if (assignedBedRest != null && assignedBedRest.IsResting)
            assignedBedRest.ForceStopSleeping();

        bool hadSleepState = HasActiveAssignedBedSleepState() || _isWakingFromBed;
        if (hadSleepState)
        {
            SetBoolField(_monsterAI, "m_sleeping", false);
            ClearBedNetworkState();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (_animator != null)
            {
                _animator.speed = _savedAnimatorSpeed;
                if (HasAnimatorParameter(_animator, "sleeping", AnimatorControllerParameterType.Bool))
                    _animator.SetBool(SleepingHash, false);
                if (HasAnimatorParameter(_animator, "attach_bed", AnimatorControllerParameterType.Bool))
                    _animator.SetBool(AttachBedHash, false);
                if (HasAnimatorParameter(_animator, "lying_down", AnimatorControllerParameterType.Trigger))
                    _animator.ResetTrigger(LyingDownHash);
                if (HasAnimatorParameter(_animator, "lying_down", AnimatorControllerParameterType.Bool))
                    _animator.SetBool(LyingDownHash, false);
            }

            _isPlayingBedSleepAnimation = false;
            _isPlacedAtBed = false;
            _activeSleepBed = null;
            _sleepBodyLocked = false;
            _isWakingFromBed = false;
            _hasPendingBedExit = false;
            _bedWakeLockTimer = 0f;
            _bedWakeGraceTimer = 0f;
            _bedSleepLogged = false;
        }

        if (_pathCorrectionTimer <= 0f || Mathf.Abs(_pathCorrectionTarget.y - transform.position.y) > 3f)
            ResetPathCorrection();
    }

    private bool TryHandleInventoryDeposit(float dt)
    {
        if (!ShouldDepositInventory())
        {
            _activeDepositContainer = null;
            return false;
        }

        if (_agent == null || _agent.Context == null)
            return false;

        ClearNativeAggro();

        if (_activeDepositContainer == null || !IsValidDepositContainer(_activeDepositContainer))
            _activeDepositContainer = FindBestDepositContainerForInventory();

        if (_activeDepositContainer == null)
        {
            Debug.LogWarning("[AgentDepositSelect] name=" + gameObject.name + " result=none reason=no-valid-container pos=" + FormatVector(transform.position));
            StopMoving();
            return true;
        }

        float distance = DistanceXZ(transform.position, _activeDepositContainer.transform.position);

        if (distance > DepositUseDistance)
        {
            Debug.LogWarning("[AgentDepositMove] name=" + gameObject.name + " chest=" + _activeDepositContainer.gameObject.name + " dist=" + distance.ToString("0.0") + " pos=" + FormatVector(transform.position));
            MoveToPoint(dt, _activeDepositContainer.transform.position, DepositUseDistance, false, "DepositInventory");
            return true;
        }

        StopMoving();
        int moved = TransferInventoryToDepositContainer(_activeDepositContainer);
        Debug.LogWarning("[AgentDepositDone] name=" + gameObject.name + " chest=" + _activeDepositContainer.gameObject.name + " moved=" + moved + " remaining=" + GetDepositableItemCount(_agent.ValheimContainer != null ? _agent.ValheimContainer.m_inventory : null));
        _activeDepositContainer = null;
        _depositCacheTimer = 0f;
        return true;
    }

    private bool ShouldDepositInventory()
    {
        if (_agent == null || _agent.ValheimContainer == null || _agent.ValheimContainer.m_inventory == null || _agent.Context == null)
            return false;
        if (!ShouldUseGenericDepositForTask())
            return false;
        Inventory inventory = _agent.ValheimContainer.m_inventory;
        if (inventory.GetAllItems().Count == 0)
            return false;
        if (GetDepositableItemCount(inventory) == 0)
            return false;
        float maxWeight = Mathf.Max(1f, _agent.MaxCarryWeight);
        float currentWeight = GetDepositableInventoryWeight(inventory);
        return currentWeight >= maxWeight * DepositStartWeightRatio || IsInventorySlotPressureHigh(inventory);
    }

    private bool ShouldUseGenericDepositForTask()
    {
        if (_agent == null || _agent.Context == null)
            return false;
        return _agent.Context.TaskMode == AgentTaskMode.Gather ||
               _agent.Context.TaskMode == AgentTaskMode.Lumbering ||
               _agent.Context.TaskMode == AgentTaskMode.Mining ||
               _agent.Context.TaskMode == AgentTaskMode.Quarrying ||
               _agent.Context.TaskMode == AgentTaskMode.Hunt;
    }

    private Container FindBestDepositContainerForInventory()
    {
        Container assigned = FindAssignedDepositContainer();
        if (assigned != null)
            return assigned;
        return FindCachedFallbackDepositContainer();
    }

    private Container FindCachedFallbackDepositContainer()
    {
        _depositCacheTimer -= Time.deltaTime;
        if (_cachedFallbackDepositContainer != null && IsValidDepositContainer(_cachedFallbackDepositContainer) && _depositCacheTimer > 0f)
            return _cachedFallbackDepositContainer;
        _depositCacheTimer = DepositFallbackCacheInterval;
        string reason;
        _cachedFallbackDepositContainer = FindBestMatchingResourceContainer(out reason);
        if (_cachedFallbackDepositContainer == null)
        {
            _cachedFallbackDepositContainer = FindNearestDepositContainer();
            reason = _cachedFallbackDepositContainer != null ? "nearestFallback" : "none";
        }
        Debug.LogWarning("[AgentDepositSelect] name=" + gameObject.name + " result=" + (_cachedFallbackDepositContainer != null ? _cachedFallbackDepositContainer.gameObject.name : "none") + " reason=" + reason + " pos=" + FormatVector(transform.position));
        return _cachedFallbackDepositContainer;
    }

    private Container FindAssignedDepositContainer()
    {
        if (_zNetView == null || !_zNetView.IsValid())
            return null;

        ZDO zdo = _zNetView.GetZDO();

        if (zdo == null)
            return null;

        int count = Mathf.Clamp(zdo.GetInt("agent_deposit_chest_count", 0), 0, MaxDepositChests);
        Container best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Vector3 chestPosition = zdo.GetVec3("agent_deposit_chest_" + i + "_pos", Vector3.zero);
            Container container = FindContainerNearPosition(chestPosition);

            if (container == null)
                continue;

            float distance = DistanceXZ(transform.position, container.transform.position);

            if (distance < bestDistance)
            {
                best = container;
                bestDistance = distance;
            }
        }

        return best;
    }

    private Container FindBestMatchingResourceContainer(out string reason)
    {
        reason = "noInventory";
        if (_agent == null || _agent.ValheimContainer == null || _agent.ValheimContainer.m_inventory == null)
            return null;
        List<ItemDrop.ItemData> carriedItems = _agent.ValheimContainer.m_inventory.GetAllItems();
        Container best = null;
        int bestMatches = 0;
        float bestScore = float.MaxValue;
        string bestItemName = string.Empty;
        Vector3 anchor = GetCurrentAnchorPosition();
        float searchRadius = GetDepositSearchRadius();
        foreach (Container container in UnityEngine.Object.FindObjectsByType<Container>(FindObjectsSortMode.None))
        {
            if (!IsValidDepositContainer(container))
                continue;
            float anchorDistance = DistanceXZ(container.transform.position, anchor);
            float npcDistance = DistanceXZ(transform.position, container.transform.position);
            if (anchorDistance > searchRadius && npcDistance > DepositChestSearchRadius)
                continue;
            string matchedItem;
            int matches = CountMatchingItemTypes(carriedItems, container.m_inventory, out matchedItem);
            if (matches <= 0)
                continue;
            float score = npcDistance - matches * 8f;
            if (matches > bestMatches || matches == bestMatches && score < bestScore)
            {
                best = container;
                bestMatches = matches;
                bestScore = score;
                bestItemName = matchedItem;
            }
        }
        reason = best != null ? "matchingItem:" + bestItemName + ":matches=" + bestMatches : "noMatchingChest";
        return best;
    }

    private static int CountMatchingItemTypes(List<ItemDrop.ItemData> sourceItems, Inventory targetInventory, out string matchedItemName)
    {
        matchedItemName = string.Empty;
        if (sourceItems == null || targetInventory == null)
            return 0;
        List<ItemDrop.ItemData> targetItems = targetInventory.GetAllItems();
        int matches = 0;
        for (int i = 0; i < sourceItems.Count; i++)
        {
            ItemDrop.ItemData source = sourceItems[i];
            if (source == null || source.m_shared == null || IsReservedAgentToolStatic(source))
                continue;
            for (int j = 0; j < targetItems.Count; j++)
            {
                ItemDrop.ItemData target = targetItems[j];
                if (target == null || target.m_shared == null)
                    continue;
                if (target.m_shared.m_name == source.m_shared.m_name)
                {
                    matches++;
                    if (string.IsNullOrEmpty(matchedItemName))
                        matchedItemName = source.m_shared.m_name;
                    break;
                }
            }
        }
        return matches;
    }

    private bool TryWaitForRestedBeforeActivity(float dt)
    {
        if (_agent == null || _agent.Context == null)
            return false;

        if (ShouldBypassRestPreparationForTask())
        {
            _restPreparationTimer = 0f;
            return false;
        }

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
        Vector3 comfortPoint = _agent.Context.HomeZone.Center;
        AgentComfortAreaScanner comfortScanner = GetComponent<AgentComfortAreaScanner>();

        if (comfortScanner != null && comfortScanner.TryGetBestComfortPoint(out Vector3 detectedComfortPoint))
            comfortPoint = detectedComfortPoint;

        float distance = DistanceXZ(transform.position, comfortPoint);

        if (distance > HomeReturnStopDistance)
        {
            MoveToPoint(dt, comfortPoint, HomeReturnStopDistance, false, "RestPrepareComfort");
            return true;
        }

        StopMoving();

        if (_character != null)
            _character.SetMoveDir(Vector3.zero);

        return true;
    }
    private bool TryHandleIdleComfortSeeking(float dt)
    {
        if (_agent == null || _agent.Context == null)
            return false;

        if (_agent.Context.TaskMode != AgentTaskMode.None)
            return false;

        if (_agent.Context.StateMode == AgentStateMode.Follow)
            return false;

        AgentRested rested = GetComponent<AgentRested>();

        if (rested == null || rested.IsRested)
            return false;

        AgentComfortAreaScanner comfortScanner = GetComponent<AgentComfortAreaScanner>();

        if (comfortScanner == null)
            return false;

        if (!comfortScanner.TryGetBestComfortPoint(out Vector3 comfortPoint))
            return false;

        float distance = DistanceXZ(transform.position, comfortPoint);

        if (distance > 2.5f)
        {
            MoveToPoint(dt, comfortPoint, 2.5f, distance > 8f, "IdleComfort");
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
        _huntStateLogTimer = Mathf.Max(0f, _huntStateLogTimer - dt);
        _huntNoTargetLogTimer = Mathf.Max(0f, _huntNoTargetLogTimer - dt);

        if (_agent.Context.TaskMode != AgentTaskMode.Hunt)
            ResetHuntStateIfNeeded();
    }

    private bool UpdateBedWakeLock(float dt)
    {
        EnsureDynamicBody();
        StopMoving();

        if (_character != null)
            _character.SetMoveDir(Vector3.zero);

        _bedWakeLockTimer -= dt;

        if (_bedWakeLockTimer <= 0f)
        {
            _isWakingFromBed = false;
            _hasPendingBedExit = false;
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
                if (_agent.Context.StateMode == AgentStateMode.StayHome &&
                    _agent.Context.HomeZone != null &&
                    _agent.Context.HomeZone.IsSet)
                {
                    UpdatePatrol(
                        dt,
                        _agent.Context.HomeZone.Center,
                        Mathf.Max(1f, _agent.Context.HomeZone.Radius));

                    return true;
                }

                return false;

            case AgentTaskMode.Fishing:
                AgentFishingController fishing = GetComponent<AgentFishingController>();
                return fishing != null && fishing.UpdateFishingTask(dt);

            case AgentTaskMode.Farming:
                if (_agent.Context.StateMode != AgentStateMode.StayHome)
                    return false;

                AgentFarmingController farming = GetComponent<AgentFarmingController>();
                return farming != null && farming.UpdateFarmingTask(dt);
            case AgentTaskMode.Smelting:
                if (_agent.Context.StateMode != AgentStateMode.StayHome)
                    return false;

                AgentSmeltingController smelting = GetComponent<AgentSmeltingController>();
                return smelting != null && smelting.UpdateSmeltingTask(dt);
            case AgentTaskMode.Repair:
                if (_agent.Context.StateMode != AgentStateMode.StayHome)
                    return false;

                AgentRepairController repair = GetComponent<AgentRepairController>();
                return repair != null && repair.UpdateRepairTask(dt);

            case AgentTaskMode.Cook:
                if (_agent.Context.StateMode != AgentStateMode.StayHome)
                    return false;

                AgentCookController cook = GetComponent<AgentCookController>();
                return cook != null && cook.UpdateCookingTask(dt);

            case AgentTaskMode.Gather:
            case AgentTaskMode.Lumbering:
            case AgentTaskMode.Mining:
            case AgentTaskMode.Quarrying:
                AgentResourceWorkController resource = GetComponent<AgentResourceWorkController>();
                return resource != null && resource.UpdateResourceTask(dt);

            case AgentTaskMode.None:
            default:
                return false;
        }
    }

    private bool IsExclusiveControlledTask()
    {
        return false;
    }

    private bool IsControlledNonCombatTask()
    {
        if (_agent == null || _agent.Context == null)
            return false;
        return _agent.Context.TaskMode == AgentTaskMode.Gather ||
               _agent.Context.TaskMode == AgentTaskMode.Lumbering ||
               _agent.Context.TaskMode == AgentTaskMode.Mining ||
               _agent.Context.TaskMode == AgentTaskMode.Quarrying ||
               _agent.Context.TaskMode == AgentTaskMode.Patrol ||
               _agent.Context.TaskMode == AgentTaskMode.Cook ||
               _agent.Context.TaskMode == AgentTaskMode.Smelting ||
               _agent.Context.TaskMode == AgentTaskMode.Farming ||
               _agent.Context.TaskMode == AgentTaskMode.Repair ||
               _agent.Context.TaskMode == AgentTaskMode.Fishing;
    }

    private bool IsResourceWorkTask()
    {
        if (_agent == null || _agent.Context == null)
            return false;
        return _agent.Context.TaskMode == AgentTaskMode.Gather ||
               _agent.Context.TaskMode == AgentTaskMode.Lumbering ||
               _agent.Context.TaskMode == AgentTaskMode.Mining ||
               _agent.Context.TaskMode == AgentTaskMode.Quarrying;
    }

    private bool IsPatrolTask()
    {
        return _agent != null && _agent.Context != null && _agent.Context.TaskMode == AgentTaskMode.Patrol;
    }

    private bool ShouldInterruptControlledTaskForThreat()
    {
        if (_agent == null || _agent.Context == null)
            return false;
        if (_agent.Context.BehaviourMode == AgentBehaviourMode.Passive)
            return false;
        float range = _agent.Context.BehaviourMode == AgentBehaviourMode.Aggressive ? WorkTaskAggressiveInterruptRange : WorkTaskDefensiveInterruptRange;
        Character enemy = FindNearbyCombatEnemy(range);
        return enemy != null;
    }

    private bool IsMobileWorkTask()
    {
        if (_agent == null || _agent.Context == null)
            return false;

        return _agent.Context.TaskMode == AgentTaskMode.Hunt ||
               _agent.Context.TaskMode == AgentTaskMode.Gather ||
               _agent.Context.TaskMode == AgentTaskMode.Lumbering ||
               _agent.Context.TaskMode == AgentTaskMode.Mining ||
               _agent.Context.TaskMode == AgentTaskMode.Quarrying ||
               _agent.Context.TaskMode == AgentTaskMode.Fishing;
    }

    private bool ShouldBypassRestPreparationForTask()
    {
        return IsMobileWorkTask();
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

    private void ValidateNativeAggroState(float dt)
    {
        _nativeAggroValidateTimer -= dt;

        if (_nativeAggroValidateTimer > 0f)
            return;

        _nativeAggroValidateTimer = NativeAggroValidateInterval;

        Character target = GetNativeTargetCreature();
        object staticTarget = GetFieldValue(_monsterAI, "m_targetStatic");

        if (target != null)
        {
            if (target.IsDead() || target.gameObject == null || !TargetAllowedByCurrentMode(target))
            {
                ClearNativeAggro();
                return;
            }
        }

        if (staticTarget != null)
        {
            Object unityObject = staticTarget as Object;

            if (unityObject == null)
            {
                SetObjectField(_monsterAI, "m_targetStatic", null);
                staticTarget = null;
            }
        }

        if (target == null && staticTarget == null)
            SetBoolField(_monsterAI, "m_alerted", false);
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
        {
            PrepareCombatEquipment(existingTarget);
            return true;
        }

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

    private float GetHomeCombatLeashRadius()
    {
        if (_agent == null || _agent.Context == null || _agent.Context.HomeZone == null || !_agent.Context.HomeZone.IsSet)
            return 0f;

        return Mathf.Max(1f, _agent.Context.HomeZone.Radius + HomeCombatLeashExtra);
    }

    private float GetHomeWorkLeashRadius()
    {
        if (_agent == null || _agent.Context == null || _agent.Context.HomeZone == null || !_agent.Context.HomeZone.IsSet)
            return 0f;

        return Mathf.Max(1f, _agent.Context.HomeZone.Radius + HomeWorkLeashExtra);
    }

    private float GetHomeHardLeashRadius()
    {
        if (_agent == null || _agent.Context == null || _agent.Context.HomeZone == null || !_agent.Context.HomeZone.IsSet)
            return 0f;

        return Mathf.Max(1f, _agent.Context.HomeZone.Radius + HomeHardReturnExtra);
    }

    private bool IsBeyondHomeHardLeash(Vector3 position)
    {
        if (_agent == null || _agent.Context == null || _agent.Context.StateMode != AgentStateMode.StayHome || _agent.Context.HomeZone == null || !_agent.Context.HomeZone.IsSet)
            return false;

        return DistanceXZ(position, _agent.Context.HomeZone.Center) > GetHomeHardLeashRadius();
    }

    private float GetHuntSearchRange()
    {
        if (_agent != null && _agent.Context != null && _agent.Context.StateMode == AgentStateMode.StayHome && _agent.Context.HomeZone != null && _agent.Context.HomeZone.IsSet)
            return Mathf.Max(HuntTargetRange, GetHomeWorkLeashRadius());

        return HuntTargetRange;
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
        float radius = GetHomeCombatLeashRadius();
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
            LogFollowDiagnostic(distance, false, "teleport");
            return;
        }
        if (distance <= _agent.Context.FollowStopDistance)
        {
            StopMoving();
            LogFollowDiagnostic(distance, false, "arrived");
            return;
        }
        float playerSpeed = player.GetVelocity().magnitude;
        bool shouldRun = distance > _agent.Context.FollowStopDistance + 4f || playerSpeed > 4f;
        MoveToPoint(dt, player.transform.position, _agent.Context.FollowStopDistance, shouldRun, "Follow");
        LogFollowDiagnostic(distance, shouldRun, "move");
    }

    private void LogFollowDiagnostic(float distance, bool run, string reason)
    {
        if (!DebugAgentTaskDecisions)
            return;
        _followDebugTimer -= Time.deltaTime;
        if (_followDebugTimer > 0f)
            return;
        _followDebugTimer = 2f;
        Player player = Player.m_localPlayer;
        string playerPos = player != null ? FormatVector(player.transform.position) : "none";
        Debug.LogWarning("[AgentFollow] name=" + gameObject.name + " reason=" + reason + " agent=" + FormatVector(transform.position) + " player=" + playerPos + " dist=" + distance.ToString("0.0") + " stop=" + (_agent != null && _agent.Context != null ? _agent.Context.FollowStopDistance.ToString("0.0") : "0") + " run=" + run);
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

        if (IsMobileWorkTask())
        {
            float hardLeash = GetHomeHardLeashRadius();
            float workLeash = GetHomeWorkLeashRadius();

            if (hardLeash > 0f && distanceFromHome > hardLeash)
            {
                MoveToPoint(dt, home, HomeReturnStopDistance, true, "ReturnHomeHardLeash");
                return;
            }

            UpdateHomeWander(dt, home, Mathf.Max(radius, workLeash));
            return;
        }

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
            Vector2 offset = UnityEngine.Random.insideUnitCircle * range;
            Vector3 candidate = home + new Vector3(offset.x, 0f, offset.y);

            if (DistanceXZ(candidate, home) > radius)
                continue;

            if (ZoneSystem.instance == null || !ZoneSystem.instance.FindFloor(candidate, out float height))
                continue;
            candidate.y = height;
            WaterVolume waterVolume = null;
            if (Floating.IsUnderWater(candidate, ref waterVolume))
                continue;

            _homeWanderTarget = candidate;
            _homeWanderTargetSet = true;
            return;
        }

        _homeWanderTarget = home;
        _homeWanderTargetSet = true;
    }

    private void LogHuntControl(string reason)
    {
        if (!DebugAgentTaskDecisions)
            return;
        _huntControlDebugTimer -= Time.deltaTime;
        if (_huntControlDebugTimer > 0f)
            return;
        _huntControlDebugTimer = 2f;
        string playerPos = Player.m_localPlayer != null ? FormatVector(Player.m_localPlayer.transform.position) : "none";
        Debug.LogWarning("[AgentHuntControl] name=" + gameObject.name +
            " reason=" + reason +
            " state=" + (_agent != null && _agent.Context != null ? _agent.Context.StateMode.ToString() : "None") +
            " task=" + (_agent != null && _agent.Context != null ? _agent.Context.TaskMode.ToString() : "None") +
            " huntState=" + _huntState +
            " nativeTarget=" + FormatCharacter(GetNativeTargetCreature()) +
            " player=" + playerPos +
            " pos=" + FormatVector(transform.position));
    }

    private bool IsStayHomeHuntOutsideLeash(out Vector3 home, out float distance, out float radius)
    {
        home = transform.position;
        distance = 0f;
        radius = 0f;
        if (_agent == null || _agent.Context == null || _agent.Context.TaskMode != AgentTaskMode.Hunt || _agent.Context.StateMode != AgentStateMode.StayHome)
            return false;
        if (_agent.Context.HomeZone == null || !_agent.Context.HomeZone.IsSet)
            return false;
        home = _agent.Context.HomeZone.Center;
        radius = Mathf.Max(1f, _agent.Context.HomeZone.Radius);
        distance = DistanceXZ(transform.position, home);
        return distance > radius + HomeReturnDistanceGrace;
    }

    private void ResetHuntAndReturnHome(float dt, string reason)
    {
        ClearHuntCycleState();
        ChangeHuntState(AgentHuntState.Searching);
        if (IsStayHomeHuntOutsideLeash(out Vector3 home, out float distance, out float radius))
        {
            MoveToPoint(dt, home, HomeReturnStopDistance, distance > 8f, reason);
            return;
        }
        StopMoving();
    }

    private void LogHuntState(string reason, Character target)
    {
        if (_huntStateLogTimer > 0f)
            return;
        _huntStateLogTimer = 3f;
        string targetText = target != null ? GetPrefabName(target.gameObject) + " dist=" + DistanceXZ(transform.position, target.transform.position).ToString("0.0") : "none";
        float homeDist = 0f;
        float homeRadius = 0f;
        if (_agent != null && _agent.Context != null && _agent.Context.HomeZone != null && _agent.Context.HomeZone.IsSet)
        {
            homeDist = DistanceXZ(transform.position, _agent.Context.HomeZone.Center);
            homeRadius = _agent.Context.HomeZone.Radius;
        }
        Debug.LogWarning("[AgentHuntState] name=" + gameObject.name + " reason=" + reason + " huntState=" + _huntState + " target=" + targetText + " native=" + FormatCharacter(GetNativeTargetCreature()) + " home=" + homeDist.ToString("0.0") + "/" + homeRadius.ToString("0.0") + " pos=" + FormatVector(transform.position));
    }

    private void ClearCombatWeaponWhenNoHuntTarget()
    {
        if (_humanoid == null)
            return;
        if (GetNativeTargetCreature() != null || _huntTarget != null)
            return;
        ItemDrop.ItemData current = _humanoid.GetCurrentWeapon();
        if (current == null)
            return;
        try
        {
            _humanoid.UnequipItem(current, false);
        }
        catch
        {
        }
    }

    private bool UpdateHunt(float dt)
    {
        LogHuntControl("UpdateHunt");
        TryPromoteNativeHuntTarget();
        if ((_huntTarget == null || !IsValidHuntTarget(_huntTarget)) && _huntState != AgentHuntState.CollectingLoot && _huntState != AgentHuntState.Depositing && IsStayHomeHuntOutsideLeash(out Vector3 huntHome, out float huntHomeDistance, out float huntHomeRadius))
        {
            ClearNativeAggro();
            _huntTarget = null;
            _currentLootTarget = null;
            _huntSearchTimer = HuntSearchInterval;
            if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(huntHome, out float huntHomeFloor))
                huntHome.y = huntHomeFloor;
            MoveToPoint(dt, huntHome, HomeReturnStopDistance, huntHomeDistance > 8f, "HuntReturnNoTarget");
            ChangeHuntState(AgentHuntState.ReturningToAnchor);
            return true;
        }
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

    private bool TryPromoteNativeHuntTarget()
    {
        Character nativeTarget = GetNativeTargetCreature();
        if (nativeTarget == null || !IsValidHuntTarget(nativeTarget))
            return false;
        _huntTarget = nativeTarget;
        _huntTargetLastKnownPosition = nativeTarget.transform.position;
        float distance = DistanceXZ(transform.position, nativeTarget.transform.position);
        LogHuntState("native-promoted", nativeTarget);
        if (distance <= HuntNativeEngageRange)
            ChangeHuntState(AgentHuntState.Fighting);
        else
            ChangeHuntState(AgentHuntState.TravellingToTarget);
        return true;
    }

    private bool HuntSearching()
    {
        if (_huntSearchTimer <= 0f)
        {
            _huntSearchTimer = HuntSearchInterval;
            _huntTarget = FindHuntTarget();

            if (_huntTarget != null)
            {
                _huntTargetLastKnownPosition = _huntTarget.transform.position;
                LogHunt("Target selected " + GetPrefabName(_huntTarget.gameObject) + " distance=" + DistanceXZ(transform.position, _huntTarget.transform.position).ToString("0.0"), true);
                LogHuntState("selected", _huntTarget);
                ChangeHuntState(AgentHuntState.TravellingToTarget);
                return true;
            }
        }

        ClearNativeAggro();
        ClearCombatWeaponWhenNoHuntTarget();
        if (IsStayHomeHuntOutsideLeash(out Vector3 home, out float homeDistance, out float homeRadius))
        {
            MoveToPoint(Time.deltaTime, home, HomeReturnStopDistance, homeDistance > 8f, "HuntSearchReturnHome");
            return true;
        }
        Vector3 anchor = GetCurrentAnchorPosition();
        float wanderRadius = Mathf.Max(6f, Mathf.Min(GetHuntSearchRange(), GetWorkRadius()) * 0.8f);
        if (_character != null && (_character.InWater() || _character.IsSwimming()))
        {
            _homeWanderTargetSet = false;
            MoveToPoint(Time.deltaTime, anchor, HomeReturnStopDistance, false, "HuntWaterReturn");
            return true;
        }
        UpdateHomeWander(Time.deltaTime, anchor, wanderRadius);
        return true;
    }

    private void HuntTravelling(float dt)
    {
        if (!IsValidHuntTarget(_huntTarget))
        {
            LogHuntState("invalid-target", _huntTarget);
            ResetHuntAndReturnHome(dt, "HuntInvalidTargetReturn");
            return;
        }

        _huntTargetLastKnownPosition = _huntTarget.transform.position;
        if (IsStayHomeHuntOutsideLeash(out Vector3 home, out float homeDistance, out float homeRadius) && homeDistance > Mathf.Max(homeRadius + 8f, GetHomeWorkLeashRadius()))
        {
            ResetHuntAndReturnHome(dt, "HuntLeashReturnHome");
            return;
        }
        if (IsBeyondHomeHardLeash(_huntTargetLastKnownPosition))
        {
            ClearNativeAggro();
            ChangeHuntState(AgentHuntState.Searching);
            return;
        }
        if (_character != null && (_character.InWater() || _character.IsSwimming()) && _agent != null && _agent.Context != null && _agent.Context.StateMode == AgentStateMode.StayHome)
        {
            Vector3 anchor = GetCurrentAnchorPosition();
            if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(anchor, out float anchorHeight))
                anchor.y = anchorHeight;
            MoveToPoint(dt, anchor, HomeReturnStopDistance, true, "HuntWaterReturn");
            return;
        }
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

        bool pickedUp = false;

        if (pickup != null && inventory != null)
            pickedUp = pickup.TryPickupDrop(_currentLootTarget, inventory);

        if (!pickedUp)
            pickedUp = TryPickupLoot(_currentLootTarget);

        if (pickedUp)
        {
            _currentLootTarget = null;
            _lootSearchTimer = 0f;
            return;
        }

        _ignoredLootInstanceIds.Add(_currentLootTarget.GetInstanceID());
        _currentLootTarget = null;
        _lootSearchTimer = 0f;
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
    private Vector3 GetWorkAnchor()
    {
        if (_agent == null || _agent.Context == null)
            return transform.position;

        if (_agent.Context.StateMode == AgentStateMode.Follow && Player.m_localPlayer != null)
            return Player.m_localPlayer.transform.position;

        if (_agent.Context.StateMode == AgentStateMode.StayHome &&
            _agent.Context.HomeZone != null &&
            _agent.Context.HomeZone.IsSet)
        {
            return _agent.Context.HomeZone.Center;
        }

        if (_agent.Context.IdleOrigin != Vector3.zero)
            return _agent.Context.IdleOrigin;

        return transform.position;
    }

    private float GetWorkRadius()
    {
        if (_agent == null || _agent.Context == null)
            return 35f;

        if (_agent.Context.StateMode == AgentStateMode.Follow)
            return 35f;

        if (_agent.Context.StateMode == AgentStateMode.StayHome &&
            _agent.Context.HomeZone != null &&
            _agent.Context.HomeZone.IsSet)
        {
            return Mathf.Max(4f, _agent.Context.HomeZone.Radius);
        }

        return Mathf.Max(8f, _agent.Context.IdleRadius);
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
        Character best = FindHuntTargetFromCandidates(Character.GetAllCharacters(), out int totalCharacters, out int skippedSelf, out int skippedDead, out int skippedPlayer, out int skippedAgent, out int seen, out int validPrefab, out int validRange, out int invalidPrefab, out int outsideAnchor, out int outsideChase, out string rejectedNames, out string seenNames);
        if (best == null)
        {
            Character[] fallback = UnityEngine.Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
            best = FindHuntTargetFromCandidates(fallback, out totalCharacters, out skippedSelf, out skippedDead, out skippedPlayer, out skippedAgent, out seen, out validPrefab, out validRange, out invalidPrefab, out outsideAnchor, out outsideChase, out rejectedNames, out seenNames);
        }
        if (best == null)
        {
            LogHuntNoTarget(totalCharacters, skippedSelf, skippedDead, skippedPlayer, skippedAgent, seen, validPrefab, validRange, invalidPrefab, outsideAnchor, outsideChase, rejectedNames, seenNames);
            LogHuntScanEmpty(totalCharacters, skippedSelf, skippedDead, skippedPlayer, skippedAgent, seen, validPrefab, validRange, invalidPrefab, outsideAnchor, outsideChase, GetCurrentAnchorPosition(), GetHuntSearchRange(), rejectedNames, seenNames);
        }
        else if (_huntNoTargetLogTimer <= 0f)
        {
            _huntNoTargetLogTimer = 6f;
            Debug.LogWarning("[AgentHuntAcquire] name=" + gameObject.name +
                " selected=" + GetPrefabName(best.gameObject) +
                " dist=" + DistanceXZ(transform.position, best.transform.position).ToString("0.0") +
                " homeDist=" + DistanceXZ(best.transform.position, GetCurrentAnchorPosition()).ToString("0.0") +
                " pos=" + FormatVector(transform.position));
        }
        return best;
    }

    private Character FindHuntTargetFromCandidates(IEnumerable<Character> candidates, out int totalCharacters, out int skippedSelf, out int skippedDead, out int skippedPlayer, out int skippedAgent, out int seen, out int validPrefab, out int validRange, out int invalidPrefab, out int outsideAnchor, out int outsideChase, out string rejectedNames, out string seenNames)
    {
        Character best = null;
        float searchRange = GetHuntSearchRange();
        float bestDistance = HuntMaxChaseDistance;
        Vector3 anchor = GetCurrentAnchorPosition();
        totalCharacters = 0;
        skippedSelf = 0;
        skippedDead = 0;
        skippedPlayer = 0;
        skippedAgent = 0;
        seen = 0;
        validPrefab = 0;
        validRange = 0;
        invalidPrefab = 0;
        outsideAnchor = 0;
        outsideChase = 0;
        rejectedNames = string.Empty;
        seenNames = string.Empty;
        if (candidates == null)
            return null;
        foreach (Character candidate in candidates)
        {
            if (candidate == null)
                continue;
            totalCharacters++;
            string prefabName = GetPrefabName(candidate.gameObject);
            if (seenNames.Length < 180)
                seenNames += (seenNames.Length == 0 ? string.Empty : ",") + prefabName;
            if (candidate == _character)
            {
                skippedSelf++;
                continue;
            }
            if (candidate.IsDead())
            {
                skippedDead++;
                continue;
            }
            if (candidate.IsPlayer())
            {
                skippedPlayer++;
                continue;
            }
            if (candidate.GetComponent<AgentComponent>() != null)
            {
                skippedAgent++;
                continue;
            }
            seen++;
            if (!IsHuntableCandidate(candidate, prefabName))
            {
                invalidPrefab++;
                if (invalidPrefab <= 8)
                    rejectedNames += (rejectedNames.Length == 0 ? string.Empty : ",") + prefabName;
                continue;
            }
            validPrefab++;
            float distance = DistanceXZ(transform.position, candidate.transform.position);
            float anchorDistance = DistanceXZ(candidate.transform.position, anchor);
            if (_agent != null && _agent.Context != null && _agent.Context.StateMode == AgentStateMode.StayHome && _agent.Context.HomeZone != null && _agent.Context.HomeZone.IsSet)
            {
                float allowedHomeDistance = Mathf.Max(6f, _agent.Context.HomeZone.Radius - 3f);
                if (anchorDistance > allowedHomeDistance)
                {
                    outsideAnchor++;
                    continue;
                }
            }
            else if (anchorDistance > searchRange && distance > searchRange)
            {
                outsideAnchor++;
                continue;
            }
            if (distance > HuntMaxChaseDistance)
            {
                outsideChase++;
                continue;
            }
            validRange++;
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }
        return best;
    }

    private void LogHuntNoTarget(int totalCharacters, int skippedSelf, int skippedDead, int skippedPlayer, int skippedAgent, int seen, int validPrefab, int validRange, int invalidPrefab, int outsideAnchor, int outsideChase, string rejectedNames, string seenNames)
    {
        if (_huntNoTargetLogTimer > 0f)
            return;
        _huntNoTargetLogTimer = 6f;
        Debug.LogWarning("[AgentHuntNoTarget] name=" + gameObject.name +
            " total=" + totalCharacters +
            " self=" + skippedSelf +
            " dead=" + skippedDead +
            " players=" + skippedPlayer +
            " agents=" + skippedAgent +
            " seen=" + seen +
            " valid=" + validPrefab +
            " range=" + validRange +
            " invalid=" + invalidPrefab +
            " outsideAnchor=" + outsideAnchor +
            " outsideChase=" + outsideChase +
            " rejected=" + rejectedNames +
            " seenNames=" + seenNames +
            " pos=" + FormatVector(transform.position));
    }

    private void LogHuntScanEmpty(int totalCharacters, int skippedSelf, int skippedDead, int skippedPlayer, int skippedAgent, int seen, int validPrefab, int validRange, int invalidPrefab, int outsideAnchor, int outsideChase, Vector3 anchor, float searchRange, string rejectedNames, string seenNames)
    {
        if (!DebugHuntScan)
            return;
        _huntScanDebugTimer -= Time.deltaTime;
        if (_huntScanDebugTimer > 0f)
            return;
        _huntScanDebugTimer = 5f;
        Debug.LogWarning("[AgentHuntScan] empty name=" + gameObject.name +
            " total=" + totalCharacters +
            " self=" + skippedSelf +
            " dead=" + skippedDead +
            " players=" + skippedPlayer +
            " agents=" + skippedAgent +
            " seen=" + seen +
            " validPrefab=" + validPrefab +
            " validRange=" + validRange +
            " invalidPrefab=" + invalidPrefab +
            " outsideAnchor=" + outsideAnchor +
            " outsideChase=" + outsideChase +
            " rejected=" + rejectedNames +
            " seenNames=" + seenNames +
            " anchor=" + FormatVector(anchor) +
            " searchRange=" + searchRange.ToString("0.0") +
            " pos=" + FormatVector(transform.position));
    }

    private bool IsValidHuntTarget(Character candidate)
    {
        if (candidate == null || candidate == _character)
            return false;
        if (candidate.IsDead() || candidate.IsPlayer())
            return false;
        if (candidate.GetComponent<AgentComponent>() != null)
            return false;
        if (!IsHuntableCandidate(candidate, GetPrefabName(candidate.gameObject)))
            return false;
        float distance = DistanceXZ(transform.position, candidate.transform.position);
        if (distance > HuntMaxChaseDistance)
            return false;
        if (_agent != null && _agent.Context != null && _agent.Context.StateMode == AgentStateMode.StayHome && _agent.Context.HomeZone != null && _agent.Context.HomeZone.IsSet)
        {
            float anchorDistance = DistanceXZ(candidate.transform.position, _agent.Context.HomeZone.Center);
            float allowedHomeDistance = Mathf.Max(6f, _agent.Context.HomeZone.Radius - 3f);
            if (anchorDistance > allowedHomeDistance)
                return false;
        }
        return true;
    }

    private bool IsHuntableCandidate(Character candidate, string prefabName)
    {
        if (candidate == null)
            return false;
        if (HuntablePrefabs.Contains(prefabName))
            return true;
        for (int i = 0; i < HuntablePrefabs.Count; i++)
        {
        }
        if (Contains(prefabName, "Boar") || Contains(prefabName, "Deer") || Contains(prefabName, "Neck") || Contains(prefabName, "Hare") || Contains(prefabName, "Wolf") || Contains(prefabName, "Greyling") || Contains(prefabName, "Greydwarf"))
            return true;
        if (_character != null && BaseAI.IsEnemy(_character, candidate))
            return true;
        return false;
    }

    private ItemDrop FindNearestLoot()
    {
        ItemDrop[] drops = UnityEngine.Object.FindObjectsByType<ItemDrop>(FindObjectsSortMode.None);
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

        if (dropView != null)
        {
            if (!dropView.IsValid())
                return;

            if (!dropView.IsOwner())
                dropView.ClaimOwnership();

            if (ZNetScene.instance != null)
            {
                ZNetScene.instance.Destroy(drop.gameObject);
                return;
            }

            return;
        }

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

        return !HasBlockingSleepThreat();
    }
    private bool ShouldTaskBlockNightSleep()
    {
        return _agent != null &&
               _agent.Context != null &&
               _agent.Context.TaskMode != AgentTaskMode.None;
    }
    private bool HasActiveAssignedBedSleepState()
    {
        return _bedSleepStateApplied || _isPlacedAtBed || _activeSleepBed != null || _sleepBodyLocked;
    }

    private bool HasBlockingSleepThreat()
    {
        if (_agent == null || _agent.Context == null || _agent.Context.HomeZone == null || !_agent.Context.HomeZone.IsSet || _character == null)
            return false;

        Vector3 home = _agent.Context.HomeZone.Center;
        float radius = Mathf.Max(8f, _agent.Context.HomeZone.Radius);

        foreach (Character candidate in Character.GetAllCharacters())
        {
            if (candidate == null || candidate == _character || candidate.IsDead() || candidate.IsPlayer())
                continue;

            if (DistanceXZ(candidate.transform.position, home) > radius)
                continue;

            if (!BaseAI.IsEnemy(_character, candidate))
                continue;

            if (IsPassiveWildlifeSleepIgnored(candidate))
                continue;

            return true;
        }

        return false;
    }

    private bool IsPassiveWildlifeSleepIgnored(Character candidate)
    {
        if (candidate == null || candidate.gameObject == null)
            return false;

        string prefabName = GetPrefabName(candidate.gameObject);
        return Contains(prefabName, "Deer") || Contains(prefabName, "Boar") || Contains(prefabName, "Hare") || Contains(prefabName, "Neck");
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
        ResetHuntStateIfNeeded();
        _activeDepositContainer = null;
        float distanceToBed = Mathf.Min(GetDistanceToBedSurface(bed), DistanceXZ(transform.position, bed.transform.position));

        if (distanceToBed <= BedReachDistance)
        {
            StopMoving();

            if (!_isPlacedAtBed)
            {
                PlaceAtBedSleepPosition(bed);
                _isPlacedAtBed = true;
            }
            else
            {
                if (_character != null)
                    _character.SetMoveDir(Vector3.zero);

                Rigidbody body = GetComponent<Rigidbody>();

                if (body != null && !body.isKinematic)
                {
                    ClearBodyVelocityIfDynamic(body);
                }
            }

            _activeSleepBed = bed;
            EnterAssignedBedSleepState();

            if (!_bedSleepLogged)
                _bedSleepLogged = true;

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

            _activeSleepBed = bed;
            EnterAssignedBedSleepState();
        }
    }

    private Bed FindBedNearPosition(Vector3 position)
    {
        Bed best = null;
        float bestDistance = 10f;
        Bed[] beds = UnityEngine.Object.FindObjectsByType<Bed>(FindObjectsSortMode.None);

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
        if (bed == null)
            return;

        Transform sleepTransform = bed.m_spawnPoint != null ? bed.m_spawnPoint : bed.transform;
        Vector3 exit = bed.transform.position + bed.transform.right * BedExitSideOffset;

        if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(exit, out float exitHeight))
            exit.y = exitHeight;

        _pendingBedExitPosition = exit;
        _hasPendingBedExit = true;
        PlaceAtPosition(sleepTransform.position, sleepTransform.rotation);
        LockBodyForBedSleep(sleepTransform.position, sleepTransform.rotation);
    }

    private void MaintainBedSleepPosition(Bed bed)
    {
        if (bed == null)
            return;

        Transform sleepTransform = bed.m_spawnPoint != null ? bed.m_spawnPoint : bed.transform;
        transform.position = sleepTransform.position;
        transform.rotation = sleepTransform.rotation;

        if (_body == null)
            _body = GetComponent<Rigidbody>();

        if (_body != null)
        {
            _body.position = sleepTransform.position;
            _body.rotation = sleepTransform.rotation;
            ClearBodyVelocityIfDynamic(_body);
            _body.useGravity = false;
            _body.isKinematic = true;
            _sleepBodyLocked = true;
        }
    }

    private void LockBodyForBedSleep(Vector3 position, Quaternion rotation)
    {
        if (_body == null)
            _body = GetComponent<Rigidbody>();

        if (_body == null)
            return;

        EnsureDynamicBody();
        _body.position = position;
        _body.rotation = rotation;
        _sleepBodyLocked = false;
    }

    private void UnlockBodyFromBedSleep()
    {
        EnsureDynamicBody();
        _sleepBodyLocked = false;
    }


    private void EnsureDynamicBody()
    {
        if (_body == null)
            _body = GetComponent<Rigidbody>();

        if (_body == null)
            return;

        if (_body.isKinematic)
            _body.isKinematic = false;

        if (!_body.useGravity)
            _body.useGravity = true;
    }

    private void PlaceAtPosition(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;

        if (_body == null)
            _body = GetComponent<Rigidbody>();

        if (_body != null)
        {
            EnsureDynamicBody();
            _body.position = position;
            _body.rotation = rotation;
            ClearBodyVelocityIfDynamic(_body);
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
        ClearTaskMotionForSleep();

        if (_character != null)
            _character.SetMoveDir(Vector3.zero);

        if (_humanoid != null)
        {
            _humanoid.SetRun(false);
            _humanoid.SetWalk(false);
        }

        if (_activeSleepBed != null)
            MaintainBedSleepPosition(_activeSleepBed);

        ClearNativeAggro();

        SetBoolField(_monsterAI, "m_sleeping", true);
        SetBoolField(_monsterAI, "m_alerted", false);

        if (!_bedSleepStateApplied)
        {
            if (_zanim == null)
                _zanim = GetComponent<ZSyncAnimation>();

            if (_zanim != null)
                _zanim.SetBool("attach_bed", true);

            if (_zNetView != null && _zNetView.IsValid())
            {
                ZDO zdo = _zNetView.GetZDO();
                if (zdo != null)
                    zdo.Set(ZDOVars.s_inBed, true);
            }

            _bedSleepStateApplied = true;
        }

        if (!_isPlayingBedSleepAnimation)
            _bedWakeGraceTimer = BedWakeGraceDuration;

        PlaySleepAnimation();
    }


    private void LogSleepLockState(string owner)
    {
        _sleepLockLogTimer -= Time.deltaTime;
        if (_sleepLockLogTimer > 0f)
            return;
        _sleepLockLogTimer = 3f;
        string weapon = "none";
        if (_humanoid != null)
        {
            ItemDrop.ItemData current = _humanoid.GetCurrentWeapon();
            if (current != null && current.m_dropPrefab != null)
                weapon = current.m_dropPrefab.name;
            else if (current != null && current.m_shared != null)
                weapon = current.m_shared.m_name;
        }
        float speed = _body != null ? _body.linearVelocity.magnitude : (_character != null ? _character.GetVelocity().magnitude : 0f);
        Debug.LogWarning("[AgentSleepLock] name=" + gameObject.name + " owner=" + owner + " pos=" + FormatVector(transform.position) + " vel=" + speed.ToString("0.00") + " weapon=" + weapon + " bodyKinematic=" + (_body != null && _body.isKinematic));
    }

    private void ClearTaskMotionForSleep()
    {
        _pathCorrectionTimer = 0f;
        _pathStuckTimer = 0f;
        _moveFalseDoneCount = 0;
        _homeWanderTargetSet = false;
        _activeDepositContainer = null;
        ClearHuntCycleState();
        if (_humanoid != null)
        {
            ItemDrop.ItemData current = _humanoid.GetCurrentWeapon();
            if (current != null)
            {
                try
                {
                    _humanoid.UnequipItem(current, false);
                }
                catch
                {
                }
            }
            _humanoid.SetRun(false);
            _humanoid.SetWalk(false);
        }
        if (_body == null)
            _body = GetComponent<Rigidbody>();
        if (_body != null)
        {
            ClearBodyVelocityIfDynamic(_body);
            if (_activeSleepBed != null || _sleepBodyLocked || _isPlacedAtBed || _bedSleepStateApplied)
            {
                _body.useGravity = false;
                _body.isKinematic = true;
                _sleepBodyLocked = true;
            }
        }
    }

    private void ClearAssignedBedSleepState()
    {
        bool shouldPlayWakeup = _bedSleepStateApplied || _isPlacedAtBed || _activeSleepBed != null || _hasPendingBedExit;
        bool hadAnySleepState = shouldPlayWakeup || _isPlayingBedSleepAnimation || _sleepBodyLocked;

        EnsureDynamicBody();
        SetBoolField(_monsterAI, "m_sleeping", false);
        ClearBedNetworkState();

        if (shouldPlayWakeup)
            StartBedWakeup();
        else if (hadAnySleepState)
            ClearWakeupWithoutAnimatorTransition();

        _bedSleepLogged = false;
        _isPlacedAtBed = false;
        _activeSleepBed = null;
        _sleepBodyLocked = false;
        _bedWakeGraceTimer = 0f;
    }

    private void ClearBedNetworkState()
    {
        if (_zanim == null)
            _zanim = GetComponent<ZSyncAnimation>();

        if (_zanim != null)
            _zanim.SetBool("attach_bed", false);

        if (_zNetView != null && _zNetView.IsValid())
        {
            ZDO zdo = _zNetView.GetZDO();
            if (zdo != null)
                zdo.Set(ZDOVars.s_inBed, false);
        }

        _bedSleepStateApplied = false;
    }


    private void StartBedWakeup()
    {
        EnsureDynamicBody();
        StopMoving();

        if (_character != null)
            _character.SetMoveDir(Vector3.zero);

        SetBoolField(_monsterAI, "m_sleeping", false);
        ClearBedNetworkState();
        _sleepBodyLocked = false;

        bool canPlayWakeup = _isPlacedAtBed || _activeSleepBed != null || _hasPendingBedExit;

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_animator != null)
        {
            _animator.speed = _savedAnimatorSpeed;

            if (HasAnimatorParameter(_animator, "sleeping", AnimatorControllerParameterType.Bool))
                _animator.SetBool(SleepingHash, false);

            if (HasAnimatorParameter(_animator, "attach_bed", AnimatorControllerParameterType.Bool))
                _animator.SetBool(AttachBedHash, false);

            if (HasAnimatorParameter(_animator, "lying_down", AnimatorControllerParameterType.Trigger))
                _animator.ResetTrigger(LyingDownHash);

            if (HasAnimatorParameter(_animator, "lying_down", AnimatorControllerParameterType.Bool))
                _animator.SetBool(LyingDownHash, false);

            if (canPlayWakeup && HasAnimatorParameter(_animator, "wakeup", AnimatorControllerParameterType.Trigger))
                _animator.SetTrigger(WakeupHash);
            else if (canPlayWakeup && HasAnimatorState(_animator, "WakeUp"))
                _animator.CrossFadeInFixedTime("WakeUp", 0.15f);
            else if (HasAnimatorState(_animator, "Idle"))
                _animator.CrossFadeInFixedTime("Idle", 0.15f);
        }

        _isPlayingBedSleepAnimation = false;
        _isPlacedAtBed = false;
        _activeSleepBed = null;
        _bedWakeGraceTimer = 0f;

        if (_hasPendingBedExit)
        {
            PlaceAtPosition(_pendingBedExitPosition, transform.rotation);
            _hasPendingBedExit = false;
        }

        _isWakingFromBed = canPlayWakeup;
        _bedWakeLockTimer = canPlayWakeup ? 0.35f : 0f;
    }


    private void PlaySleepAnimation()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
            return;

        bool usedAnimatorSleepPath = false;

        if (HasAnimatorParameter(_animator, "attach_bed", AnimatorControllerParameterType.Bool))
        {
            _animator.SetBool(AttachBedHash, true);
            usedAnimatorSleepPath = true;
        }

        if (HasAnimatorParameter(_animator, "sleeping", AnimatorControllerParameterType.Bool))
        {
            _animator.SetBool(SleepingHash, true);
            usedAnimatorSleepPath = true;
        }

        if (HasAnimatorParameter(_animator, "lying_down", AnimatorControllerParameterType.Trigger))
        {
            _animator.SetTrigger(LyingDownHash);
            usedAnimatorSleepPath = true;
        }

        if (_isPlayingBedSleepAnimation)
            return;

        _savedAnimatorSpeed = _animator.speed;
        _animator.speed = 1f;

        if (usedAnimatorSleepPath)
        {
            _animator.Update(0f);
            _isPlayingBedSleepAnimation = true;
            return;
        }

        if (HasAnimatorState(_animator, "Laydown"))
        {
            _animator.CrossFadeInFixedTime("Laydown", 0.15f);
            _isPlayingBedSleepAnimation = true;
            return;
        }

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
            Debug.LogWarning("[Agent Bed] Sleep animator path missing; skipping sleep/wakeup animator transitions on " + gameObject.name);
        }

        _isPlayingBedSleepAnimation = false;
        StopMoving();

        if (_character != null)
            _character.SetMoveDir(Vector3.zero);
    }

    private void ClearWakeupWithoutAnimatorTransition()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_animator != null)
        {
            _animator.speed = _savedAnimatorSpeed;
            if (HasAnimatorParameter(_animator, "sleeping", AnimatorControllerParameterType.Bool))
                _animator.SetBool(SleepingHash, false);
            if (HasAnimatorParameter(_animator, "attach_bed", AnimatorControllerParameterType.Bool))
                _animator.SetBool(AttachBedHash, false);
            if (HasAnimatorParameter(_animator, "lying_down", AnimatorControllerParameterType.Trigger))
                _animator.ResetTrigger(LyingDownHash);
            if (HasAnimatorParameter(_animator, "lying_down", AnimatorControllerParameterType.Bool))
                _animator.SetBool(LyingDownHash, false);
            if (HasAnimatorState(_animator, "Idle"))
                _animator.CrossFadeInFixedTime("Idle", 0.10f);
        }

        _isPlayingBedSleepAnimation = false;
        _isWakingFromBed = false;
        _hasPendingBedExit = false;
        _bedWakeLockTimer = 0f;
    }

    private void TryAttachCharacterToBed(Bed bed)
    {
        if (_character == null || bed == null)
            return;
        MethodInfo[] methods = _character.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];
            if (method == null || method.Name != "AttachStart")
                continue;
            object[] args = BuildAttachStartArgs(method, bed);
            if (args == null)
                continue;
            try
            {
                method.Invoke(_character, args);
                return;
            }
            catch
            {
            }
        }
    }
    private object[] BuildAttachStartArgs(MethodInfo method, Bed bed)
    {
        ParameterInfo[] parameters = method.GetParameters();
        object[] args = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            System.Type type = parameters[i].ParameterType;
            if (type == typeof(Transform))
                args[i] = bed.transform;
            else if (type == typeof(GameObject))
                args[i] = bed.gameObject;
            else if (type == typeof(Vector3))
                args[i] = _pendingBedExitPosition;
            else if (type == typeof(string))
                args[i] = "lying_down";
            else if (type == typeof(bool))
                args[i] = true;
            else if (type.IsValueType)
                args[i] = System.Activator.CreateInstance(type);
            else
                args[i] = null;
        }
        return args;
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
        float patrolRadius = Mathf.Max(2f, radius * 0.55f);

        for (int attempt = 0; attempt < 16; attempt++)
        {
            _patrolAngle += PatrolStepDegrees * _patrolDirection;

            if (_patrolAngle > 360f)
                _patrolAngle -= 360f;

            if (_patrolAngle < 0f)
                _patrolAngle += 360f;

            float angleRad = _patrolAngle * Mathf.Deg2Rad;
            Vector3 candidate = home + new Vector3(Mathf.Cos(angleRad) * patrolRadius, 0f, Mathf.Sin(angleRad) * patrolRadius);

            if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(candidate, out float floorHeight))
                candidate.y = floorHeight;

            float waterLevel = Floating.GetWaterLevel(candidate, ref _patrolWaterVolume);
            if (waterLevel > candidate.y + 0.25f)
                continue;

            _patrolTarget = candidate;
            return;
        }

        _patrolTarget = home;
        if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(_patrolTarget, out float homeFloor))
            _patrolTarget.y = homeFloor;
    }

    public AgentMoveResult MoveForTaskEx(float dt, Vector3 point, float stopDistance, bool run, string reason)
    {
        if (_character == null || _character.IsDead())
            return AgentMoveResult.Invalid;

        if (_body == null)
            _body = GetComponent<Rigidbody>();

        if (_body != null && _body.isKinematic)
        {
            StopMoving();
            return AgentMoveResult.Blocked;
        }

        float distance = DistanceXZ(transform.position, point);

        if (distance <= stopDistance)
            return MoveToPoint(dt, point, stopDistance, run, reason) ? AgentMoveResult.Arrived : AgentMoveResult.Moving;

        bool moved = MoveToPoint(dt, point, stopDistance, run, reason);

        if (moved)
            return AgentMoveResult.Arrived;

        if (_pathStuckTimer >= 3.5f)
            return AgentMoveResult.Failed;

        return AgentMoveResult.Moving;
    }

    public bool MoveForTask(float dt, Vector3 point, float stopDistance, bool run, string reason)
    {
        return MoveForTaskEx(dt, point, stopDistance, run, reason) == AgentMoveResult.Arrived;
    }

    private bool MoveToPoint(float dt, Vector3 point, float stopDistance, bool run, string reason)
    {
        if (_body == null)
            _body = GetComponent<Rigidbody>();

        if (_body != null && _body.isKinematic)
        {
            StopMoving();
            ResetPathCorrection();
            return false;
        }

        float distance = DistanceXZ(transform.position, point);

        if (distance <= stopDistance)
        {
            StopMoving();
            ResetPathCorrection();
            LogMove(reason, point, stopDistance, run, distance, "arrived");
            return true;
        }
        if (_character != null && (_character.IsSwimming() || _character.InWater()))
        {
            ResetPathCorrection();
            MoveDirectlyToward(point, run);
            ApplyWaterMoveVelocity(point, run);
            LogMove(reason, point, stopDistance, run, distance, "water direct");
            return false;
        }

        if (_doorHandler == null)
            _doorHandler = GetComponent<AgentDoorHandler>();

        if (_doorHandler != null && _doorHandler.TryHandleDoorForMove(dt, point, run, reason))
            return false;
        AgentStamina stamina = GetComponent<AgentStamina>();

        if (run && stamina != null && stamina.Stamina <= 1f)
            run = false;

        if (_pathCorrectionTimer > 0f && Mathf.Abs(_pathCorrectionTarget.y - transform.position.y) > 3f)
            ResetPathCorrection();

        if (_pathCorrectionTimer > 0f)
        {
            _pathCorrectionTimer -= dt;
            FacePoint(point);
            MoveDirectlyToward(_pathCorrectionTarget, true);

            if (_moveToMethod != null && _monsterAI != null)
                _moveToMethod.Invoke(_monsterAI, new object[] { dt, _pathCorrectionTarget, 0.65f, true });

            return false;
        }

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

        if (moveFinished && distance > stopDistance + 0.35f)
        {
            _moveFalseDoneCount++;
            FacePoint(point);
            MoveDirectlyToward(point, run);
            if (_moveFalseDoneCount >= MoveFalseDoneThreshold)
            {
                StartPathCorrection(point);
                _moveFalseDoneCount = 0;
            }
            LogMove(reason, point, stopDistance, run, distance, "false-done corrected result=" + result);
            return false;
        }

        _moveFalseDoneCount = 0;

        if (!moveFinished && IsPathStuck(dt))
        {
            if (TryStairStepAssist(point, distance))
            {
                LogMove(reason, point, stopDistance, true, distance, "stair assist");
                return false;
            }
            StartPathCorrection(point);
            LogMove(reason, _pathCorrectionTarget, stopDistance, true, distance, "path correction");
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

        if (_pathStuckCheckTimer < 0.35f)
            return false;

        float moved = Vector3.Distance(transform.position, _pathStuckCheckPosition);
        float velocity = _character.GetVelocity().magnitude;
        _pathStuckCheckTimer = 0f;
        _pathStuckCheckPosition = transform.position;

        if (velocity > 0.28f || moved > 0.28f)
        {
            _pathStuckTimer = 0f;
            return false;
        }

        _pathStuckTimer += 0.35f;
        return _pathStuckTimer >= 0.7f;
    }

    private bool TryStairStepAssist(Vector3 point, float horizontalDistance)
    {
        if (_character == null)
            return false;
        float vertical = point.y - transform.position.y;
        if (vertical < 0.25f || vertical > 3.0f)
            return false;
        if (horizontalDistance > 7.5f)
            return false;
        FacePoint(point);
        MoveDirectlyToward(point, true);
        TryInvokeCharacterJump();
        if (_body == null)
            _body = GetComponent<Rigidbody>();
        if (_body != null && !_body.isKinematic)
        {
            Vector3 velocity = _body.linearVelocity;
            if (velocity.y < 1.5f)
                velocity.y = 1.5f;
            _body.linearVelocity = velocity;
        }
        _pathStuckTimer = 0f;
        _pathCorrectionTimer = 0.25f;
        _pathCorrectionTarget = point;
        return true;
    }

    private void TryInvokeCharacterJump()
    {
        if (_character == null)
            return;
        MethodInfo method = FindMethod(_character.GetType(), "Jump");
        if (method == null)
            return;
        ParameterInfo[] parameters = method.GetParameters();
        object[] args = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType == typeof(bool))
                args[i] = true;
            else if (parameters[i].ParameterType.IsValueType)
                args[i] = System.Activator.CreateInstance(parameters[i].ParameterType);
            else
                args[i] = null;
        }
        try
        {
            method.Invoke(_character, args);
        }
        catch
        {
        }
    }

    private void StartPathCorrection(Vector3 finalTarget)
    {
        if (_character == null)
            return;

        Vector3 direction = finalTarget - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            direction = transform.forward;

        direction.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, direction).normalized * _pathCorrectionSide;
        _pathCorrectionSide *= -1;
        _pathCorrectionTarget = transform.position + direction * PathCorrectionForward + side * PathCorrectionSide;

        if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(_pathCorrectionTarget, out float height))
            _pathCorrectionTarget.y = height;

        _pathCorrectionTimer = PathCorrectionDuration;
        _pathStuckTimer = 0f;
        _pathStuckCheckTimer = 0f;
        FacePoint(finalTarget);
        MoveDirectlyToward(_pathCorrectionTarget, true);
    }

    private void ResetPathCorrection()
    {
        _pathCorrectionTimer = 0f;
        _pathStuckTimer = 0f;
        _pathStuckCheckTimer = 0f;
        _pathStuckCheckPosition = transform.position;
    }

    private void FacePoint(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction.normalized, Vector3.up), 720f * Time.deltaTime);
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
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), 720f * Time.deltaTime);
        _character.SetMoveDir(direction);

        if (_humanoid != null)
            _humanoid.SetRun(run);
    }

    private void ApplyWaterMoveVelocity(Vector3 point, bool run)
    {
        if (_body == null)
            _body = GetComponent<Rigidbody>();
        if (_body == null || _body.isKinematic)
            return;
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return;
        direction.Normalize();
        float speed = run ? 3.2f : 2.1f;
        Vector3 velocity = _body.linearVelocity;
        velocity.x = direction.x * speed;
        velocity.z = direction.z * speed;
        if (velocity.y < -0.5f)
            velocity.y = -0.5f;
        _body.linearVelocity = velocity;
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

        if (_stopMovingMethod != null && _monsterAI != null)
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

    private bool IsPassiveWildlifeTarget(Character target)
    {
        if (target == null || target.gameObject == null)
            return false;
        string prefab = GetPrefabName(target.gameObject);
        return Contains(prefab, "Deer") || Contains(prefab, "Hare");
    }

    private bool TargetAllowedByCurrentMode(Character target)
    {
        if (target == null || target.IsDead())
            return false;
        if (IsControlledNonCombatTask())
        {
            if (IsPassiveWildlifeTarget(target))
                return false;
            float workThreatLimit = _agent.Context.BehaviourMode == AgentBehaviourMode.Aggressive ? WorkTaskAggressiveInterruptRange : WorkTaskDefensiveInterruptRange;
            return DistanceXZ(transform.position, target.transform.position) <= workThreatLimit;
        }
        if (IsBeyondHomeHardLeash(target.transform.position))
            return false;

        if (_agent.Context.TaskMode == AgentTaskMode.Hunt && _huntState == AgentHuntState.Fighting && target == _huntTarget)
            return DistanceXZ(transform.position, target.transform.position) <= HuntMaxChaseDistance;

        if (_agent.Context.BehaviourMode == AgentBehaviourMode.Passive)
            return false;

        if (_agent.Context.StateMode == AgentStateMode.StayHome && _agent.Context.HomeZone != null && _agent.Context.HomeZone.IsSet)
            return DistanceXZ(target.transform.position, _agent.Context.HomeZone.Center) <= GetHomeCombatLeashRadius();

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

        PrepareCombatEquipment(target);

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

    private void PrepareCombatEquipment(Character target)
    {
        if (_humanoid == null)
            _humanoid = GetComponent<Humanoid>();
        if (_humanoid == null)
            return;
        ItemDrop.ItemData current = _humanoid.GetCurrentWeapon();
        if (IsReservedAgentTool(current))
            TryUnequipItem(current);
        TryEquipBestCombatWeaponAndShield(target);
    }

    private void TryEquipBestCombatWeapon(Character target)
    {
        TryEquipBestCombatWeaponAndShield(target);
    }

    private void TryEquipBestCombatWeaponAndShield(Character target)
    {
        if (_humanoid == null)
            return;
        Inventory inventory = _humanoid.GetInventory();
        if (inventory == null)
            return;
        ItemDrop.ItemData currentWeapon = _humanoid.GetCurrentWeapon();
        if (IsReservedAgentTool(currentWeapon))
        {
            TryUnequipItem(currentWeapon);
            currentWeapon = null;
        }
        ItemDrop.ItemData bestWeapon = FindBestCombatWeapon(inventory);
        if (bestWeapon != null && bestWeapon != currentWeapon)
        {
            TryEquipItem(bestWeapon);
            currentWeapon = bestWeapon;
        }
        if (CanUseShieldWithWeapon(currentWeapon))
        {
            ItemDrop.ItemData shield = FindBestShield(inventory);
            if (shield != null)
                TryEquipItem(shield);
        }
    }

    private ItemDrop.ItemData FindBestCombatWeapon(Inventory inventory)
    {
        if (inventory == null)
            return null;
        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        ItemDrop.ItemData best = null;
        float bestScore = 0f;
        for (int i = 0; i < items.Count; i++)
        {
            ItemDrop.ItemData item = items[i];
            if (item == null || item.m_shared == null)
                continue;
            if (IsReservedAgentTool(item) || IsShield(item))
                continue;
            float score = GetCombatDamageScore(item);
            if (score <= 0f)
                continue;
            if (score > bestScore)
            {
                bestScore = score;
                best = item;
            }
        }
        return best;
    }

    private ItemDrop.ItemData FindBestShield(Inventory inventory)
    {
        if (inventory == null)
            return null;
        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        ItemDrop.ItemData best = null;
        float bestScore = 0f;
        for (int i = 0; i < items.Count; i++)
        {
            ItemDrop.ItemData item = items[i];
            if (item == null || item.m_shared == null)
                continue;
            if (!IsShield(item))
                continue;
            float score = item.m_shared.m_blockPower;
            if (score > bestScore)
            {
                bestScore = score;
                best = item;
            }
        }
        return best;
    }

    private bool CanUseShieldWithWeapon(ItemDrop.ItemData weapon)
    {
        if (weapon == null || weapon.m_shared == null)
            return true;
        string type = weapon.m_shared.m_itemType.ToString();
        if (Contains(type, "Bow") || Contains(type, "TwoHanded") || Contains(type, "Two handed") || Contains(type, "Tool"))
            return false;
        return true;
    }

    private bool IsShield(ItemDrop.ItemData item)
    {
        if (item == null || item.m_shared == null)
            return false;
        string type = item.m_shared.m_itemType.ToString();
        return Contains(type, "Shield");
    }

    private float GetCombatDamageScore(ItemDrop.ItemData item)
    {
        if (item == null || item.m_shared == null)
            return 0f;
        HitData.DamageTypes damage = item.GetDamage();
        return damage.m_blunt +
               damage.m_slash +
               damage.m_pierce +
               damage.m_fire +
               damage.m_frost +
               damage.m_lightning +
               damage.m_poison +
               damage.m_spirit;
    }

    private bool TryEquipItem(ItemDrop.ItemData item)
    {
        if (_humanoid == null || item == null)
            return false;
        try
        {
            return _humanoid.EquipItem(item, false);
        }
        catch
        {
            return false;
        }
    }

    private void TryUnequipItem(ItemDrop.ItemData item)
    {
        if (_humanoid == null || item == null)
            return;
        try
        {
            _humanoid.UnequipItem(item, false);
        }
        catch
        {
        }
    }

    private void ClearNativeAggro()
    {
        if (_monsterAI == null)
            return;
        SetObjectField(_monsterAI, "m_targetCreature", null);
        SetObjectField(_monsterAI, "m_targetStatic", null);
        SetBoolField(_monsterAI, "m_alerted", false);
        _lastAssignedNativeTarget = null;
        if (_character != null)
            _character.SetMoveDir(Vector3.zero);
        if (_humanoid != null)
        {
            _humanoid.SetRun(false);
            _humanoid.SetWalk(false);
        }
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
        Vector3 anchor = GetCurrentAnchorPosition();
        float searchRadius = GetDepositSearchRadius();
        Container best = null;
        float bestDistance = Mathf.Max(DepositChestSearchRadius, searchRadius);
        foreach (Container container in UnityEngine.Object.FindObjectsByType<Container>(FindObjectsSortMode.None))
        {
            if (!IsValidDepositContainer(container))
                continue;
            float anchorDistance = DistanceXZ(anchor, container.transform.position);
            float npcDistance = DistanceXZ(transform.position, container.transform.position);
            if (anchorDistance > searchRadius && npcDistance > DepositChestSearchRadius)
                continue;
            if (npcDistance < bestDistance)
            {
                bestDistance = npcDistance;
                best = container;
            }
        }
        return best;
    }

    private Container FindContainerNearPosition(Vector3 position)
    {
        Container best = null;
        float bestDistance = AssignedChestMatchDistance;

        foreach (Container container in UnityEngine.Object.FindObjectsByType<Container>(FindObjectsSortMode.None))
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

    private float GetDepositSearchRadius()
    {
        if (_agent != null && _agent.Context != null && _agent.Context.HomeZone != null && _agent.Context.HomeZone.IsSet)
            return Mathf.Max(DepositChestSearchRadius, _agent.Context.HomeZone.Radius + DepositNoAssignedChestSearchExtraRadius);
        return DepositChestSearchRadius;
    }

    private int GetDepositableItemCount(Inventory inventory)
    {
        if (inventory == null)
            return 0;
        int count = 0;
        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && !IsReservedAgentTool(items[i]))
                count++;
        }
        return count;
    }

    private bool IsInventorySlotPressureHigh(Inventory inventory)
    {
        if (inventory == null)
            return false;
        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        int depositable = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && !IsReservedAgentTool(items[i]))
                depositable++;
        }
        if (depositable <= 0)
            return false;
        if (items.Count >= 28)
            return true;
        return false;
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
        if (_agent == null || _agent.ValheimContainer == null || _agent.ValheimContainer.m_inventory == null)
            return false;
        List<ItemDrop.ItemData> items = _agent.ValheimContainer.m_inventory.GetAllItems();
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && !IsReservedAgentTool(items[i]))
                return true;
        }
        return false;
    }

    private float GetDepositableInventoryWeight(Inventory inventory)
    {
        if (inventory == null)
            return 0f;
        float weight = 0f;
        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        for (int i = 0; i < items.Count; i++)
        {
            ItemDrop.ItemData item = items[i];
            if (item != null && !IsReservedAgentTool(item))
                weight += item.GetWeight();
        }
        return weight;
    }
    private bool IsReservedAgentTool(ItemDrop.ItemData item)
    {
        if (item == null || item.m_shared == null)
            return false;
        string type = item.m_shared.m_itemType.ToString();
        if (Contains(type, "Tool"))
            return true;
        if (item.m_dropPrefab != null)
        {
            string prefab = item.m_dropPrefab.name;
            if (Contains(prefab, "Pickaxe") || Contains(prefab, "Axe") || Contains(prefab, "Hoe") || Contains(prefab, "Cultivator"))
                return true;
        }
        string sharedName = item.m_shared.m_name;
        if (Contains(sharedName, "pickaxe") || Contains(sharedName, "axe") || Contains(sharedName, "hoe") || Contains(sharedName, "cultivator"))
            return true;
        HitData.DamageTypes damage = item.GetDamage();
        return damage.m_pickaxe > 0f;
    }

    private static bool IsReservedAgentToolStatic(ItemDrop.ItemData item)
    {
        if (item == null || item.m_shared == null)
            return false;
        string type = item.m_shared.m_itemType.ToString();
        if (!string.IsNullOrEmpty(type) && type.IndexOf("Tool", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (item.m_dropPrefab != null)
        {
            string prefab = item.m_dropPrefab.name;
            if (!string.IsNullOrEmpty(prefab) &&
                (prefab.IndexOf("Pickaxe", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 prefab.IndexOf("Axe", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 prefab.IndexOf("Hoe", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 prefab.IndexOf("Cultivator", System.StringComparison.OrdinalIgnoreCase) >= 0))
                return true;
        }
        string sharedName = item.m_shared.m_name;
        if (!string.IsNullOrEmpty(sharedName) &&
            (sharedName.IndexOf("pickaxe", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             sharedName.IndexOf("axe", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             sharedName.IndexOf("hoe", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             sharedName.IndexOf("cultivator", System.StringComparison.OrdinalIgnoreCase) >= 0))
            return true;
        HitData.DamageTypes damage = item.GetDamage();
        return damage.m_pickaxe > 0f;
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
            if (IsReservedAgentTool(item))
                continue;
            ItemDrop.ItemData clone = item.Clone();
            if (clone == null || !target.AddItem(clone))
                continue;
            source.RemoveItem(item);
            moved++;
        }
        return moved;
    }

    private static void ClearBodyVelocityIfDynamic(Rigidbody body)
    {
        if (body == null || body.isKinematic)
            return;

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
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
    }

    private static bool Contains(string value, string term)
    {
        return !string.IsNullOrEmpty(value) &&
               !string.IsNullOrEmpty(term) &&
               value.IndexOf(term, System.StringComparison.OrdinalIgnoreCase) >= 0;
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

    private string FormatCharacter(Character character)
    {
        if (character == null)
            return "none";
        return character.gameObject.name + " dead=" + character.IsDead() + " pos=" + FormatVector(character.transform.position);
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

    private static void SetVector3Field(object instance, string name, Vector3 value)
    {
        FieldInfo field = FindField(instance?.GetType(), name);

        if (field == null || field.FieldType != typeof(Vector3))
            return;

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