using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class AgentBedNightStagger : MonoBehaviour
{
    private ZNetView _zNetView;
    private bool _wasNight;
    private float _nightDelay;
    private float _morningDelay;

    public bool NightReady { get; private set; }
    public bool MorningReady { get; private set; }

    private void Awake()
    {
        _zNetView = GetComponent<ZNetView>();
        _wasNight = EnvMan.IsNight();
        ResetNightDelay();
        ResetMorningDelay();
        NightReady = !_wasNight;
        MorningReady = _wasNight;
    }

    private void Update()
    {
        bool isNight = EnvMan.IsNight();

        if (isNight && !_wasNight)
        {
            ResetNightDelay();
            NightReady = false;
            MorningReady = false;
        }

        if (!isNight && _wasNight)
        {
            ResetMorningDelay();
            MorningReady = false;
            NightReady = false;
        }

        _wasNight = isNight;

        if (isNight && !NightReady)
        {
            _nightDelay -= Time.deltaTime;

            if (_nightDelay <= 0f)
                NightReady = true;
        }

        if (!isNight && !MorningReady)
        {
            _morningDelay -= Time.deltaTime;

            if (_morningDelay <= 0f)
                MorningReady = true;
        }
    }

    private void ResetNightDelay()
    {
        _nightDelay = GetStableDelay(0.35f, 6.0f, 17);
    }

    private void ResetMorningDelay()
    {
        _morningDelay = GetStableDelay(0.25f, 4.0f, 43);
    }

    private float GetStableDelay(float min, float max, int salt)
    {
        int seed = salt;

        if (_zNetView != null && _zNetView.IsValid())
        {
            ZDO zdo = _zNetView.GetZDO();

            if (zdo != null)
                seed ^= zdo.m_uid.GetHashCode();
        }
        else
        {
            seed ^= gameObject.GetInstanceID();
        }

        float t = Mathf.Abs(seed % 10000) / 10000f;
        return Mathf.Lerp(min, max, t);
    }
}

public static class AgentBedRegistry
{
    private static readonly List<Bed> Beds = new List<Bed>();
    private static float _nextRefreshTime;
    private const float RefreshInterval = 5f;

    public static Bed FindBedNear(Vector3 position, float radius)
    {
        RefreshIfNeeded();

        Bed best = null;
        float bestDistance = radius;

        for (int i = Beds.Count - 1; i >= 0; i--)
        {
            Bed bed = Beds[i];

            if (bed == null || bed.gameObject == null)
            {
                Beds.RemoveAt(i);
                continue;
            }

            float distance = Vector3.Distance(bed.transform.position, position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = bed;
            }
        }

        return best;
    }

    public static void RefreshNow()
    {
        Beds.Clear();
        Beds.AddRange(UnityEngine.Object.FindObjectsByType<Bed>(FindObjectsSortMode.None));
        _nextRefreshTime = Time.time + RefreshInterval;
    }

    private static void RefreshIfNeeded()
    {
        if (Time.time < _nextRefreshTime && Beds.Count > 0)
            return;

        RefreshNow();
    }
}

[HarmonyPatch(typeof(AgentComponent), "Awake")]
public static class AgentBedPerformanceInstallerPatch
{
    private static void Postfix(AgentComponent __instance)
    {
        if (__instance == null)
            return;

        if (__instance.GetComponent<AgentBedNightStagger>() == null)
            __instance.gameObject.AddComponent<AgentBedNightStagger>();
    }
}

[HarmonyPatch(typeof(AgentBehaviourController), "ShouldSleepAtAssignedBed")]
public static class AgentBedSleepStaggerPatch
{
    private static bool Prefix(AgentBehaviourController __instance, ref bool __result)
    {
        if (__instance == null || !EnvMan.IsNight())
            return true;

        AgentBedNightStagger stagger = __instance.GetComponent<AgentBedNightStagger>();

        if (stagger == null || stagger.NightReady)
            return true;

        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(AgentBehaviourController), "FindBedNearPosition")]
public static class AgentBehaviourBedLookupPatch
{
    private static bool Prefix(Vector3 position, ref Bed __result)
    {
        __result = AgentBedRegistry.FindBedNear(position, 10f);
        return false;
    }
}

[HarmonyPatch(typeof(AgentBedExitAssist), "FindBedNearPosition")]
public static class AgentBedExitBedLookupPatch
{
    private static bool Prefix(Vector3 position, ref Bed __result)
    {
        __result = AgentBedRegistry.FindBedNear(position, 10f);
        return false;
    }
}

[HarmonyPatch(typeof(AgentBedExitAssist), "Update")]
public static class AgentBedExitStaggerPatch
{
    private static bool Prefix(AgentBedExitAssist __instance)
    {
        if (__instance == null || EnvMan.IsNight())
            return true;

        AgentBedNightStagger stagger = __instance.GetComponent<AgentBedNightStagger>();

        if (stagger == null || stagger.MorningReady)
            return true;

        return false;
    }
}

[HarmonyPatch(typeof(AgentBehaviourController), "PlaySleepAnimation")]
public static class AgentSleepParameterDrivenPatch
{
    private static bool Prefix(AgentBehaviourController __instance)
    {
        if (__instance == null)
            return false;

        Animator animator = GetAnimator(__instance);

        if (animator == null)
            return false;

        SetField(__instance, "_savedAnimatorSpeed", animator.speed);
        animator.speed = 1f;
        SetBool(animator, "sleeping", true);
        SetTriggerIfPresent(animator, "sleep_enter");
        SetTriggerIfPresent(animator, "SleepEnter");
        SetField(__instance, "_isPlayingBedSleepAnimation", true);
        return false;
    }

    private static Animator GetAnimator(AgentBehaviourController controller)
    {
        Animator animator = GetField<Animator>(controller, "_animator");

        if (animator == null)
        {
            animator = controller.GetComponentInChildren<Animator>();

            if (animator != null)
                SetField(controller, "_animator", animator);
        }

        return animator;
    }

    private static void SetBool(Animator animator, string name, bool value)
    {
        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];

            if (parameter.name == name && parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameter.nameHash, value);
                return;
            }
        }
    }

    private static void SetTriggerIfPresent(Animator animator, string name)
    {
        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];

            if (parameter.name == name && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(parameter.nameHash);
                animator.SetTrigger(parameter.nameHash);
                return;
            }
        }
    }

    private static T GetField<T>(object instance, string name) where T : class
    {
        FieldInfo field = FindField(instance.GetType(), name);
        return field != null ? field.GetValue(instance) as T : null;
    }

    private static void SetField(object instance, string name, object value)
    {
        FieldInfo field = FindField(instance.GetType(), name);

        if (field != null)
            field.SetValue(instance, value);
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

[HarmonyPatch(typeof(AgentBehaviourController), "StartBedWakeup")]
public static class AgentWakeParameterDrivenPatch
{
    private static bool Prefix(AgentBehaviourController __instance)
    {
        if (__instance == null)
            return false;

        StopMoving(__instance);
        Character character = GetField<Character>(__instance, "_character");

        if (character != null)
            character.SetMoveDir(Vector3.zero);

        Animator animator = GetAnimator(__instance);

        if (animator != null)
        {
            float savedSpeed = GetFieldValue<float>(__instance, "_savedAnimatorSpeed", 1f);
            animator.speed = savedSpeed <= 0f ? 1f : savedSpeed;
            SetBool(animator, "sleeping", false);
            SetTriggerIfPresent(animator, "wakeup");
            SetTriggerIfPresent(animator, "WakeUp");
        }

        SetField(__instance, "_isPlayingBedSleepAnimation", false);
        SetField(__instance, "_isWakingFromBed", true);
        SetField(__instance, "_bedWakeLockTimer", 2.2f);
        return false;
    }

    private static void StopMoving(AgentBehaviourController controller)
    {
        MethodInfo method = FindMethod(typeof(AgentBehaviourController), "StopMoving");

        if (method != null)
            method.Invoke(controller, null);
    }

    private static Animator GetAnimator(AgentBehaviourController controller)
    {
        Animator animator = GetField<Animator>(controller, "_animator");

        if (animator == null)
        {
            animator = controller.GetComponentInChildren<Animator>();

            if (animator != null)
                SetField(controller, "_animator", animator);
        }

        return animator;
    }

    private static void SetBool(Animator animator, string name, bool value)
    {
        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];

            if (parameter.name == name && parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameter.nameHash, value);
                return;
            }
        }
    }

    private static void SetTriggerIfPresent(Animator animator, string name)
    {
        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];

            if (parameter.name == name && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(parameter.nameHash);
                animator.SetTrigger(parameter.nameHash);
                return;
            }
        }
    }

    private static T GetField<T>(object instance, string name) where T : class
    {
        FieldInfo field = FindField(instance.GetType(), name);
        return field != null ? field.GetValue(instance) as T : null;
    }

    private static T GetFieldValue<T>(object instance, string name, T fallback)
    {
        FieldInfo field = FindField(instance.GetType(), name);

        if (field == null)
            return fallback;

        object value = field.GetValue(instance);

        if (value is T typed)
            return typed;

        return fallback;
    }

    private static void SetField(object instance, string name, object value)
    {
        FieldInfo field = FindField(instance.GetType(), name);

        if (field != null)
            field.SetValue(instance, value);
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
