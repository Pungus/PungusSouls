// BlacksmithTools, Version=2.0.3.0, Culture=neutral, PublicKeyToken=null
// BlacksmithTools.Main
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

public class Main : BaseUnityPlugin
{
	public static ManualLogSource log;

	public static Harmony harmony;

	public static Assembly assembly;

	public static string modFolder;

	public static ConfigFile configFile;

	public static ConfigEntry<bool> reorderEnabled;

	public static ConfigEntry<bool> bodyHidingEnabled;

	public static ConfigEntry<bool> loggingEnabled;
	public void Start()
	{
		harmony.PatchAll(assembly);
	}

	public void Awake()
	{
		configFile = base.Config;
		reorderEnabled = configFile.Bind("bone reorder", "enabled", defaultValue: true, new ConfigDescription("", null));
		bodyHidingEnabled = configFile.Bind("bodypart hiding", "enabled", defaultValue: true, new ConfigDescription("", null));
		loggingEnabled = configFile.Bind("logging", "enabled", defaultValue: false, new ConfigDescription("", null));
		if (bodyHidingEnabled.Value)
		{
			BodypartSystem.BindConfigs();
		}
	}
}
