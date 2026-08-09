using Core.Agent;
using HarmonyLib;
using Modules.HomeZone;
using System.Collections.Generic;
using UnityEngine;
using static Core.Agent.AgentContext;

namespace Modules.Death
{
    public class AgentTombstoneResurrect : MonoBehaviour
    {
        internal static readonly int RespawnPrefabHash = "PS_Respawn_Prefab".GetStableHashCode();
        internal static readonly int RespawnNameHash = "PS_Respawn_Name".GetStableHashCode();
        internal static readonly int RespawnOwnerHash = "PS_Respawn_Owner".GetStableHashCode();
        internal static readonly int RespawnHomeZoneSetHash = "PS_Respawn_HomeZone_Set".GetStableHashCode();
        internal static readonly int RespawnHomeZoneCenterHash = "PS_Respawn_HomeZone_Center".GetStableHashCode();
        internal static readonly int RespawnHomeZoneRadiusHash = "PS_Respawn_HomeZone_Radius".GetStableHashCode();
        internal static readonly int RespawnHomeZoneModeHash = "PS_Respawn_HomeZone_Mode".GetStableHashCode();
        internal static readonly int RespawnBehaviourModeOutdatedHash = "PS_Respawn_Behaviour_Mode".GetStableHashCode();
        internal static readonly int RespawnBehaviourModeHash = "PS_Respawn_Behaviour_Mode".GetStableHashCode();
        internal static readonly int RespawnStateModeHash = "PS_Respawn_State_Mode".GetStableHashCode();
        internal static readonly int RespawnTaskModeHash = "PS_Respawn_Task_Mode".GetStableHashCode();
        internal static readonly int RespawnIdleOriginHash = "PS_Respawn_Idle_Origin".GetStableHashCode();
        internal static readonly int RespawnIdleRadiusHash = "PS_Respawn_Idle_Radius".GetStableHashCode();
        internal static readonly int RespawnFollowStopDistanceHash = "PS_Respawn_Follow_Stop_Distance".GetStableHashCode();
        internal static readonly int RespawnFollowResumeDistanceHash = "PS_Respawn_Follow_Resume_Distance".GetStableHashCode();
        internal static readonly int TombInvWidthHash = "PS_TombInvWidth".GetStableHashCode();
        internal static readonly int TombInvHeightHash = "PS_TombInvHeight".GetStableHashCode();
        internal static readonly int RespawnSkillsHash = "PS_Respawn_Skills".GetStableHashCode();

        internal const string AgentTombstoneKey = "agent_tombstone";
        private const string ResurrectCooldownUntilKey = "agent_resurrect_cooldown_until";
        private const string AgentSkillsKey = "agent_skills_v1";
        private const float ResurrectCooldownSeconds = 60f;

        private ZNetView _nview;
        internal static float LastResurrectTime { get; private set; }

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
        }

        internal bool TryInstantResurrect(Player player, out string message)
        {
            message = string.Empty;

            if (_nview == null)
                _nview = GetComponent<ZNetView>();

            if (_nview == null || !_nview.IsValid())
            {
                message = "Tombstone unavailable";
                return false;
            }

            ZDO zdo = _nview.GetZDO();

            if (zdo == null)
            {
                message = "Tombstone data unavailable";
                return false;
            }

            if (!IsNpcTombstone(zdo))
            {
                message = "Not an NPC tombstone";
                return false;
            }

            string owner = zdo.GetString(RespawnOwnerHash);

            if (string.IsNullOrEmpty(owner))
            {
                message = "This NPC has already been resurrected";
                return false;
            }

            string localOwner = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID().ToString() : string.Empty;

            if (!string.IsNullOrEmpty(localOwner) && owner != localOwner)
            {
                message = "You do not own this tombstone";
                return false;
            }

            if (!_nview.IsOwner())
                _nview.ClaimOwnership();

            float now = GetTimeSeconds();
            float cooldownUntil = zdo.GetFloat(ResurrectCooldownUntilKey, 0f);

            if (now < cooldownUntil)
            {
                message = "Resurrection cooldown: " + Mathf.CeilToInt(cooldownUntil - now) + "s";
                return false;
            }

            if (!DoResurrect())
            {
                message = "Could not resurrect NPC";
                return false;
            }

            zdo.Set(ResurrectCooldownUntilKey, now + ResurrectCooldownSeconds);
            LastResurrectTime = now;
            message = "NPC resurrected";
            return true;
        }

        internal string GetResurrectHoverText()
        {
            if (_nview == null)
                _nview = GetComponent<ZNetView>();

            ZDO zdo = _nview != null && _nview.IsValid() ? _nview.GetZDO() : null;

            if (zdo == null || !IsNpcTombstone(zdo))
                return null;

            string owner = zdo.GetString(RespawnOwnerHash);

            if (string.IsNullOrEmpty(owner))
                return null;

            string localOwner = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID().ToString() : string.Empty;

            if (!string.IsNullOrEmpty(localOwner) && owner != localOwner)
                return null;

            string name = zdo.GetString(RespawnNameHash);

            if (string.IsNullOrEmpty(name))
                name = "Companion";

            float now = GetTimeSeconds();
            float cooldownUntil = zdo.GetFloat(ResurrectCooldownUntilKey, 0f);

            if (now < cooldownUntil)
                return name + "\nResurrection cooldown: " + Mathf.CeilToInt(cooldownUntil - now) + "s";

            return name + "\n[<color=yellow><b>$KEY_Use</b></color>] Resurrect";
        }

        private bool DoResurrect()
        {
            ZDO tombZdo = _nview != null && _nview.IsValid() ? _nview.GetZDO() : null;

            if (tombZdo == null)
                return false;

            string prefabName = tombZdo.GetString(RespawnPrefabHash);
            string agentName = tombZdo.GetString(RespawnNameHash);
            string owner = tombZdo.GetString(RespawnOwnerHash);

            if (string.IsNullOrEmpty(prefabName) || string.IsNullOrEmpty(owner))
                return false;

            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabName) : null;

            if (prefab == null)
            {
                Debug.LogError("[AgentDeath] Respawn prefab not found: " + prefabName);
                return false;
            }

            Vector3 spawnPos = FindSpawnPosition(transform.position);
            Quaternion spawnRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject spawned = Instantiate(prefab, spawnPos, spawnRot);
            ZNetView spawnedView = spawned.GetComponent<ZNetView>();

            if (spawnedView == null || !spawnedView.IsValid() || spawnedView.GetZDO() == null)
            {
                Debug.LogError("[AgentDeath] Respawn failed, spawned agent has no ZDO");
                SafeDestroyObject(spawned);
                return false;
            }

            RestoreAgentState(spawned, tombZdo);
            RestoreAgentName(spawned, agentName);
            RestoreAgentSkills(spawnedView, tombZdo);
            TransferItemsToAgent(spawned);
            PlayResurrectEffects(spawnPos);

            MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, (string.IsNullOrEmpty(agentName) ? "Companion" : agentName) + " has been resurrected");

            if (TombstoneStillHasItems())
            {
                tombZdo.Set(RespawnOwnerHash, string.Empty);
                tombZdo.Set(RespawnPrefabHash, string.Empty);
                Destroy(this);
                return true;
            }

            SafeDestroyThisTombstone();
            return true;
        }

        private void SafeDestroyThisTombstone()
        {
            if (_nview == null)
                _nview = GetComponent<ZNetView>();

            if (_nview != null)
            {
                if (!_nview.IsValid())
                    return;

                if (!_nview.IsOwner())
                    _nview.ClaimOwnership();

                if (ZNetScene.instance != null)
                {
                    ZNetScene.instance.Destroy(gameObject);
                    return;
                }

                return;
            }

            Destroy(gameObject);
        }

        private static void SafeDestroyObject(GameObject target)
        {
            if (target == null)
                return;

            ZNetView view = target.GetComponent<ZNetView>();

            if (view != null)
            {
                if (!view.IsValid())
                    return;

                if (!view.IsOwner())
                    view.ClaimOwnership();

                if (ZNetScene.instance != null)
                {
                    ZNetScene.instance.Destroy(target);
                    return;
                }

                return;
            }

            Destroy(target);
        }

        private static Vector3 FindSpawnPosition(Vector3 origin)
        {
            Vector3 spawn = origin + Vector3.up * 0.5f;

            if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(spawn, out float height))
                spawn.y = height + 0.1f;

            return spawn;
        }

        private void RestoreAgentState(GameObject spawned, ZDO tombZdo)
        {
            AgentComponent agent = spawned.GetComponent<AgentComponent>();

            if (agent == null || agent.Context == null)
                return;

            bool hasHomeZone = tombZdo.GetBool(RespawnHomeZoneSetHash);
            Vector3 homeCenter = tombZdo.GetVec3(RespawnHomeZoneCenterHash, Vector3.zero);
            float homeRadius = tombZdo.GetFloat(RespawnHomeZoneRadiusHash, 0f);
            int legacyHomeMode = tombZdo.GetInt(RespawnHomeZoneModeHash, 0);
            int behaviourMode = tombZdo.GetInt(RespawnBehaviourModeHash, (int)AgentBehaviourMode.Defensive);
            int stateMode = tombZdo.GetInt(RespawnStateModeHash, (int)AgentStateMode.Idle);
            int taskMode = tombZdo.GetInt(RespawnTaskModeHash, (int)AgentTaskMode.None);
            Vector3 idleOrigin = tombZdo.GetVec3(RespawnIdleOriginHash, spawned.transform.position);
            float idleRadius = tombZdo.GetFloat(RespawnIdleRadiusHash, agent.Context.IdleRadius);
            float followStopDistance = tombZdo.GetFloat(RespawnFollowStopDistanceHash, agent.Context.FollowStopDistance);
            float followResumeDistance = tombZdo.GetFloat(RespawnFollowResumeDistanceHash, agent.Context.FollowResumeDistance);

            if (hasHomeZone && homeRadius > 0f)
            {
                agent.Context.HomeZone.SetHomeZone(homeCenter, homeRadius);
                agent.Context.HasHomeZone = true;
                agent.SaveHomeToZDO(homeCenter, homeRadius);
            }

            agent.Context.BehaviourMode = ClampEnum((AgentBehaviourMode)behaviourMode, AgentBehaviourMode.Defensive);
            agent.Context.StateMode = ClampEnum((AgentStateMode)stateMode, AgentStateMode.Idle);
            agent.Context.TaskMode = ClampEnum((AgentTaskMode)taskMode, AgentTaskMode.None);
            agent.Context.IdleOrigin = idleOrigin;
            agent.Context.IdleRadius = Mathf.Clamp(idleRadius, 2f, 200f);
            agent.Context.FollowStopDistance = Mathf.Clamp(followStopDistance, 1f, 20f);
            agent.Context.FollowResumeDistance = Mathf.Clamp(followResumeDistance, agent.Context.FollowStopDistance + 0.5f, 25f);
            agent.Context.HomeZoneMode = ClampEnum((HomeZoneMode)legacyHomeMode, HomeZoneMode.Passive);
            agent.Context.SyncLegacyFields();
            agent.SaveBehaviourStateToZDO();
        }

        private void RestoreAgentName(GameObject spawned, string agentName)
        {
            Character character = spawned.GetComponent<Character>();

            if (character != null && !string.IsNullOrEmpty(agentName))
                character.m_name = agentName;
        }

        private void RestoreAgentSkills(ZNetView spawnedView, ZDO tombZdo)
        {
            string skillsData = tombZdo.GetString(RespawnSkillsHash, string.Empty);

            if (string.IsNullOrEmpty(skillsData))
                return;

            ZDO spawnedZdo = spawnedView != null && spawnedView.IsValid() ? spawnedView.GetZDO() : null;

            if (spawnedZdo != null)
                spawnedZdo.Set(AgentSkillsKey, skillsData);
        }

        private void TransferItemsToAgent(GameObject spawned)
        {
            Container tombContainer = GetComponent<Container>();

            if (tombContainer == null)
                return;

            Inventory source = tombContainer.GetInventory();

            if (source == null)
                return;

            AgentComponent agent = spawned.GetComponent<AgentComponent>();
            Container targetContainer = agent != null && agent.ValheimContainer != null ? agent.ValheimContainer : spawned.GetComponent<Container>();

            if (targetContainer == null || targetContainer.GetInventory() == null)
                return;

            Inventory target = targetContainer.GetInventory();
            List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>(source.GetAllItems());

            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null)
                    continue;

                ItemDrop.ItemData clone = item.Clone();

                if (clone == null || !target.AddItem(clone))
                    continue;

                source.RemoveItem(item);
            }

            CompactInventoryGrid(target);
            target.m_onChanged?.Invoke();
            source.m_onChanged?.Invoke();
        }

        private bool TombstoneStillHasItems()
        {
            Container tombContainer = GetComponent<Container>();
            return tombContainer != null && tombContainer.GetInventory() != null && tombContainer.GetInventory().NrOfItems() > 0;
        }

        private static void CompactInventoryGrid(Inventory inventory)
        {
            if (inventory == null)
                return;

            int width = Mathf.Max(1, inventory.GetWidth());
            int index = 0;

            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item == null)
                    continue;

                item.m_gridPos = new Vector2i(index % width, index / width);
                index++;
            }
        }

        private void PlayResurrectEffects(Vector3 pos)
        {
            GameObject fx = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("vfx_offering") : null;

            if (fx != null)
                Instantiate(fx, pos + Vector3.up, Quaternion.identity);

            GameObject sfx = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("sfx_GP_activate") : null;

            if (sfx != null)
                Instantiate(sfx, pos, Quaternion.identity);
        }

        internal static void WriteRespawnData(
            ZDO tombZdo,
            string prefabName,
            string name,
            string ownerId,
            bool hasHomeZone,
            Vector3 homeZoneCenter,
            float homeZoneRadius,
            HomeZoneMode mode,
            int tombInventoryWidth,
            int tombInventoryHeight)
        {
            WriteRespawnData(tombZdo, prefabName, name, ownerId, hasHomeZone, homeZoneCenter, homeZoneRadius, mode, AgentBehaviourMode.Defensive, hasHomeZone ? AgentStateMode.StayHome : AgentStateMode.Idle, AgentTaskMode.None, Vector3.zero, 18f, 3f, 4f, tombInventoryWidth, tombInventoryHeight, string.Empty);
        }

        internal static void WriteRespawnData(
            ZDO tombZdo,
            string prefabName,
            string name,
            string ownerId,
            bool hasHomeZone,
            Vector3 homeZoneCenter,
            float homeZoneRadius,
            HomeZoneMode mode,
            AgentBehaviourMode behaviourMode,
            AgentStateMode stateMode,
            AgentTaskMode taskMode,
            Vector3 idleOrigin,
            float idleRadius,
            float followStopDistance,
            float followResumeDistance,
            int tombInventoryWidth,
            int tombInventoryHeight,
            string skillsData = "")
        {
            if (tombZdo == null)
                return;

            tombZdo.Set(AgentTombstoneKey, true);
            tombZdo.Set(RespawnPrefabHash, prefabName);
            tombZdo.Set(RespawnNameHash, name);
            tombZdo.Set(RespawnOwnerHash, ownerId);
            tombZdo.Set(RespawnHomeZoneSetHash, hasHomeZone);
            tombZdo.Set(RespawnHomeZoneCenterHash, homeZoneCenter);
            tombZdo.Set(RespawnHomeZoneRadiusHash, homeZoneRadius);
            tombZdo.Set(RespawnHomeZoneModeHash, (int)mode);
            tombZdo.Set(RespawnBehaviourModeHash, (int)behaviourMode);
            tombZdo.Set(RespawnStateModeHash, (int)stateMode);
            tombZdo.Set(RespawnTaskModeHash, (int)taskMode);
            tombZdo.Set(RespawnIdleOriginHash, idleOrigin);
            tombZdo.Set(RespawnIdleRadiusHash, idleRadius);
            tombZdo.Set(RespawnFollowStopDistanceHash, followStopDistance);
            tombZdo.Set(RespawnFollowResumeDistanceHash, followResumeDistance);
            tombZdo.Set(TombInvWidthHash, tombInventoryWidth);
            tombZdo.Set(TombInvHeightHash, tombInventoryHeight);
            tombZdo.Set(RespawnSkillsHash, skillsData ?? string.Empty);
        }

        private static T ClampEnum<T>(T value, T fallback) where T : struct
        {
            return System.Enum.IsDefined(typeof(T), value) ? value : fallback;
        }

        private static bool IsNpcTombstone(ZDO zdo)
        {
            return zdo != null && zdo.GetBool(AgentTombstoneKey, false);
        }

        private static float GetTimeSeconds()
        {
            if (ZNet.instance == null)
                return Time.time;

            System.Reflection.MethodInfo method = ZNet.instance.GetType().GetMethod("GetTimeSeconds", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            if (method == null || method.GetParameters().Length != 0)
                return Time.time;

            object value = method.Invoke(ZNet.instance, null);

            if (value is float f)
                return f;

            if (value is double d)
                return (float)d;

            if (value is int i)
                return i;

            return Time.time;
        }
    }

    [HarmonyPatch]
    public static class TombstoneInstantResurrectPatch
    {
        [HarmonyPatch(typeof(TombStone), "Awake")]
        [HarmonyPostfix]
        private static void TombStoneAwakePostfix(TombStone __instance)
        {
            EnsureAgentTombstoneComponent(__instance);
        }

        [HarmonyPatch(typeof(TombStone), "Interact")]
        [HarmonyPrefix]
        private static bool TombStoneInteractPrefix(TombStone __instance, Humanoid character, bool hold, bool alt)
        {
            if (__instance == null || character == null)
                return true;

            AgentTombstoneResurrect resurrect = EnsureAgentTombstoneComponent(__instance);

            if (resurrect == null)
                return true;

            if (hold)
                return false;

            if (alt)
                return true;

            Player player = character as Player ?? Player.m_localPlayer;

            if (resurrect.TryInstantResurrect(player, out string message))
                return false;

            if (!string.IsNullOrEmpty(message))
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, message);

            return false;
        }

        [HarmonyPatch(typeof(TombStone), "GetHoverText")]
        [HarmonyPostfix]
        private static void TombStoneHoverTextPostfix(TombStone __instance, ref string __result)
        {
            if (__instance == null)
                return;

            AgentTombstoneResurrect resurrect = EnsureAgentTombstoneComponent(__instance);

            if (resurrect == null)
                return;

            string hover = resurrect.GetResurrectHoverText();

            if (!string.IsNullOrEmpty(hover))
                __result = hover;
        }

        private static AgentTombstoneResurrect EnsureAgentTombstoneComponent(TombStone tombstone)
        {
            if (tombstone == null)
                return null;

            AgentTombstoneResurrect resurrect = tombstone.GetComponent<AgentTombstoneResurrect>();

            if (resurrect != null)
                return resurrect;

            ZNetView nview = tombstone.GetComponent<ZNetView>();
            ZDO zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;

            if (zdo == null || !zdo.GetBool(AgentTombstoneResurrect.AgentTombstoneKey, false))
                return null;

            return tombstone.gameObject.AddComponent<AgentTombstoneResurrect>();
        }
    }
}
