using HarmonyLib;
using PungusSouls;
using System.Collections.Generic;
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

    public static readonly Dictionary<string, List<bodyPart>> bodypartSettings = new Dictionary<string, List<bodyPart>>();
    public static readonly Dictionary<string, List<int>> bodypartSettingsAsBones = new Dictionary<string, List<int>>();

    public static void RegisterHiddenBodyParts(string itemPrefabName, params bodyPart[] parts)
    {
        if (string.IsNullOrWhiteSpace(itemPrefabName) || parts == null || parts.Length == 0)
        {
            return;
        }

        RegisterHiddenBones(itemPrefabName, Util.BodyPartToBoneIndexes(parts));
    }

    public static void RegisterHiddenBodyParts(GameObject itemPrefab, params bodyPart[] parts)
    {
        if (itemPrefab == null)
        {
            return;
        }

        RegisterHiddenBodyParts(itemPrefab.name, parts);
    }

    public static void RegisterHiddenBones(string itemPrefabName, params int[] boneIndexes)
    {
        if (string.IsNullOrWhiteSpace(itemPrefabName) || boneIndexes == null || boneIndexes.Length == 0)
        {
            return;
        }

        List<int> bones = GetOrCreateBoneList(NormalizePrefabName(itemPrefabName));

        for (int i = 0; i < boneIndexes.Length; i++)
        {
            int bone = boneIndexes[i];

            if (bone < 0 || bones.Contains(bone))
            {
                continue;
            }

            bones.Add(bone);
        }
    }

    public static void RegisterHiddenBones(GameObject itemPrefab, params int[] boneIndexes)
    {
        if (itemPrefab == null)
        {
            return;
        }

        RegisterHiddenBones(itemPrefab.name, boneIndexes);
    }

    public static void ClearHiddenBodyParts(string itemPrefabName)
    {
        if (string.IsNullOrWhiteSpace(itemPrefabName))
        {
            return;
        }

        string normalizedName = NormalizePrefabName(itemPrefabName);
        bodypartSettings.Remove(normalizedName);
        bodypartSettingsAsBones.Remove(normalizedName);
    }

    public static void ClearAllHiddenBodyParts()
    {
        bodypartSettings.Clear();
        bodypartSettingsAsBones.Clear();
        RefreshAllBodyPartControllers();
    }

    public static void RefreshAllBodyPartControllers()
    {
        BodyPartController[] controllers = Object.FindObjectsByType<BodyPartController>(FindObjectsSortMode.None);

        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] != null)
            {
                controllers[i].FullUpdate();
            }
        }
    }

    public static void BindConfigs()
    {
        PartCfgToBoneindexes();
        CleanupCfgs();
    }

    public static void PartCfgToBoneindexes()
    {
        foreach (KeyValuePair<string, List<bodyPart>> setting in bodypartSettings)
        {
            if (string.IsNullOrWhiteSpace(setting.Key) || setting.Value == null || setting.Value.Count == 0)
            {
                continue;
            }

            List<int> bones = GetOrCreateBoneList(NormalizePrefabName(setting.Key));
            bones.AddRange(Util.BodyPartToBoneIndexes(setting.Value.ToArray()));
        }
    }

    public static void CleanupCfgs()
    {
        foreach (string key in bodypartSettingsAsBones.Keys.ToArray())
        {
            if (bodypartSettingsAsBones[key] == null)
            {
                bodypartSettingsAsBones[key] = new List<int>();
                continue;
            }

            bodypartSettingsAsBones[key] = bodypartSettingsAsBones[key]
                .Where(index => index >= 0)
                .Distinct()
                .ToList();
        }
    }

    private static List<int> GetOrCreateBoneList(string itemPrefabName)
    {
        string normalizedName = NormalizePrefabName(itemPrefabName);

        if (!bodypartSettingsAsBones.TryGetValue(normalizedName, out List<int> bones))
        {
            bones = new List<int>();
            bodypartSettingsAsBones[normalizedName] = bones;
        }

        return bones;
    }

    private static string NormalizePrefabName(string itemPrefabName)
    {
        if (string.IsNullOrWhiteSpace(itemPrefabName))
        {
            return string.Empty;
        }

        return itemPrefabName.Replace("(Clone)", string.Empty).Trim();
    }

    private static bool IsBodyHidingEnabled()
    {
        return PungusSoulsPlugin.bodyHidingEnabled != null && PungusSoulsPlugin.bodyHidingEnabled.Value;
    }

    private static void EquipmentChanged(VisEquipment visEquipment)
    {
        if (!IsBodyHidingEnabled() || visEquipment == null)
        {
            return;
        }

        BodyPartController controller = visEquipment.GetComponent<BodyPartController>();

        if (controller == null && ShouldAttachController(visEquipment))
        {
            controller = visEquipment.gameObject.AddComponent<BodyPartController>();
            controller.Setup(visEquipment);
            return;
        }

        controller?.FullUpdate();
    }

    private static bool ShouldAttachController(VisEquipment visEquipment)
    {
        if (!IsBodyHidingEnabled() || visEquipment == null)
        {
            return false;
        }

        if (!visEquipment.m_isPlayer)
        {
            return false;
        }

        if (visEquipment.GetComponent<Player>() == null)
        {
            return false;
        }

        if (visEquipment.GetComponent<AgentComponent>() != null ||
            visEquipment.GetComponent<AgentPrefabProfile>() != null ||
            visEquipment.GetComponentInParent<AgentComponent>() != null ||
            visEquipment.GetComponentInParent<AgentPrefabProfile>() != null)
        {
            return false;
        }

        return visEquipment.m_bodyModel != null &&
               visEquipment.m_bodyModel.sharedMesh != null &&
               visEquipment.m_bodyModel.sharedMesh.isReadable;
    }

    [HarmonyPatch(typeof(VisEquipment), "Awake")]
    [HarmonyPostfix]
    private static void VisEqAwakePatch(VisEquipment __instance)
    {
        if (!ShouldAttachController(__instance))
        {
            return;
        }

        BodyPartController controller = __instance.GetComponent<BodyPartController>();

        if (controller == null)
        {
            controller = __instance.gameObject.AddComponent<BodyPartController>();
        }

        controller.Setup(__instance);
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
    private static void SetShoulderPatch(VisEquipment __instance, bool __result)
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
        private readonly List<VisEquipment.PlayerModel> originalModels = new List<VisEquipment.PlayerModel>();
        private readonly Dictionary<int, Mesh> generatedMeshes = new Dictionary<int, Mesh>();
        private VisEquipment visEquipment;

        public void Setup(VisEquipment source)
        {
            visEquipment = source;
            SaveOriginalModels();
            FullUpdate();
            Util.LogMessage("Bodypart controller attached to " + visEquipment.name);
        }

        public void FullUpdate()
        {
            UpdateBodyModel();
        }

        public void UpdateBodyModel()
        {
            if (!TryGetCurrentModel(out int modelIndex, out VisEquipment.PlayerModel originalModel))
            {
                return;
            }

            PartCfgToBoneindexes();
            CleanupCfgs();

            List<int> bonesToHide = GetBonesToHide(visEquipment);

            if (bonesToHide.Count == 0)
            {
                RestoreOriginalMesh(modelIndex, originalModel);
                return;
            }

            Mesh sourceMesh = originalModel.m_mesh;

            if (sourceMesh == null || !sourceMesh.isReadable)
            {
                return;
            }

            int cacheKey = BuildCacheKey(modelIndex, bonesToHide);

            if (!generatedMeshes.TryGetValue(cacheKey, out Mesh generatedMesh) || generatedMesh == null)
            {
                generatedMesh = Amputate(Object.Instantiate(sourceMesh), bonesToHide);
                generatedMesh.name = sourceMesh.name;
                generatedMeshes[cacheKey] = generatedMesh;
            }

            visEquipment.m_models[modelIndex].m_mesh = generatedMesh;
        }

        private bool TryGetCurrentModel(out int modelIndex, out VisEquipment.PlayerModel originalModel)
        {
            modelIndex = -1;
            originalModel = null;

            if (visEquipment == null || visEquipment.m_models == null || visEquipment.m_models.Length == 0)
            {
                return false;
            }

            modelIndex = visEquipment.GetModelIndex();

            if (modelIndex < 0 || modelIndex >= visEquipment.m_models.Length || modelIndex >= originalModels.Count)
            {
                return false;
            }

            originalModel = originalModels[modelIndex];
            return originalModel != null && originalModel.m_mesh != null;
        }

        private void RestoreOriginalMesh(int modelIndex, VisEquipment.PlayerModel originalModel)
        {
            if (visEquipment == null || visEquipment.m_models == null || originalModel == null)
            {
                return;
            }

            visEquipment.m_models[modelIndex].m_mesh = originalModel.m_mesh;
        }

        private static List<int> GetBonesToHide(VisEquipment visEquipment)
        {
            HashSet<int> bones = new HashSet<int>();
            int[] equippedHashes = Util.GetEquippedHashes(visEquipment);

            for (int i = 0; i < equippedHashes.Length; i++)
            {
                int equippedHash = equippedHashes[i];

                foreach (KeyValuePair<string, List<int>> setting in bodypartSettingsAsBones)
                {
                    if (setting.Value == null || setting.Value.Count == 0)
                    {
                        continue;
                    }

                    string key = NormalizePrefabName(setting.Key);
                    string correctedKey = Util.CorrectArmorPrefabName(key);

                    if (key.GetStableHashCode() != equippedHash && correctedKey.GetStableHashCode() != equippedHash)
                    {
                        continue;
                    }

                    for (int boneIndex = 0; boneIndex < setting.Value.Count; boneIndex++)
                    {
                        bones.Add(setting.Value[boneIndex]);
                    }
                }
            }

            return bones.ToList();
        }

        private static int BuildCacheKey(int modelIndex, List<int> bonesToHide)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + modelIndex;

                for (int i = 0; i < bonesToHide.Count; i++)
                {
                    hash = hash * 31 + bonesToHide[i];
                }

                return hash;
            }
        }

        private static Mesh Amputate(Mesh body, List<int> bonesToHide)
        {
            if (body == null || bonesToHide == null || bonesToHide.Count == 0)
            {
                return body;
            }

            HashSet<int> hiddenBones = new HashSet<int>(bonesToHide);
            BoneWeight[] boneWeights = body.boneWeights;

            for (int subMesh = 0; subMesh < body.subMeshCount; subMesh++)
            {
                List<int> triangles = new List<int>(body.GetTriangles(subMesh));

                for (int triangleIndex = 0; triangleIndex + 2 < triangles.Count;)
                {
                    if (ShouldRemoveTriangle(triangles, triangleIndex, boneWeights, hiddenBones))
                    {
                        triangles.RemoveRange(triangleIndex, 3);
                    }
                    else
                    {
                        triangleIndex += 3;
                    }
                }

                body.SetTriangles(triangles.ToArray(), subMesh);
            }

            body.RecalculateBounds();
            return body;
        }

        private static bool ShouldRemoveTriangle(List<int> triangles, int triangleIndex, BoneWeight[] boneWeights, HashSet<int> hiddenBones)
        {
            for (int i = 0; i < 3; i++)
            {
                int vertexIndex = triangles[triangleIndex + i];

                if (vertexIndex < 0 || vertexIndex >= boneWeights.Length)
                {
                    continue;
                }

                if (IsDominatedByHiddenBone(boneWeights[vertexIndex], hiddenBones))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDominatedByHiddenBone(BoneWeight boneWeight, HashSet<int> hiddenBones)
        {
            float maxWeight = Mathf.Max(boneWeight.weight0, boneWeight.weight1, boneWeight.weight2, boneWeight.weight3);

            if (maxWeight <= 0f)
            {
                return false;
            }

            for (int i = 0; i < 4; i++)
            {
                int boneIndex = GetBoneIndex(boneWeight, i);

                if (!hiddenBones.Contains(boneIndex))
                {
                    continue;
                }

                if (GetBoneWeight(boneWeight, i) / maxWeight > 0.9f)
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetBoneIndex(BoneWeight boneWeight, int bone)
        {
            switch (bone)
            {
                case 0:
                    return boneWeight.boneIndex0;
                case 1:
                    return boneWeight.boneIndex1;
                case 2:
                    return boneWeight.boneIndex2;
                case 3:
                    return boneWeight.boneIndex3;
                default:
                    return -1;
            }
        }

        private static float GetBoneWeight(BoneWeight boneWeight, int bone)
        {
            switch (bone)
            {
                case 0:
                    return boneWeight.weight0;
                case 1:
                    return boneWeight.weight1;
                case 2:
                    return boneWeight.weight2;
                case 3:
                    return boneWeight.weight3;
                default:
                    return 0f;
            }
        }

        private void SaveOriginalModels()
        {
            originalModels.Clear();
            ClearGeneratedMeshes();

            if (visEquipment == null || visEquipment.m_models == null)
            {
                return;
            }

            for (int i = 0; i < visEquipment.m_models.Length; i++)
            {
                VisEquipment.PlayerModel playerModel = visEquipment.m_models[i];
                originalModels.Add(CloneModel(playerModel));
            }
        }

        private static VisEquipment.PlayerModel CloneModel(VisEquipment.PlayerModel source)
        {
            Material material = null;
            Mesh mesh = null;

            if (source != null && source.m_baseMaterial != null)
            {
                material = new Material(source.m_baseMaterial);
                material.name = source.m_baseMaterial.name;
            }

            if (source != null && source.m_mesh != null)
            {
                mesh = Object.Instantiate(source.m_mesh);
                mesh.name = source.m_mesh.name;
            }

            return new VisEquipment.PlayerModel
            {
                m_baseMaterial = material,
                m_mesh = mesh
            };
        }

        private void OnDestroy()
        {
            ClearGeneratedMeshes();
        }

        private void ClearGeneratedMeshes()
        {
            foreach (Mesh mesh in generatedMeshes.Values)
            {
                if (mesh != null)
                {
                    Object.Destroy(mesh);
                }
            }

            generatedMeshes.Clear();
        }
    }
}
