using System;
using System.Collections.Generic;
using UnityEngine;

namespace PungusSouls
{
    public class NpcGiveItemController : MonoBehaviour, Hoverable, Interactable
    {
        [Serializable]
        public class UseItemEntry
        {
            public ItemDrop m_prefab;
            public string m_dialog = "";
            public string m_setsGlobalKey = "";
            public bool m_removesItem = true;
        }

        public string m_name = "NPC";
        public string m_defaultDialog = "";
        public string m_randomUseItemAlreadyReceived = "I already received that.";
        public string m_randomGiveItemNo = "I do not need that.";
        public List<UseItemEntry> m_useItems = new List<UseItemEntry>();

        private void Awake()
        {

            if (m_useItems == null)
            {
                m_useItems = new List<UseItemEntry>();
            }


            if (m_useItems.Count == 0)
            {
                PopulateDefaultUseItems();
            }


            Collider[] colliders = GetComponentsInChildren<Collider>(true);
        }

        private void PopulateDefaultUseItems()
        {
            AddUseItem("DullEmber_item_tier1", "dullember", "Andre accepts the Dull Ember.");
            AddUseItem("LargeEmber_Item_tier2", "largeember", "Andre accepts the Large Ember.");
            AddUseItem("DivineEmber_Item_tier3", "divineember", "Andre accepts the Divine Ember.");
            AddUseItem("DarkEmber_Item_tier4", "darkember", "Andre accepts the Dark Ember.");
            AddUseItem("FlameEmber_Item_tier5", "flameember", "Andre accepts the Flame Ember.");
            AddUseItem("ChaosEmber_Item_tier6", "chaosember", "Andre accepts the Chaos Ember.");
            AddUseItem("CrystalEmber_Item_tier7", "crystalember", "Andre accepts the Crystal Ember.");
            AddUseItem("GiantEmber_Item_tier8", "giantember", "Andre accepts the Giant Ember.");
        }

        private void AddUseItem(string prefabName, string globalKey, string dialog)
        {
            if (ZNetScene.instance == null)
            {
                return;
            }

            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);

            if (prefab == null)
            {
                return;
            }

            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();

            if (itemDrop == null)
            {
                return;
            }

            m_useItems.Add(new UseItemEntry
            {
                m_prefab = itemDrop,
                m_dialog = dialog,
                m_setsGlobalKey = globalKey,
                m_removesItem = true
            });
        }

        public string GetHoverName()
        {

            return Localization.instance != null
                ? Localization.instance.Localize(m_name)
                : m_name;
        }

        public string GetHoverText()
        {

            string text =
                m_name +
                "\n[<color=yellow><b>$KEY_Use</b></color>] $raven_interact";

            if (m_useItems.Count > 0)
            {
                text += "\n[<color=yellow><b>1-8</b></color>] $npc_giveitem";
            }

            return Localization.instance != null
                ? Localization.instance.Localize(text)
                : text;
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {

            if (hold)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(m_defaultDialog))
            {
                Say(m_defaultDialog);
            }

            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            if (user == null || item == null || item.m_shared == null)
            {
                return false;
            }

            if (m_useItems == null || m_useItems.Count == 0)
            {
                Say(m_randomGiveItemNo);
                return true;
            }

            foreach (UseItemEntry useItem in m_useItems)
            {
                if (useItem == null || useItem.m_prefab == null)
                {
                    continue;
                }

                ItemDrop.ItemData requiredItem = useItem.m_prefab.m_itemData;

                if (requiredItem == null || requiredItem.m_shared == null)
                {
                    continue;
                }

                string givenPrefab = item.m_dropPrefab != null ? item.m_dropPrefab.name.Replace("(Clone)", "") : "";
                string requiredPrefab = useItem.m_prefab.gameObject.name.Replace("(Clone)", "");
                bool prefabMatch = !string.IsNullOrEmpty(givenPrefab) && givenPrefab == requiredPrefab;
                bool sharedNameMatch = item.m_shared.m_name == requiredItem.m_shared.m_name;

                if (!prefabMatch && !sharedNameMatch)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(useItem.m_setsGlobalKey) &&
                    ZoneSystem.instance != null &&
                    ZoneSystem.instance.GetGlobalKey(useItem.m_setsGlobalKey))
                {
                    Say(m_randomUseItemAlreadyReceived);
                    return true;
                }

                if (!string.IsNullOrEmpty(useItem.m_dialog))
                {
                    Say(useItem.m_dialog);
                }

                if (!string.IsNullOrEmpty(useItem.m_setsGlobalKey) &&
                    ZoneSystem.instance != null)
                {
                    ZoneSystem.instance.SetGlobalKey(useItem.m_setsGlobalKey);
                }

                if (useItem.m_removesItem)
                {
                    user.GetInventory().RemoveItem(item, 1);
                    user.ShowRemovedMessage(item, 1);
                }

                return true;
            }

            Say(m_randomGiveItemNo);
            return true;
        }

        private void Say(string text)
        {
            string localized =
                Localization.instance != null
                    ? Localization.instance.Localize(text)
                    : text;

            Player player = Player.m_localPlayer;

            if (player != null)
            {
                player.Message(MessageHud.MessageType.Center, localized);
            }
        }
    }
}