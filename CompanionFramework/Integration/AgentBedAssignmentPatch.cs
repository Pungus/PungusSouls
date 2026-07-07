using Core.Agent;
using HarmonyLib;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Integration
{
    [HarmonyPatch]
    public static class AgentBedAssignmentPatch
    {
        private static GameObject _root;
        private static Button _previousButton;
        private static Button _nextButton;
        private static Button _assignButton;
        private static Button _closeButton;
        private static TextMeshProUGUI _agentText;
        private static TextMeshProUGUI _titleText;
        private static TextMeshProUGUI _hintText;
        private static readonly List<AgentComponent> _agents = new List<AgentComponent>();
        private static int _selectedIndex;
        private static Bed _currentBed;

        private const float NpcSearchDistance = 80f;

        [HarmonyPatch(typeof(Bed), "Interact")]
        [HarmonyPrefix]
        private static bool BedInteractPrefix(Bed __instance, Humanoid human, bool repeat, bool alt)
        {
            if (!alt)
                return true;

            if (human == null || human != Player.m_localPlayer)
                return true;

            Open(__instance);
            return false;
        }

        private static void Open(Bed bed)
        {
            if (bed == null || Hud.instance == null || Hud.instance.m_rootObject == null)
                return;

            Close();

            Integration.AgentTMPFont.EnsureDefault();
            _currentBed = bed;

            _root = new GameObject("AgentBedAssignmentUi", typeof(RectTransform), typeof(Image));
            _root.transform.SetParent(Hud.instance.m_rootObject.transform, false);

            RectTransform rootRect = _root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(380f, 142f);
            rootRect.anchoredPosition = new Vector2(0f, -205f);

            Image rootImage = _root.GetComponent<Image>();
            rootImage.color = new Color(0.12f, 0.09f, 0.06f, 0.94f);

            AddBorder(_root.transform);

            _titleText = CreateText(_root.transform, "Title", "Assign bed", 18f, TextAlignmentOptions.Center, new Color(1f, 0.68f, 0.28f, 1f));
            RectTransform titleRect = _titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-18f, 28f);
            titleRect.anchoredPosition = new Vector2(0f, -8f);

            GameObject selector = new GameObject("NpcSelector", typeof(RectTransform), typeof(Image));
            selector.transform.SetParent(_root.transform, false);

            RectTransform selectorRect = selector.GetComponent<RectTransform>();
            selectorRect.anchorMin = new Vector2(0.5f, 1f);
            selectorRect.anchorMax = new Vector2(0.5f, 1f);
            selectorRect.pivot = new Vector2(0.5f, 1f);
            selectorRect.sizeDelta = new Vector2(340f, 34f);
            selectorRect.anchoredPosition = new Vector2(0f, -42f);

            Image selectorImage = selector.GetComponent<Image>();
            selectorImage.color = new Color(0.08f, 0.06f, 0.04f, 0.98f);

            _previousButton = CreateButton(selector.transform, "Previous", "‹", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(34f, 34f), new Vector2(17f, 0f), 18f);
            _nextButton = CreateButton(selector.transform, "Next", "›", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(34f, 34f), new Vector2(-17f, 0f), 18f);

            GameObject labelGo = new GameObject("AgentLabel", typeof(RectTransform));
            labelGo.transform.SetParent(selector.transform, false);

            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(42f, 0f);
            labelRect.offsetMax = new Vector2(-42f, 0f);

            _agentText = CreateText(labelGo.transform, "Text", "No NPC with home nearby", 15f, TextAlignmentOptions.Center, new Color(1f, 0.95f, 0.78f, 1f));
            RectTransform agentTextRect = _agentText.GetComponent<RectTransform>();
            agentTextRect.anchorMin = Vector2.zero;
            agentTextRect.anchorMax = Vector2.one;
            agentTextRect.offsetMin = Vector2.zero;
            agentTextRect.offsetMax = Vector2.zero;

            _assignButton = CreateButton(_root.transform, "Assign", "Assign bed to selected NPC", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(260f, 34f), new Vector2(-42f, 22f), 15f);
            _closeButton = CreateButton(_root.transform, "Close", "X", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(34f, 34f), new Vector2(-28f, 22f), 15f);

            _hintText = CreateText(_root.transform, "Hint", "Choose an NPC with a home zone", 13f, TextAlignmentOptions.Center, new Color(0.85f, 0.8f, 0.65f, 1f));
            RectTransform hintRect = _hintText.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.sizeDelta = new Vector2(-18f, 22f);
            hintRect.anchoredPosition = new Vector2(0f, 60f);

            _previousButton.onClick.AddListener(SelectPrevious);
            _nextButton.onClick.AddListener(SelectNext);
            _assignButton.onClick.AddListener(AssignBed);
            _closeButton.onClick.AddListener(Close);

            RefreshAgents(bed.transform.position);

            AgentContainerComponent.IsRadialOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static Button CreateButton(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition, float fontSize)
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
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.18f, 0.12f, 0.06f, 0.96f);
            colors.highlightedColor = new Color(0.38f, 0.24f, 0.08f, 0.96f);
            colors.pressedColor = new Color(0.62f, 0.36f, 0.12f, 0.96f);
            colors.disabledColor = new Color(0.06f, 0.05f, 0.04f, 0.62f);
            button.colors = colors;

            TextMeshProUGUI label = CreateText(go.transform, "Text", text, fontSize, TextAlignmentOptions.Center, new Color(1f, 0.78f, 0.35f, 1f));
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
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

        private static void RefreshAgents(Vector3 bedPosition)
        {
            _agents.Clear();

            foreach (AgentComponent agent in Object.FindObjectsOfType<AgentComponent>())
            {
                if (agent == null || agent.Context == null)
                    continue;

                if (agent.Context.HomeZone == null || !agent.Context.HomeZone.IsSet)
                    continue;

                float distanceToNpc = Vector3.Distance(agent.transform.position, bedPosition);
                float distanceToHome = Vector3.Distance(agent.Context.HomeZone.Center, bedPosition);

                if (Mathf.Min(distanceToNpc, distanceToHome) > NpcSearchDistance)
                    continue;

                _agents.Add(agent);
            }

            if (_selectedIndex >= _agents.Count)
                _selectedIndex = _agents.Count - 1;

            if (_selectedIndex < 0)
                _selectedIndex = 0;

            RefreshAgentText();
        }

        private static void SelectPrevious()
        {
            if (_agents.Count == 0)
                return;

            _selectedIndex--;

            if (_selectedIndex < 0)
                _selectedIndex = _agents.Count - 1;

            RefreshAgentText();
        }

        private static void SelectNext()
        {
            if (_agents.Count == 0)
                return;

            _selectedIndex++;

            if (_selectedIndex >= _agents.Count)
                _selectedIndex = 0;

            RefreshAgentText();
        }

        private static void RefreshAgentText()
        {
            bool hasAgent = _agents.Count > 0 && _selectedIndex >= 0 && _selectedIndex < _agents.Count;

            if (_agentText != null)
                _agentText.text = hasAgent ? BuildAgentLabel(_agents[_selectedIndex]) : "No NPC with home nearby";

            if (_assignButton != null)
                _assignButton.interactable = hasAgent;

            bool multi = _agents.Count > 1;

            if (_previousButton != null)
                _previousButton.interactable = multi;

            if (_nextButton != null)
                _nextButton.interactable = multi;
        }

        private static string BuildAgentLabel(AgentComponent agent)
        {
            if (agent == null || agent.gameObject == null)
                return "NPC";

            string name = agent.gameObject.name.Replace("(Clone)", string.Empty).Trim();
            return string.IsNullOrEmpty(name) ? "NPC" : name;
        }

        private static void AssignBed()
        {
            if (_currentBed == null)
                return;

            if (_agents.Count == 0 || _selectedIndex < 0 || _selectedIndex >= _agents.Count)
                return;

            AgentComponent agent = _agents[_selectedIndex];
            SaveAssignedBed(agent, _currentBed);

            MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, "Bed assigned to " + BuildAgentLabel(agent));
            Close();
        }

        private static void SaveAssignedBed(AgentComponent agent, Bed bed)
        {
            if (agent == null || bed == null)
                return;

            ZNetView agentView = agent.GetComponent<ZNetView>();

            if (agentView != null && agentView.IsValid() && !agentView.IsOwner())
                agentView.ClaimOwnership();

            if (agentView == null || !agentView.IsValid())
                return;

            ZDO zdo = agentView.GetZDO();

            if (zdo == null)
                return;

            zdo.Set("agent_has_bed", true);
            zdo.Set("agent_bed_pos", bed.transform.position);
            zdo.Set("agent_bed_name", bed.gameObject.name);

            //Debug.Log("[Agent] Bed assigned to " + agent.name + ": " + bed.name + " pos=" + bed.transform.position);
        }

        private static void Close()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }

            _currentBed = null;
            AgentContainerComponent.IsRadialOpen = false;
            AgentContainerComponent.BlockInputUntil = Time.time + 0.25f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        [HarmonyPatch(typeof(Bed), "GetHoverText")]
        private static class Bed_GetHoverText_Patch
        {
            private static void Postfix(ref string __result)
            {
                __result += "\n[Shift + E] Assign to NPC";
            }
        }
    }
}
