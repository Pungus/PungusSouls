using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

public sealed class AgentNpcIconSet
{
    public Sprite HudIcon;
    public Sprite MapIcon;
    public Sprite DeadMapIcon;
}

public static class AgentNpcIconRegistry
{
    private static readonly Dictionary<string, AgentNpcIconSet> IconsById = new Dictionary<string, AgentNpcIconSet>(StringComparer.OrdinalIgnoreCase);
    private static bool _loaded;

    public static void Register(string id, Sprite hudIcon = null, Sprite mapIcon = null, Sprite deadMapIcon = null)
    {
        id = Normalize(id);

        if (string.IsNullOrEmpty(id))
            return;

        if (!IconsById.TryGetValue(id, out AgentNpcIconSet set))
        {
            set = new AgentNpcIconSet();
            IconsById[id] = set;
        }

        if (hudIcon != null)
            set.HudIcon = hudIcon;

        if (mapIcon != null)
            set.MapIcon = mapIcon;

        if (deadMapIcon != null)
            set.DeadMapIcon = deadMapIcon;
    }

    public static void RegisterEmbedded(string id, Assembly assembly, string hudResource = null, string mapResource = null, string deadResource = null)
    {
        if (assembly == null)
            assembly = Assembly.GetExecutingAssembly();

        Register(
            id,
            string.IsNullOrEmpty(hudResource) ? null : LoadSpriteFromEmbeddedResource(assembly, hudResource),
            string.IsNullOrEmpty(mapResource) ? null : LoadSpriteFromEmbeddedResource(assembly, mapResource),
            string.IsNullOrEmpty(deadResource) ? null : LoadSpriteFromEmbeddedResource(assembly, deadResource)
        );
    }

    public static Sprite GetHudIcon(AgentComponent agent)
    {
        AgentNpcIconSet set = GetIconSet(agent);
        return set != null && set.HudIcon != null ? set.HudIcon : AgentIconLoader.HudIcon;
    }

    public static Sprite GetMapIcon(AgentComponent agent, bool dead)
    {
        AgentNpcIconSet set = GetIconSet(agent);

        if (set != null)
        {
            if (dead && set.DeadMapIcon != null)
                return set.DeadMapIcon;

            if (set.MapIcon != null)
                return set.MapIcon;
        }

        return dead ? AgentIconLoader.DeadMarker : AgentIconLoader.AgentMarker;
    }

    public static AgentNpcIconSet GetIconSet(AgentComponent agent)
    {
        EnsureLoaded();

        foreach (string id in GetCandidateIds(agent))
        {
            if (IconsById.TryGetValue(id, out AgentNpcIconSet set))
                return set;
        }

        return null;
    }

    public static void EnsureLoaded()
    {
        if (_loaded)
            return;

        _loaded = true;
        LoadEmbeddedIcons(Assembly.GetExecutingAssembly());
        LoadFileIcons();
    }

    public static void Reload()
    {
        IconsById.Clear();
        _loaded = false;
        EnsureLoaded();
    }

    private static IEnumerable<string> GetCandidateIds(AgentComponent agent)
    {
        if (agent == null)
            yield break;

        AgentPrefabProfile profile = agent.GetComponent<AgentPrefabProfile>();

        if (profile != null)
        {
            if (!string.IsNullOrEmpty(profile.IconId))
                yield return Normalize(profile.IconId);

            if (!string.IsNullOrEmpty(profile.AgentId))
                yield return Normalize(profile.AgentId);
        }

        Character character = agent.GetComponent<Character>();

        if (character != null && !string.IsNullOrEmpty(character.m_name))
            yield return Normalize(character.m_name);

        if (agent.gameObject != null)
            yield return Normalize(agent.gameObject.name);
    }

    private static void LoadEmbeddedIcons(Assembly assembly)
    {
        if (assembly == null)
            return;

        string[] resourceNames;

        try
        {
            resourceNames = assembly.GetManifestResourceNames();
        }
        catch
        {
            return;
        }

        for (int i = 0; i < resourceNames.Length; i++)
        {
            string resourceName = resourceNames[i];

            if (string.IsNullOrEmpty(resourceName) || !resourceName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                continue;

            string iconName = GetEmbeddedIconFileNameWithoutExtension(resourceName);

            if (string.IsNullOrEmpty(iconName))
                continue;

            RegisterIconByName(iconName, LoadSpriteFromEmbeddedResource(assembly, resourceName));
        }
    }

    private static void LoadFileIcons()
    {
        foreach (string folder in GetIconFolders())
            LoadIconsFromFolder(folder);
    }

    private static IEnumerable<string> GetIconFolders()
    {
        string assemblyDirectory = GetAssemblyDirectory();

        if (!string.IsNullOrEmpty(assemblyDirectory))
        {
            yield return Path.Combine(assemblyDirectory, "assets", "icons");
            yield return Path.Combine(assemblyDirectory, "Assets", "Icons");
            yield return Path.Combine(assemblyDirectory, "icons");
        }

        yield return Path.Combine(Paths.PluginPath, "PungusSouls", "assets", "icons");
        yield return Path.Combine(Paths.PluginPath, "PungusSouls", "Assets", "Icons");
        yield return Path.Combine(Paths.PluginPath, "PungusSouls", "icons");
    }

    private static void LoadIconsFromFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            return;

        string[] files = Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories);

        for (int i = 0; i < files.Length; i++)
        {
            string iconName = Path.GetFileNameWithoutExtension(files[i]);
            RegisterIconByName(iconName, LoadSpriteFromFile(files[i]));
        }
    }

    private static void RegisterIconByName(string iconName, Sprite sprite)
    {
        if (string.IsNullOrEmpty(iconName) || sprite == null)
            return;

        string id = iconName;
        IconKind kind = IconKind.Map;

        if (iconName.EndsWith("_hud", StringComparison.OrdinalIgnoreCase))
        {
            id = iconName.Substring(0, iconName.Length - 4);
            kind = IconKind.Hud;
        }
        else if (iconName.EndsWith("_map", StringComparison.OrdinalIgnoreCase))
        {
            id = iconName.Substring(0, iconName.Length - 4);
            kind = IconKind.Map;
        }
        else if (iconName.EndsWith("_dead", StringComparison.OrdinalIgnoreCase))
        {
            id = iconName.Substring(0, iconName.Length - 5);
            kind = IconKind.DeadMap;
        }

        id = Normalize(id);

        if (!IconsById.TryGetValue(id, out AgentNpcIconSet set))
        {
            set = new AgentNpcIconSet();
            IconsById[id] = set;
        }

        if (kind == IconKind.Hud)
            set.HudIcon = sprite;
        else if (kind == IconKind.DeadMap)
            set.DeadMapIcon = sprite;
        else
            set.MapIcon = sprite;
    }

    private static Sprite LoadSpriteFromEmbeddedResource(Assembly assembly, string resourceName)
    {
        if (assembly == null || string.IsNullOrEmpty(resourceName))
            return null;

        try
        {
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return null;

                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                return CreateSprite(data, GetEmbeddedIconFileNameWithoutExtension(resourceName));
            }
        }
        catch
        {
            return null;
        }
    }

    private static Sprite LoadSpriteFromFile(string path)
    {
        try
        {
            byte[] data = File.ReadAllBytes(path);
            return CreateSprite(data, Path.GetFileNameWithoutExtension(path));
        }
        catch
        {
            return null;
        }
    }

    private static Sprite CreateSprite(byte[] data, string name)
    {
        if (data == null || data.Length == 0)
            return null;

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!texture.LoadImage(data))
            return null;

        texture.name = string.IsNullOrEmpty(name) ? "agent_icon" : name;
        texture.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static string GetEmbeddedIconFileNameWithoutExtension(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName))
            return string.Empty;

        string value = resourceName;

        if (value.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            value = value.Substring(0, value.Length - 4);

        int slash = Math.Max(value.LastIndexOf('/'), value.LastIndexOf('\\'));

        if (slash >= 0 && slash < value.Length - 1)
            value = value.Substring(slash + 1);

        int dot = value.LastIndexOf('.');

        if (dot >= 0 && dot < value.Length - 1)
            value = value.Substring(dot + 1);

        return value;
    }

    private static string GetAssemblyDirectory()
    {
        try
        {
            string location = Assembly.GetExecutingAssembly().Location;

            if (string.IsNullOrEmpty(location))
                return string.Empty;

            return Path.GetDirectoryName(location);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        value = value.Replace("(Clone)", string.Empty).Trim();

        if (value.StartsWith("$ps_", StringComparison.OrdinalIgnoreCase))
            value = value.Substring(4);

        if (value.StartsWith("PS_", StringComparison.OrdinalIgnoreCase))
            value = value.Substring(3);

        value = value.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
        return value.ToLowerInvariant();
    }

    private enum IconKind
    {
        Hud,
        Map,
        DeadMap
    }
}
