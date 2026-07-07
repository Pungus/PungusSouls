using System;
using TMPro;
using UnityEngine;

namespace Integration
{
    public static class AgentTMPFont
    {
        private static TMP_FontAsset _cachedTmpFont;

        public static TMP_FontAsset Get()
        {
            if (_cachedTmpFont != null && !IsBroken(_cachedTmpFont))
                return _cachedTmpFont;

            TMP_FontAsset existing = FindExisting();

            if (existing != null && !IsBroken(existing))
            {
                _cachedTmpFont = existing;
                TMP_Settings.defaultFontAsset = _cachedTmpFont;
                return _cachedTmpFont;
            }

            return null;
        }

        public static void EnsureDefault()
        {
            TMP_FontAsset font = Get();

            if (font != null && !IsBroken(font))
                TMP_Settings.defaultFontAsset = font;
        }

        public static TextMeshProUGUI AddText(GameObject go)
        {
            TMP_FontAsset font = Get();

            if (font != null && !IsBroken(font))
                TMP_Settings.defaultFontAsset = font;

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            Apply(text);
            return text;
        }

        public static void Apply(TMP_Text text)
        {
            if (text == null)
                return;

            TMP_FontAsset font = Get();

            if (font != null && !IsBroken(font))
                text.font = font;
        }

        public static bool IsBroken(TMP_FontAsset font)
        {
            if (font == null)
                return true;

            return font.name.IndexOf("LiberationSans", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static TMP_FontAsset FindExisting()
        {
            InventoryGui inventoryGui = InventoryGui.instance;

            if (inventoryGui != null)
            {
                TMP_Text[] inventoryTexts = inventoryGui.GetComponentsInChildren<TMP_Text>(true);

                foreach (TMP_Text text in inventoryTexts)
                {
                    if (text != null && text.font != null && !IsBroken(text.font))
                        return text.font;
                }
            }

            Hud hud = Hud.instance;

            if (hud != null)
            {
                TMP_Text[] hudTexts = hud.GetComponentsInChildren<TMP_Text>(true);

                foreach (TMP_Text text in hudTexts)
                {
                    if (text != null && text.font != null && !IsBroken(text.font))
                        return text.font;
                }
            }

            TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();

            foreach (TMP_FontAsset font in fonts)
            {
                if (font != null && !IsBroken(font))
                    return font;
            }

            return null;
        }
    }
}
