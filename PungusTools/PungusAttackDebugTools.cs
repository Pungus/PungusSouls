using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

public static class PungusAttackDebugTools
{
    public static void DumpAttackData(string prefabName)
    {
        GameObject prefab = FindItemPrefab(prefabName);

        if (prefab == null)
        {
            Debug.LogWarning("[AttackDump] Could not find item prefab: " + prefabName);
            return;
        }

        ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();

        if (itemDrop == null || itemDrop.m_itemData == null || itemDrop.m_itemData.m_shared == null)
        {
            Debug.LogWarning("[AttackDump] Missing ItemDrop/shared data for " + prefab.name);
            return;
        }

        object shared = itemDrop.m_itemData.m_shared;
        DumpAttack(prefab.name, "primary", GetMemberValue(shared, "m_attack"));
        DumpAttack(prefab.name, "secondary", GetMemberValue(shared, "m_secondaryAttack"));
    }

    private static void DumpAttack(string prefabName, string label, object attack)
    {
        if (attack == null)
        {
            //Debug.Log("[AttackDump] " + prefabName + " " + label + " attack=null");
            return;
        }
    }

    private static GameObject FindItemPrefab(string prefabName)
    {
        prefabName = CleanName(prefabName);

        if (string.IsNullOrEmpty(prefabName))
            return null;

        if (ObjectDB.instance != null && ObjectDB.instance.m_items != null)
        {
            for (int i = 0; i < ObjectDB.instance.m_items.Count; i++)
            {
                GameObject item = ObjectDB.instance.m_items[i];

                if (item != null && string.Equals(CleanName(item.name), prefabName, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
        }

        if (ZNetScene.instance != null)
            return ZNetScene.instance.GetPrefab(prefabName);

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
        if (instance == null || string.IsNullOrEmpty(name))
            return null;

        Type type = instance.GetType();

        while (type != null)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field != null)
                return field.GetValue(instance);

            PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (property != null && property.GetIndexParameters().Length == 0)
                return property.GetValue(instance, null);

            type = type.BaseType;
        }

        return null;
    }

    private static string CleanName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        int cloneIndex = value.IndexOf("(Clone)", StringComparison.OrdinalIgnoreCase);

        if (cloneIndex >= 0)
            value = value.Substring(0, cloneIndex);

        return value.Trim();
    }
}

[HarmonyPatch(typeof(Terminal), "InitTerminal")]
public static class PungusAttackDebugCommandPatch
{
    private static bool _registered;

    private static void Postfix()
    {
        if (_registered)
            return;

        _registered = true;
        new Terminal.ConsoleCommand("ps_dumpattack", "Dump attack data for an item prefab. Usage: ps_dumpattack dragontooth", Execute);
    }

    private static void Execute(Terminal.ConsoleEventArgs args)
    {
        if (args == null || args.Length < 2)
        {
            Write(args, "Usage: ps_dumpattack <itemPrefabName>");
            return;
        }

        PungusAttackDebugTools.DumpAttackData(args[1]);
        Write(args, "Dumped attack data for " + args[1] + " to log.");
    }

    private static void Write(Terminal.ConsoleEventArgs args, string text)
    {
        if (args != null && args.Context != null)
            args.Context.AddString(text);
    }
}
