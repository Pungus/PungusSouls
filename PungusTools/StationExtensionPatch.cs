using System;
using System.Reflection;
using UnityEngine;

public class ConditionalStationExtensionController : MonoBehaviour
{
    public GameObject conditionalObject;
    public GameObject extensionHost;
    public StationExtension templateExtension;
    public float checkInterval = 0.25f;

    private StationExtension runtimeExtension;
    private bool lastVisible;
    private float nextCheck;

    private void Awake()
    {
        if (extensionHost == null)
        {
            extensionHost = gameObject;
        }

        Sync(true);
    }

    private void Update()
    {
        if (Time.time < nextCheck)
        {
            return;
        }

        nextCheck = Time.time + checkInterval;
        Sync(false);
    }

    private void OnDestroy()
    {
        RemoveRuntimeExtension();
    }

    private void Sync(bool force)
    {
        if (conditionalObject == null || templateExtension == null || extensionHost == null)
        {
            return;
        }

        bool visible = conditionalObject.activeInHierarchy;

        if (!force && visible == lastVisible)
        {
            return;
        }

        lastVisible = visible;

        if (visible)
        {
            AddRuntimeExtension();
        }
        else
        {
            RemoveRuntimeExtension();
        }
    }

    private void AddRuntimeExtension()
    {
        if (runtimeExtension != null)
        {
            return;
        }

        if (!extensionHost.activeInHierarchy)
        {
            return;
        }

        runtimeExtension = extensionHost.AddComponent<StationExtension>();
        CopyStationExtensionFields(templateExtension, runtimeExtension);
        runtimeExtension.enabled = false;
        runtimeExtension.enabled = true;
    }

    private void RemoveRuntimeExtension()
    {
        if (runtimeExtension == null)
        {
            return;
        }

        Destroy(runtimeExtension);
        runtimeExtension = null;
    }

    private static void CopyStationExtensionFields(StationExtension source, StationExtension target)
    {
        FieldInfo[] fields = typeof(StationExtension).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (FieldInfo field in fields)
        {
            if (field.IsInitOnly || field.IsLiteral)
            {
                continue;
            }

            field.SetValue(target, field.GetValue(source));
        }
    }
}