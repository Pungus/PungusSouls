// BlacksmithTools, Version=2.0.3.0, Culture=neutral, PublicKeyToken=null
// BlacksmithTools.BoneReorder
using System;
using System.Collections.Generic;
using BepInEx.Logging;
using BlacksmithTools;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class BoneReorder
{
	[HarmonyPatch(typeof(VisEquipment), "AttachItem")]
	[HarmonyPostfix]
	private static void AttachItemPatch(VisEquipment __instance, GameObject __result, int itemHash)
	{
		if (Main.reorderEnabled.Value && !(__result == null) && __result.name.StartsWith("attach_skin") && ObjectDB.instance.GetItemPrefab(itemHash) != null)
		{
			SetSMRBones(__instance, __result, itemHash);
		}
	}

	[HarmonyPatch(typeof(VisEquipment), "AttachArmor")]
	[HarmonyPostfix]
	private static void AttachArmorPatch(VisEquipment __instance, List<GameObject> __result, int itemHash)
	{
		if (!Main.reorderEnabled.Value)
		{
			return;
		}
		foreach (GameObject item in __result)
		{
			if (item.name.StartsWith("attach_skin"))
			{
				SetSMRBones(__instance, item, itemHash);
			}
		}
	}

	public static void SetSMRBones(VisEquipment ve, GameObject instance, int hash)
	{
		Util.LogMessage("Reordering bones");
		try
		{
			SkinnedMeshRenderer componentInChildren = instance.GetComponentInChildren<SkinnedMeshRenderer>();
			SkinnedMeshRenderer[] componentsInChildren = instance.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
			SkinnedMeshRenderer[] array = componentsInChildren;
			foreach (SkinnedMeshRenderer smr in array)
			{
				SetBones(smr, GetBoneNames(componentInChildren), ve.m_bodyModel.rootBone);
			}
		}
		catch (Exception ex)
		{
			Util.LogMessage(ex.Message, LogLevel.Error);
		}
	}

	public static string[] GetBoneNames(SkinnedMeshRenderer smr)
	{
		List<string> list = new List<string>();
		Transform[] bones = smr.bones;
		foreach (Transform transform in bones)
		{
			list.Add(transform.name);
		}
		return list.ToArray();
	}

	public static void SetBones(SkinnedMeshRenderer smr, string[] boneNames, Transform skeletonRoot)
	{
		Transform[] array = new Transform[smr.bones.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = Util.FindInChildren(skeletonRoot, boneNames[i]);
		}
		smr.bones = array;
		smr.rootBone = skeletonRoot;
	}
}
