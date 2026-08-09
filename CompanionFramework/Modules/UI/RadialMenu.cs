using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class RadialMenuEntry
{
    public string Label;
    public Action Action;
    public readonly List<RadialMenuEntry> Children = new List<RadialMenuEntry>();

    public RadialMenuEntry(string label, Action action = null)
    {
        Label = label;
        Action = action;
    }

    public RadialMenuEntry Add(string label, Action action = null)
    {
        RadialMenuEntry child = new RadialMenuEntry(label, action);
        Children.Add(child);
        return child;
    }
}

public class RadialMenu : MonoBehaviour
{
    private readonly List<RadialMenuEntry> _roots = new List<RadialMenuEntry>();
    private readonly List<GameObject> _items = new List<GameObject>();
    private readonly List<GameObject> _children = new List<GameObject>();
    private Action<int> _legacyCallback;
    private RadialMenuEntry _selectedEntry;
    private int _selectedIndex = -1;
    private int _selectedRootIndex = -1;
    private RectTransform _rect;

    private const float RootRadius = 190f;
    private const float ChildRadius = 305f;
    private const float ItemWidth = 126f;
    private const float ItemHeight = 42f;

    public void Init(List<string> labels, Action<int> callback)
    {
        _legacyCallback = callback;
        _roots.Clear();

        if (labels != null)
        {
            for (int i = 0; i < labels.Count; i++)
            {
                int index = i;
                _roots.Add(new RadialMenuEntry(labels[i], () => _legacyCallback?.Invoke(index)));
            }
        }

        Rebuild();
    }

    public void Init(List<RadialMenuEntry> entries)
    {
        _legacyCallback = null;
        _roots.Clear();

        if (entries != null)
            _roots.AddRange(entries);

        Rebuild();
    }

    public void SetOptions(List<string> labels, Action<int> callback)
    {
        Init(labels, callback);
    }

    public void SetOptions(List<RadialMenuEntry> entries)
    {
        Init(entries);
    }

    public void Confirm()
    {
        if (_selectedEntry != null && _selectedEntry.Action != null)
        {
            _selectedEntry.Action.Invoke();
            return;
        }

        if (_legacyCallback != null && _selectedIndex >= 0)
            _legacyCallback.Invoke(_selectedIndex);
    }

    public void Cancel()
    {
        Destroy(gameObject);
    }

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();

        if (_rect == null)
            _rect = gameObject.AddComponent<RectTransform>();
    }

    private void Rebuild()
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        int previousRootIndex = _selectedRootIndex;

        ClearObjects(_items);
        ClearObjects(_children);
        _selectedEntry = null;
        _selectedIndex = -1;

        if (_roots.Count == 0)
            return;

        for (int i = 0; i < _roots.Count; i++)
        {
            float angle = GetAngle(i, _roots.Count);
            Vector2 position = AngleToPosition(angle, RootRadius);
            GameObject item = CreateItem(_roots[i].Label, position, false);
            RadialMenuItem menuItem = item.AddComponent<RadialMenuItem>();
            int index = i;
            menuItem.Init(
                () => SelectRoot(index),
                () => ExecuteEntry(_roots[index], index)
            );
            _items.Add(item);
        }

        if (previousRootIndex >= 0 && previousRootIndex < _roots.Count)
            SelectRoot(previousRootIndex);
    }

    private void SelectRoot(int index)
    {
        if (index < 0 || index >= _roots.Count)
            return;

        _selectedEntry = _roots[index];
        _selectedIndex = index;
        _selectedRootIndex = index;
        ClearObjects(_children);

        RadialMenuEntry root = _roots[index];

        if (root.Children.Count == 0)
            return;

        float rootAngle = GetAngle(index, _roots.Count);
        float spread = Mathf.Min(90f, 24f * Mathf.Max(1, root.Children.Count - 1));
        float start = rootAngle - spread * 0.5f;

        for (int i = 0; i < root.Children.Count; i++)
        {
            float angle = root.Children.Count == 1 ? rootAngle : start + spread * (i / (float)(root.Children.Count - 1));
            Vector2 position = AngleToPosition(angle, ChildRadius);
            RadialMenuEntry child = root.Children[i];
            GameObject childGo = CreateItem(child.Label, position, true);
            RadialMenuItem childItem = childGo.AddComponent<RadialMenuItem>();
            childItem.Init(
                () => SelectChild(child),
                () => ExecuteEntry(child, -1)
            );
            _children.Add(childGo);
        }
    }

    private void SelectChild(RadialMenuEntry child)
    {
        _selectedEntry = child;
        _selectedIndex = -1;
    }

    private void ExecuteEntry(RadialMenuEntry entry, int legacyIndex)
    {
        if (entry != null && entry.Action != null)
        {
            entry.Action.Invoke();
            return;
        }

        if (_legacyCallback != null && legacyIndex >= 0)
            _legacyCallback.Invoke(legacyIndex);
    }

    private GameObject CreateItem(string label, Vector2 anchoredPosition, bool child)
    {
        GameObject go = new GameObject(child ? "RadialChild" : "RadialItem", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(ItemWidth, ItemHeight);
        rect.anchoredPosition = anchoredPosition;

        Image image = go.GetComponent<Image>();
        image.color = child ? new Color(0.13f, 0.08f, 0.045f, 0.96f) : new Color(0.09f, 0.06f, 0.04f, 0.96f);

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.38f, 0.23f, 0.08f, 0.98f);
        colors.pressedColor = new Color(0.62f, 0.36f, 0.12f, 0.98f);
        button.colors = colors;

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(5f, 0f);
        textRect.offsetMax = new Vector2(-5f, 0f);

        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = child ? 13f : 14f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.83f, 0.42f, 1f);
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;

        return go;
    }

    private static float GetAngle(int index, int count)
    {
        if (count <= 0)
            return 90f;

        return 90f - index * 360f / count;
    }

    private static Vector2 AngleToPosition(float angle, float radius)
    {
        float radians = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius);
    }

    private static void ClearObjects(List<GameObject> objects)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i] != null)
                Destroy(objects[i]);
        }

        objects.Clear();
    }
}

public class RadialMenuItem : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private Action _hover;
    private Action _click;

    public void Init(Action hover, Action click)
    {
        _hover = hover;
        _click = click;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hover?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        _click?.Invoke();
    }
}
