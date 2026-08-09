using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[HarmonyLib.HarmonyPatch(typeof(Hud), "Awake")]
public static class AgentNpcHudPanelBootstrapPatch
{
    private static void Postfix(Hud __instance)
    {
        if (__instance == null || __instance.GetComponent<AgentNpcHudPanel>() != null)
            return;

        __instance.gameObject.AddComponent<AgentNpcHudPanel>();
    }
}

public class AgentNpcHudPanel : MonoBehaviour
{
    private sealed class Row
    {
        public GameObject Root;
        public Image Icon;
        public TextMeshProUGUI Name;
        public TextMeshProUGUI Lines;
        public AgentComponent Agent;
    }

    private readonly List<Row> _rows = new List<Row>();
    private readonly List<AgentComponent> _agentSnapshot = new List<AgentComponent>();
    private GameObject _root;
    private RectTransform _rootRect;
    private float _refreshTimer;

    private const float RefreshInterval = 0.5f;
    private const int MaxRows = 8;

    private void Awake()
    {
        BuildPanel();
    }

    private void Update()
    {
        if (_root == null)
            BuildPanel();

        if (_root == null)
            return;

        _refreshTimer -= Time.deltaTime;

        if (_refreshTimer > 0f)
            return;

        _refreshTimer = RefreshInterval;
        Refresh();
    }

    private void BuildPanel()
    {
        if (_root != null)
            return;

        AgentIconLoader.EnsureLoaded();
        Transform parent = Hud.instance != null && Hud.instance.m_rootObject != null ? Hud.instance.m_rootObject.transform : transform;
        _root = new GameObject("AgentNpcHudPanel", typeof(RectTransform));
        _root.transform.SetParent(parent, false);
        _rootRect = _root.GetComponent<RectTransform>();
        _rootRect.anchorMin = new Vector2(1f, 1f);
        _rootRect.anchorMax = new Vector2(1f, 1f);
        _rootRect.pivot = new Vector2(1f, 1f);
        _rootRect.sizeDelta = new Vector2(335f, 740f);
        _rootRect.anchoredPosition = new Vector2(-18f, -238f);

        for (int i = 0; i < MaxRows; i++)
        {
            Row row = CreateRow(_root.transform, i);
            row.Root.SetActive(false);
            _rows.Add(row);
        }

        _root.SetActive(false);
    }

    private Row CreateRow(Transform parent, int index)
    {
        GameObject root = new GameObject("AgentHudRow", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(335f, 88f);
        rect.anchoredPosition = new Vector2(0f, -index * 92f);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.04f, 0.68f);
        bg.raycastTarget = false;

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(root.transform, false);

        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.sizeDelta = new Vector2(34f, 34f);
        iconRect.anchoredPosition = new Vector2(8f, 0f);

        Image icon = iconGo.GetComponent<Image>();
        icon.sprite = AgentIconLoader.HudIcon;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TextMeshProUGUI name = CreateText(root.transform, "Name", 14f, TextAlignmentOptions.Left);
        RectTransform nameRect = name.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.offsetMin = new Vector2(48f, -22f);
        nameRect.offsetMax = new Vector2(-6f, -3f);

        TextMeshProUGUI lines = CreateText(root.transform, "Stats", 10f, TextAlignmentOptions.Left);
        RectTransform linesRect = lines.GetComponent<RectTransform>();
        linesRect.anchorMin = new Vector2(0f, 0f);
        linesRect.anchorMax = new Vector2(1f, 1f);
        linesRect.offsetMin = new Vector2(48f, 4f);
        linesRect.offsetMax = new Vector2(-6f, -24f);

        return new Row { Root = root, Icon = icon, Name = name, Lines = lines };
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, float size, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp;

        try
        {
            tmp = Integration.AgentTMPFont.AddText(go);
        }
        catch
        {
            tmp = go.AddComponent<TextMeshProUGUI>();
        }

        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = new Color(1f, 0.95f, 0.78f, 1f);
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    private void Refresh()
    {
        AgentHudRegistry.GetSnapshot(_agentSnapshot);
        Player player = Player.m_localPlayer;

        _agentSnapshot.Sort((a, b) =>
        {
            if (a == null || b == null)
                return 0;

            if (player == null)
                return string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase);

            float da = Vector3.Distance(player.transform.position, a.transform.position);
            float db = Vector3.Distance(player.transform.position, b.transform.position);
            return da.CompareTo(db);
        });

        int rowIndex = 0;

        for (int i = 0; i < _agentSnapshot.Count && rowIndex < _rows.Count; i++)
        {
            AgentComponent agent = _agentSnapshot[i];

            if (agent == null || agent.gameObject == null)
                continue;

            Character character = agent.GetComponent<Character>();

            if (character != null && character.IsDead())
                continue;

            UpdateRow(_rows[rowIndex], agent, character);
            rowIndex++;
        }

        for (int i = rowIndex; i < _rows.Count; i++)
            _rows[i].Root.SetActive(false);

        _root.SetActive(rowIndex > 0);
    }

    private void UpdateRow(Row row, AgentComponent agent, Character character)
    {
        row.Agent = agent;
        row.Root.SetActive(true);
        row.Icon.sprite = AgentNpcIconRegistry.GetHudIcon(agent);
        row.Name.text = CleanName(agent.gameObject.name);

        float health = character != null ? character.GetHealth() : 0f;
        float maxHealth = character != null ? character.GetMaxHealth() : 0f;

        AgentStamina stamina = agent.GetComponent<AgentStamina>();
        float staminaValue = stamina != null ? stamina.Stamina : 0f;
        float maxStamina = stamina != null ? stamina.MaxStamina : 0f;

        AgentRested rested = agent.GetComponent<AgentRested>();
        float restedRemaining = rested != null ? rested.RestedRemaining : 0f;
        int comfort = rested != null ? rested.ComfortLevel : 0;

        row.Lines.text =
            "HP " + health.ToString("0") + "/" + maxHealth.ToString("0") +
            "  STA " + staminaValue.ToString("0") + "/" + maxStamina.ToString("0") +
            "\n" + GetContextSummary(agent) +
            "\nRest " + FormatTime(restedRemaining) + " C" + comfort + "  " + GetFoodSummary(agent) +
            "\n" + GetCarrySummary(agent) + "  " + GetToolSummary(agent);
    }
    private static string GetContextSummary(AgentComponent agent)
    {
        if (agent == null || agent.Context == null)
            return "Unknown";

        return agent.Context.StateMode + " | " + agent.Context.TaskMode + " | " + agent.Context.BehaviourMode;
    }

    private static string GetCarrySummary(AgentComponent agent)
    {
        if (agent == null || agent.ValheimContainer == null || agent.ValheimContainer.m_inventory == null)
            return "Carry --/--";

        float weight = agent.ValheimContainer.m_inventory.GetTotalWeight();
        float max = Mathf.Max(1f, agent.MaxCarryWeight);
        return "Carry " + weight.ToString("0") + "/" + max.ToString("0");
    }

    private static string GetFoodSummary(AgentComponent agent)
    {
        AgentFood food = agent != null ? agent.GetComponent<AgentFood>() : null;

        if (food == null)
            return "Food --";

        return "Food " + GetActiveFoodCount(food) + "/3";
    }

    private static string GetToolSummary(AgentComponent agent)
    {
        if (agent == null)
            return "Tool --";

        Humanoid humanoid = agent.GetComponent<Humanoid>();
        ItemDrop.ItemData item = humanoid != null ? humanoid.GetCurrentWeapon() : null;

        if (item == null && agent.ValheimContainer != null && agent.ValheimContainer.m_inventory != null)
            item = FindFirstWorkTool(agent.ValheimContainer.m_inventory);

        return "Tool " + FormatItemName(item);
    }

    private static ItemDrop.ItemData FindFirstWorkTool(Inventory inventory)
    {
        if (inventory == null)
            return null;

        List<ItemDrop.ItemData> items = inventory.GetAllItems();

        for (int i = 0; i < items.Count; i++)
        {
            ItemDrop.ItemData item = items[i];

            if (item == null || item.m_shared == null)
                continue;

            HitData.DamageTypes damage = item.GetDamage();

            if (damage.m_chop > 0f || damage.m_pickaxe > 0f)
                return item;
        }

        return null;
    }

    private static string FormatItemName(ItemDrop.ItemData item)
    {
        if (item == null)
            return "--";

        string name = string.Empty;

        if (item.m_shared != null && !string.IsNullOrEmpty(item.m_shared.m_name))
            name = item.m_shared.m_name;
        else if (item.m_dropPrefab != null)
            name = item.m_dropPrefab.name;

        if (string.IsNullOrEmpty(name))
            return "--";

        if (Localization.instance != null)
            name = Localization.instance.Localize(name);

        return name.Replace("$item_", string.Empty).Replace("_", " ");
    }

    private static int GetActiveFoodCount(AgentFood food)
    {
        if (food == null)
            return 0;

        PropertyInfo property = food.GetType().GetProperty("ActiveFoodCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (property != null && property.PropertyType == typeof(int))
            return (int)property.GetValue(food, null);

        if (property != null && property.PropertyType == typeof(float))
            return Mathf.RoundToInt((float)property.GetValue(food, null));

        int count = 0;

        for (int i = 0; i < AgentFood.MaxFoodSlots; i++)
        {
            AgentFood.FoodEffect effect = food.GetFood(i);

            if (effect.IsActive)
                count++;
        }

        return count;
    }

    private static string FormatTime(float seconds)
    {
        if (seconds <= 0f)
            return "--";

        int total = Mathf.CeilToInt(seconds);
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    private static string CleanName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "NPC";

        string result = value.Replace("(Clone)", string.Empty).Trim();
        return string.IsNullOrEmpty(result) ? "NPC" : result;
    }
}
