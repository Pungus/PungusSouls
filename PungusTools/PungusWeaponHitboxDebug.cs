using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

public static class PungusWeaponHitboxDebug
{
    public static bool Enabled;
    public static bool ShowAllHumanoids;
    public static bool ShowPrimary = true;
    public static bool ShowSecondary = true;
    public static KeyCode ToggleKey = KeyCode.F7;
}

public class PungusWeaponHitboxDebugOverlay : MonoBehaviour
{
    public static PungusWeaponHitboxDebugOverlay Instance { get; private set; }

    private readonly List<LineRenderer> _lines = new List<LineRenderer>();
    private readonly List<Humanoid> _targets = new List<Humanoid>();
    private Material _material;
    private int _lineIndex;
    private float _refreshTimer;
    private float _logTimer;

    private const int ArcSegments = 28;
    private const int CircleSegments = 32;
    private const float LineWidth = 0.045f;

    private void Awake()
    {
        Instance = this;
        Shader shader = Shader.Find("Hidden/Internal-Colored") ?? Shader.Find("Sprites/Default");
        _material = new Material(shader);
        _material.hideFlags = HideFlags.HideAndDontSave;

        if (_material.HasProperty("_SrcBlend"))
            _material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);

        if (_material.HasProperty("_DstBlend"))
            _material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);

        if (_material.HasProperty("_Cull"))
            _material.SetInt("_Cull", (int)CullMode.Off);

        if (_material.HasProperty("_ZWrite"))
            _material.SetInt("_ZWrite", 0);

        if (_material.HasProperty("_ZTest"))
            _material.SetInt("_ZTest", (int)CompareFunction.Always);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(PungusWeaponHitboxDebug.ToggleKey))
        {
            PungusWeaponHitboxDebug.Enabled = !PungusWeaponHitboxDebug.Enabled;
            //Debug.Log("[WeaponHitboxDebug] " + (PungusWeaponHitboxDebug.Enabled ? "ON" : "OFF"));
        }

        if (!PungusWeaponHitboxDebug.Enabled)
        {
            HideAll();
            return;
        }

        _refreshTimer -= Time.deltaTime;

        if (_refreshTimer <= 0f)
        {
            _refreshTimer = 0.35f;
            RefreshTargets();
        }

        _lineIndex = 0;
        int drawn = 0;

        for (int i = 0; i < _targets.Count; i++)
        {
            Humanoid humanoid = _targets[i];

            if (humanoid == null)
                continue;

            Character character = humanoid.GetComponent<Character>();

            if (character != null && character.IsDead())
                continue;

            if (DrawWeapon(humanoid))
                drawn++;
        }

        for (int i = _lineIndex; i < _lines.Count; i++)
            _lines[i].gameObject.SetActive(false);

        _logTimer -= Time.deltaTime;

        if (_logTimer <= 0f)
        {
            _logTimer = 2f;
            //Debug.Log("[WeaponHitboxDebug] targets=" + _targets.Count + " drawn=" + drawn + " lines=" + _lineIndex);
        }
    }

    private void RefreshTargets()
    {
        _targets.Clear();

        if (PungusWeaponHitboxDebug.ShowAllHumanoids)
        {
            _targets.AddRange(UnityEngine.Object.FindObjectsOfType<Humanoid>());
            return;
        }

        if (Player.m_localPlayer != null)
            _targets.Add(Player.m_localPlayer.GetComponent<Humanoid>());
    }

    private bool DrawWeapon(Humanoid humanoid)
    {
        ItemDrop.ItemData weapon = GetCurrentWeapon(humanoid);

        if (weapon == null || weapon.m_shared == null)
            return false;

        bool drew = false;

        if (PungusWeaponHitboxDebug.ShowPrimary)
            drew |= DrawAttack(humanoid.transform, GetMemberValue(weapon.m_shared, "m_attack"), new Color(1f, 0.65f, 0.05f, 1f));

        if (PungusWeaponHitboxDebug.ShowSecondary)
            drew |= DrawAttack(humanoid.transform, GetMemberValue(weapon.m_shared, "m_secondaryAttack"), new Color(0.1f, 0.75f, 1f, 1f));

        return drew;
    }

    private bool DrawAttack(Transform attacker, object attack, Color color)
    {
        if (attacker == null || attack == null)
            return false;

        float range = Mathf.Max(0.05f, GetFloat(attack, "m_attackRange", 2f));
        float height = Mathf.Max(0.05f, GetFloat(attack, "m_attackHeight", 1f));
        float angle = Mathf.Clamp(GetFloat(attack, "m_attackAngle", 60f), 1f, 360f);
        float rayWidth = Mathf.Max(0f, GetFloat(attack, "m_attackRayWidth", 0f));
        float offset = GetFloat(attack, "m_attackOffset", 0f);

        Vector3 forward = attacker.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = attacker.forward;

        forward.Normalize();

        Vector3 origin = attacker.position + Vector3.up * height + attacker.right * offset;
        float half = angle * 0.5f;
        DrawLine(origin, origin + forward * range, Color.white);
        DrawLine(origin, origin + Quaternion.Euler(0f, -half, 0f) * forward * range, color);
        DrawLine(origin, origin + Quaternion.Euler(0f, half, 0f) * forward * range, color);
        DrawArc(origin, forward, range, angle, color);

        if (rayWidth > 0.01f)
            DrawCircle(origin + forward * range, rayWidth, new Color(1f, 0.1f, 0.1f, 0.9f));

        return true;
    }

    private void DrawArc(Vector3 origin, Vector3 forward, float radius, float angle, Color color)
    {
        float half = angle * 0.5f;
        Vector3 previous = origin + Quaternion.Euler(0f, -half, 0f) * forward * radius;

        for (int i = 1; i <= ArcSegments; i++)
        {
            float a = Mathf.Lerp(-half, half, i / (float)ArcSegments);
            Vector3 current = origin + Quaternion.Euler(0f, a, 0f) * forward * radius;
            DrawLine(previous, current, color);
            previous = current;
        }
    }

    private void DrawCircle(Vector3 center, float radius, Color color)
    {
        Vector3 previous = center + Vector3.right * radius;

        for (int i = 1; i <= CircleSegments; i++)
        {
            float a = i * Mathf.PI * 2f / CircleSegments;
            Vector3 current = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            DrawLine(previous, current, color);
            previous = current;
        }
    }

    private void DrawLine(Vector3 from, Vector3 to, Color color)
    {
        LineRenderer line = GetLine();
        line.gameObject.SetActive(true);
        line.positionCount = 2;
        line.startColor = color;
        line.endColor = color;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
    }

    private LineRenderer GetLine()
    {
        if (_lineIndex < _lines.Count)
            return _lines[_lineIndex++];

        GameObject go = new GameObject("WeaponHitboxLine");
        go.transform.SetParent(transform, false);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.material = _material;
        line.useWorldSpace = true;
        line.widthMultiplier = LineWidth;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sortingOrder = 5000;
        _lines.Add(line);
        _lineIndex++;
        return line;
    }

    private void HideAll()
    {
        for (int i = 0; i < _lines.Count; i++)
            _lines[i].gameObject.SetActive(false);
    }

    private static ItemDrop.ItemData GetCurrentWeapon(Humanoid humanoid)
    {
        if (humanoid == null)
            return null;

        MethodInfo method = FindMethod(humanoid.GetType(), "GetCurrentWeapon");

        if (method != null)
        {
            object value = method.Invoke(humanoid, null);

            if (value is ItemDrop.ItemData item)
                return item;
        }

        Inventory inventory = humanoid.GetInventory();

        if (inventory != null)
        {
            List<ItemDrop.ItemData> items = inventory.GetAllItems();

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].m_equipped)
                    return items[i];
            }
        }

        return null;
    }

    private static float GetFloat(object instance, string name, float fallback)
    {
        object value = GetMemberValue(instance, name);

        if (value is float f)
            return f;

        if (value is int i)
            return i;

        return fallback;
    }

    private static object GetMemberValue(object instance, string name)
    {
        if (instance == null)
            return null;

        Type type = instance.GetType();

        while (type != null)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field != null)
                return field.GetValue(instance);

            type = type.BaseType;
        }

        return null;
    }

    private static MethodInfo FindMethod(Type type, string name)
    {
        while (type != null)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method != null)
                return method;

            type = type.BaseType;
        }

        return null;
    }
}

[HarmonyPatch(typeof(Player), "Update")]
public static class PungusWeaponHitboxDebugPlayerUpdatePatch
{
    private static void Postfix()
    {
        if (PungusWeaponHitboxDebugOverlay.Instance != null)
            return;

        GameObject go = new GameObject("PungusWeaponHitboxDebugOverlay");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<PungusWeaponHitboxDebugOverlay>();
    }
}

[HarmonyPatch(typeof(Terminal), "InitTerminal")]
public static class PungusWeaponHitboxDebugCommandPatch
{
    private static bool _registered;

    private static void Postfix()
    {
        if (_registered)
            return;

        _registered = true;
        new Terminal.ConsoleCommand("ps_hitboxes", "Toggle weapon attack bounds. Usage: ps_hitboxes [on|off|all|player|primary|secondary|both]", Execute);
    }

    private static void Execute(Terminal.ConsoleEventArgs args)
    {
        if (args == null || args.Length <= 1)
        {
            PungusWeaponHitboxDebug.Enabled = !PungusWeaponHitboxDebug.Enabled;
            Write(args, "Weapon hitbox debug: " + (PungusWeaponHitboxDebug.Enabled ? "ON" : "OFF"));
            return;
        }

        for (int i = 1; i < args.Length; i++)
        {
            string option = args[i].ToLowerInvariant();

            if (option == "on")
                PungusWeaponHitboxDebug.Enabled = true;
            else if (option == "off")
                PungusWeaponHitboxDebug.Enabled = false;
            else if (option == "all")
                PungusWeaponHitboxDebug.ShowAllHumanoids = true;
            else if (option == "player")
                PungusWeaponHitboxDebug.ShowAllHumanoids = false;
            else if (option == "primary")
            {
                PungusWeaponHitboxDebug.ShowPrimary = true;
                PungusWeaponHitboxDebug.ShowSecondary = false;
            }
            else if (option == "secondary")
            {
                PungusWeaponHitboxDebug.ShowPrimary = false;
                PungusWeaponHitboxDebug.ShowSecondary = true;
            }
            else if (option == "both")
            {
                PungusWeaponHitboxDebug.ShowPrimary = true;
                PungusWeaponHitboxDebug.ShowSecondary = true;
            }
        }

        Write(args, "Weapon hitbox debug: " + (PungusWeaponHitboxDebug.Enabled ? "ON" : "OFF"));
    }

    private static void Write(Terminal.ConsoleEventArgs args, string text)
    {
        if (args != null && args.Context != null)
            args.Context.AddString(text);
    }
}
