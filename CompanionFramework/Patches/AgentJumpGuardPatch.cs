using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class AgentJumpUtility
{
    private static readonly int[] JumpStateHashes =
    {
        Animator.StringToHash("jump"),
        Animator.StringToHash("Jump"),
        Animator.StringToHash("jump_start"),
        Animator.StringToHash("JumpStart"),
        Animator.StringToHash("jumping"),
        Animator.StringToHash("Jumping")
    };

    public static bool CanUseJump(Component component)
    {
        if (component == null)
            return false;

        AgentComponent agent = component.GetComponent<AgentComponent>();


        if (agent == null)
            return true;

        AgentPrefabProfile profile = component.GetComponent<AgentPrefabProfile>();

        if (profile != null && !profile.CanJump)
            return false;

        Animator animator = component.GetComponentInChildren<Animator>();

        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        for (int layer = 0; layer < animator.layerCount; layer++)
        {
            for (int i = 0; i < JumpStateHashes.Length; i++)
            {
                if (animator.HasState(layer, JumpStateHashes[i]))
                    return true;
            }
        }

        return HasJumpParameter(animator);
    }

    private static bool HasJumpParameter(Animator animator)
    {
        if (animator == null)
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];

            if (parameter == null)
                continue;

            string name = parameter.name;

            if (string.IsNullOrEmpty(name))
                continue;

            if (name == "jump" || name == "Jump" || name == "jumping" || name == "Jumping")
                return true;
        }

        return false;
    }
}

[HarmonyPatch]
public static class AgentNativeJumpGuardPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        MethodInfo[] methods = typeof(Character).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];

            if (method != null && method.Name == "Jump")
                yield return method;
        }
    }

    private static bool Prefix(Character __instance)
    {
        if (__instance == null)
            return true;

        AgentComponent agent = __instance.GetComponent<AgentComponent>();

        if (agent == null)
            return true;

        AgentPrefabProfile profile = __instance.GetComponent<AgentPrefabProfile>();

        if (__instance.name.IndexOf("Sif", System.StringComparison.OrdinalIgnoreCase) >= 0)
            Debug.Log("[SifJumpCheck] Character.Jump called on " + __instance.name + " CanJump=" + (profile != null ? profile.CanJump.ToString() : "no profile"));

        if (profile != null && !profile.CanJump)
            return false;
        return AgentJumpUtility.CanUseJump(__instance);
    }
}