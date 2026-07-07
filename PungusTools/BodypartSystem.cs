using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using PungusSouls;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[HarmonyPatch]
public static class BodypartSystem
{
	public enum bodyPart
	{
		Head,
		Torso,
		ArmUpperLeft,
		ArmLowerLeft,
		HandLeft,
		ArmUpperRight,
		ArmLowerRight,
		HandRight,
		LegUpperLeft,
		LegLowerLeft,
		FootLeft,
		LegUpperRight,
		LegLowerRight,
		FootRight,
		All,
		Beard,
		Hair
	}

	public static Dictionary<string, List<bodyPart>> bodypartSettings = new Dictionary<string, List<bodyPart>>();

	public static Dictionary<string, List<int>> bodypartSettingsAsBones = new Dictionary<string, List<int>>();

	[HarmonyPatch(typeof(VisEquipment), "Awake")]
	[HarmonyPostfix]
	private static void VisEqAwakePatch(VisEquipment __instance)
	{
		if (PungusSoulsPlugin.bodyHidingEnabled.Value && __instance.m_isPlayer && !(__instance.m_bodyModel?.sharedMesh == null) && __instance.m_bodyModel.sharedMesh.isReadable)
		{
			BodyPartController bodyPartController = __instance.gameObject.AddComponent<BodyPartController>();
			bodyPartController.Setup(__instance);
		}
	}

	public static void BindConfigs()
	{
		string[] files = Directory.GetFiles(Paths.ConfigPath);
		string[] array = files;
		foreach (string text in array)
		{
			string fileName = Path.GetFileName(text);
			if (!fileName.StartsWith("bsmith."))
			{
				continue;
			}
			string text2 = fileName.Remove(0, 7);
			text2 = text2.Remove(text2.Length - 4, 4);
			Util.LogMessage("Loaded configuration file for " + text2);
			if (bodypartSettingsAsBones.ContainsKey(text2))
			{
				return;
			}
			bodypartSettingsAsBones.Add(text2, new List<int>());
			ConfigFile configFile = new ConfigFile(text, saveOnInit: true);
			ConfigEntry<string> configEntry = configFile.Bind("Body Parts", "List", "", "List of body parts to hide, delimited by a semilocor. List of valid values on mod page");
			string[] array2 = configEntry.Value.Split(';');
			for (int j = 0; j < array2.Length; j++)
			{
				bodypartSettingsAsBones[text2].AddRange(Util.BodyPartToBoneIndexes(array2[j]));
			}
			ConfigEntry<string> configEntry2 = configFile.Bind("Body Parts", "Bone List", "", "List of bone indexes, body model geometry weighted to these bones will be hidden, delimited by a semilocor. List of valid values on mod page");
			string[] array3 = configEntry2.Value.Split(';');
			for (int k = 0; k < array3.Length; k++)
			{
				if (int.TryParse(array3[k], out var result))
				{
					bodypartSettingsAsBones[text2].Add(result);
				}
			}
			Util.LogMessage(bodypartSettingsAsBones[text2].Count + " bones for " + text2);
		}
		PartCfgToBoneindexes();
		CleanupCfgs();
	}

	public static void PartCfgToBoneindexes()
	{
		foreach (string key in bodypartSettings.Keys)
		{
			if (!bodypartSettingsAsBones.ContainsKey(key))
			{
				bodypartSettingsAsBones.Add(key, new List<int>());
			}
			bodypartSettingsAsBones[key].AddRange(Util.BodyPartToBoneIndexes(bodypartSettings[key].ToArray()));
		}
	}

	public static void CleanupCfgs()
	{
		foreach (KeyValuePair<string, List<bodyPart>> bodypartSetting in bodypartSettings)
		{
			bodypartSettingsAsBones[bodypartSetting.Key] = new List<int>(bodypartSettingsAsBones[bodypartSetting.Key].Distinct().ToArray());
		}
	}

	private static void EquipmentChanged(VisEquipment viseq)
	{
		if (PungusSoulsPlugin.bodyHidingEnabled.Value)
		{
			viseq.GetComponent<BodyPartController>()?.FullUpdate();
			Util.LogMessage("Equipment changed ");
		}
	}

	[HarmonyPatch(typeof(VisEquipment), "SetRightHandEquipped")]
	[HarmonyPostfix]
	private static void SetRightHandPatch(VisEquipment __instance, bool __result)
	{
		if (__result)
		{
			EquipmentChanged(__instance);
		}
	}

	[HarmonyPatch(typeof(VisEquipment), "SetLeftHandEquipped")]
	[HarmonyPostfix]
	private static void SetLeftHandPatch(VisEquipment __instance, bool __result)
	{
		if (__result)
		{
			EquipmentChanged(__instance);
		}
	}

	[HarmonyPatch(typeof(VisEquipment), "SetChestEquipped")]
	[HarmonyPostfix]
	private static void SetChestPatch(VisEquipment __instance, bool __result, int hash)
	{
		if (__result)
		{
			EquipmentChanged(__instance);
		}
	}

	[HarmonyPatch(typeof(VisEquipment), "SetLegEquipped")]
	[HarmonyPostfix]
	private static void SetLegPatch(VisEquipment __instance, bool __result, int hash)
	{
		if (__result)
		{
			EquipmentChanged(__instance);
		}
	}

	[HarmonyPatch(typeof(VisEquipment), "SetHelmetEquipped")]
	[HarmonyPostfix]
	private static void SetHelmetPatch(VisEquipment __instance, bool __result)
	{
		if (__result)
		{
			EquipmentChanged(__instance);
		}
	}

	[HarmonyPatch(typeof(VisEquipment), "SetShoulderEquipped")]
	[HarmonyPostfix]
	private static void SetShoulderPtach(VisEquipment __instance, bool __result)
	{
		if (__result)
		{
			EquipmentChanged(__instance);
		}
	}

	[HarmonyPatch(typeof(VisEquipment), "SetUtilityEquipped")]
	[HarmonyPostfix]
	private static void SetUtilityPatch(VisEquipment __instance, bool __result)
	{
		if (__result)
		{
			EquipmentChanged(__instance);
		}
	}
    internal class BodyPartController : MonoBehaviour
    {
        public List<VisEquipment.PlayerModel> originalModels = new List<VisEquipment.PlayerModel>();

        public VisEquipment viseq;

        public void FullUpdate()
        {
            UpdateBodyModel();
        }

        public void UpdateBodyModel()
        {
            if (BodypartSystem.bodypartSettingsAsBones.Keys.Count != BodypartSystem.bodypartSettings.Keys.Count)
            {
                BodypartSystem.PartCfgToBoneindexes();
                BodypartSystem.CleanupCfgs();
            }
            List<int> list = new List<int>();
            int[] equippedHashes = Util.GetEquippedHashes(viseq);
            foreach (int num in equippedHashes)
            {
                foreach (string key in BodypartSystem.bodypartSettingsAsBones.Keys)
                {
                    if (key.GetStableHashCode() == num || Util.CorrectArmorPrefabName(key).GetStableHashCode() == num)
                    {
                        list.AddRange(BodypartSystem.bodypartSettingsAsBones[key].ToArray());
                    }
                }
            }
            Util.LogMessage("Hiding " + list.Count + " bones");
            if (list.Count == 0)
            {
                viseq.m_models[viseq.GetModelIndex()].m_mesh = originalModels[viseq.GetModelIndex()].m_mesh;
                return;
            }
            Mesh mesh = originalModels[viseq.GetModelIndex()].m_mesh;
            Mesh mesh2 = Amputate(Object.Instantiate(mesh), list.ToArray());
            mesh2.name = mesh.name;
            viseq.m_models[viseq.GetModelIndex()].m_mesh = mesh2;
        }

        private Mesh Amputate(Mesh body, int[] bonesToHide)
        {
            BoneWeight[] boneWeights = body.boneWeights;
            for (int i = 0; i < body.subMeshCount; i++)
            {
                List<int> list = new List<int>(body.GetTriangles(i));
                int num = 0;
                while (num < list.Count)
                {
                    bool flag = false;
                    int num2 = 0;
                    for (int j = 0; j < 2; j++)
                    {
                        if (flag)
                        {
                            break;
                        }
                        BoneWeight boneWeight = boneWeights[list[num + j]];
                        float num3 = Mathf.Max(boneWeight.weight0, boneWeight.weight1, boneWeight.weight2, boneWeight.weight3);
                        for (int k = 0; k < 4; k++)
                        {
                            int boneIndex = GetBoneIndex(boneWeight, k);
                            foreach (int num4 in bonesToHide)
                            {
                                if (flag)
                                {
                                    break;
                                }
                                if (boneIndex == num4)
                                {
                                    float boneWeight2 = GetBoneWeight(boneWeight, k);
                                    if (boneWeight2 / num3 > 0.9f && ++num2 == 1)
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (flag)
                    {
                        list.RemoveAt(num);
                        list.RemoveAt(num);
                        list.RemoveAt(num);
                    }
                    else
                    {
                        num += 3;
                    }
                }
                body.SetTriangles(list.ToArray(), i);
            }
            return body;
        }

        private int GetBoneIndex(BoneWeight boneWeight, int bone)
        {
            return bone switch
            {
                0 => boneWeight.boneIndex0,
                1 => boneWeight.boneIndex1,
                2 => boneWeight.boneIndex2,
                3 => boneWeight.boneIndex3,
                _ => -1,
            };
        }

        private float GetBoneWeight(BoneWeight boneWeight, int bone)
        {
            return bone switch
            {
                0 => boneWeight.weight0,
                1 => boneWeight.weight1,
                2 => boneWeight.weight2,
                3 => boneWeight.weight3,
                _ => 0f,
            };
        }

        public void Setup(VisEquipment _viseq)
        {
            viseq = _viseq;
            SaveOriginalModels();
            UpdateBodyModel();
            Util.LogMessage("bodypart controller attached to " + viseq.name);
        }

        private void SaveOriginalModels()
        {
            for (int i = 0; i < viseq.m_models.Length; i++)
            {
                VisEquipment.PlayerModel playerModel = viseq.m_models[i];
                if (playerModel.m_baseMaterial == null)
                {
                    Util.LogMessage("mat null");
                }
                Material material = new Material(playerModel.m_baseMaterial);
                material.name = playerModel.m_baseMaterial.name;
                if (playerModel.m_mesh == null)
                {
                    Util.LogMessage("mesh null");
                }
                Mesh mesh = Object.Instantiate(playerModel.m_mesh);
                mesh.name = playerModel.m_mesh.name;
                originalModels.Add(new VisEquipment.PlayerModel
                {
                    m_baseMaterial = material,
                    m_mesh = mesh
                });
            }
        }
    }
}
