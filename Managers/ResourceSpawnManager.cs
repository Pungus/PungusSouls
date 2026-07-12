using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace PungusSouls
{
    public class ResourceSpawnDefinition
    {
        public GameObject Prefab;
        public Heightmap.Biome Biome;
        public string GroupId = "solo";
        public bool OverrideGroupDensity;
        public bool OverrideGroupPlacement;
        public float MinPerZone = 1f;
        public float MaxPerZone = 3f;
        public float MinAltitude = -1000f;
        public float MaxAltitude = 1000f;
        public float MinTilt = 0f;
        public float MaxTilt = 35f;
        public bool InForest;
        public float ForestThresholdMin = 0f;
        public float ForestThresholdMax = 1f;
        public float MinTerrainDelta = 0f;
        public float MaxTerrainDelta = 2f;
        public int GroupSizeMin = 1;
        public int GroupSizeMax = 1;
        public float GroupRadius = 0f;
        public float GroundOffset = 0f;
        public float SpawnCountMin = 1f;
        public float SpawnCountMax = 3f;
        public float MinDistanceFromSame = 0f;
        public int ClusterMin = 1;
        public int ClusterMax = 1;
        public Heightmap.BiomeArea BiomeArea = Heightmap.BiomeArea.Everything;
    }

    public static class ResourceSpawnManager
    {
        public class ResourceSpawnGroupDefinition
        {
            public string Id;
            public float MinPerZone = 1f;
            public float MaxPerZone = 3f;
            public int GroupSizeMin = 1;
            public int GroupSizeMax = 1;
            public float GroupRadius = 0f;
            public float MinTerrainDelta = 0f;
            public float MaxTerrainDelta = 2f;
            public float MinTilt = 0f;
            public float MaxTilt = 35f;
            public bool InForest;
            public float ForestThresholdMin = 0f;
            public float ForestThresholdMax = 1f;
            public float GroundOffset = 0f;

            public ResourceSpawnGroupDefinition(string id)
            {
                Id = string.IsNullOrEmpty(id) ? string.Empty : id.Trim();
            }

            public ResourceSpawnGroupDefinition(string id, float minPerZone, float maxPerZone, int groupSizeMin, int groupSizeMax, float groupRadius)
            {
                Id = string.IsNullOrEmpty(id) ? string.Empty : id.Trim();
                MinPerZone = minPerZone;
                MaxPerZone = maxPerZone;
                GroupSizeMin = groupSizeMin;
                GroupSizeMax = groupSizeMax;
                GroupRadius = groupRadius;
            }
        }

        private static readonly List<ResourceSpawnDefinition> Pending = new List<ResourceSpawnDefinition>();
        private static readonly Dictionary<string, ResourceSpawnGroupDefinition> Groups = new Dictionary<string, ResourceSpawnGroupDefinition>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ResourceSpawnDefinition> Definitions = new Dictionary<string, ResourceSpawnDefinition>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<Vector3>> SpawnedPositions = new Dictionary<string, List<Vector3>>(StringComparer.OrdinalIgnoreCase);
        private static bool Initialized;
        private static bool DefaultGroupsRegistered;
        private static bool GroundOffsetFieldWarningLogged;

        public static void RegisterGroup(ResourceSpawnGroupDefinition group)
        {
            if (group == null || string.IsNullOrEmpty(group.Id))
                return;

            ValidateGroup(group);
            Groups[group.Id] = group;
        }

        public static void Register(ResourceSpawnDefinition def)
        {
            RegisterDefaultGroups();

            if (def == null)
            {
                return;
            }

            if (def.Prefab == null)
            {
                return;
            }
            if (def.MinDistanceFromSame > 0f)
            { 
                def.OverrideGroupDensity = true; 
            }
            if (!Initialized)
            {
                Pending.Add(def);
                return;
            }

            AddVegetation(def);
        }

        public static void RegisterPending()
        {
            RegisterDefaultGroups();

            if (ZoneSystem.instance == null)
            {
                return;
            }

            Initialized = true;

            for (int i = 0; i < Pending.Count; i++)
                AddVegetation(Pending[i]);

            Pending.Clear();
        }

        public static bool TryGetDefinition(string prefabName, out ResourceSpawnDefinition definition)
        {
            return Definitions.TryGetValue(prefabName, out definition);
        }

        public static ResourceSpawnDefinition GetDefinition(GameObject prefab)
        {
            if (prefab == null)
                return null;

            Definitions.TryGetValue(prefab.name, out ResourceSpawnDefinition definition);
            return definition;
        }

        public static void ClearSpawnTracking()
        {
            SpawnedPositions.Clear();
        }
        private static bool IsTooCloseToTrackedPosition(
    string prefabName,
    Vector3 position,
    float radius)
        {
            if (!SpawnedPositions.TryGetValue(prefabName, out List<Vector3> positions))
                return false;

            for (int i = 0; i < positions.Count; i++)
            {
                if (Vector3.Distance(positions[i], position) < radius)
                    return true;
            }

            return false;
        }

        private static bool IsTooCloseToExistingInstance(
            GameObject newInstance,
            string prefabName,
            Vector3 position,
            float radius)
        {
            ZNetView[] views = UnityEngine.Object.FindObjectsOfType<ZNetView>();

            for (int i = 0; i < views.Length; i++)
            {
                ZNetView view = views[i];

                if (view == null || view.gameObject == null)
                    continue;

                GameObject existing = view.gameObject;

                if (existing == newInstance)
                    continue;

                string existingName = NormalizePrefabName(existing.name);

                if (!string.Equals(existingName, prefabName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (Vector3.Distance(existing.transform.position, position) < radius)
                    return true;
            }

            return false;
        }

        private static void RegisterTrackedPosition(
            string prefabName,
            Vector3 position)
        {
            if (!SpawnedPositions.TryGetValue(prefabName, out List<Vector3> positions))
            {
                positions = new List<Vector3>();
                SpawnedPositions[prefabName] = positions;
            }

            positions.Add(position);
        }

        private static string NormalizePrefabName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            const string cloneSuffix = "(Clone)";

            if (name.EndsWith(cloneSuffix, StringComparison.OrdinalIgnoreCase))
                return name.Substring(0, name.Length - cloneSuffix.Length);

            return name;
        }
        public static void HandlePlacedVegetation(
            GameObject instance,
            ZoneSystem.ZoneVegetation vegetation,
            Vector3 position)
        {
            if (instance == null || vegetation == null || vegetation.m_prefab == null)
                return;

            string prefabName = vegetation.m_prefab.name;

            if (!Definitions.TryGetValue(prefabName, out ResourceSpawnDefinition definition))
                return;

            if (definition.MinDistanceFromSame <= 0f)
                return;

            if (IsTooCloseToTrackedPosition(prefabName, position, definition.MinDistanceFromSame))
            {
                DestroySpawnedInstance(instance);
                return;
            }

            if (IsTooCloseToExistingInstance(instance, prefabName, position, definition.MinDistanceFromSame))
            {
                Debug.Log("[PungusSouls] Removing too-close tracked " + prefabName);
                Debug.Log("[PungusSouls] Removing too-close existing " + prefabName);
                DestroySpawnedInstance(instance);
                return;
            }
            Debug.Log(
            "[PungusSouls] Checking " +
            prefabName +
            " at " +
            position +
            " MinDistanceFromSame=" +
            definition.MinDistanceFromSame);
            RegisterTrackedPosition(prefabName, position);
        }

        private static void DestroySpawnedInstance(GameObject instance)
        {
            if (instance == null)
                return;

            ZNetView zNetView = instance.GetComponent<ZNetView>();

            if (zNetView != null && ZNetScene.instance != null)
            {
                ZNetScene.instance.Destroy(instance);
                return;
            }

            UnityEngine.Object.Destroy(instance);
        }

        private static void RegisterDefaultGroups()
        {
            if (DefaultGroupsRegistered)
                return;

            DefaultGroupsRegistered = true;
            RegisterGroup(new ResourceSpawnGroupDefinition("solo", 1f, 3f, 1, 1, 0f));
            RegisterGroup(new ResourceSpawnGroupDefinition("small", 1f, 3f, 2, 4, 3f));
            RegisterGroup(new ResourceSpawnGroupDefinition("cluster", 1f, 2f, 4, 8, 6f));
            RegisterGroup(new ResourceSpawnGroupDefinition("patch", 1f, 1f, 8, 16, 10f));
            RegisterGroup(new ResourceSpawnGroupDefinition("rare", 0f, 1f, 1, 1, 0f));
        }

        private static void AddVegetation(ResourceSpawnDefinition def)
        {
            if (ZoneSystem.instance == null)
            {
                return;
            }

            ResourceSpawnDefinition resolved = ResolveDefinition(def);
            Debug.Log(
            "[PungusSouls] Registering vegetation " +
            resolved.Prefab.name +
            " MinPerZone=" +
            resolved.MinPerZone +
            " MaxPerZone=" +
            resolved.MaxPerZone +
            " GroupSizeMin=" +
            resolved.GroupSizeMin +
            " GroupSizeMax=" +
            resolved.GroupSizeMax +
            " GroupRadius=" +
            resolved.GroupRadius +
            " MinDistanceFromSame=" +
            resolved.MinDistanceFromSame);
            ZoneSystem.ZoneVegetation veg = new ZoneSystem.ZoneVegetation
            {
                m_name = resolved.Prefab.name,
                m_prefab = resolved.Prefab,
                m_enable = true,
                m_biome = resolved.Biome,
                m_biomeArea = resolved.BiomeArea,
                m_min = resolved.MinPerZone,
                m_max = resolved.MaxPerZone,
                m_minAltitude = resolved.MinAltitude,
                m_maxAltitude = resolved.MaxAltitude,
                m_minTilt = resolved.MinTilt,
                m_maxTilt = resolved.MaxTilt,
                m_inForest = resolved.InForest,
                m_forestTresholdMin = resolved.ForestThresholdMin,
                m_forestTresholdMax = resolved.ForestThresholdMax,
                m_minTerrainDelta = resolved.MinTerrainDelta,
                m_maxTerrainDelta = resolved.MaxTerrainDelta,
                m_groupSizeMin = resolved.GroupSizeMin,
                m_groupSizeMax = resolved.GroupSizeMax,
                m_groupRadius = resolved.GroupRadius,
                m_blockCheck = true
            };

            ApplyGroundOffset(veg, resolved.GroundOffset);
            ZoneSystem.instance.m_vegetation.Add(veg);
            Definitions[resolved.Prefab.name] = resolved;
        }

        private static ResourceSpawnDefinition ResolveDefinition(ResourceSpawnDefinition source)
        {
            ResourceSpawnDefinition resolved = Copy(source);
            string groupId = string.IsNullOrEmpty(source.GroupId) ? "solo" : source.GroupId.Trim();

            if (!Groups.TryGetValue(groupId, out ResourceSpawnGroupDefinition group))
                Groups.TryGetValue("solo", out group);

            if (group != null)
            {
                if (!source.OverrideGroupDensity)
                {
                    resolved.MinPerZone = group.MinPerZone;
                    resolved.MaxPerZone = group.MaxPerZone;
                    resolved.GroupSizeMin = group.GroupSizeMin;
                    resolved.GroupSizeMax = group.GroupSizeMax;
                    resolved.GroupRadius = group.GroupRadius;
                }

                if (!source.OverrideGroupPlacement)
                {
                    resolved.MinTilt = group.MinTilt;
                    resolved.MaxTilt = group.MaxTilt;
                    resolved.InForest = group.InForest;
                    resolved.ForestThresholdMin = group.ForestThresholdMin;
                    resolved.ForestThresholdMax = group.ForestThresholdMax;
                    resolved.MinTerrainDelta = group.MinTerrainDelta;
                    resolved.MaxTerrainDelta = group.MaxTerrainDelta;
                    resolved.GroundOffset = group.GroundOffset + source.GroundOffset;
                }
            }

            ApplyAliasFields(source, resolved);
            ValidateDefinition(resolved);
            return resolved;
        }

        private static void ApplyAliasFields(ResourceSpawnDefinition source, ResourceSpawnDefinition resolved)
        {
            if (!Approximately(source.SpawnCountMin, 1f) || !Approximately(source.SpawnCountMax, 3f))
            {
                resolved.MinPerZone = source.SpawnCountMin;
                resolved.MaxPerZone = source.SpawnCountMax;
            }

            if (source.ClusterMin != 1 || source.ClusterMax != 1)
            {
                resolved.GroupSizeMin = source.ClusterMin;
                resolved.GroupSizeMax = source.ClusterMax;
            }
        }

        private static bool Approximately(float a, float b)
        {
            return Mathf.Abs(a - b) <= 0.0001f;
        }

        private static ResourceSpawnDefinition Copy(ResourceSpawnDefinition source)
        {
            return new ResourceSpawnDefinition
            {
                Prefab = source.Prefab,
                Biome = source.Biome,
                GroupId = source.GroupId,
                OverrideGroupDensity = source.OverrideGroupDensity,
                OverrideGroupPlacement = source.OverrideGroupPlacement,
                MinPerZone = source.MinPerZone,
                MaxPerZone = source.MaxPerZone,
                MinAltitude = source.MinAltitude,
                MaxAltitude = source.MaxAltitude,
                MinTilt = source.MinTilt,
                MaxTilt = source.MaxTilt,
                InForest = source.InForest,
                ForestThresholdMin = source.ForestThresholdMin,
                ForestThresholdMax = source.ForestThresholdMax,
                MinTerrainDelta = source.MinTerrainDelta,
                MaxTerrainDelta = source.MaxTerrainDelta,
                GroupSizeMin = source.GroupSizeMin,
                GroupSizeMax = source.GroupSizeMax,
                GroupRadius = source.GroupRadius,
                GroundOffset = source.GroundOffset,
                SpawnCountMin = source.SpawnCountMin,
                SpawnCountMax = source.SpawnCountMax,
                MinDistanceFromSame = source.MinDistanceFromSame,
                ClusterMin = source.ClusterMin,
                ClusterMax = source.ClusterMax,
                BiomeArea = source.BiomeArea
            };
        }

        private static void ApplyGroundOffset(ZoneSystem.ZoneVegetation vegetation, float groundOffset)
        {
            if (Mathf.Abs(groundOffset) <= 0.0001f)
                return;

            if (TrySetFloatField(vegetation, "m_groundOffset", groundOffset))
                return;

            if (TrySetFloatField(vegetation, "m_groundOffsetMin", groundOffset) | TrySetFloatField(vegetation, "m_groundOffsetMax", groundOffset))
                return;

            if (!GroundOffsetFieldWarningLogged)
            {
                GroundOffsetFieldWarningLogged = true;
            }
        }

        private static bool TrySetFloatField(object instance, string fieldName, float value)
        {
            if (instance == null || string.IsNullOrEmpty(fieldName))
                return false;

            FieldInfo field = FindField(instance.GetType(), fieldName);

            if (field == null || field.FieldType != typeof(float))
                return false;

            field.SetValue(instance, value);
            return true;
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (field != null)
                    return field;

                type = type.BaseType;
            }

            return null;
        }

        private static void ValidateGroup(ResourceSpawnGroupDefinition group)
        {
            group.MinPerZone = Mathf.Max(0f, group.MinPerZone);
            group.MaxPerZone = Mathf.Max(group.MinPerZone, group.MaxPerZone);
            group.GroupSizeMin = Mathf.Max(1, group.GroupSizeMin);
            group.GroupSizeMax = Mathf.Max(group.GroupSizeMin, group.GroupSizeMax);
            group.GroupRadius = Mathf.Max(0f, group.GroupRadius);
            group.MinTilt = Mathf.Clamp(group.MinTilt, 0f, 90f);
            group.MaxTilt = Mathf.Clamp(Mathf.Max(group.MinTilt, group.MaxTilt), 0f, 90f);
            group.ForestThresholdMin = Mathf.Clamp01(group.ForestThresholdMin);
            group.ForestThresholdMax = Mathf.Clamp01(Mathf.Max(group.ForestThresholdMin, group.ForestThresholdMax));
            group.MaxTerrainDelta = Mathf.Max(group.MinTerrainDelta, group.MaxTerrainDelta);
        }

        private static void ValidateDefinition(ResourceSpawnDefinition def)
        {
            def.MinPerZone = Mathf.Max(0f, def.MinPerZone);
            def.MaxPerZone = Mathf.Max(def.MinPerZone, def.MaxPerZone);
            def.GroupSizeMin = Mathf.Max(1, def.GroupSizeMin);
            def.GroupSizeMax = Mathf.Max(def.GroupSizeMin, def.GroupSizeMax);
            def.GroupRadius = Mathf.Max(0f, def.GroupRadius);
            def.MinTilt = Mathf.Clamp(def.MinTilt, 0f, 90f);
            def.MaxTilt = Mathf.Clamp(Mathf.Max(def.MinTilt, def.MaxTilt), 0f, 90f);
            def.ForestThresholdMin = Mathf.Clamp01(def.ForestThresholdMin);
            def.ForestThresholdMax = Mathf.Clamp01(Mathf.Max(def.ForestThresholdMin, def.ForestThresholdMax));
            def.MaxTerrainDelta = Mathf.Max(def.MinTerrainDelta, def.MaxTerrainDelta);
            def.MinDistanceFromSame = Mathf.Max(0f, def.MinDistanceFromSame);
        }
    }

    [HarmonyPatch]
    public static class PlaceVegetationPatch
    {
        private const int VegetationLocalIndex = 4;
        private const int PositionLocalIndex = 17;

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(ZoneSystem), "PlaceVegetation");
        }

        [HarmonyPrepare]
        private static bool Prepare()
        {
            MethodBase method = TargetMethod();

            if (method == null)
            {
                return false;
            }

            return true;
        }

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = instructions.ToList();

            MethodInfo instantiateMethod = AccessTools.Method(
                typeof(UnityEngine.Object),
                nameof(UnityEngine.Object.Instantiate),
                new[]
                {
                    typeof(GameObject),
                    typeof(Vector3),
                    typeof(Quaternion)
                });

            MethodInfo handleMethod = AccessTools.Method(
                typeof(ResourceSpawnManager),
                nameof(ResourceSpawnManager.HandlePlacedVegetation));

            if (instantiateMethod == null || handleMethod == null)
            {
                return code;
            }

            List<int> instantiateIndexes = new List<int>();

            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].Calls(instantiateMethod))
                    instantiateIndexes.Add(i);
            }

            if (instantiateIndexes.Count == 0)
            {
                return code;
            }

            for (int i = instantiateIndexes.Count - 1; i >= 0; i--)
            {
                int insertIndex = instantiateIndexes[i] + 1;

                code.InsertRange(insertIndex, new[]
                {
                    new CodeInstruction(OpCodes.Dup),
                    new CodeInstruction(OpCodes.Ldloc_S, VegetationLocalIndex),
                    new CodeInstruction(OpCodes.Ldloc_S, PositionLocalIndex),
                    new CodeInstruction(OpCodes.Call, handleMethod)
                });
            }

            return code;
        }
    }

    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
    public static class ResourceRegistrationPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            ResourceSpawnManager.ClearSpawnTracking();
            ResourceSpawnManager.RegisterPending();
        }
    }
}
