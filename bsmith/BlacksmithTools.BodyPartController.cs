// BlacksmithTools, Version=2.0.3.0, Culture=neutral, PublicKeyToken=null
// BlacksmithTools.BodyPartController
using System.Collections.Generic;
using BlacksmithTools;
using UnityEngine;

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
				if (key.GetStableHashCode() == num || Util.CorrectRRRArmorPrefabName(key).GetStableHashCode() == num)
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
