using HarmonyLib;
using System.Reflection;
using UnityEngine;

[HarmonyPatch(typeof(AgentRested), "IsFireplaceActive")]
public static class AgentRestedFireplaceSafetyPatch
{
    private static bool Prefix(Fireplace fireplace, ref bool __result)
    {
        __result = false;

        if (fireplace == null || fireplace.gameObject == null || !fireplace.isActiveAndEnabled)
            return false;

        ZNetView view = fireplace.GetComponent<ZNetView>();

        if (view != null && !view.IsValid())
            return false;

        try
        {
            MethodInfo method = FindMethod(fireplace.GetType(), "IsBurning");

            if (method != null && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
            {
                __result = (bool)method.Invoke(fireplace, null);
                return false;
            }

            FieldInfo enabledField = FindField(fireplace.GetType(), "m_enabled");

            if (enabledField != null && enabledField.FieldType == typeof(bool))
            {
                __result = (bool)enabledField.GetValue(fireplace);
                return false;
            }

            __result = true;
            return false;
        }
        catch
        {
            __result = false;
            return false;
        }
    }

    private static MethodInfo FindMethod(System.Type type, string name)
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
