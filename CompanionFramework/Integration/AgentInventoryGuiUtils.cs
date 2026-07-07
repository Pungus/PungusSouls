using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Integration
{

    public static class AgentFriendlyNameUtility
    {
        public static string GetDisplayName(AgentComponent agent)
        {
            if (agent == null)
                return "NPC";

            Character character = agent.GetComponent<Character>();

            if (character != null && !string.IsNullOrEmpty(character.m_name))
                return Clean(character.m_name);

            AgentPrefabProfile profile = agent.GetComponent<AgentPrefabProfile>();

            if (profile != null && !string.IsNullOrEmpty(profile.AgentId))
                return Clean(profile.AgentId);

            return Clean(agent.gameObject.name);
        }

        private static string Clean(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "NPC";

            value = value.Replace("(Clone)", string.Empty).Trim();

            if (value.StartsWith("$ps_", System.StringComparison.OrdinalIgnoreCase))
                value = value.Substring(4);

            if (value.StartsWith("PS_", System.StringComparison.OrdinalIgnoreCase))
                value = value.Substring(3);

            value = value.Replace("_", " ").Trim();

            return string.IsNullOrEmpty(value) ? "NPC" : value;
        }
    }

    internal static class AgentInventoryGuiShared
    {
        public static Transform FindContainerUiParent(InventoryGui gui)
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

        public static Transform GetTransformField(object instance, string fieldName)
        {
            if (instance == null)
                return null;

            FieldInfo field = AccessTools.Field(instance.GetType(), fieldName);
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

        public static AgentComponent GetAgentFromContainer(Container container)
        {
            return AgentInventoryRegistry.GetAgent(container);
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = AgentTMPFont.AddText(go);
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        public static void AddBorder(Transform parent)
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

        public static TMP_FontAsset FindFont(InventoryGui gui)
        {
            if (gui != null)
            {
                TMP_Text[] texts = gui.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text text in texts)
                {
                    if (text != null && text.font != null && !IsBrokenTmpFont(text.font))
                        return text.font;
                }
            }

            TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (TMP_FontAsset font in fonts)
            {
                if (font != null && !IsBrokenTmpFont(font))
                    return font;
            }

            return TMP_Settings.defaultFontAsset;
        }

        public static bool IsBrokenTmpFont(TMP_FontAsset font)
        {
            if (font == null)
                return true;

            return font.name.IndexOf("LiberationSans", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string CleanItemName(string itemName)
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
    }

    [HarmonyPatch(typeof(InventoryGui), "Awake")]
    public static class AgentDepositChestGuiPatch_Awake
    {
        public static GameObject Root;
        public static TMP_Dropdown NpcDropdown;
        public static Button AssignButton;
        public static TextMeshProUGUI TitleText;
        public static TextMeshProUGUI AssignText;
        public static readonly List<AgentComponent> DropdownAgents = new List<AgentComponent>();

        private const int MaxDepositChests = 8;
        private const float DuplicateChestDistance = 2f;
        private const float NpcSearchDistance = 80f;
        private static TMP_FontAsset _font;

        private static void Postfix(InventoryGui __instance)
        {
            EnsureControls(__instance);
        }

        public static AgentComponent GetAgentFromCurrentContainer(InventoryGui gui)
        {
            if (gui == null || gui.m_currentContainer == null)
                return null;

            return AgentInventoryGuiShared.GetAgentFromContainer(gui.m_currentContainer);
        }

        public static void EnsureControls(InventoryGui gui)
        {
            if (Root != null || gui == null)
                return;

            Transform parent = AgentInventoryGuiShared.FindContainerUiParent(gui);
            if (parent == null)
                return;

            _font = AgentInventoryGuiShared.FindFont(gui);
            Root = new GameObject("AgentDepositPopout", typeof(RectTransform), typeof(Image));
            Root.transform.SetParent(parent, false);

            RectTransform rootRect = Root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0.5f);
            rootRect.anchorMax = new Vector2(1f, 0.5f);
            rootRect.pivot = new Vector2(0f, 0.5f);
            rootRect.sizeDelta = new Vector2(255f, 138f);
            rootRect.anchoredPosition = new Vector2(16f, -8f);

            Image rootImage = Root.GetComponent<Image>();
            rootImage.color = new Color(0.12f, 0.09f, 0.07f, 0.92f);
            AgentInventoryGuiShared.AddBorder(Root.transform);

            TitleText = CreateText(Root.transform, "Title", "NPC deposit", 16f, TextAlignmentOptions.Center, new Color(1f, 0.68f, 0.28f, 1f));
            RectTransform titleRect = TitleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-18f, 26f);
            titleRect.anchoredPosition = new Vector2(0f, -8f);

            NpcDropdown = CreateDropdown(Root.transform);
            AssignButton = CreateButton(Root.transform, "AssignButton", "Add chest to selected NPC", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(224f, 34f), new Vector2(0f, 16f), out AssignText);
            AssignButton.onClick.AddListener(AddCurrentChestToSelectedNpc);
            SetControlsActive(false);
        }

        public static void MoveControlsToContainerPanel(InventoryGui gui)
        {
            if (gui == null || Root == null)
                return;

            Transform parent = AgentInventoryGuiShared.FindContainerUiParent(gui);
            if (parent == null)
                return;

            if (Root.transform.parent != parent)
                Root.transform.SetParent(parent, false);

            RectTransform rootRect = Root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0.5f);
            rootRect.anchorMax = new Vector2(1f, 0.5f);
            rootRect.pivot = new Vector2(0f, 0.5f);
            rootRect.sizeDelta = new Vector2(255f, 138f);
            rootRect.anchoredPosition = new Vector2(16f, -8f);
        }

        public static void RefreshNpcDropdown(Container currentContainer)
        {
            if (NpcDropdown == null)
                return;

            int oldValue = NpcDropdown.value;
            DropdownAgents.Clear();
            NpcDropdown.ClearOptions();
            List<string> options = new List<string>();

            AgentComponent[] agents = UnityEngine.Object.FindObjectsOfType<AgentComponent>();
            foreach (AgentComponent agent in agents)
            {
                if (agent == null || agent.Context == null)
                    continue;

                if (agent.Context.HomeZone == null || !agent.Context.HomeZone.IsSet)
                    continue;

                if (currentContainer != null)
                {
                    float distanceToChest = Vector3.Distance(agent.transform.position, currentContainer.transform.position);
                    float distanceToHome = Vector3.Distance(agent.Context.HomeZone.Center, currentContainer.transform.position);

                    if (Mathf.Min(distanceToChest, distanceToHome) > NpcSearchDistance)
                        continue;
                }

                DropdownAgents.Add(agent);
                options.Add(AgentFriendlyNameUtility.GetDisplayName(agent));
            }

            if (options.Count == 0)
            {
                options.Add("No NPC with home nearby");
                NpcDropdown.AddOptions(options);
                NpcDropdown.value = 0;
                NpcDropdown.interactable = false;

                if (AssignButton != null)
                    AssignButton.interactable = false;

                NpcDropdown.RefreshShownValue();
                return;
            }

            NpcDropdown.AddOptions(options);
            NpcDropdown.value = Mathf.Clamp(oldValue, 0, options.Count - 1);
            NpcDropdown.interactable = true;

            if (AssignButton != null)
                AssignButton.interactable = true;

            NpcDropdown.RefreshShownValue();
        }

        public static void SetControlsActive(bool active)
        {
            if (Root != null)
                Root.SetActive(active);
        }

        private static string BuildAgentLabel(AgentComponent agent)
        {
            if (agent == null || agent.gameObject == null)
                return "NPC";

            string name = agent.gameObject.name.Replace("(Clone)", string.Empty).Trim();
            return string.IsNullOrEmpty(name) ? "NPC" : name;
        }

        private static TMP_Dropdown CreateDropdown(Transform parent)
        {
            GameObject root = new GameObject("NpcDropdown", typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.sizeDelta = new Vector2(224f, 34f);
            rootRect.anchoredPosition = new Vector2(0f, -42f);

            Image rootImage = root.GetComponent<Image>();
            rootImage.color = new Color(0.08f, 0.06f, 0.04f, 0.98f);

            TMP_Dropdown dropdown = root.GetComponent<TMP_Dropdown>();
            dropdown.targetGraphic = rootImage;

            TextMeshProUGUI caption = CreateText(root.transform, "Label", "Select NPC", 15f, TextAlignmentOptions.Left, new Color(1f, 0.95f, 0.78f, 1f));
            RectTransform captionRect = caption.GetComponent<RectTransform>();
            captionRect.anchorMin = Vector2.zero;
            captionRect.anchorMax = Vector2.one;
            captionRect.offsetMin = new Vector2(10f, 0f);
            captionRect.offsetMax = new Vector2(-28f, 0f);
            dropdown.captionText = caption;

            TextMeshProUGUI arrow = CreateText(root.transform, "Arrow", "▼", 12f, TextAlignmentOptions.Center, new Color(1f, 0.68f, 0.28f, 1f));
            RectTransform arrowRect = arrow.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1f, 0f);
            arrowRect.anchorMax = new Vector2(1f, 1f);
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.sizeDelta = new Vector2(26f, 0f);
            arrowRect.anchoredPosition = Vector2.zero;

            GameObject template = CreateDropdownTemplate(root.transform);
            dropdown.template = template.GetComponent<RectTransform>();
            template.SetActive(false);
            dropdown.options.Clear();
            dropdown.options.Add(new TMP_Dropdown.OptionData("No NPC with home nearby"));
            dropdown.value = 0;
            dropdown.RefreshShownValue();
            return dropdown;
        }

        private static GameObject CreateDropdownTemplate(Transform parent)
        {
            GameObject template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            template.transform.SetParent(parent, false);

            RectTransform templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.sizeDelta = new Vector2(0f, 136f);
            templateRect.anchoredPosition = new Vector2(0f, -4f);

            Image templateImage = template.GetComponent<Image>();
            templateImage.color = new Color(0.08f, 0.06f, 0.04f, 0.98f);

            ScrollRect scrollRect = template.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(template.transform, false);

            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = Color.clear;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 34f);
            contentRect.anchoredPosition = Vector2.zero;

            GameObject item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(content.transform, false);

            RectTransform itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(1f, 1f);
            itemRect.pivot = new Vector2(0.5f, 1f);
            itemRect.sizeDelta = new Vector2(0f, 34f);

            Toggle toggle = item.GetComponent<Toggle>();

            GameObject bg = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(item.transform, false);

            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImage = bg.GetComponent<Image>();
            bgImage.color = new Color(0.08f, 0.06f, 0.04f, 0.98f);
            toggle.targetGraphic = bgImage;

            GameObject check = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(item.transform, false);

            RectTransform checkRect = check.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0f, 0f);
            checkRect.anchorMax = new Vector2(0f, 1f);
            checkRect.pivot = new Vector2(0f, 0.5f);
            checkRect.sizeDelta = new Vector2(5f, 0f);
            checkRect.anchoredPosition = Vector2.zero;

            Image checkImage = check.GetComponent<Image>();
            checkImage.color = new Color(1f, 0.65f, 0.2f, 0.95f);
            toggle.graphic = checkImage;

            TextMeshProUGUI itemLabel = CreateText(item.transform, "Item Label", "Option", 15f, TextAlignmentOptions.Left, new Color(1f, 0.95f, 0.78f, 1f));
            RectTransform itemLabelRect = itemLabel.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(12f, 0f);
            itemLabelRect.offsetMax = new Vector2(-8f, 0f);

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            return template;
        }

        private static Button CreateButton(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition, out TextMeshProUGUI label)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.18f, 0.12f, 0.06f, 0.96f);

            Button button = go.GetComponent<Button>();
            label = CreateText(go.transform, "Text", text, 15f, TextAlignmentOptions.Center, new Color(1f, 0.78f, 0.35f, 1f));

            RectTransform textRect = label.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment, Color color)
        {
            TextMeshProUGUI tmp = AgentInventoryGuiShared.CreateText(parent, name, text, size, alignment, color);

            if (_font != null)
                tmp.font = _font;

            return tmp;
        }

        private static void AddCurrentChestToSelectedNpc()
        {
            InventoryGui gui = InventoryGui.instance;

            if (gui == null || gui.m_currentContainer == null)
                return;

            if (DropdownAgents.Count == 0 || NpcDropdown == null || NpcDropdown.value < 0 || NpcDropdown.value >= DropdownAgents.Count)
            {
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, "Select an NPC first");
                return;
            }

            Container container = gui.m_currentContainer;
            AgentComponent selectedAgent = DropdownAgents[NpcDropdown.value];
            string result = AddDepositChest(selectedAgent, container);
            MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, result);
        }

        private static string AddDepositChest(AgentComponent agent, Container container)
        {
            if (agent == null || container == null)
                return "Deposit chest assignment failed";

            if (AgentInventoryGuiShared.GetAgentFromContainer(container) != null)
                return "Cannot assign NPC inventory as deposit chest";

            if (container.m_inventory == null)
                return "Container inventory unavailable";

            ZNetView agentView = agent.GetComponent<ZNetView>();

            if (agentView != null && agentView.IsValid() && !agentView.IsOwner())
                agentView.ClaimOwnership();

            if (agentView == null || !agentView.IsValid())
                return "NPC ZNetView unavailable";

            ZDO zdo = agentView.GetZDO();

            if (zdo == null)
                return "NPC ZDO unavailable";

            Vector3 chestPosition = container.transform.position;
            int count = Mathf.Max(0, zdo.GetInt("agent_deposit_chest_count", 0));

            for (int i = 0; i < count; i++)
            {
                Vector3 existingPosition = zdo.GetVec3("agent_deposit_chest_" + i + "_pos", Vector3.zero);

                if (Vector3.Distance(existingPosition, chestPosition) <= DuplicateChestDistance)
                    return "Chest already assigned to selected NPC";
            }

            if (count >= MaxDepositChests)
                return "Selected NPC chest list full";

            zdo.Set("agent_deposit_chest_count", count + 1);
            zdo.Set("agent_deposit_chest_" + count + "_pos", chestPosition);
            zdo.Set("agent_deposit_chest_" + count + "_name", container.gameObject.name);
            zdo.Set("agent_has_deposit_chest", true);
            zdo.Set("agent_deposit_chest_pos", chestPosition);
            zdo.Set("agent_deposit_chest_name", container.gameObject.name);

            return "Deposit chest added to " + AgentFriendlyNameUtility.GetDisplayName(agent);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "Update")]
    public static class AgentDepositChestGuiPatch_Update
    {
        private static float _refreshTimer;
        private static Container _lastContainer;

        private static void Postfix(InventoryGui __instance)
        {
            AgentDepositChestGuiPatch_Awake.EnsureControls(__instance);
            bool show = ShouldShowControls(__instance);
            AgentDepositChestGuiPatch_Awake.SetControlsActive(show);

            if (!show)
            {
                _lastContainer = null;
                return;
            }

            AgentDepositChestGuiPatch_Awake.MoveControlsToContainerPanel(__instance);
            _refreshTimer -= Time.deltaTime;

            if (_lastContainer != __instance.m_currentContainer)
            {
                _lastContainer = __instance.m_currentContainer;
                _refreshTimer = 0f;
            }

            if (_refreshTimer <= 0f)
            {
                _refreshTimer = 2f;
                AgentDepositChestGuiPatch_Awake.RefreshNpcDropdown(__instance.m_currentContainer);
            }
        }

        private static bool ShouldShowControls(InventoryGui gui)
        {
            if (!InventoryGui.IsVisible())
                return false;

            if (gui == null || gui.m_currentContainer == null || gui.m_currentContainer.m_inventory == null)
                return false;

            if (AgentDepositChestGuiPatch_Awake.GetAgentFromCurrentContainer(gui) != null)
                return false;

            return true;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "Awake")]
    public static class AgentFoodInventoryGuiPatch_Awake
    {
        public static GameObject Root;
        public static TextMeshProUGUI TitleText;
        public static readonly List<AgentFoodSlotView> Slots = new List<AgentFoodSlotView>();

        private static void Postfix(InventoryGui __instance)
        {
            EnsureFoodPanel(__instance);
        }

        public static void EnsureFoodPanel(InventoryGui gui)
        {
            if (Root != null || gui == null)
                return;

            Transform parent = AgentInventoryGuiShared.FindContainerUiParent(gui);
            if (parent == null)
                return;

            AgentTMPFont.EnsureDefault();
            Root = new GameObject("AgentFoodInventoryPanel", typeof(RectTransform), typeof(Image));
            Root.transform.SetParent(parent, false);

            RectTransform rootRect = Root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0.5f);
            rootRect.anchorMax = new Vector2(1f, 0.5f);
            rootRect.pivot = new Vector2(0f, 0.5f);
            rootRect.sizeDelta = new Vector2(286f, 116f);
            rootRect.anchoredPosition = new Vector2(16f, -180f);

            Image rootImage = Root.GetComponent<Image>();
            rootImage.color = new Color(0.12f, 0.09f, 0.07f, 0.94f);
            rootImage.raycastTarget = true;
            AgentInventoryGuiShared.AddBorder(Root.transform);

            TitleText = AgentInventoryGuiShared.CreateText(Root.transform, "Title", "Food", 16f, TextAlignmentOptions.Left, new Color(1f, 0.78f, 0.35f, 1f));
            RectTransform titleRect = TitleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.sizeDelta = new Vector2(-18f, 24f);
            titleRect.anchoredPosition = new Vector2(12f, -8f);

            Slots.Clear();
            for (int i = 0; i < AgentFoodInventory.Width; i++)
                Slots.Add(CreateFoodSlot(Root.transform, i));

            SetVisible(false);
        }

        private static AgentFoodSlotView CreateFoodSlot(Transform parent, int index)
        {
            GameObject slotGo = new GameObject("FoodSlot" + index, typeof(RectTransform), typeof(Image), typeof(AgentFoodSlotView));
            slotGo.transform.SetParent(parent, false);

            RectTransform slotRect = slotGo.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0f, 1f);
            slotRect.anchorMax = new Vector2(0f, 1f);
            slotRect.pivot = new Vector2(0f, 1f);
            slotRect.sizeDelta = new Vector2(72f, 72f);
            slotRect.anchoredPosition = new Vector2(16f + index * 84f, -36f);

            Image slotImage = slotGo.GetComponent<Image>();
            slotImage.color = new Color(0.06f, 0.045f, 0.035f, 0.98f);
            slotImage.raycastTarget = true;

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(slotGo.transform, false);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(42f, 42f);
            iconRect.anchoredPosition = new Vector2(0f, 6f);

            Image icon = iconGo.GetComponent<Image>();
            icon.color = Color.white;
            icon.raycastTarget = false;
            icon.preserveAspect = true;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(slotGo.transform, false);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.sizeDelta = new Vector2(-6f, 22f);
            labelRect.anchoredPosition = new Vector2(0f, 4f);

            TextMeshProUGUI label = AgentTMPFont.AddText(labelGo);
            label.text = "Empty";
            label.fontSize = 11f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.85f, 0.78f, 0.6f, 1f);
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;

            AgentFoodSlotView view = slotGo.GetComponent<AgentFoodSlotView>();
            view.Init(index, icon, label, slotImage);
            return view;
        }

        public static void MovePanelToContainerPanel(InventoryGui gui)
        {
            if (gui == null || Root == null)
                return;

            Transform parent = AgentInventoryGuiShared.FindContainerUiParent(gui);
            if (parent == null)
                return;

            if (Root.transform.parent != parent)
                Root.transform.SetParent(parent, false);

            RectTransform rootRect = Root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0.5f);
            rootRect.anchorMax = new Vector2(1f, 0.5f);
            rootRect.pivot = new Vector2(0f, 0.5f);
            rootRect.sizeDelta = new Vector2(286f, 116f);
            rootRect.anchoredPosition = new Vector2(16f, -180f);
        }

        public static void SetVisible(bool visible)
        {
            if (Root != null)
                Root.SetActive(visible);
        }

        public static void UpdatePanel(AgentComponent agent)
        {
            AgentFoodInventory foodInventory = agent != null ? agent.GetComponent<AgentFoodInventory>() : null;
            Inventory inventory = foodInventory != null ? foodInventory.Inventory : null;

            for (int i = 0; i < Slots.Count; i++)
                Slots[i].SetAgent(agent, inventory);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "Update")]
    public static class AgentFoodInventoryGuiPatch_Update
    {
        private static float _refreshTimer;
        private static Container _lastContainer;
        private static AgentComponent _cachedAgent;

        private static void Postfix(InventoryGui __instance)
        {
            AgentFoodInventoryGuiPatch_Awake.EnsureFoodPanel(__instance);
            AgentComponent agent = GetOpenAgent(__instance);
            bool show = agent != null;
            AgentFoodInventoryGuiPatch_Awake.SetVisible(show);

            if (!show)
                return;

            AgentFoodInventoryGuiPatch_Awake.MovePanelToContainerPanel(__instance);
            _refreshTimer -= Time.deltaTime;

            if (_refreshTimer <= 0f)
            {
                _refreshTimer = 0.2f;
                AgentFoodInventoryGuiPatch_Awake.UpdatePanel(agent);
            }
        }

        public static AgentComponent GetOpenAgent(InventoryGui gui)
        {
            if (!InventoryGui.IsVisible() || gui == null || gui.m_currentContainer == null)
            {
                _lastContainer = null;
                _cachedAgent = null;
                return null;
            }

            if (_lastContainer == gui.m_currentContainer)
                return _cachedAgent;

            _lastContainer = gui.m_currentContainer;
            _cachedAgent = AgentInventoryGuiShared.GetAgentFromContainer(gui.m_currentContainer);
            return _cachedAgent;
        }
    }

    public class AgentFoodSlotView : MonoBehaviour, IPointerClickHandler, IDropHandler
    {
        private int _slot;
        private Image _icon;
        private TextMeshProUGUI _label;
        private Image _background;
        private AgentComponent _agent;
        private Inventory _foodInventory;

        public void Init(int slot, Image icon, TextMeshProUGUI label, Image background)
        {
            _slot = slot;
            _icon = icon;
            _label = label;
            _background = background;
        }

        public void SetAgent(AgentComponent agent, Inventory foodInventory)
        {
            _agent = agent;
            _foodInventory = foodInventory;
            Refresh();
        }

        public void OnDrop(PointerEventData eventData)
        {
            TryPlaceDraggedItem();
            Refresh();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
                return;

            if (TryPlaceDraggedItem())
            {
                Refresh();
                return;
            }

            ItemDrop.ItemData item = GetItemInFoodSlot(_foodInventory, _slot);

            if (item != null)
            {
                MoveFoodSlotToNpcInventory(item);
                Refresh();
                return;
            }

            TryMoveFirstFoodFromOpenInventories();
            Refresh();
        }

        private bool TryPlaceDraggedItem()
        {
            InventoryGui gui = InventoryGui.instance;

            if (gui == null || _agent == null || _foodInventory == null)
                return false;

            ItemDrop.ItemData dragItem = GetDragItem(gui);

            if (dragItem == null)
                return false;

            if (!AgentFoodInventory.IsFoodItem(dragItem))
            {
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, "Only food can go in NPC food slots");
                return false;
            }

            Inventory source = GetDragInventory(gui) ?? FindInventoryContainingItem(gui, dragItem);

            if (source == null)
                return false;

            int dragAmount = GetDragAmount(gui, dragItem);
            bool moved = MoveStackToFoodSlot(source, dragItem, _foodInventory, _slot, dragAmount);

            if (moved)
            {
                ClearDrag(gui);
                ForceInventoryGuiRefresh(gui);
            }

            return moved;
        }

        private bool TryMoveFirstFoodFromOpenInventories()
        {
            InventoryGui gui = InventoryGui.instance;

            if (gui == null || _foodInventory == null)
                return false;

            Inventory playerInventory = Player.m_localPlayer != null ? Player.m_localPlayer.GetInventory() : null;

            if (TryMoveFirstFoodFromInventory(playerInventory))
                return true;

            if (gui.m_currentContainer != null)
                return TryMoveFirstFoodFromInventory(gui.m_currentContainer.m_inventory);

            return false;
        }

        private bool TryMoveFirstFoodFromInventory(Inventory source)
        {
            if (source == null)
                return false;

            List<ItemDrop.ItemData> items = source.GetAllItemsInGridOrder();
            for (int i = 0; i < items.Count; i++)
            {
                ItemDrop.ItemData item = items[i];

                if (AgentFoodInventory.IsFoodItem(item) && MoveStackToFoodSlot(source, item, _foodInventory, _slot, item.m_stack))
                    return true;
            }

            return false;
        }

        private bool MoveStackToFoodSlot(Inventory source, ItemDrop.ItemData item, Inventory target, int slot, int amount)
        {
            if (source == null || target == null || item == null)
                return false;

            if (GetItemInFoodSlot(target, slot) != null)
                return false;

            if (!AgentFoodInventory.IsFoodItem(item))
                return false;

            int moveAmount = Mathf.Clamp(amount <= 0 ? item.m_stack : amount, 1, Mathf.Max(1, item.m_stack));
            ItemDrop.ItemData clone = item.Clone();

            if (clone == null)
                return false;

            clone.m_stack = moveAmount;
            clone.m_gridPos = new Vector2i(slot, 0);

            if (!target.AddItem(clone))
                return false;

            ItemDrop.ItemData targetItem = GetItemInFoodSlot(target, slot);

            if (targetItem != null)
                targetItem.m_gridPos = new Vector2i(slot, 0);

            RemoveAmountFromSource(source, item, moveAmount);
            target.m_onChanged?.Invoke();
            source.m_onChanged?.Invoke();
            return true;
        }

        private void RemoveAmountFromSource(Inventory source, ItemDrop.ItemData item, int amount)
        {
            if (source == null || item == null)
                return;

            if (amount >= item.m_stack)
            {
                source.RemoveItem(item);
                return;
            }

            item.m_stack -= amount;
        }

        private bool MoveFoodSlotToNpcInventory(ItemDrop.ItemData item)
        {
            if (_agent == null || _agent.ValheimContainer == null || _agent.ValheimContainer.m_inventory == null || _foodInventory == null || item == null)
                return false;

            ItemDrop.ItemData clone = item.Clone();

            if (clone == null)
                return false;

            if (!_agent.ValheimContainer.m_inventory.AddItem(clone))
            {
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, "NPC inventory full");
                return false;
            }

            _foodInventory.RemoveItem(item);
            _foodInventory.m_onChanged?.Invoke();
            _agent.ValheimContainer.m_inventory.m_onChanged?.Invoke();
            return true;
        }

        private void Refresh()
        {
            ItemDrop.ItemData item = GetItemInFoodSlot(_foodInventory, _slot);

            if (item == null)
            {
                if (_icon != null)
                {
                    _icon.sprite = null;
                    _icon.enabled = false;
                }

                if (_label != null)
                {
                    _label.text = "Empty";
                    _label.color = new Color(0.8f, 0.74f, 0.58f, 1f);
                }

                if (_background != null)
                    _background.color = new Color(0.06f, 0.045f, 0.035f, 0.98f);

                return;
            }

            if (_icon != null)
            {
                _icon.sprite = GetItemIcon(item);
                _icon.enabled = _icon.sprite != null;
            }

            if (_label != null)
            {
                string stack = item.m_stack > 1 ? " x" + item.m_stack : string.Empty;
                _label.text = AgentInventoryGuiShared.CleanItemName(item.m_shared != null ? item.m_shared.m_name : "Food") + stack;
                _label.color = new Color(1f, 0.95f, 0.78f, 1f);
            }

            if (_background != null)
                _background.color = new Color(0.13f, 0.09f, 0.055f, 0.98f);
        }

        private static ItemDrop.ItemData GetItemInFoodSlot(Inventory inventory, int slot)
        {
            if (inventory == null)
                return null;

            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item != null && item.m_gridPos.x == slot && item.m_gridPos.y == 0)
                    return item;
            }

            return null;
        }

        private static Sprite GetItemIcon(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null || item.m_shared.m_icons == null || item.m_shared.m_icons.Length == 0)
                return null;

            int variant = Mathf.Clamp(item.m_variant, 0, item.m_shared.m_icons.Length - 1);
            return item.m_shared.m_icons[variant];
        }

        private static ItemDrop.ItemData GetDragItem(InventoryGui gui)
        {
            FieldInfo field = AccessTools.Field(typeof(InventoryGui), "m_dragItem");
            return field != null ? field.GetValue(gui) as ItemDrop.ItemData : null;
        }

        private static Inventory GetDragInventory(InventoryGui gui)
        {
            FieldInfo field = AccessTools.Field(typeof(InventoryGui), "m_dragInventory");
            return field != null ? field.GetValue(gui) as Inventory : null;
        }

        private static int GetDragAmount(InventoryGui gui, ItemDrop.ItemData dragItem)
        {
            FieldInfo field = AccessTools.Field(typeof(InventoryGui), "m_dragAmount");

            if (field != null)
            {
                object value = field.GetValue(gui);

                if (value is int intValue && intValue > 0)
                    return intValue;
            }

            return dragItem != null ? Mathf.Max(1, dragItem.m_stack) : 1;
        }

        private static void ClearDrag(InventoryGui gui)
        {
            if (gui == null)
                return;

            SetField(gui, "m_dragItem", null);
            SetField(gui, "m_dragInventory", null);
            SetField(gui, "m_dragAmount", 0);
            HideGameObjectField(gui, "m_dragGo");
            HideGameObjectField(gui, "m_dragItemIcon");
        }

        private static void SetField(object instance, string name, object value)
        {
            if (instance == null)
                return;

            FieldInfo field = AccessTools.Field(instance.GetType(), name);

            if (field == null)
                return;

            if (value == null && field.FieldType.IsValueType)
                return;

            field.SetValue(instance, value);
        }

        private static void HideGameObjectField(object instance, string name)
        {
            FieldInfo field = AccessTools.Field(instance.GetType(), name);

            if (field == null)
                return;

            object value = field.GetValue(instance);

            if (value is GameObject gameObject)
                gameObject.SetActive(false);
            else if (value is Component component)
                component.gameObject.SetActive(false);
        }

        private static void ForceInventoryGuiRefresh(InventoryGui gui)
        {
            if (gui == null)
                return;

            MethodInfo method = AccessTools.Method(typeof(InventoryGui), "UpdateInventory");

            if (method != null && method.GetParameters().Length == 0)
                method.Invoke(gui, null);
        }

        private static Inventory FindInventoryContainingItem(InventoryGui gui, ItemDrop.ItemData item)
        {
            if (item == null)
                return null;

            Inventory playerInventory = Player.m_localPlayer != null ? Player.m_localPlayer.GetInventory() : null;

            if (playerInventory != null && playerInventory.ContainsItem(item))
                return playerInventory;

            if (gui != null && gui.m_currentContainer != null && gui.m_currentContainer.m_inventory != null && gui.m_currentContainer.m_inventory.ContainsItem(item))
                return gui.m_currentContainer.m_inventory;

            AgentComponent agent = AgentFoodInventoryGuiPatch_Update.GetOpenAgent(gui);
            AgentFoodInventory foodInventory = agent != null ? agent.GetComponent<AgentFoodInventory>() : null;

            if (foodInventory != null && foodInventory.Inventory != null && foodInventory.Inventory.ContainsItem(item))
                return foodInventory.Inventory;

            return null;
        }
    }

    [HarmonyPatch(typeof(Inventory))]
    public static class Inventory_AddItem_Patch
    {
        [HarmonyPatch("AddItem", new Type[] { typeof(GameObject), typeof(int) })]
        [HarmonyPrefix]
        public static bool Prefix(Inventory __instance, GameObject prefab, int amount)
        {
            AgentComponent agent = AgentInventoryRegistry.GetAgent(__instance);

            if (agent == null || prefab == null)
                return true;

            ItemDrop item = prefab.GetComponent<ItemDrop>();

            if (item == null || item.m_itemData == null || item.m_itemData.m_shared == null)
                return true;

            float itemWeight = item.m_itemData.m_shared.m_weight * amount;
            float current = __instance.GetTotalWeight();
            float max = agent.MaxCarryWeight;

            if (current + itemWeight > max)
            {
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, "Too heavy!");
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "Awake")]
    public static class InventoryGui_Awake_Patch
    {
        public static TextMeshProUGUI NpcWeightText;

        private static void Postfix(InventoryGui __instance)
        {
            EnsureNpcWeightLabel(__instance);
        }

        public static void EnsureNpcWeightLabel(InventoryGui gui)
        {
            if (NpcWeightText != null || gui == null || gui.m_weight == null)
                return;

            Transform parent = AgentInventoryGuiShared.FindContainerUiParent(gui) ?? gui.m_weight.transform.parent;

            if (parent == null)
                return;

            GameObject go = UnityEngine.Object.Instantiate(gui.m_weight.gameObject, parent, false);
            go.name = "NPCWeight";
            NpcWeightText = go.GetComponent<TextMeshProUGUI>();

            if (NpcWeightText == null)
                NpcWeightText = AgentTMPFont.AddText(go);

            TMP_FontAsset font = AgentInventoryGuiShared.FindFont(gui);

            if (font != null)
                NpcWeightText.font = font;

            NpcWeightText.text = string.Empty;
            NpcWeightText.fontSize = gui.m_weight.fontSize;
            NpcWeightText.fontSizeMin = gui.m_weight.fontSizeMin;
            NpcWeightText.fontSizeMax = gui.m_weight.fontSizeMax;
            NpcWeightText.enableAutoSizing = gui.m_weight.enableAutoSizing;
            NpcWeightText.alignment = TextAlignmentOptions.Right;
            NpcWeightText.raycastTarget = false;

            RectTransform rect = go.GetComponent<RectTransform>();

            if (rect != null)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.sizeDelta = new Vector2(220f, 32f);
                rect.anchoredPosition = new Vector2(-22f, -44f);
            }

            go.SetActive(false);
        }

        public static void MoveNpcWeightLabelToContainerPanel(InventoryGui gui)
        {
            if (gui == null || NpcWeightText == null)
                return;

            Transform parent = AgentInventoryGuiShared.FindContainerUiParent(gui);

            if (parent == null || NpcWeightText.transform.parent == parent)
                return;

            NpcWeightText.transform.SetParent(parent, false);
            RectTransform rect = NpcWeightText.GetComponent<RectTransform>();

            if (rect != null)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.sizeDelta = new Vector2(220f, 32f);
                rect.anchoredPosition = new Vector2(-22f, -44f);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "Update")]
    public static class InventoryGui_Update_Patch
    {
        private static Container _lastContainer;
        private static AgentComponent _cachedAgent;
        private static float _refreshTimer;

        private static void Postfix(InventoryGui __instance)
        {
            if (!InventoryGui.IsVisible())
            {
                _lastContainer = null;
                _cachedAgent = null;
                SetNpcText(null, string.Empty);
                return;
            }

            if (__instance == null || __instance.m_currentContainer == null)
            {
                _lastContainer = null;
                _cachedAgent = null;
                SetNpcText(__instance, string.Empty);
                return;
            }

            if (_lastContainer != __instance.m_currentContainer)
            {
                _lastContainer = __instance.m_currentContainer;
                _cachedAgent = AgentInventoryGuiShared.GetAgentFromContainer(__instance.m_currentContainer);
                _refreshTimer = 0f;
            }

            if (_cachedAgent == null || __instance.m_currentContainer.m_inventory == null)
            {
                SetNpcText(__instance, string.Empty);
                return;
            }

            _refreshTimer -= Time.deltaTime;

            if (_refreshTimer > 0f)
                return;

            _refreshTimer = 0.25f;
            float current = __instance.m_currentContainer.m_inventory.GetTotalWeight();
            float max = _cachedAgent.MaxCarryWeight;
            SetNpcText(__instance, "NPC: " + current.ToString("0") + "/" + max.ToString("0"));
        }

        private static void SetNpcText(InventoryGui gui, string text)
        {
            InventoryGui_Awake_Patch.EnsureNpcWeightLabel(gui);
            TextMeshProUGUI label = InventoryGui_Awake_Patch.NpcWeightText;

            if (label == null)
                return;

            if (!string.IsNullOrEmpty(text))
                InventoryGui_Awake_Patch.MoveNpcWeightLabelToContainerPanel(gui);

            label.text = text;
            label.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "Awake")]
    public static class AgentNeedsInventoryGuiPatch_Awake
    {
        public static GameObject Root;
        public static TextMeshProUGUI TitleText;
        public static TextMeshProUGUI StaminaText;
        public static TextMeshProUGUI FoodText;
        public static TextMeshProUGUI HintText;

        private static void Postfix(InventoryGui __instance)
        {
            EnsurePanel(__instance);
        }

        public static void EnsurePanel(InventoryGui gui)
        {
            if (Root != null || gui == null)
                return;

            Transform parent = AgentInventoryGuiShared.FindContainerUiParent(gui);
            if (parent == null)
                return;

            AgentTMPFont.EnsureDefault();
            Root = new GameObject("AgentNeedsPanel", typeof(RectTransform), typeof(Image));
            Root.transform.SetParent(parent, false);

            RectTransform rootRect = Root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0.5f);
            rootRect.anchorMax = new Vector2(1f, 0.5f);
            rootRect.pivot = new Vector2(0f, 0.5f);
            rootRect.sizeDelta = new Vector2(270f, 174f);
            rootRect.anchoredPosition = new Vector2(16f, 142f);

            Image rootImage = Root.GetComponent<Image>();
            rootImage.color = new Color(0.12f, 0.09f, 0.07f, 0.92f);
            AgentInventoryGuiShared.AddBorder(Root.transform);

            TitleText = AgentInventoryGuiShared.CreateText(Root.transform, "Title", "NPC needs", 16f, TextAlignmentOptions.Center, new Color(1f, 0.68f, 0.28f, 1f));
            StaminaText = AgentInventoryGuiShared.CreateText(Root.transform, "Stamina", "Stamina: -- / --", 15f, TextAlignmentOptions.Left, new Color(1f, 0.95f, 0.78f, 1f));
            FoodText = AgentInventoryGuiShared.CreateText(Root.transform, "Food", "Food:\n--\n--\n--", 14f, TextAlignmentOptions.Left, new Color(0.9f, 0.86f, 0.7f, 1f));
            HintText = AgentInventoryGuiShared.CreateText(Root.transform, "Hint", "Place food in this inventory", 13f, TextAlignmentOptions.Center, new Color(0.72f, 0.67f, 0.54f, 1f));

            RectTransform titleRect = TitleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-18f, 26f);
            titleRect.anchoredPosition = new Vector2(0f, -8f);

            RectTransform staminaRect = StaminaText.GetComponent<RectTransform>();
            staminaRect.anchorMin = new Vector2(0f, 1f);
            staminaRect.anchorMax = new Vector2(1f, 1f);
            staminaRect.pivot = new Vector2(0.5f, 1f);
            staminaRect.sizeDelta = new Vector2(-24f, 24f);
            staminaRect.anchoredPosition = new Vector2(0f, -38f);

            RectTransform foodRect = FoodText.GetComponent<RectTransform>();
            foodRect.anchorMin = new Vector2(0f, 1f);
            foodRect.anchorMax = new Vector2(1f, 1f);
            foodRect.pivot = new Vector2(0.5f, 1f);
            foodRect.sizeDelta = new Vector2(-24f, 82f);
            foodRect.anchoredPosition = new Vector2(0f, -66f);

            RectTransform hintRect = HintText.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.sizeDelta = new Vector2(-18f, 24f);
            hintRect.anchoredPosition = new Vector2(0f, 8f);

            Root.SetActive(false);
        }

        public static void MovePanelToContainerPanel(InventoryGui gui)
        {
            if (gui == null || Root == null)
                return;

            Transform parent = AgentInventoryGuiShared.FindContainerUiParent(gui);

            if (parent == null)
                return;

            if (Root.transform.parent != parent)
                Root.transform.SetParent(parent, false);

            RectTransform rootRect = Root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0.5f);
            rootRect.anchorMax = new Vector2(1f, 0.5f);
            rootRect.pivot = new Vector2(0f, 0.5f);
            rootRect.sizeDelta = new Vector2(270f, 174f);
            rootRect.anchoredPosition = new Vector2(16f, 142f);
        }

        public static void SetVisible(bool visible)
        {
            if (Root != null)
                Root.SetActive(visible);
        }

        public static void UpdatePanel(AgentComponent agent)
        {
            if (agent == null)
                return;

            AgentStamina stamina = agent.GetComponent<AgentStamina>();
            AgentFood food = agent.GetComponent<AgentFood>();

            if (TitleText != null)
                TitleText.text = "NPC needs";

            if (StaminaText != null)
            {
                if (stamina != null)
                {
                    float pct = Mathf.Clamp01(stamina.GetStaminaPercentage()) * 100f;
                    StaminaText.text = "Stamina: " + stamina.Stamina.ToString("0") + " / " + stamina.MaxStamina.ToString("0") + "  (" + pct.ToString("0") + "%)";
                    StaminaText.color = pct < 20f ? new Color(1f, 0.35f, 0.25f, 1f) : pct < 50f ? new Color(1f, 0.78f, 0.35f, 1f) : new Color(1f, 0.95f, 0.78f, 1f);
                }
                else
                {
                    StaminaText.text = "Stamina: module missing";
                    StaminaText.color = new Color(1f, 0.35f, 0.25f, 1f);
                }
            }

            if (FoodText != null)
                FoodText.text = food != null ? BuildFoodText(food) : "Food:\nmodule missing";

            if (HintText != null)
                HintText.text = food != null && food.NeedsFood() ? "Needs food in NPC inventory" : "Food and stamina are active";
        }

        private static string BuildFoodText(AgentFood food)
        {
            List<string> lines = new List<string>();
            lines.Add("Food:");

            for (int i = 0; i < AgentFood.MaxFoodSlots; i++)
            {
                AgentFood.FoodEffect effect = food.GetFood(i);

                if (!effect.IsActive)
                {
                    lines.Add("- Empty");
                    continue;
                }

                float minutes = Mathf.Max(0f, effect.RemainingTime) / 60f;
                string name = AgentInventoryGuiShared.CleanItemName(effect.ItemName);
                lines.Add("- " + name + "  " + minutes.ToString("0") + "m  H" + effect.HealthBonus.ToString("0") + " S" + effect.StaminaBonus.ToString("0"));
            }

            return string.Join("\n", lines.ToArray());
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "Update")]
    public static class AgentNeedsInventoryGuiPatch_Update
    {
        private static float _refreshTimer;
        private static Container _lastContainer;
        private static AgentComponent _cachedAgent;

        private static void Postfix(InventoryGui __instance)
        {
            AgentNeedsInventoryGuiPatch_Awake.EnsurePanel(__instance);
            AgentComponent agent = FindOpenAgent(__instance);
            bool show = agent != null;
            AgentNeedsInventoryGuiPatch_Awake.SetVisible(show);

            if (!show)
                return;

            AgentNeedsInventoryGuiPatch_Awake.MovePanelToContainerPanel(__instance);
            _refreshTimer -= Time.deltaTime;

            if (_refreshTimer <= 0f)
            {
                _refreshTimer = 0.25f;
                AgentNeedsInventoryGuiPatch_Awake.UpdatePanel(agent);
            }
        }

        private static AgentComponent FindOpenAgent(InventoryGui gui)
        {
            if (!InventoryGui.IsVisible() || gui == null || gui.m_currentContainer == null)
            {
                _lastContainer = null;
                _cachedAgent = null;
                return null;
            }

            if (_lastContainer == gui.m_currentContainer)
                return _cachedAgent;

            _lastContainer = gui.m_currentContainer;
            _cachedAgent = AgentInventoryGuiShared.GetAgentFromContainer(gui.m_currentContainer);
            return _cachedAgent;
        }
    }
}
