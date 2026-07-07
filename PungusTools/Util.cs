using BepInEx.Logging;
using HarmonyLib;
using PungusSouls;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class Util
{
	public static Transform FindInChildrenWc(Transform trans, string name)
	{
		if (trans.name.ToLower().Contains(name.ToLower()))
		{
			return trans;
		}
		for (int i = 0; i < trans.childCount; i++)
		{
			Transform transform = FindInChildrenWc(trans.GetChild(i), name);
			if (transform != null)
			{
				return transform;
			}
		}
		return null;
	}

	public static Transform FindInChildren(Transform trans, string name)
	{
		if (trans.name == name)
		{
			return trans;
		}
		for (int i = 0; i < trans.childCount; i++)
		{
			Transform transform = FindInChildren(trans.GetChild(i), name);
			if (transform != null)
			{
				return transform;
			}
		}
		return null;
	}

	public static List<BodypartSystem.bodyPart> StringToParts(string partstring)
	{
		List<BodypartSystem.bodyPart> list = new List<BodypartSystem.bodyPart>();
		string[] array = partstring.Split(';');
		foreach (string value in array)
		{
			if (Enum.TryParse<BodypartSystem.bodyPart>(value, out var result) && !list.Contains(result))
			{
				list.Add(result);
			}
		}
		return list;
	}

	public static string CorrectArmorPrefabName(string name)
	{
		string[] array = name.Split('@');
		if (array.Length > 1)
		{
			return array[1];
		}
		return name;
	}

	public static string[] GetEquippedItemNames(VisEquipment ve, ObjectDB db)
	{
		if (ve == null || db == null)
		{
			return new string[0];
		}
		List<string> list = new List<string>();
		int[] equippedHashes = GetEquippedHashes(ve);
		foreach (int hash in equippedHashes)
		{
			GameObject itemPrefab = db.GetItemPrefab(hash);
			if (!(itemPrefab == null))
			{
				list.Add(itemPrefab.name);
			}
		}
		return list.ToArray();
	}

	public static int[] GetEquippedHashes(VisEquipment ve)
	{
		if (ve == null)
		{
			return new int[0];
		}
		List<int> list = new List<int>();
		if (ve.m_currentLeftItemHash != 0)
		{
			list.Add(ve.m_currentLeftItemHash);
		}
		if (ve.m_currentRightItemHash != 0)
		{
			list.Add(ve.m_currentRightItemHash);
		}
		if (ve.m_currentChestItemHash != 0)
		{
			list.Add(ve.m_currentChestItemHash);
		}
		if (ve.m_currentLegItemHash != 0)
		{
			list.Add(ve.m_currentLegItemHash);
		}
		if (ve.m_currentHelmetItemHash != 0)
		{
			list.Add(ve.m_currentHelmetItemHash);
		}
		if (ve.m_currentShoulderItemHash != 0)
		{
			list.Add(ve.m_currentShoulderItemHash);
		}
		if (ve.m_currentBeardItemHash != 0)
		{
			list.Add(ve.m_currentBeardItemHash);
		}
		if (ve.m_currentHairItemHash != 0)
		{
			list.Add(ve.m_currentHairItemHash);
		}
		if (ve.m_currentUtilityItemHash != 0)
		{
			list.Add(ve.m_currentUtilityItemHash);
		}
		if (ve.m_currentLeftBackItemHash != 0)
		{
			list.Add(ve.m_currentLeftBackItemHash);
		}
		if (ve.m_currentRightBackItemHash != 0)
		{
			list.Add(ve.m_currentRightBackItemHash);
		}
		return list.ToArray();
	}

	public static void LogMessage(string message, LogLevel level = LogLevel.Message)
	{
		if (PungusSoulsPlugin.loggingEnabled.Value)
		{
			if (level == LogLevel.Message)
			{
                PungusSoulsPlugin.log.LogMessage(message);
			}
			if (level == LogLevel.Warning)
			{
                PungusSoulsPlugin.log.LogWarning(message);
			}
			if (level == LogLevel.Error)
			{
                PungusSoulsPlugin.log.LogError(message);
			}
		}
	}

	public static int[] BodyPartToBoneIndexes(string part)
	{
		if (Enum.TryParse<BodypartSystem.bodyPart>(part, out var result))
		{
			return BodyPartToBoneIndexes(result);
		}
		return new int[1] { -100 };
	}

	public static int[] BodyPartToBoneIndexes(BodypartSystem.bodyPart[] part)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < part.Length; i++)
		{
			list.AddRange(BodyPartToBoneIndexes(part[i]));
		}
		return list.ToArray();
	}

	public static int[] BodyPartToBoneIndexes(BodypartSystem.bodyPart part)
	{
		return part switch
		{
			BodypartSystem.bodyPart.All => new int[1] { -1 }, 
			BodypartSystem.bodyPart.Head => new int[3] { 4, 5, 6 }, 
			BodypartSystem.bodyPart.Torso => new int[6] { 0, 1, 2, 3, 7, 26 }, 
			BodypartSystem.bodyPart.ArmUpperLeft => new int[1] { 8 }, 
			BodypartSystem.bodyPart.ArmLowerLeft => new int[1] { 9 }, 
			BodypartSystem.bodyPart.HandLeft => new int[17]
			{
				10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
				20, 21, 22, 23, 24, 25, 26
			}, 
			BodypartSystem.bodyPart.ArmUpperRight => new int[1] { 27 }, 
			BodypartSystem.bodyPart.ArmLowerRight => new int[1] { 28 }, 
			BodypartSystem.bodyPart.HandRight => new int[16]
			{
				29, 30, 31, 32, 33, 34, 35, 36, 37, 38,
				39, 40, 41, 42, 43, 44
			}, 
			BodypartSystem.bodyPart.LegUpperLeft => new int[1] { 45 }, 
			BodypartSystem.bodyPart.LegLowerLeft => new int[1] { 46 }, 
			BodypartSystem.bodyPart.FootLeft => new int[2] { 47, 48 }, 
			BodypartSystem.bodyPart.LegUpperRight => new int[1] { 49 }, 
			BodypartSystem.bodyPart.LegLowerRight => new int[1] { 50 }, 
			BodypartSystem.bodyPart.FootRight => new int[2] { 51, 52 }, 
			_ => new int[1] { -100 }, 
		};
	}

    [HarmonyPatch(typeof(VisEquipment), "AttachItem")]
    [HarmonyPostfix]
    private static void AttachItemPatch(VisEquipment __instance, GameObject __result, int itemHash)
    {
        if (PungusSoulsPlugin.reorderEnabled.Value && !(__result == null) && __result.name.StartsWith("attach_skin") && ObjectDB.instance.GetItemPrefab(itemHash) != null)
        {
            SetSMRBones(__instance, __result, itemHash);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), "AttachArmor")]
    [HarmonyPostfix]
    private static void AttachArmorPatch(VisEquipment __instance, List<GameObject> __result, int itemHash)
    {
        if (!PungusSoulsPlugin.reorderEnabled.Value)
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
