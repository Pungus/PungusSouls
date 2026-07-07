using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

[HarmonyPatch(typeof(Minimap), "Awake")]
public static class AgentMapMarkerOverlayBootstrapPatch
{
    private static void Postfix(Minimap __instance)
    {
        if (__instance == null || __instance.GetComponent<AgentMapMarkerOverlay>() != null)
            return;

        __instance.gameObject.AddComponent<AgentMapMarkerOverlay>();
    }
}

public class AgentMapMarkerOverlay : MonoBehaviour
{
    private sealed class Marker
    {
        public GameObject Root;
        public RectTransform Rect;
        public Image Icon;
        public AgentComponent Agent;
    }

    private readonly List<Marker> _markers = new List<Marker>();
    private readonly List<AgentComponent> _agentSnapshot = new List<AgentComponent>();
    private RectTransform _smallMapRoot;
    private RectTransform _largeMapRoot;
    private MethodInfo _worldToMapPointMethod;
    private float _refreshTimer;

    private const float RefreshInterval = 1f;
    private const int MaxMarkers = 32;
    private const float SmallMarkerSize = 22f;
    private const float LargeMarkerSize = 28f;

    private void Awake()
    {
        CacheMinimapData();
        BuildMarkers();
    }

    private void Update()
    {
        if (_smallMapRoot == null && _largeMapRoot == null)
            CacheMinimapData();

        _refreshTimer -= Time.deltaTime;

        if (_refreshTimer <= 0f)
        {
            _refreshTimer = RefreshInterval;
            AgentHudRegistry.GetSnapshot(_agentSnapshot);
        }

        UpdateMarkers();
    }

    private void CacheMinimapData()
    {
        _worldToMapPointMethod = FindMethod(typeof(Minimap), "WorldToMapPoint", typeof(Vector3));
        _smallMapRoot = FindRectTransform("m_smallRoot") ?? FindRectTransform("m_smallMapRoot") ?? FindRectTransform("m_small");
        _largeMapRoot = FindRectTransform("m_largeRoot") ?? FindRectTransform("m_largeMapRoot") ?? FindRectTransform("m_large");
    }

    private void BuildMarkers()
    {
        if (_markers.Count > 0)
            return;

        AgentIconLoader.EnsureLoaded();
        RectTransform root = _smallMapRoot != null ? _smallMapRoot : GetComponent<RectTransform>();

        if (root == null)
            return;

        for (int i = 0; i < MaxMarkers; i++)
        {
            GameObject go = new GameObject("AgentMapMarker", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(SmallMarkerSize, SmallMarkerSize);

            Image icon = go.GetComponent<Image>();
            icon.sprite = AgentIconLoader.AgentMarker;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            go.SetActive(false);
            _markers.Add(new Marker { Root = go, Rect = rect, Icon = icon });
        }
    }

    private void UpdateMarkers()
    {
        if (_markers.Count == 0)
        {
            BuildMarkers();
            return;
        }

        RectTransform mapRoot = GetActiveMapRoot();

        if (mapRoot == null)
        {
            HideAll();
            return;
        }

        bool largeMap = _largeMapRoot != null && mapRoot == _largeMapRoot;
        float size = largeMap ? LargeMarkerSize : SmallMarkerSize;
        int markerIndex = 0;

        for (int i = 0; i < _agentSnapshot.Count && markerIndex < _markers.Count; i++)
        {
            AgentComponent agent = _agentSnapshot[i];

            if (agent == null || agent.gameObject == null)
                continue;

            if (!TryWorldToMapLocal(agent.transform.position, mapRoot, out Vector2 position))
                continue;

            Character character = agent.GetComponent<Character>();
            bool dead = character != null && character.IsDead();
            Marker marker = _markers[markerIndex];

            if (marker.Root.transform.parent != mapRoot)
                marker.Root.transform.SetParent(mapRoot, false);

            marker.Agent = agent;
            marker.Rect.sizeDelta = new Vector2(size, size);
            marker.Rect.anchoredPosition = position;
            marker.Icon.sprite = AgentNpcIconRegistry.GetMapIcon(agent, dead);
            marker.Root.SetActive(true);
            markerIndex++;
        }

        for (int i = markerIndex; i < _markers.Count; i++)
            _markers[i].Root.SetActive(false);
    }

    private RectTransform GetActiveMapRoot()
    {
        if (_largeMapRoot != null && _largeMapRoot.gameObject.activeInHierarchy)
            return _largeMapRoot;

        if (_smallMapRoot != null && _smallMapRoot.gameObject.activeInHierarchy)
            return _smallMapRoot;

        return null;
    }

    private bool TryWorldToMapLocal(Vector3 world, RectTransform mapRoot, out Vector2 local)
    {
        local = Vector2.zero;

        if (_worldToMapPointMethod != null && Minimap.instance != null)
        {
            object result = _worldToMapPointMethod.Invoke(Minimap.instance, new object[] { world });

            if (result is Vector3 vector3)
            {
                local = new Vector2(vector3.x, vector3.y);
                return true;
            }

            if (result is Vector2 vector2)
            {
                local = vector2;
                return true;
            }
        }

        Player player = Player.m_localPlayer;

        if (player == null)
            return false;

        Vector3 delta = world - player.transform.position;
        float scale = mapRoot.rect.width / 160f;
        local = new Vector2(delta.x * scale, delta.z * scale);
        float max = mapRoot.rect.width * 0.46f;
        local.x = Mathf.Clamp(local.x, -max, max);
        local.y = Mathf.Clamp(local.y, -max, max);
        return true;
    }

    private void HideAll()
    {
        for (int i = 0; i < _markers.Count; i++)
            _markers[i].Root.SetActive(false);
    }

    private RectTransform FindRectTransform(string fieldName)
    {
        FieldInfo field = FindField(typeof(Minimap), fieldName);

        if (field == null || Minimap.instance == null)
            return null;

        object value = field.GetValue(Minimap.instance);

        if (value is RectTransform rect)
            return rect;

        if (value is GameObject go)
            return go.GetComponent<RectTransform>();

        if (value is Component component)
            return component.GetComponent<RectTransform>();

        return null;
    }

    private static MethodInfo FindMethod(System.Type type, string name, params System.Type[] parameters)
    {
        while (type != null)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameters, null);

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
}
