using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
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
        private static readonly Dictionary<string, List<Vector3>>SpawnedPositions = new();
        private static readonly List<ResourceSpawnDefinition> Pending = new List<ResourceSpawnDefinition>();
        private static readonly Dictionary<string, ResourceSpawnGroupDefinition> Groups = new Dictionary<string, ResourceSpawnGroupDefinition>(StringComparer.OrdinalIgnoreCase);
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
                Debug.LogError("Resource spawn definition is null");
                return;
            }

            if (def.Prefab == null)
            {
                Debug.LogError("Resource prefab is null");
                return;
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
                Debug.LogError("ZoneSystem still not available");
                return;
            }

            Initialized = true;

            for (int i = 0; i < Pending.Count; i++)
                AddVegetation(Pending[i]);

            Pending.Clear();
        }

        private static readonly Dictionary<string, ResourceSpawnDefinition>
            Definitions = new(StringComparer.OrdinalIgnoreCase);

        public static bool TryGetDefinition(
            string prefabName,
            out ResourceSpawnDefinition definition)
        {
            return Definitions.TryGetValue(
                prefabName,
                out definition);
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

        public static ResourceSpawnDefinition GetDefinition(
            GameObject prefab)
        {
            if (prefab == null)
                return null;

            Definitions.TryGetValue(
                prefab.name,
                out var definition);

            return definition;
        }


        public static bool CanSpawnAtPosition(
            string prefabName,
            Vector3 position)
        {
            if (!Definitions.TryGetValue(
                    prefabName,
                    out var definition))
            {
                return true;
            }

            if (definition.MinDistanceFromSame <= 0f)
                return true;

            if (!SpawnedPositions.TryGetValue(
                    prefabName,
                    out var positions))
            {
                return true;
            }

            foreach (var existing in positions)
            {
                if (Vector3.Distance(
                        existing,
                        position) < definition.MinDistanceFromSame)
                {
                    return false;
                }
            }

            return true;
        }

        public static void RegisterSpawn(
    string prefabName,
    Vector3 position)
        {
            if (!SpawnedPositions.TryGetValue(
                    prefabName,
                    out var positions))
            {
                positions = new List<Vector3>();
                SpawnedPositions[prefabName] = positions;
            }

            positions.Add(position);
        }

        private static bool HasResourceNearby(string prefabName,Vector3 position,float radius)
        {
            foreach (var znet in UnityEngine.Object.FindObjectsOfType<ZNetView>())
            {
                if (!znet.name.StartsWith(prefabName))
                    continue;

                if (Vector3.Distance(
                        znet.transform.position,
                        position) < radius)
                {
                    return true;
                }
            }

            return false;
        }
        private static void AddVegetation(
    ResourceSpawnDefinition def)
{
    ZoneSystem.ZoneVegetation veg =
        new ZoneSystem.ZoneVegetation
        {
            m_name = def.Prefab.name,
            m_prefab = def.Prefab,
            m_enable = true,

            m_biome = def.Biome,
            m_biomeArea = def.BiomeArea,

            m_min = def.MinPerZone,
            m_max = def.MaxPerZone,

            m_groupSizeMin = def.GroupSizeMin,
            m_groupSizeMax = def.GroupSizeMax,
            m_groupRadius = def.GroupRadius,

            m_blockCheck = true
        };
        
    ApplyGroundOffset(
        veg,
        def.GroundOffset);
        
    ZoneSystem.instance.m_vegetation.Add(veg);

    Definitions[def.Prefab.name] = def;
}


        private static ResourceSpawnDefinition ResolveDefinition(ResourceSpawnDefinition source)
        {
            ResourceSpawnDefinition resolved = Copy(source);
            string groupId = string.IsNullOrEmpty(source.GroupId) ? "solo" : source.GroupId.Trim();

            if (!Groups.TryGetValue(groupId, out ResourceSpawnGroupDefinition group))
                Groups.TryGetValue("solo", out group);

            if (group == null)
            {
                ValidateDefinition(resolved);
                return resolved;
            }

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

            ValidateDefinition(resolved);
            return resolved;
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
                Debug.LogWarning("ResourceSpawnManager could not find a ground-offset field on ZoneSystem.ZoneVegetation. GroundOffset values will be ignored by this Valheim build.");
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
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

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
        }
    }

    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
    public static class ResourceRegistrationPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            ResourceSpawnManager.RegisterPending();
        }
    }


    [HarmonyPatch(typeof(ZoneSystem), "PlaceVegetation")]
    public static class PlaceVegetationPatch
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction>
            Transpiler(
                IEnumerable<CodeInstruction> instructions)
        {
            var code =
                new List<CodeInstruction>(
                    instructions);

            return code;
        }

    }

}
