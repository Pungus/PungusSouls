using HarmonyLib;
using System.Reflection;
using UnityEngine;

[HarmonyPatch(typeof(AgentBehaviourController), "MoveDirectlyToward")]
public static class AgentMovementDirectFallbackPatch
{
    private static bool Prefix(AgentBehaviourController __instance, Vector3 point, bool run)
    {
        if (__instance == null)
            return false;

        Character character = GetField<Character>(__instance, "_character");
        Humanoid humanoid = GetField<Humanoid>(__instance, "_humanoid");

        if (character == null)
            return false;

        Vector3 direction = point - __instance.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return false;

        direction.Normalize();
        character.SetMoveDir(direction);

        if (humanoid != null)
            humanoid.SetRun(run);

        return false;
    }

    private static T GetField<T>(object instance, string name) where T : class
    {
        FieldInfo field = FindField(instance.GetType(), name);
        return field != null ? field.GetValue(instance) as T : null;
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
