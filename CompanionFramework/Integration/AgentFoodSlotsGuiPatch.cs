using HarmonyLib;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Integration
{
    [HarmonyPatch(typeof(InventoryGui), "Awake")]
    public static class AgentFoodSlotsGuiPatch_Awake
    {
        public static GameObject Root;
        public static TextMeshProUGUI TitleText;
        public static TextMeshProUGUI HintText;
        public static readonly List<Button> SlotButtons = new List<Button>();
        public static readonly List<TextMeshProUGUI> SlotTexts = new List<TextMeshProUGUI>();

        private static void Postfix(InventoryGui __instance)
        {
            EnsurePanel(__instance);
        }

        public static void EnsurePanel(InventoryGui gui)
        {
            if (Root != null)
                return;

            if (gui == null)
                return;

            Transform parent = FindContainerUiParent(gui);

            if (parent == null)
                return;

            Integration.AgentTMPFont.EnsureDefault();

            Root = new GameObject("AgentFoodSlotsPanel", typeof(RectTransform), typeof(Image));
            Root.transform.SetParent(parent, false);

            RectTransform rootRect = Root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0.5f);
            rootRect.anchorMax = new Vector2(1f, 0.5f);
            rootRect.pivot = new Vector2(0f, 0.5f);
            rootRect.sizeDelta = new Vector2(270f, 146f);
            rootRect.anchoredPosition = new Vector2(16f, -64f);

            Image rootImage = Root.GetComponent<Image>();
            rootImage.color = new Color(0.12f, 0.09f, 0.07f, 0.92f);

            AddBorder(Root.transform);

            TitleText = CreateText(Root.transform, "Title", "Food slots", 16f, TextAlignmentOptions.Center, new Color(1f, 0.68f, 0.28f, 1f));
            RectTransform titleRect = TitleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-18f, 26f);
            titleRect.anchoredPosition = new Vector2(0f, -8f);

            for (int i = 0; i < AgentFoodSlotRules.SlotCount; i++)
            {
                int slot = i;
                Button button = CreateSlotButton(Root.transform, slot);
                button.onClick.AddListener(() => ToggleSlot(slot));
            }

            HintText = CreateText(Root.transform, "Hint", "Click empty slot to move food here", 12f, TextAlignmentOptions.Center, new Color(0.72f, 0.67f, 0.54f, 1f));
            RectTransform hintRect = HintText.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.sizeDelta = new Vector2(-18f, 22f);
            hintRect.anchoredPosition = new Vector2(0f, 8f);

            Root.SetActive(false);
        }

        public static void MovePanelToContainerPanel(InventoryGui gui)
        {
            if (gui == null || Root == null)
                return;

            Transform parent = FindContainerUiParent(gui);

            if (parent == null)
                return;

            if (Root.transform.parent != parent)
                Root.transform.SetParent(parent, false);

            RectTransform rootRect = Root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0.5f);
            rootRect.anchorMax = new Vector2(1f, 0.5f);
            rootRect.pivot = new Vector2(0f, 0.5f);
            rootRect.sizeDelta = new Vector2(270f, 146f);
            rootRect.anchoredPosition = new Vector2(16f, -64f);
        }

        public static void SetVisible(bool visible)
        {
            if (Root != null)
                Root.SetActive(visible);
        }

        public static void UpdatePanel(AgentComponent agent)
        {
            Inventory inventory = GetAgentInventory(agent);

            if (inventory == null)
                return;

            for (int i = 0; i < SlotTexts.Count && i < AgentFoodSlotRules.SlotCount; i++)
            {
                ItemDrop.ItemData item = AgentFoodSlotRules.GetFoodInSlot(inventory, i);

                if (item == null)
                {
                    SlotTexts[i].text = "Slot " + (i + 1) + "\nEmpty";
                    SlotTexts[i].color = new Color(0.8f, 0.74f, 0.58f, 1f);
                    continue;
                }

                bool isFood = AgentFoodSlotRules.IsFoodItem(item);
                string name = CleanItemName(item.m_shared != null ? item.m_shared.m_name : "Food");
                string stack = item.m_stack > 1 ? " x" + item.m_stack : string.Empty;
                SlotTexts[i].text = "Slot " + (i + 1) + "\n" + name + stack;
                SlotTexts[i].color = isFood ? new Color(1f, 0.95f, 0.78f, 1f) : new Color(1f, 0.35f, 0.25f, 1f);
            }
        }

        private static Button CreateSlotButton(Transform parent, int slot)
        {
            GameObject go = new GameObject("FoodSlot" + slot, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(78f, 54f);
            rect.anchoredPosition = new Vector2(18f + slot * 82f, -42f);

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.08f, 0.06f, 0.04f, 0.98f);

            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.08f, 0.06f, 0.04f, 0.98f);
            colors.highlightedColor = new Color(0.28f, 0.18f, 0.08f, 0.98f);
            colors.pressedColor = new Color(0.45f, 0.28f, 0.1f, 0.98f);
            button.colors = colors;

            TextMeshProUGUI label = CreateText(go.transform, "Text", "Slot " + (slot + 1) + "\nEmpty", 12f, TextAlignmentOptions.Center, new Color(0.8f, 0.74f, 0.58f, 1f));
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4f, 2f);
            labelRect.offsetMax = new Vector2(-4f, -2f);

            SlotButtons.Add(button);
            SlotTexts.Add(label);
            return button;
        }

        private static void ToggleSlot(int slot)
        {
            AgentComponent agent = FindOpenAgent(InventoryGui.instance);
            Inventory inventory = GetAgentInventory(agent);

            if (inventory == null)
                return;

            ItemDrop.ItemData existing = AgentFoodSlotRules.GetFoodInSlot(inventory, slot);
            bool changed;

            if (existing != null)
            {
                changed = AgentFoodSlotRules.TryMoveSlotItemOut(inventory, slot);

                if (!changed)
                    MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, "No free inventory space");
            }
            else
            {
                changed = AgentFoodSlotRules.TryMoveFoodIntoSlot(inventory, slot);

                if (!changed)
                    MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, "No food found in NPC inventory");
            }

            if (changed)
                UpdatePanel(agent);
        }

        private static Inventory GetAgentInventory(AgentComponent agent)
        {
            if (agent == null || agent.ValheimContainer == null)
                return null;

            return agent.ValheimContainer.m_inventory;
        }

        public static AgentComponent FindOpenAgent(InventoryGui gui)
        {
            if (!InventoryGui.IsVisible())
                return null;

            if (gui == null || gui.m_currentContainer == null)
                return null;

            Container container = gui.m_currentContainer;
            AgentComponent direct = container.GetComponent<AgentComponent>();

            if (direct != null)
                return direct;

            direct = container.GetComponentInParent<AgentComponent>();

            if (direct != null)
                return direct;

            foreach (AgentComponent agent in Object.FindObjectsOfType<AgentComponent>())
            {
                if (agent != null && agent.ValheimContainer == container)
                    return agent;
            }

            return null;
        }

        private static string CleanItemName(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return "Food";

            string value = itemName;

            if (value.StartsWith("$item_"))
                value = value.Substring(6);
            else if (value.StartsWith("$"))
                value = value.Substring(1);

            value = value.Replace("_", " ");

            if (value.Length == 0)
                return "Food";

            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = Integration.AgentTMPFont.AddText(go);
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return tmp;
        }

        private static void AddBorder(Transform parent)
        {
            CreateBorder(parent, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 2f), new Vector2(0f, -1f));
            CreateBorder(parent, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 2f), new Vector2(0f, 1f));
            CreateBorder(parent, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(2f, 0f), new Vector2(1f, 0f));
            CreateBorder(parent, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(2f, 0f), new Vector2(-1f, 0f));
        }

        private static void CreateBorder(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            GameObject go = new GameObject("Border " + name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.74f, 0.45f, 0.16f, 0.55f);
            image.raycastTarget = false;
        }

        private static Transform FindContainerUiParent(InventoryGui gui)
        {
            Transform direct = GetTransformField(gui, "m_container");

            if (direct != null)
                return direct;

            Transform containerGrid = GetTransformField(gui, "m_containerGrid");

            if (containerGrid != null && containerGrid.parent != null)
                return containerGrid.parent;

            Transform containerName = GetTransformField(gui, "m_containerName");

            if (containerName != null && containerName.parent != null)
                return containerName.parent;

            Transform root = GetTransformField(gui, "m_containerRoot");

            if (root != null)
                return root;

            return null;
        }

        private static Transform GetTransformField(object instance, string fieldName)
        {
            if (instance == null)
                return null;

            System.Reflection.FieldInfo field = AccessTools.Field(instance.GetType(), fieldName);

            if (field == null)
                return null;

            object value = field.GetValue(instance);

            if (value is Transform transform)
                return transform;

            if (value is GameObject gameObject)
                return gameObject.transform;

            if (value is Component component)
                return component.transform;

            return null;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "Update")]
    public static class AgentFoodSlotsGuiPatch_Update
    {
        private static float _refreshTimer;

        private static void Postfix(InventoryGui __instance)
        {
            AgentFoodSlotsGuiPatch_Awake.EnsurePanel(__instance);

            AgentComponent agent = AgentFoodSlotsGuiPatch_Awake.FindOpenAgent(__instance);
            bool show = agent != null;
            AgentFoodSlotsGuiPatch_Awake.SetVisible(show);

            if (!show)
                return;

            AgentFoodSlotsGuiPatch_Awake.MovePanelToContainerPanel(__instance);

            _refreshTimer -= Time.deltaTime;

            if (_refreshTimer <= 0f)
            {
                _refreshTimer = 0.25f;
                AgentFoodSlotsGuiPatch_Awake.UpdatePanel(agent);
            }
        }
    }
}
