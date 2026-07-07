using BepInEx;
using System.IO;
using UnityEngine;

public static class AgentIconLoader
{
    private static Sprite _agentMarker;
    private static Sprite _deadMarker;
    private static Sprite _hudIcon;
    private static bool _loaded;

    public static Sprite AgentMarker
    {
        get
        {
            EnsureLoaded();
            return _agentMarker;
        }
    }

    public static Sprite DeadMarker
    {
        get
        {
            EnsureLoaded();
            return _deadMarker;
        }
    }

    public static Sprite HudIcon
    {
        get
        {
            EnsureLoaded();
            return _hudIcon;
        }
    }

    public static void EnsureLoaded()
    {
        if (_loaded)
            return;

        _loaded = true;
        _agentMarker = LoadSprite("agent_marker", "agent_marker.png", new Color(0.95f, 0.75f, 0.25f, 1f));
        _deadMarker = LoadSprite("agent_marker_dead", "agent_marker_dead.png", new Color(0.45f, 0.45f, 0.45f, 1f));
        _hudIcon = LoadSprite("agent_hud_icon", "agent_hud_icon.png", new Color(0.95f, 0.75f, 0.25f, 1f));
    }

    public static void Reload()
    {
        _loaded = false;
        _agentMarker = null;
        _deadMarker = null;
        _hudIcon = null;
        EnsureLoaded();
    }

    private static Sprite LoadSprite(string assetName, string fileName, Color fallbackColor)
    {
        Sprite sprite = LoadSpriteFromBundle(assetName);

        if (sprite != null)
            return sprite;

        sprite = LoadSpriteFromFile(fileName);

        if (sprite != null)
            return sprite;

        return CreateFallbackSprite(fallbackColor);
    }

    private static Sprite LoadSpriteFromBundle(string assetName)
    {
        try
        {
            if (PungusSouls.PungusSoulsPlugin.assetBundle == null)
                return null;

            Sprite sprite = PungusSouls.PungusSoulsPlugin.assetBundle.LoadAsset<Sprite>(assetName);

            if (sprite != null)
                return sprite;

            Texture2D texture = PungusSouls.PungusSoulsPlugin.assetBundle.LoadAsset<Texture2D>(assetName);

            if (texture == null)
                return null;

            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
        catch
        {
            return null;
        }
    }

    private static Sprite LoadSpriteFromFile(string fileName)
    {
        try
        {
            string[] paths =
            {
                Path.Combine(Paths.PluginPath, "PungusSouls", "assets", fileName),
                Path.Combine(Paths.PluginPath, "PungusSouls", fileName),
                Path.Combine(Paths.PluginPath, fileName)
            };

            string path = null;

            for (int i = 0; i < paths.Length; i++)
            {
                if (File.Exists(paths[i]))
                {
                    path = paths[i];
                    break;
                }
            }

            if (string.IsNullOrEmpty(path))
                return null;

            byte[] data = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (!texture.LoadImage(data))
                return null;

            texture.name = Path.GetFileNameWithoutExtension(fileName);
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
        catch
        {
            return null;
        }
    }

    private static Sprite CreateFallbackSprite(Color color)
    {
        Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Vector2 center = new Vector2(15.5f, 15.5f);

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                bool outer = distance <= 14f;
                bool inner = distance <= 9f;
                texture.SetPixel(x, y, outer ? (inner ? color : new Color(0f, 0f, 0f, 0.9f)) : clear);
            }
        }

        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(texture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f), 100f);
    }
}
