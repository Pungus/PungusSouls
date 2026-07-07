using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentDeferredModuleStartup : MonoBehaviour
{
    private static int _spawnBudgetFrame;
    private static int _spawnBudgetCount;
    private const int MaxHeavyStartsPerFrame = 1;

    public IEnumerator StartDeferred(System.Action action)
    {
        yield return null;
        yield return null;

        while (!TryConsumeBudget())
            yield return null;

        action?.Invoke();
    }

    private static bool TryConsumeBudget()
    {
        if (_spawnBudgetFrame != Time.frameCount)
        {
            _spawnBudgetFrame = Time.frameCount;
            _spawnBudgetCount = 0;
        }

        if (_spawnBudgetCount >= MaxHeavyStartsPerFrame)
            return false;

        _spawnBudgetCount++;
        return true;
    }
}

[HarmonyPatch(typeof(AgentRested), "Start")]
public static class AgentRestedStartDelayPatch
{
    private static bool Prefix(AgentRested __instance)
    {
        if (__instance == null)
            return false;

        AgentDeferredModuleStartup startup = __instance.GetComponent<AgentDeferredModuleStartup>();

        if (startup == null)
            startup = __instance.gameObject.AddComponent<AgentDeferredModuleStartup>();

        startup.StartCoroutine(startup.StartDeferred(() => InvokePrivate(__instance, "TryInit")));
        return false;
    }

    private static void InvokePrivate(object instance, string methodName)
    {
        System.Reflection.MethodInfo method = instance.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (method != null)
            method.Invoke(instance, null);
    }
}
