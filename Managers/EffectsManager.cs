using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace PungusSouls.PungusTools
{
    public static class PungusEffectIds
    {
        public const string SE_Sunbro = "se_sunbro";
        public const string SE_Cat = "se_cat";
        public const string SE_Wolf = "se_wolf";
        public const string SE_Onion = "se_onion";
        public const string SE_Mushroom = "se_mushroom";
        public const string SE_Sun = "se_sun";
        public const string SE_OnionBuff = "se_onion_buff";
        public const string SE_SunbroBuff = "se_sunbro_buff";
        public const string SE_CatBuff = "se_cat_buff";
        public const string SE_WolfBuff = "se_wolf_buff";
        public const string SE_FireWeaponBuff = "SE_Firebuff";
        public const string SE_LightningWeaponBuff = "SE_Lightningbuff";
        public const string SE_DummyWeaponBuff = "SE_Frostbuff";
        public const float SunRange = 30f;
        public const float OnionRange = 10f;
        public const float SunbroRange = 12f;
        public const float CatRange = 6f;
        public const float WolfRange = 25f;
        public const float MushroomRange = 10f;
    }

    public static class PungusConsumableBuffState
    {
        private static readonly Dictionary<string, ConsumableBuffDefinition> ConsumableBuffs = new Dictionary<string, ConsumableBuffDefinition>();
        private static bool initialized;
        private static string activeBuff;
        private static string activeVfxPrefab;
        private static float activeUntil;

        public static void RegisterDefaults()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            Register("$ps_fire_resin", PungusEffectIds.SE_FireWeaponBuff, "buff_fire", 5f);
            Register("$ps_lightning_resin", PungusEffectIds.SE_LightningWeaponBuff, "buff_lightning", 5f);
            Register("$ps_frost_resin", PungusEffectIds.SE_DummyWeaponBuff, "buff_frost", 5f);
        }

        public static void Register(string itemName, string statusEffectName, string vfxPrefabName, float duration)
        {
            if (string.IsNullOrEmpty(itemName) || string.IsNullOrEmpty(statusEffectName))
            {
                return;
            }

            ConsumableBuffs[itemName] = new ConsumableBuffDefinition
            {
                StatusEffectName = statusEffectName,
                VfxPrefabName = vfxPrefabName,
                Duration = Mathf.Max(0.1f, duration)
            };
        }

        public static bool ActivateForItem(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
            {
                return false;
            }

            return ActivateForItemName(item.m_shared.m_name);
        }

        public static bool ActivateForItemName(string itemName)
        {
            RegisterDefaults();

            if (string.IsNullOrEmpty(itemName))
            {
                return false;
            }

            if (!ConsumableBuffs.TryGetValue(itemName, out ConsumableBuffDefinition definition))
            {
                return false;
            }

            activeBuff = definition.StatusEffectName;
            activeVfxPrefab = definition.VfxPrefabName;
            activeUntil = Time.time + definition.Duration;
            return true;
        }

        public static void ActivateDirect(string statusEffectName, string vfxPrefabName, float duration)
        {
            if (string.IsNullOrEmpty(statusEffectName))
            {
                return;
            }

            activeBuff = statusEffectName;
            activeVfxPrefab = vfxPrefabName;
            activeUntil = Time.time + Mathf.Max(0.1f, duration);
        }

        public static string GetActiveStatusEffect()
        {
            string animationManagerBuff = TryGetAnimationManagerBuff();

            if (!string.IsNullOrEmpty(animationManagerBuff))
            {
                return animationManagerBuff;
            }

            if (string.IsNullOrEmpty(activeBuff))
            {
                return null;
            }

            if (Time.time > activeUntil)
            {
                Clear();
                return null;
            }

            return activeBuff;
        }

        public static string GetActiveVfxPrefabName()
        {
            if (string.IsNullOrEmpty(activeVfxPrefab))
            {
                string statusEffect = GetActiveStatusEffect();

                if (statusEffect == PungusEffectIds.SE_FireWeaponBuff)
                {
                    return "buff_fire";
                }

                if (statusEffect == PungusEffectIds.SE_LightningWeaponBuff)
                {
                    return "buff_lightning";
                }

                if (statusEffect == PungusEffectIds.SE_DummyWeaponBuff)
                {
                    return "buff_frost";
                }

                return null;
            }

            if (Time.time > activeUntil)
            {
                Clear();
                return null;
            }

            return activeVfxPrefab;
        }

        public static void Clear()
        {
            activeBuff = null;
            activeVfxPrefab = null;
            activeUntil = 0f;
        }

        private static string TryGetAnimationManagerBuff()
        {
            Type animationsType = AccessTools.TypeByName("PungusSouls.Animations");

            if (animationsType == null)
            {
                return null;
            }

            MethodInfo method = AccessTools.Method(animationsType, "GetActiveConsumableBuff");

            if (method == null)
            {
                return null;
            }

            object value = method.Invoke(null, null);
            return value as string;
        }

        private struct ConsumableBuffDefinition
        {
            public string StatusEffectName;
            public string VfxPrefabName;
            public float Duration;
        }
    }

    public static class PungusEffectHelpers
    {
        public static bool HasSE(Character character, string name)
        {
            if (!character)
            {
                return false;
            }

            SEMan seman = character.GetSEMan();

            if (seman == null)
            {
                return false;
            }

            return seman.HaveStatusEffect(name.GetStableHashCode());
        }

        public static void AddSE(Character character, string name, bool reset = true)
        {
            if (!character || ObjectDB.instance == null)
            {
                return;
            }

            SEMan seman = character.GetSEMan();

            if (seman == null)
            {
                return;
            }

            StatusEffect effect = ObjectDB.instance.GetStatusEffect(name.GetStableHashCode());

            if (!effect)
            {
                Debug.LogWarning($"PungusEffectHelpers.AddSE failed. Missing status effect: {name}");
                return;
            }

            seman.AddStatusEffect(effect, reset);
        }

        public static void ApplyStatusToPlayersInRange(Vector3 position, float range, string statusEffect)
        {
            List<Player> players = Player.GetAllPlayers();

            foreach (Player player in players)
            {
                if (!player)
                {
                    continue;
                }

                if (Vector3.Distance(position, player.transform.position) > range)
                {
                    continue;
                }

                AddSE(player, statusEffect, true);
            }
        }

        public static int CountPlayersInRange(Vector3 position, float range)
        {
            int count = 0;
            List<Player> players = Player.GetAllPlayers();

            foreach (Player player in players)
            {
                if (!player)
                {
                    continue;
                }

                if (Vector3.Distance(position, player.transform.position) <= range)
                {
                    count++;
                }
            }

            return count;
        }

        public static T FindAura<T>(Character character) where T : Component
        {
            if (!character)
            {
                return null;
            }

            T aura = character.GetComponent<T>();

            if (aura)
            {
                return aura;
            }

            aura = character.GetComponentInChildren<T>(true);

            if (aura)
            {
                return aura;
            }

            return character.GetComponentInParent<T>();
        }

        public static float GetSunbroRadius(Character character)
        {
            SunbroAura aura = FindAura<SunbroAura>(character);
            return aura ? aura.radius : PungusEffectIds.SunbroRange;
        }

        public static float GetSunbroTickInterval(Character character)
        {
            SunbroAura aura = FindAura<SunbroAura>(character);
            return aura ? Mathf.Max(0.05f, aura.tickInterval) : 1f;
        }

        public static float GetCatRadius(Character character)
        {
            CatAura aura = FindAura<CatAura>(character);
            return aura ? aura.radius : PungusEffectIds.CatRange;
        }

        public static float GetCatTickInterval(Character character)
        {
            CatAura aura = FindAura<CatAura>(character);
            return aura ? Mathf.Max(0.05f, aura.tickInterval) : 1f;
        }

        public static float GetMushroomRadius(Character character)
        {
            CropGrowthAura aura = FindAura<CropGrowthAura>(character);
            return aura ? aura.radius : PungusEffectIds.MushroomRange;
        }

        public static float GetMushroomTickInterval(Character character)
        {
            CropGrowthAura aura = FindAura<CropGrowthAura>(character);
            return aura ? Mathf.Max(0.25f, aura.tickInterval) : 5f;
        }

        public static float GetMushroomGrowthMultiplier(Character character)
        {
            CropGrowthAura aura = FindAura<CropGrowthAura>(character);
            return aura ? Mathf.Max(1f, aura.growthMultiplier) : 2f;
        }

        public static bool GetMushroomIncludeSaplings(Character character)
        {
            CropGrowthAura aura = FindAura<CropGrowthAura>(character);
            return !aura || aura.includeSaplings;
        }
    }

    public class AuraVfxController
    {
        private readonly Transform parent;
        private GameObject instance;
        private string currentPrefabName;

        public AuraVfxController(Transform parent)
        {
            this.parent = parent;
        }

        public void SetActive(string prefabName, bool active)
        {
            if (!active)
            {
                Clear();
                return;
            }

            if (string.IsNullOrEmpty(prefabName))
            {
                Clear();
                return;
            }

            if (instance && currentPrefabName == prefabName)
            {
                return;
            }

            Clear();

            if (ZNetScene.instance == null)
            {
                return;
            }

            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);

            if (!prefab)
            {
                Debug.LogWarning($"PungusSouls aura VFX prefab not found: {prefabName}");
                return;
            }

            GameObject[] spawned = new EffectList
            {
                m_effectPrefabs = new[]
                {
                new EffectList.EffectData
                {
                    m_prefab = prefab,
                    m_enabled = true,
                    m_attach = true,
                    m_follow = true,
                    m_inheritParentRotation = true
                }
            }
            }.Create(parent.position, parent.rotation, parent);

            if (spawned != null && spawned.Length > 0)
            {
                instance = spawned[0];
                currentPrefabName = prefabName;
            }
        }

        public void Clear()
        {
            if (instance)
            {
                UnityEngine.Object.Destroy(instance);
            }

            instance = null;
            currentPrefabName = null;
        }
    }

    public static class PungusFallController
    {
        private static readonly Dictionary<Player, float> FeatherFallUntil = new Dictionary<Player, float>();

        public static void SetFeatherFall(Player player, float duration)
        {
            if (!player)
            {
                return;
            }

            FeatherFallUntil[player] = Time.time + Mathf.Max(0.1f, duration);
        }

        public static bool HasFeatherFall(Player player)
        {
            if (!player)
            {
                return false;
            }

            if (!FeatherFallUntil.TryGetValue(player, out float until))
            {
                return false;
            }

            if (Time.time > until)
            {
                FeatherFallUntil.Remove(player);
                return false;
            }

            return true;
        }
    }

    public class SE_OnionBuff : StatusEffect
    {
    }

    public class SE_Sun : StatusEffect
    {
    }

    public class SE_SunbroBuff : StatusEffect
    {
    }

    public class SE_CatBuff : StatusEffect
    {
    }

    public class SE_WolfBuff : StatusEffect
    {
    }

    public class SE_Onion : StatusEffect
    {
        private Character owner;
        private float timer;

        public override void Setup(Character character)
        {
            base.Setup(character);
            owner = character;
        }

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);

            if (!owner)
            {
                return;
            }

            timer += dt;

            if (timer < 1f)
            {
                return;
            }

            timer = 0f;
            PungusEffectHelpers.ApplyStatusToPlayersInRange(owner.transform.position, PungusEffectIds.OnionRange, PungusEffectIds.SE_OnionBuff);
        }
    }

    public class SE_Sunbro : StatusEffect
    {
        private Character owner;
        private float timer;

        public override void Setup(Character character)
        {
            base.Setup(character);
            owner = character;
        }

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);

            if (!owner)
            {
                return;
            }

            timer += dt;
            float tickInterval = PungusEffectHelpers.GetSunbroTickInterval(owner);

            if (timer < tickInterval)
            {
                return;
            }

            timer = 0f;
            float radius = PungusEffectHelpers.GetSunbroRadius(owner);
            PungusEffectHelpers.ApplyStatusToPlayersInRange(owner.transform.position, radius, PungusEffectIds.SE_SunbroBuff);
        }
    }

    public class SE_Cat : StatusEffect
    {

    }

    public class SE_Wolf : StatusEffect
    {
        private Character owner;
        private float timer;
        private readonly Collider[] hits = new Collider[128];

        public override void Setup(Character character)
        {
            base.Setup(character);
            owner = character;
        }

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);

            if (!owner || !ZNet.instance)
            {
                return;
            }

            timer += dt;

            if (timer < 2f)
            {
                return;
            }

            timer = 0f;
            Vector3 position = owner.transform.position;
            int count = Physics.OverlapSphereNonAlloc(position, PungusEffectIds.WolfRange, hits, ~0, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Collider hit = hits[i];

                if (!hit)
                {
                    continue;
                }

                Character character = hit.GetComponentInParent<Character>();

                if (!IsWolf(character))
                {
                    continue;
                }

                ZNetView view = character.GetComponent<ZNetView>();

                if (!view || !view.IsValid() || !view.IsOwner())
                {
                    continue;
                }

                long until = ZNet.instance.GetTime().Ticks + TimeSpan.FromSeconds(6).Ticks;
                view.GetZDO().Set("se_wolf_until", until);
                view.GetZDO().Set("se_wolf_x", position.x);
                view.GetZDO().Set("se_wolf_y", position.y);
                view.GetZDO().Set("se_wolf_z", position.z);
            }
        }

        private static bool IsWolf(Character character)
        {
            return character && character.name.ToLowerInvariant().Contains("wolf");
        }
    }

    public class SE_Mushroom : StatusEffect
    {
        private Character owner;
        private float timer;
        private readonly Collider[] hits = new Collider[256];

        public override void Setup(Character character)
        {
            base.Setup(character);
            owner = character;
        }

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);

            if (!owner || !ZNet.instance)
            {
                return;
            }

            timer += dt;
            float tickInterval = PungusEffectHelpers.GetMushroomTickInterval(owner);

            if (timer < tickInterval)
            {
                return;
            }

            timer = 0f;
            float radius = PungusEffectHelpers.GetMushroomRadius(owner);
            float multiplier = PungusEffectHelpers.GetMushroomGrowthMultiplier(owner);
            bool includeSaplings = PungusEffectHelpers.GetMushroomIncludeSaplings(owner);
            int count = Physics.OverlapSphereNonAlloc(owner.transform.position, radius, hits, ~0, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Collider hit = hits[i];

                if (!hit)
                {
                    continue;
                }

                Plant plant = hit.GetComponentInParent<Plant>();

                if (plant)
                {
                    AccelerateZdoTime(plant.GetComponent<ZNetView>(), multiplier, "plantTime", "plant_time", "time planted");
                    continue;
                }

                if (!includeSaplings)
                {
                    continue;
                }

                Growup growup = hit.GetComponentInParent<Growup>();

                if (growup)
                {
                    AccelerateZdoTime(growup.GetComponent<ZNetView>(), multiplier, "spawntime", "spawnTime", "spawn_time");
                }
            }
        }

        private static void AccelerateZdoTime(ZNetView view, float multiplier, params string[] keys)
        {
            if (!view || !view.IsValid() || !view.IsOwner() || !ZNet.instance)
            {
                return;
            }

            ZDO zdo = view.GetZDO();

            if (zdo == null)
            {
                return;
            }

            DateTime now = ZNet.instance.GetTime();
            float safeMultiplier = Mathf.Max(1f, multiplier);
            double secondsToAdvance = Mathf.Max(0f, safeMultiplier - 1f);

            for (int i = 0; i < keys.Length; i++)
            {
                long ticks = zdo.GetLong(keys[i], 0L);

                if (ticks <= 0L)
                {
                    continue;
                }

                DateTime storedTime = new DateTime(ticks);
                TimeSpan elapsed = now - storedTime;

                if (elapsed.TotalSeconds <= 0.0)
                {
                    continue;
                }

                TimeSpan bonus = TimeSpan.FromSeconds(elapsed.TotalSeconds * secondsToAdvance);
                zdo.Set(keys[i], storedTime.Subtract(bonus).Ticks);
                return;
            }
        }
    }

    public class AnimationEventHandler : MonoBehaviour
    {
        public const string SE_Onion = PungusEffectIds.SE_Onion;
        public const string SE_Sun = PungusEffectIds.SE_Sun;
        public const string SE_Sunbro = PungusEffectIds.SE_Sunbro;
        public const string SE_Cat = PungusEffectIds.SE_Cat;
        public const string SE_Wolf = PungusEffectIds.SE_Wolf;
        public const string SE_OnionBuff = PungusEffectIds.SE_OnionBuff;
        public const string SE_SunbroBuff = PungusEffectIds.SE_SunbroBuff;
        public const string SE_CatBuff = PungusEffectIds.SE_CatBuff;
        public const string SE_WolfBuff = PungusEffectIds.SE_WolfBuff;

        private static readonly Dictionary<string, GameObject> BuffVfxPrefabs = new Dictionary<string, GameObject>();
        private static readonly List<GameObject> ActiveBuffVFX = new List<GameObject>();
        private static string ActiveBuffVfxName;

        public void ApplyWeaponBuff()
        {
            Player player = GetComponentInParent<Player>();

            if (!player || player != Player.m_localPlayer)
            {
                return;
            }

            ApplyVFX(player);
            ApplyStatus(player);
        }

        public void ApplyFireWeaponBuff()
        {
            PungusConsumableBuffState.ActivateDirect(PungusEffectIds.SE_FireWeaponBuff, "buff_fire", 5f);
            ApplyWeaponBuff();
        }

        public void ApplyLightningWeaponBuff()
        {
            PungusConsumableBuffState.ActivateDirect(PungusEffectIds.SE_LightningWeaponBuff, "buff_lightning", 5f);
            ApplyWeaponBuff();
        }

        public void ApplyDummyWeaponBuff()
        {
            PungusConsumableBuffState.ActivateDirect(PungusEffectIds.SE_DummyWeaponBuff, "buff_frost", 5f);
            ApplyWeaponBuff();
        }

        private string GetDefaultWeaponBuff(Player player)
        {
            ItemDrop.ItemData weapon = player.GetCurrentWeapon();

            if (weapon == null || weapon.m_shared == null)
            {
                return null;
            }

            switch (weapon.m_shared.m_name)
            {
                case "$ps_sunlightsword":
                    return PungusEffectIds.SE_LightningWeaponBuff;
                case "$ps_fireweapon_dummy":
                    return PungusEffectIds.SE_FireWeaponBuff;
                case "$ps_dummy_weapon":
                    return PungusEffectIds.SE_DummyWeaponBuff;
                default:
                    return null;
            }
        }

        private string GetDefaultWeaponBuffVfx(Player player)
        {
            ItemDrop.ItemData weapon = player.GetCurrentWeapon();

            if (weapon == null || weapon.m_shared == null)
            {
                return null;
            }

            switch (weapon.m_shared.m_name)
            {
                case "$ps_sunlightsword":
                    return "buff_lightning";
                case "$ps_fireweapon_dummy":
                    return "buff_fire";
                case "$ps_dummy_weapon":
                    return "buff_frost";
                default:
                    return null;
            }
        }

        private void ApplyStatus(Player player)
        {
            if (ObjectDB.instance == null || player == null || player.m_seman == null)
            {
                return;
            }

            string buffName = PungusConsumableBuffState.GetActiveStatusEffect();

            if (string.IsNullOrEmpty(buffName))
            {
                buffName = GetDefaultWeaponBuff(player);
            }

            if (string.IsNullOrEmpty(buffName))
            {
                return;
            }

            StatusEffect effect = ObjectDB.instance.GetStatusEffect(buffName.GetStableHashCode());

            if (effect)
            {
                player.m_seman.AddStatusEffect(effect, true);
            }
        }

        private void ApplyVFX(Player player)
        {
            ClearVFX();

            GameObject weaponVisual = player.m_visEquipment?.m_rightItemInstance;

            if (!weaponVisual)
            {
                return;
            }

            string vfxName = PungusConsumableBuffState.GetActiveVfxPrefabName();

            if (string.IsNullOrEmpty(vfxName))
            {
                vfxName = GetDefaultWeaponBuffVfx(player);
            }

            if (string.IsNullOrEmpty(vfxName))
            {
                return;
            }

            if (!BuffVfxPrefabs.TryGetValue(vfxName, out GameObject prefab) || !prefab)
            {
                return;
            }

            SpawnBuffVFX(prefab, weaponVisual.transform);
            ActiveBuffVfxName = vfxName;
        }

        public static void SyncWeaponBuffVfx(Player player)
        {
            if (!player || player != Player.m_localPlayer)
            {
                return;
            }

            string vfxName = GetCurrentWeaponBuffVfxName(player);

            if (string.IsNullOrEmpty(vfxName))
            {
                ClearVFX();
                return;
            }

            GameObject weaponVisual = player.m_visEquipment?.m_rightItemInstance;

            if (!weaponVisual)
            {
                ClearVFX();
                return;
            }

            RemoveNullVFX();

            if (ActiveBuffVFX.Count > 0 && ActiveBuffVfxName == vfxName)
            {
                return;
            }

            ClearVFX();

            if (!BuffVfxPrefabs.TryGetValue(vfxName, out GameObject prefab) || !prefab)
            {
                return;
            }

            SpawnBuffVFX(prefab, weaponVisual.transform);
            ActiveBuffVfxName = vfxName;
        }

        private static string GetCurrentWeaponBuffVfxName(Player player)
        {
            if (!player || player.m_seman == null)
            {
                return null;
            }

            if (player.m_seman.HaveStatusEffect(PungusEffectIds.SE_LightningWeaponBuff.GetStableHashCode()))
            {
                return "buff_lightning";
            }

            if (player.m_seman.HaveStatusEffect(PungusEffectIds.SE_FireWeaponBuff.GetStableHashCode()))
            {
                return "buff_fire";
            }

            if (player.m_seman.HaveStatusEffect(PungusEffectIds.SE_DummyWeaponBuff.GetStableHashCode()))
            {
                return "buff_frost";
            }

            return null;
        }

        private static void SpawnBuffVFX(GameObject prefab, Transform parent)
        {
            if (!prefab || !parent)
            {
                return;
            }

            GameObject[] spawned = new EffectList
            {
                m_effectPrefabs = new[]
                {
                    new EffectList.EffectData
                    {
                        m_prefab = prefab,
                        m_enabled = true,
                        m_attach = true,
                        m_follow = true,
                        m_inheritParentRotation = true
                    }
                }
            }.Create(parent.position, parent.rotation, parent);

            foreach (GameObject obj in spawned)
            {
                if (obj)
                {
                    ActiveBuffVFX.Add(obj);
                }
            }
        }

        private static void ClearVFX()
        {
            for (int i = ActiveBuffVFX.Count - 1; i >= 0; i--)
            {
                if (ActiveBuffVFX[i])
                {
                    UnityEngine.Object.Destroy(ActiveBuffVFX[i]);
                }
            }

            ActiveBuffVFX.Clear();
            ActiveBuffVfxName = null;
        }

        private static void RemoveNullVFX()
        {
            for (int i = ActiveBuffVFX.Count - 1; i >= 0; i--)
            {
                if (!ActiveBuffVFX[i])
                {
                    ActiveBuffVFX.RemoveAt(i);
                }
            }

            if (ActiveBuffVFX.Count == 0)
            {
                ActiveBuffVfxName = null;
            }
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class ZNetScene_Awake_Patch
        {
            private static void Postfix(ZNetScene __instance)
            {
                PungusConsumableBuffState.RegisterDefaults();
                RegisterBuffVfx(__instance, "buff_lightning");
                RegisterBuffVfx(__instance, "buff_fire");
                RegisterBuffVfx(__instance, "buff_frost");
                RegisterBuffVfx(__instance, "buff_dummy");

                GameObject playerPrefab = __instance.GetPrefab("Player");

                if (!playerPrefab)
                {
                    return;
                }

                AddHandler(playerPrefab);
                Animator[] animators = playerPrefab.GetComponentsInChildren<Animator>(true);

                for (int i = 0; i < animators.Length; i++)
                {
                    if (animators[i])
                    {
                        AddHandler(animators[i].gameObject);
                    }
                }
            }

            private static void AddHandler(GameObject target)
            {
                if (!target)
                {
                    return;
                }

                if (!target.GetComponent<AnimationEventHandler>())
                {
                    target.AddComponent<AnimationEventHandler>();
                }
            }

            private static void RegisterBuffVfx(ZNetScene scene, string prefabName)
            {
                if (scene == null || string.IsNullOrEmpty(prefabName))
                {
                    return;
                }

                GameObject prefab = scene.GetPrefab(prefabName);

                if (prefab)
                {
                    BuffVfxPrefabs[prefabName] = prefab;
                }
            }
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyAttack))]
        private static class SEMan_ModifyAttack_Patch
        {
            private static void Postfix(SEMan __instance, Skills.SkillType skill, ref HitData hitData)
            {
                Character attacker = GetCharacter(__instance);

                if (__instance.HaveStatusEffect(PungusEffectIds.SE_LightningWeaponBuff.GetStableHashCode()))
                {
                    hitData.m_damage.m_lightning += 40f;
                }

                if (__instance.HaveStatusEffect(PungusEffectIds.SE_FireWeaponBuff.GetStableHashCode()))
                {
                    hitData.m_damage.m_fire += 40f;
                }

                if (__instance.HaveStatusEffect(PungusEffectIds.SE_DummyWeaponBuff.GetStableHashCode()))
                {
                    hitData.m_damage.m_frost += 40f;
                }

                if (attacker && PungusEffectHelpers.HasSE(attacker, PungusEffectIds.SE_Sun))
                {
                    int nearbyPlayers = PungusEffectHelpers.CountPlayersInRange(attacker.transform.position, PungusEffectIds.SunRange);
                    hitData.m_damage.Modify(1f + nearbyPlayers * 0.02f);
                }

                if (attacker && PungusEffectHelpers.HasSE(attacker, PungusEffectIds.SE_SunbroBuff))
                {
                    hitData.m_damage.Modify(1.05f);
                }
            }

            private static Character GetCharacter(SEMan seman)
            {
                FieldInfo field = AccessTools.Field(typeof(SEMan), "m_character");

                if (field == null)
                {
                    return null;
                }

                return field.GetValue(seman) as Character;
            }
        }
    }


    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    public static class PungusIncomingDamagePatch
    {
        private static bool Prefix(Character __instance, HitData hit)
        {
            if (!__instance || hit == null)
            {
                return true;
            }

            Player player = __instance as Player;

            if (player && PungusFallController.HasFeatherFall(player) && IsLikelyFallDamage(hit))
            {
                hit.m_damage.m_blunt *= 0.1f;
                hit.m_pushForce = 0f;
            }

            if (player && PungusEffectHelpers.HasSE(player, PungusEffectIds.SE_SunbroBuff))
            {
                hit.m_damage.Modify(0.95f);
            }

            if (player && PungusEffectHelpers.HasSE(player, PungusEffectIds.SE_Wolf) && IsWolf(hit.GetAttacker()))
            {
                hit.m_damage = new HitData.DamageTypes();
                hit.m_pushForce = 0f;
                return false;
            }

            return true;
        }

        private static bool IsLikelyFallDamage(HitData hit)
        {
            if (hit.GetAttacker() != null)
            {
                return false;
            }

            HitData.DamageTypes damage = hit.m_damage;
            float elemental = damage.m_fire + damage.m_frost + damage.m_lightning + damage.m_poison + damage.m_spirit;
            float weapon = damage.m_slash + damage.m_pierce + damage.m_chop + damage.m_pickaxe;

            return damage.m_blunt > 0f && elemental <= 0f && weapon <= 0f;
        }

        private static bool IsWolf(Character character)
        {
            return character && character.name.ToLowerInvariant().Contains("wolf");
        }
    }

    [HarmonyPatch]
    public static class PungusOnionFoodPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Player), "EatFood");
        }

        private static bool Prepare()
        {
            return TargetMethod() != null;
        }

        private static void Postfix(Player __instance)
        {
            if (!__instance)
            {
                return;
            }

            if (!PungusEffectHelpers.HasSE(__instance, PungusEffectIds.SE_Onion))
            {
                return;
            }

            BoostLatestFood(__instance, 1.1f);
        }

        private static void BoostLatestFood(Player player, float multiplier)
        {
            FieldInfo foodsField = AccessTools.Field(typeof(Player), "m_foods");

            if (foodsField == null)
            {
                return;
            }

            object foodsObject = foodsField.GetValue(player);

            if (!(foodsObject is System.Collections.IList foods) || foods.Count == 0)
            {
                return;
            }

            object food = foods[foods.Count - 1];
            MultiplyFloatField(food, multiplier, "m_health", "m_hp", "m_food");
            MultiplyFloatField(food, multiplier, "m_stamina", "m_foodStamina");
            MultiplyFloatField(food, multiplier, "m_eitr", "m_foodEitr");
            MultiplyFloatField(food, multiplier, "m_time", "m_duration", "m_ttl", "m_foodBurnTime");
        }

        private static void MultiplyFloatField(object target, float multiplier, params string[] names)
        {
            if (target == null)
            {
                return;
            }

            Type type = target.GetType();

            foreach (string name in names)
            {
                FieldInfo field = AccessTools.Field(type, name);

                if (field == null)
                {
                    continue;
                }

                object value = field.GetValue(target);

                if (!(value is float floatValue))
                {
                    continue;
                }

                field.SetValue(target, floatValue * multiplier);
                return;
            }
        }
    }

    [HarmonyPatch]
    public static class PungusCatComfortPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Player), "GetComfortLevel");
        }

        private static bool Prepare()
        {
            return TargetMethod() != null;
        }

        private static void Postfix(Player __instance, ref int __result)
        {
            if (!__instance)
            {
                return;
            }

            if (PungusEffectHelpers.HasSE(__instance, PungusEffectIds.SE_Cat))
            {
                __result += 1;
            }
        }
    }
    [HarmonyPatch(typeof(Player), "Update")]
    public static class PungusPlayerStatusPatch
    {
        private static float nextTick;

        private static void Postfix(Player __instance)
        {
            if (!__instance || __instance != Player.m_localPlayer)
            {
                return;
            }

            if (Time.time < nextTick)
            {
                return;
            }

            nextTick = Time.time + 0.5f;
            AnimationEventHandler.SyncWeaponBuffVfx(__instance);

            if (PungusEffectHelpers.HasSE(__instance, PungusEffectIds.SE_Cat))
            {
                PungusFallController.SetFeatherFall(__instance, 1f);
            }
        }
    }

    [HarmonyPatch]
    public static class PungusWolfFactionPatch
    {
        private static bool internalCheck;

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(BaseAI), nameof(BaseAI.IsEnemy), new[] { typeof(Character), typeof(Character) });
        }

        private static bool Prepare()
        {
            MethodBase method = TargetMethod();

            if (method == null)
            {
                Debug.LogWarning("[PungusSouls] PungusWolfFactionPatch could not find BaseAI.IsEnemy(Character, Character)");
                return false;
            }

            Debug.Log("[PungusSouls] PungusWolfFactionPatch targeting BaseAI.IsEnemy(Character, Character)");
            return true;
        }

        private static void Postfix(Character a, Character b, ref bool __result)
        {
            if (internalCheck || !a || !b)
            {
                return;
            }

            Character wolf = IsWolf(a) ? a : IsWolf(b) ? b : null;
            Character other = wolf == a ? b : wolf == b ? a : null;

            if (!wolf || !other)
            {
                return;
            }

            Player player = other as Player;

            if (player && PungusEffectHelpers.HasSE(player, PungusEffectIds.SE_Wolf))
            {
                __result = false;
                return;
            }

            Player protectedPlayer = FindNearestProtectedPlayer(wolf.transform.position, PungusEffectIds.WolfRange);

            if (!protectedPlayer || other.IsPlayer())
            {
                return;
            }

            internalCheck = true;
            bool protectedPlayerConsidersTargetEnemy = BaseAI.IsEnemy(protectedPlayer, other);
            internalCheck = false;

            if (protectedPlayerConsidersTargetEnemy)
            {
                __result = true;
            }
        }

        private static bool IsWolf(Character character)
        {
            return character && character.name.ToLowerInvariant().Contains("wolf");
        }

        private static Player FindNearestProtectedPlayer(Vector3 position, float range)
        {
            Player nearest = null;
            float nearestDistance = float.MaxValue;
            List<Player> players = Player.GetAllPlayers();

            foreach (Player player in players)
            {
                if (!player || !PungusEffectHelpers.HasSE(player, PungusEffectIds.SE_Wolf))
                {
                    continue;
                }

                float distance = Vector3.Distance(position, player.transform.position);

                if (distance > range || distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = distance;
                nearest = player;
            }

            return nearest;
        }
    }
}
