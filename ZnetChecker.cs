using BepInEx;
using UnityEngine;

[BepInPlugin("debug.znet.dump", "ZNet Dump", "1.0")]
public class ZNetDump : BaseUnityPlugin
{
    private void Awake()
    {
        Logger.LogInfo("Plugin loaded");
        Invoke(nameof(Dump), 5f);
    }

    private void Dump()
    {
        Logger.LogInfo("Dump running");

        if (ZNetScene.instance == null)
        {
            Logger.LogWarning("ZNetScene is NULL");
            return;
        }

        foreach (var prefab in ZNetScene.instance.m_prefabs)
        {
            Logger.LogInfo("REGISTERED: " + prefab.name);
        }
    }
}