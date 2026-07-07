using Core.Agent;
using HarmonyLib;
using Modules.HomeZone;
using System;
using UnityEngine;
using System.Collections.Generic;
using static Core.Agent.AgentContext;
using PungusSouls;

namespace Modules.Death
{
    public static class AgentDeathTombstoneController
    {
        private const string AgentSkillsKey = "agent_skills_v1";
        /*private static GameObject GetTombstonePrefab(AgentComponent agent)
        {
            AgentPrefabProfile profile = agent != null ? agent.GetComponent<AgentPrefabProfile>() : null;

            if (profile != null && !string.IsNullOrEmpty(profile.TombstonePrefabName))
            {
                GameObject custom = FindPrefab(profile.TombstonePrefabName);

                if (custom != null)
                    return custom;

                Debug.LogWarning("[AgentDeath] Custom tombstone prefab not found: " + profile.TombstonePrefabName);
            }

            if (Player.m_localPlayer != null && Player.m_localPlayer.m_tombstone != null)
                return Player.m_localPlayer.m_tombstone;

            GameObject fallback = FindPrefab("Player_tombstone");

            if (fallback != null)
                return fallback;

            return FindPrefab("TombStone");
        }*/

        private static GameObject FindPrefab(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return null;

            string cleanName = prefabName.Trim();

            if (ZNetScene.instance != null)
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(cleanName);

                if (prefab != null)
                    return prefab;

                if (ZNetScene.instance.m_prefabs != null)
                {
                    for (int i = 0; i < ZNetScene.instance.m_prefabs.Count; i++)
                    {
                        GameObject candidate = ZNetScene.instance.m_prefabs[i];

                        if (candidate == null)
                            continue;

                        if (string.Equals(candidate.name, cleanName, StringComparison.OrdinalIgnoreCase))
                            return candidate;
                    }
                }
            }

            return null;
        }

        public static void CreateTombstoneForAgent(AgentComponent agent)
        {
            if (agent == null)
                return;

            Humanoid humanoid = agent.GetComponent<Humanoid>();
            Character character = agent.GetComponent<Character>();
            ZNetView agentView = agent.GetComponent<ZNetView>();

            if (humanoid == null || agentView == null || !agentView.IsValid() || !agentView.IsOwner())
                return;



            GameObject tombstonePrefab = Player.m_localPlayer != null ? Player.m_localPlayer.m_tombstone : null;

            if (tombstonePrefab == null)
                tombstonePrefab = FindPrefab("Player_tombstone");

            if (tombstonePrefab == null)
            {
                Debug.LogError("[AgentDeath] Could not find player tombstone prefab");
                return;
            }


            Inventory sourceInventory = GetAgentInventory(agent, humanoid);
            string agentName = character != null && !string.IsNullOrEmpty(character.m_name) ? character.m_name : agent.gameObject.name.Replace("(Clone)", string.Empty).Trim();
            Vector3 deathPosition = humanoid.GetCenterPoint();
            long tombstoneId = DateTime.UtcNow.Ticks;
            string prefabName = ResolvePrefabName(agent, agentView);
            string ownerId = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID().ToString() : string.Empty;
            string skillsData = ReadAgentSkillsData(agentView);

            humanoid.UnequipAllItems();
            ClearEquippedFlags(sourceInventory);

            GameObject tombstone = UnityEngine.Object.Instantiate(tombstonePrefab, deathPosition, agent.transform.rotation);
            ApplyCustomTombstoneVisual(agent, tombstone);
            TombStone tombStone = tombstone.GetComponent<TombStone>();

            if (tombStone != null)
                tombStone.Setup(agentName, tombstoneId);

            Container tombContainer = tombstone.GetComponent<Container>();

            if (tombContainer != null && sourceInventory != null)
                MoveInventoryToTombstone(sourceInventory, tombContainer);
            
            ZNetView tombView = tombstone.GetComponent<ZNetView>();
            ZDO tombZdo = tombView != null && tombView.IsValid() ? tombView.GetZDO() : null;

            if (tombZdo == null)
                return;
            MarkAgentTombstone(agent, tombstone, tombZdo, agentName);
            int tombWidth = tombContainer != null && tombContainer.GetInventory() != null ? tombContainer.GetInventory().GetWidth() : 8;
            int tombHeight = tombContainer != null && tombContainer.GetInventory() != null ? tombContainer.GetInventory().GetHeight() : 6;
            bool hasHomeZone = agent.Context != null && agent.Context.HomeZone != null && agent.Context.HomeZone.IsSet;
            Vector3 homeCenter = hasHomeZone ? agent.Context.HomeZone.Center : Vector3.zero;
            float homeRadius = hasHomeZone ? agent.Context.HomeZone.Radius : 0f;
            HomeZoneMode legacyMode = agent.Context != null ? agent.Context.HomeZoneMode : HomeZoneMode.Passive;
            AgentBehaviourMode behaviourMode = agent.Context != null ? agent.Context.BehaviourMode : AgentBehaviourMode.Defensive;
            AgentStateMode stateMode = agent.Context != null ? agent.Context.StateMode : AgentStateMode.Idle;
            AgentTaskMode taskMode = agent.Context != null ? agent.Context.TaskMode : AgentTaskMode.None;
            Vector3 idleOrigin = agent.Context != null ? agent.Context.IdleOrigin : agent.transform.position;
            float idleRadius = agent.Context != null ? agent.Context.IdleRadius : 18f;
            float followStopDistance = agent.Context != null ? agent.Context.FollowStopDistance : 3f;
            float followResumeDistance = agent.Context != null ? agent.Context.FollowResumeDistance : 4f;

            AgentTombstoneResurrect.WriteRespawnData(
                tombZdo,
                prefabName,
                agentName,
                ownerId,
                hasHomeZone,
                homeCenter,
                homeRadius,
                legacyMode,
                behaviourMode,
                stateMode,
                taskMode,
                idleOrigin,
                idleRadius,
                followStopDistance,
                followResumeDistance,
                tombWidth,
                tombHeight,
                skillsData
            );

            if (tombstone.GetComponent<AgentTombstoneResurrect>() == null)
                tombstone.AddComponent<AgentTombstoneResurrect>();
        }

        private static void MarkAgentTombstone(AgentComponent agent, GameObject tombstone, ZDO tombZdo, string agentName)
        {
            if (agent == null || tombstone == null || tombZdo == null)
                return;

            tombZdo.Set("agent_tombstone", true);
            tombZdo.Set("agent_tombstone_name", string.IsNullOrEmpty(agentName) ? "NPC" : agentName);
            tombZdo.Set("agent_tombstone_pos", tombstone.transform.position);
        }
        private static void ApplyCustomTombstoneVisual(AgentComponent agent, GameObject tombstone)
        {
            if (agent == null || tombstone == null)
                return;

            AgentPrefabProfile profile = agent.GetComponent<AgentPrefabProfile>();

            if (profile == null || string.IsNullOrEmpty(profile.TombstonePrefabName))
                return;

            GameObject visualPrefab = FindPrefab(profile.TombstonePrefabName);

            if (visualPrefab == null)
            {
                Debug.LogWarning("[AgentDeath] Custom tombstone visual not found: " + profile.TombstonePrefabName);
                return;
            }

            Debug.Log("=== Tombstone Hierarchy ===");

            foreach (Transform child in tombstone.transform)
            {
                Debug.Log($"Tombstone Child: {child.name}");
            }

            Transform oldVisual = tombstone.transform.Find("visual");

            if (oldVisual == null)
                oldVisual = tombstone.transform.Find("Visual");

            foreach (Renderer renderer in
                tombstone.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }


            GameObject visual = UnityEngine.Object.Instantiate(visualPrefab, tombstone.transform);
            Debug.Log($"Visual Parent = {visual.transform.parent?.name}");
            visual.name = "custom_visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            foreach (Renderer renderer in
                tombstone.GetComponentsInChildren<Renderer>())
            {
                if (renderer.transform.IsChildOf(
                    visual.transform))
                {
                    continue;
                }

                renderer.enabled = false;
            }

        }
        private static Inventory GetAgentInventory(AgentComponent agent, Humanoid humanoid)
        {
            if (agent != null && agent.ValheimContainer != null)
            {
                Inventory inventory = agent.ValheimContainer.GetInventory();

                if (inventory != null)
                    return inventory;
            }

            return humanoid != null ? humanoid.GetInventory() : null;
        }

        private static void ClearEquippedFlags(Inventory inventory)
        {
            if (inventory == null)
                return;

            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item != null)
                    item.m_equipped = false;
            }
        }

        private static void MoveInventoryToTombstone(Inventory sourceInventory, Container tombContainer)
        {
            Inventory tombInventory = tombContainer.GetInventory();

            if (tombInventory == null)
                return;

            tombInventory.MoveInventoryToGrave(sourceInventory);
            CompactInventoryGrid(tombInventory);
            tombInventory.m_onChanged?.Invoke();
            sourceInventory.m_onChanged?.Invoke();
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

        private static string ReadAgentSkillsData(ZNetView agentView)
        {
            ZDO agentZdo = agentView != null && agentView.IsValid() ? agentView.GetZDO() : null;
            return agentZdo != null ? agentZdo.GetString(AgentSkillsKey, string.Empty) : string.Empty;
        }

        private static string ResolvePrefabName(AgentComponent agent, ZNetView agentView)
        {
            ZDO zdo = agentView != null && agentView.IsValid() ? agentView.GetZDO() : null;

            if (zdo != null && ZNetScene.instance != null)
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());

                if (prefab != null)
                    return prefab.name;
            }

            return agent != null ? agent.gameObject.name.Replace("(Clone)", string.Empty).Trim() : string.Empty;
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        public static class CustomPrefabPatch
        {
            private static void Postfix(ZNetScene __instance)
            {
                GameObject prefab =
                    PungusSoulsPlugin.assetBundle.LoadAsset<GameObject>(
                        "sif_tombstone");

                if (prefab == null)
                {
                    Debug.LogError(
                        "[Souls] Failed to load sif_tombstone");
                    return;
                }

                if (!__instance.m_prefabs.Contains(prefab))
                {
                    __instance.m_prefabs.Add(prefab);
                }

                Debug.Log(
                    "[Souls] Registered tombstone prefab: " +
                    prefab.name);

            }
        }

        [HarmonyPatch(typeof(TombStone), "UpdateDespawn")]
        public static class AgentTombstonePatch
        {
            static bool Prefix(
                TombStone __instance,
                ref Container ___m_container)
            {
                ZNetView nview =
                    __instance.GetComponent<ZNetView>();

                if (nview == null ||
                    !nview.IsValid())
                {
                    return true;
                }

                if (!nview.GetZDO().GetBool("agent_tombstone"))
                {
                    return true;
                }

                return false;
            }
        }


    }
}
