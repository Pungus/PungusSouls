using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using ItemManager;
using PungusSouls.PungusTools;

namespace PungusSouls
{
    public static class Animations
    {
        private static readonly Dictionary<string, AnimationClip> _customClips = new Dictionary<string, AnimationClip>();
        private static readonly Dictionary<string, Dictionary<string, string>> _weaponMaps = new Dictionary<string, Dictionary<string, string>>();
        private static readonly Dictionary<string, Dictionary<string, string>> _consumableMaps = new Dictionary<string, Dictionary<string, string>>();
        private static readonly Dictionary<string, float> _consumableDurations = new Dictionary<string, float>();
        private static readonly Dictionary<string, string> _consumableBuffs = new Dictionary<string, string>();
        private static string _activeConsumableBuff;
        private static float _activeConsumableBuffUntil;

        private static AnimatorOverrideController _override;
        private static RuntimeAnimatorController _baseRuntime;

        private static List<KeyValuePair<AnimationClip, AnimationClip>> _overrideList;
        private static List<KeyValuePair<AnimationClip, AnimationClip>> _defaultOverrideList;

        private static bool _assetsLoaded;
        private static string _lastAppliedKey;

        private static string _activeConsumable;
        private static float _activeConsumableUntil;

        public static void LoadAssets()
        {
            if (_assetsLoaded)
            {
                return;
            }

            _assetsLoaded = true;

            _customClips["AbyssGreatswordAttack3"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("AbyssGreatswordAttack2");

            _customClips["Ultragreatsword_Attack1"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Ultragreatsword_Attack1");
            _customClips["Ultragreatsword_Attack2"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Ultragreatsword_Attack2");
            _customClips["Ultragreatsword_Attack3"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Ultragreatsword_Attack3");

            _customClips["RKPG_Attack_1"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("RKPG_Attack_1");
            _customClips["RKPG_Attack_2"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("RKPG_Attack_2");
            _customClips["RKPG_Attack_3"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("RKPG_Attack_3");

            _customClips["StraightSword1"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("StraightSword1");
            _customClips["StraightSword2"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("StraightSword2");
            _customClips["StraightSword3"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("StraightSword3");

            _customClips["HollowSlayer1"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("HollowSlayer1");
            _customClips["HollowSlayer2"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("HollowSlayer2");
            _customClips["HollowSlayer3"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("HollowSlayer3");


            _customClips["DarkSword"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("DarkSword");

            _customClips["GS_Idle"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("GS_Idle");
            _customClips["GS_Sprint_Forward"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("1H_GS_Run");
            _customClips["GS_Jog_Forward"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("1H_GS_Jog");
            _customClips["GS_Walk_Forward"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("1H_GS_Walk");

            _customClips["Greatsword_Attack1"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("1H_GS_Attack1");
            _customClips["Greatsword_Attack2"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("1H_GS_Attack2");
            _customClips["Greatsword_Attack3"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("1H_GS_Attack3");
            _customClips["Greatbow_Draw"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Greatbow_Draw");
            _customClips["Greatbow_Hold"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Greatbow_Hold");
            _customClips["Greatbow_Fire"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Greatbow_Fire");

            _customClips["Curvedsword1"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Curvedsword1");
            _customClips["Curvedsword2"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Curvedsword2");
            _customClips["Curvedsword3"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Curvedsword3");
            _customClips["Curvedsword_alt"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Curvedsword_alt");

            _customClips["MorneRage"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("MorneRage");

            _customClips["Dualswords1"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Dualswords1");
            _customClips["Dualswords2"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Dualswords2");
            _customClips["Dualswords3"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Dualswords3");
            _customClips["Dualswords_alt"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Dualswordsalt");

            _customClips["Dualspears1"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Dualspears1");
            _customClips["Dualspears2"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Dualspears2");
            _customClips["Dualspears3"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Dualspears3");
            _customClips["Dualspears_alt"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Dualspears4");

            _customClips["Dualhammers1"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Dualhammers1");
            _customClips["Dualhammers2"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Dualhammers2");
            _customClips["Dualhammers3"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Dualhammers3");

            _customClips["Mlgs_heavy"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Mlgs_heavy");

            _customClips["2handclub_1"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("2handclub_1");
            _customClips["2handclub_2"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("2handclub_2");
            _customClips["2handclub_3"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("2handclub_3");

            _customClips["rapier1"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("rapier1");
            _customClips["rapier2"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("rapier2");
            _customClips["rapier3"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("rapier3");

            _customClips["Dancer1"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Dancer1");
            _customClips["Dancer2"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Dancer2");
            _customClips["Dancer3"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Dancer3");
            _customClips["Dancer4"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Dancer4");
            _customClips["Dancer5"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Dancer5");
            _customClips["Dancer6"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Dancer6");

            _customClips["BuffWeapon"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("BuffWeapon");
            _customClips["Drink_Estus"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Drink_Estus");
            _customClips["Legion"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Legion");
            _customClips["SpellCast"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("SpellCast");
            _customClips["soulmass"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("soulmass");
            _customClips["wog"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("wog");
            
            _customClips["Lightningspear_spell"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("Lightningspear_spell");

/*            _customClips["CustomConsumableUse"] = PungusSoulsPlugin.asset.LoadAsset<AnimationClip>("CustomConsumableUse");*/

            _weaponMaps["$ps_sunlightseal"] = new Dictionary<string, string>
            {
                { "throw_spear", "Lightningspear_spell" },
                { "Ground Staff Attack", "wog" }
            };
            _weaponMaps["CanvasTalisman"] = new Dictionary<string, string>
            {
                { "throw_spear", "Lightningspear_spell" },
                { "Ground Staff Attack", "wog" }
            };
            _weaponMaps["$ps_staffwood"] = new Dictionary<string, string>
            {
                { "Fireball Staff Attacks (1)", "SpellCast" },
                { "Fireball Staff Attacks (2)", "SpellCast" }
            };
            _weaponMaps["$ps_darkmoonstaff"] = new Dictionary<string, string>
            {
                { "Fireball Staff Attacks (1)", "SpellCast" },
                { "Fireball Staff Attacks (2)", "SpellCast" }
            };
            _weaponMaps["$ps_manuscatalyst"] = new Dictionary<string, string>
            {
                { "Fireball Staff Attacks (1)", "SpellCast" },
                { "Fireball Staff Attacks (2)", "SpellCast" }
            };
            _weaponMaps["$ps_crystalstaff"] = new Dictionary<string, string>
            {
                { "Fireball Staff Attacks (1)", "SpellCast" },
                { "Fireball Staff Attacks (2)", "SpellCast" }
            };
            _weaponMaps["$ps_olenford"] = new Dictionary<string, string>
            {
                { "Fireball Staff Attacks (1)", "SpellCast" },
                { "Fireball Staff Attacks (2)", "SpellCast" }
            };

            _weaponMaps["$ps_rapier"] = new Dictionary<string, string>
            {
                { "Attack1", "rapier1" },
                { "Attack2", "rapier2" },
                { "Attack3", "rapier1" },
                { "Sword-Attack-R4", "rapier3" }
            };
            _weaponMaps["$ps_crystalrapier"] = new Dictionary<string, string>
            {
                { "Attack1", "rapier1" },
                { "Attack2", "rapier2" },
                { "Attack3", "rapier1" },
                { "Sword-Attack-R4", "rapier3" }
            };

            _weaponMaps["$ps_sunlightsword"] = new Dictionary<string, string>
            {
                { "Attack1", "StraightSword1" },
                { "Attack2", "StraightSword2" },
                { "MaceAltAttack", "BuffWeapon" }
            };

            _weaponMaps["$ps_Glordsword"] = new Dictionary<string, string>
            {
                { "Greatsword Secondary Attack", "MorneRage" }
            };

            _weaponMaps["$ps_MLGreatsword"] = new Dictionary<string, string>
            {
                { "Greatsword Secondary Attack", "Mlgs_heavy" }
            };

            _weaponMaps["$ps_dspear"] = new Dictionary<string, string>
            {
                { "throw_spear", "Lightningspear_spell" }
            };

            _weaponMaps["$ps_abyssgreatsword"] = new Dictionary<string, string>
            {
                { "Greatsword Idle", "GS_Idle" },
                { "Greatsword Run", "GS_Sprint_Forward" },
                { "Greatsword Jog", "GS_Jog_Forward" },
                { "Greatsword Walking", "GS_Walk_Forward" },
                { "Greatsword BaseAttack (1)", "Greatsword_Attack1" },
                { "Greatsword BaseAttack (2)", "Greatsword_Attack2" },
                { "Greatsword BaseAttack (3)", "Greatsword_Attack3" },
                { "Greatsword Secondary Attack", "AbyssGreatswordAttack3" }
            };

            _weaponMaps["$ps_ringedgreatswords"] = new Dictionary<string, string>
            {
                { "Knife Attack Combo (1)", "RKPG_Attack_1" },
                { "Knife Attack Combo (2)", "RKPG_Attack_2" },
                { "Knife Attack Combo (3)", "RKPG_Attack_3" }
            };

            _weaponMaps["$ps_blackknightugs"] = new Dictionary<string, string>
            {
                { "Greatsword BaseAttack (1)", "Ultragreatsword_Attack1" },
                { "Greatsword BaseAttack (2)", "Ultragreatsword_Attack2" },
                { "Greatsword BaseAttack (3)", "Ultragreatsword_Attack3" }
            };

            _weaponMaps["$ps_DSbow"] = new Dictionary<string, string>
            {
                { "Bow Aim Idle 01", "Greatbow_Hold" },
                { "Bow Aim", "Greatbow_Hold" },
                { "Bow Aim Recoil", "Greatbow_Fire" },
                { "bow fire", "Greatbow_Fire" }
            };

            _weaponMaps["$ps_goldtracer"] = new Dictionary<string, string>
            {
                { "knife_slash", "Curvedsword1" },
                { "knife_slash1", "Curvedsword2" },
                { "knife_slash2", "Curvedsword3" },
                { "Knife JumpAttack", "Curvedsword_alt" }
            };

            _weaponMaps["$ps_goldsilvertracers"] = new Dictionary<string, string>
            {
                { "Knife Attack Combo (1)", "Dualswords1" },
                { "Knife Attack Combo (2)", "Dualswords2" },
                { "Knife Attack Combo (3)", "Dualswords3" },
                { "Knife_Secondary", "Dualswords_alt" }
            };

            _weaponMaps["$ps_drangspears"] = new Dictionary<string, string>
            {
                { "DualAxes Attack 1", "Dualspears1" },
                { "DualAxes Attack 2 2", "Dualspears2" },
                { "DualAxes Attack 3 2", "Dualspears3" },
                { "DualAxes Attack Cleave", "Dualspears_alt" }
            };

            _weaponMaps["$ps_dranghammers"] = new Dictionary<string, string>
            {
                { "DualAxes Attack 1", "Dualhammers1" },
                { "DualAxes Attack 2 2", "Dualhammers2" },
                { "DualAxes Attack 3 2", "Dualhammers3" },
            };

            _weaponMaps["$ps_dancer_twinblades"] = new Dictionary<string, string>
            {
                { "Knife Attack Combo (1)", "Dancer1" },
                { "Knife Attack Combo (2)", "Dancer2" },
                { "Knife Attack Combo (3)", "Dancer3" },
                { "Attack1", "Dancer4" },
                { "Attack2", "Dancer5" },
                { "Attack3", "Dancer6" }
            };

            _weaponMaps["$ps_DragonTooth"] = new Dictionary<string, string>
            {
                { "Attack1", "2handclub_1" },
                { "Attack2", "2handclub_2" },
                { "Attack3", "2handclub_3" }
            };
            _weaponMaps["$ps_furysword"] = new Dictionary<string, string>
            {
                { "Attack1", "Curvedsword1" },
                { "Attack2", "Curvedsword2" },
                { "Attack3", "Curvedsword3" }
            };
            _weaponMaps["$ps_shotel"] = new Dictionary<string, string>
            {
                { "Attack1", "Curvedsword1" },
                { "Attack2", "Curvedsword2" },
                { "Attack3", "Curvedsword3" }
            };
            _weaponMaps["$ps_darksword"] = new Dictionary<string, string>
            {
                { "Attack1", "StraightSword1" },
                { "Attack2", "StraightSword2" },
                { "Attack3", "DarkSword" }
            };

            _consumableMaps["$ps_charcoalpineresin"] = CreateConsumableBuffMap("BuffWeapon");
            _consumableMaps["$ps_goldpineresin"] = CreateConsumableBuffMap("BuffWeapon");
            _consumableMaps["$ps_palepineresin"] = CreateConsumableBuffMap("BuffWeapon");
            _consumableMaps["$ps_rottenpineresin"] = CreateConsumableBuffMap("BuffWeapon");

            _consumableDurations["$ps_charcoalpineresin"] = 2.5f;
            _consumableDurations["$ps_goldpineresin"] = 2.5f;
            _consumableDurations["$ps_palepineresin"] = 2.5f;
            _consumableDurations["$ps_rottenpineresin"] = 2.5f;

            _consumableBuffs["$ps_charcoalpineresin"] = PungusEffectIds.SE_FireWeaponBuff;
            _consumableBuffs["$ps_goldpineresin"] = PungusEffectIds.SE_LightningWeaponBuff;
            _consumableBuffs["$ps_palepineresin"] = PungusEffectIds.SE_DummyWeaponBuff;
            _consumableBuffs["$ps_rottenpineresin"] = PungusEffectIds.SE_DummyWeaponBuff;

            NpcAnimations.RegisterNpcWeaponAnimationMap(
            "havel",
            "$ps_dragontooth",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Attack1", "Greatsword_Attack1" },
                { "Attack2", "Greatsword_Attack2" },
                { "Attack3", "Greatsword_Attack3" }
            }
        );
}

        private static Dictionary<string, string> CreateConsumableBuffMap(string clipName)
        {
            return new Dictionary<string, string>
            {
                { "Eat", "BuffWeapon" },
                { "Eat1", "BuffWeapon" },
                { "consume", "BuffWeapon" }
            };
        }

        public static string GetActiveConsumableBuff()
        {
            if (string.IsNullOrEmpty(_activeConsumableBuff))
            {
                return null;
            }

            if (Time.time > _activeConsumableBuffUntil)
            {
                _activeConsumableBuff = null;
                _activeConsumableBuffUntil = 0f;
                return null;
            }

            return _activeConsumableBuff;
        }

        private static bool TryGetConsumableMapKey(ItemDrop.ItemData item, out string key)
        {
            key = null;

            foreach (string candidate in GetItemNameCandidates(item))
            {
                if (_consumableMaps.ContainsKey(candidate))
                {
                    key = candidate;
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<string> GetItemNameCandidates(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                yield break;
            }

            if (item.m_shared != null && !string.IsNullOrEmpty(item.m_shared.m_name))
            {
                yield return item.m_shared.m_name;
            }

            if (item.m_dropPrefab != null && !string.IsNullOrEmpty(item.m_dropPrefab.name))
            {
                string prefabName = item.m_dropPrefab.name.Replace("(Clone)", string.Empty).Trim();
                yield return prefabName;

                if (!prefabName.StartsWith("$"))
                {
                    yield return "$" + prefabName;
                }

                if (!prefabName.StartsWith("$ps_"))
                {
                    yield return "$ps_" + prefabName;
                }
            }
        }

        private static void Init(Player p)
        {
            if (p?.m_animator == null)
            {
                return;
            }

            RuntimeAnimatorController runtime = p.m_animator.runtimeAnimatorController;

            if (runtime == null)
            {
                return;
            }

            if (_override != null && runtime == _override)
            {
                return;
            }

            if (_override != null && p.m_animator.runtimeAnimatorController == _override)
            {
                return;
            }

            if (runtime is AnimatorOverrideController existingOverride)
            {
                runtime = existingOverride.runtimeAnimatorController;
            }

            if (_override != null && _baseRuntime == runtime)
            {
                p.m_animator.runtimeAnimatorController = _override;
                return;
            }

            _baseRuntime = runtime;
            _override = new AnimatorOverrideController(_baseRuntime);
            p.m_animator.runtimeAnimatorController = _override;

            _overrideList = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            _override.GetOverrides(_overrideList);

            _defaultOverrideList = new List<KeyValuePair<AnimationClip, AnimationClip>>();

            for (int i = 0; i < _overrideList.Count; i++)
            {
                AnimationClip baseClip = _overrideList[i].Key;
                AnimationClip defaultClip = _overrideList[i].Value;

                if (defaultClip == null)
                {
                    defaultClip = baseClip;
                }

                _defaultOverrideList.Add(new KeyValuePair<AnimationClip, AnimationClip>(baseClip, defaultClip));
            }

            _lastAppliedKey = null;
        }

        public static bool TryGetCustomClip(string name, out AnimationClip clip)
        {
            LoadAssets();
            return _customClips.TryGetValue(name, out clip) && clip != null;
        }

        private static void Apply(Player p)
        {
            if (_override == null || p?.m_animator == null || _overrideList == null || _defaultOverrideList == null)
            {
                return;
            }

            Dictionary<string, string> map = BuildCurrentMap(p, out string appliedKey);

            if (appliedKey == _lastAppliedKey)
            {
                return;
            }

            _lastAppliedKey = appliedKey;

            bool changed = false;

            for (int i = 0; i < _overrideList.Count; i++)
            {
                AnimationClip baseClip = _defaultOverrideList[i].Key;
                AnimationClip targetClip = _defaultOverrideList[i].Value;

                if (baseClip != null && map != null && map.TryGetValue(baseClip.name, out string replacementName))
                {
                    if (_customClips.TryGetValue(replacementName, out AnimationClip replacement) && replacement != null)
                    {
                        targetClip = replacement;
                    }
                }

                if (_overrideList[i].Value != targetClip)
                {
                    _overrideList[i] = new KeyValuePair<AnimationClip, AnimationClip>(baseClip, targetClip);
                    changed = true;
                }
            }

            if (changed)
            {
                _override.ApplyOverrides(_overrideList);
            }
        }

        private static Dictionary<string, string> BuildCurrentMap(Player p, out string appliedKey)
        {
            Dictionary<string, string> combined = null;

            string weaponName = "";
            string consumableName = "";

            ItemDrop.ItemData weapon = p.GetCurrentWeapon();

            if (weapon != null && weapon.m_shared != null)
            {
                weaponName = weapon.m_shared.m_name;

                if (_weaponMaps.TryGetValue(weaponName, out Dictionary<string, string> weaponMap))
                {
                    combined = new Dictionary<string, string>(weaponMap);
                }
            }

            if (!string.IsNullOrEmpty(_activeConsumable) && Time.time <= _activeConsumableUntil)
            {
                consumableName = _activeConsumable;

                if (_consumableMaps.TryGetValue(_activeConsumable, out Dictionary<string, string> consumableMap))
                {
                    if (combined == null)
                    {
                        combined = new Dictionary<string, string>();
                    }

                    foreach (KeyValuePair<string, string> pair in consumableMap)
                    {
                        combined[pair.Key] = pair.Value;
                    }
                }
            }
            else
            {
                _activeConsumable = null;
                _activeConsumableUntil = 0f;
            }

            appliedKey = weaponName + "|" + consumableName;

            if (combined == null || combined.Count == 0)
            {
                appliedKey = "";
                return null;
            }

            return combined;
        }

        private static void ActivateConsumableOverride(Player p, ItemDrop.ItemData item)
        {
            if (p == null || item == null || item.m_shared == null)
            {
                return;
            }

            LoadAssets();

            if (!TryGetConsumableMapKey(item, out string itemName))
            {
                return;
            }

            float duration = 2.5f;

            if (_consumableDurations.TryGetValue(itemName, out float configuredDuration))
            {
                duration = configuredDuration;
            }

            if (_consumableBuffs.TryGetValue(itemName, out string buffName))
            {
                _activeConsumableBuff = buffName;
                _activeConsumableBuffUntil = Time.time + Mathf.Max(0.1f, duration);
                PungusConsumableBuffState.ActivateDirect(buffName, null, duration);
            }
            else
            {
                PungusConsumableBuffState.ActivateForItem(item);
            }

            _activeConsumable = itemName;
            _activeConsumableUntil = Time.time + Mathf.Max(0.1f, duration);
            _lastAppliedKey = null;

            Refresh(p);
        }

        private static void Refresh(Player p)
        {
            LoadAssets();
            Init(p);
            Apply(p);
        }

        [HarmonyPatch(typeof(Player), "Start")]
        private static class StartPatch
        {
            private static void Postfix(Player __instance)
            {
                if (__instance == Player.m_localPlayer)
                {
                    Refresh(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(Player), "OnSpawned")]
        private static class SpawnPatch
        {
            private static void Postfix(Player __instance)
            {
                if (__instance == Player.m_localPlayer)
                {
                    Refresh(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(Player), "Update")]
        private static class UpdatePatch
        {
            private static float nextRefreshTime;

            private static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer)
                {
                    return;
                }

                if (Time.time < nextRefreshTime)
                {
                    return;
                }

                nextRefreshTime = Time.time + 0.1f;

                Refresh(__instance);
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        private static class EquipPatch
        {
            private static void Postfix(Humanoid __instance)
            {
                if (__instance is Player p && p == Player.m_localPlayer)
                {
                    Refresh(p);
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        private static class UnequipPatch
        {
            private static void Postfix(Humanoid __instance)
            {
                if (__instance is Player p && p == Player.m_localPlayer)
                {
                    Refresh(p);
                }
            }
        }

        [HarmonyPatch]
        private static class UseItemPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return AccessTools.GetDeclaredMethods(typeof(Humanoid))
                    .Concat(AccessTools.GetDeclaredMethods(typeof(Player)))
                    .Where(method => method.Name == "UseItem" || method.Name == "ConsumeItem");
            }

            private static void Prefix(Humanoid __instance, object[] __args)
            {
                if (!(__instance is Player p) || p != Player.m_localPlayer)
                {
                    return;
                }

                if (__args == null)
                {
                    return;
                }

                for (int i = 0; i < __args.Length; i++)
                {
                    if (__args[i] is ItemDrop.ItemData item)
                    {
                        ActivateConsumableOverride(p, item);
                        return;
                    }
                }
            }
        }
    }
}