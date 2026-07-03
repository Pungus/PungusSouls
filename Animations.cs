using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ItemManager;

namespace PungusSouls
{
    public static class Animations
    {
        private static readonly Dictionary<string, AnimationClip> _customClips = new();
        private static readonly Dictionary<string, Dictionary<string, string>> _weaponMaps = new();

        private static AnimatorOverrideController _override;
        private static RuntimeAnimatorController _baseRuntime;

        private static List<KeyValuePair<AnimationClip, AnimationClip>> _overrideList;

        private static bool _assetsLoaded;
        private static string _lastWeapon;

        // ---------------- INIT ----------------

        public static void LoadAssets()
        {
            if (_assetsLoaded) return;
            _assetsLoaded = true;

            var asset = PrefabManager.RegisterAssetBundle("souls");

            _customClips["AbyssGreatswordAttack3"] = asset.LoadAsset<AnimationClip>("AbyssGreatswordAttack2");

            _customClips["Ultragreatsword_Attack1"] = asset.LoadAsset<AnimationClip>("Ultragreatsword_Attack1");
            _customClips["Ultragreatsword_Attack2"] = asset.LoadAsset<AnimationClip>("Ultragreatsword_Attack2");
            _customClips["Ultragreatsword_Attack3"] = asset.LoadAsset<AnimationClip>("Ultragreatsword_Attack3");

            _customClips["RKPG_Attack_1"] = asset.LoadAsset<AnimationClip>("RKPG_Attack_1");
            _customClips["RKPG_Attack_2"] = asset.LoadAsset<AnimationClip>("RKPG_Attack_2");
            _customClips["RKPG_Attack_3"] = asset.LoadAsset<AnimationClip>("RKPG_Attack_3");

            _customClips["GS_Idle"] = asset.LoadAsset<AnimationClip>("GS_Idle");
            _customClips["GS_Sprint_Forward"] = asset.LoadAsset<AnimationClip>("GS_Sprint_Forward");
            _customClips["GS_Jog_Forward"] = asset.LoadAsset<AnimationClip>("GS_Jog_Forward");
            _customClips["GS_Walk_Forward"] = asset.LoadAsset<AnimationClip>("GS_Walk_Forward");
            _customClips["GS_Strafe_Left"] = asset.LoadAsset<AnimationClip>("GS_Strafe_Left_Fast");
            _customClips["GS_Strafe_Right"] = asset.LoadAsset<AnimationClip>("GS_Strafe_Right_Fast");

            _customClips["Greatsword_Attack1"] = asset.LoadAsset<AnimationClip>("Greatsword_Attack1");
            _customClips["Greatsword_Attack2"] = asset.LoadAsset<AnimationClip>("Greatsword_Attack2");
            _customClips["Greatsword_Attack3"] = asset.LoadAsset<AnimationClip>("Greatsword_Attack3");

            _customClips["Greatbow_Draw"] = asset.LoadAsset<AnimationClip>("Greatbow_Draw");
            _customClips["Greatbow_Hold"] = asset.LoadAsset<AnimationClip>("Greatbow_Hold");
            _customClips["Greatbow_Fire"] = asset.LoadAsset<AnimationClip>("Greatbow_Fire");

            _customClips["Curvedsword1"] = asset.LoadAsset<AnimationClip>("Curvedsword1");
            _customClips["Curvedsword2"] = asset.LoadAsset<AnimationClip>("Curvedsword2");
            _customClips["Curvedsword3"] = asset.LoadAsset<AnimationClip>("Curvedsword3");
            _customClips["Curvedsword_alt"] = asset.LoadAsset<AnimationClip>("Curvedsword_alt");

            _customClips["MorneRage"] = asset.LoadAsset<AnimationClip>("MorneRage");

            _customClips["Dualswords1"] = asset.LoadAsset<AnimationClip>("Dualswords1");
            _customClips["Dualswords2"] = asset.LoadAsset<AnimationClip>("Dualswords2");
            _customClips["Dualswords3"] = asset.LoadAsset<AnimationClip>("Dualswords3");
            _customClips["Dualswords_alt"] = asset.LoadAsset<AnimationClip>("Dualswordsalt");

            _customClips["Mlgs_heavy"] = asset.LoadAsset<AnimationClip>("Mlgs_heavy");

            _customClips["2handclub_1"] = asset.LoadAsset<AnimationClip>("2handclub_1");
            _customClips["2handclub_2"] = asset.LoadAsset<AnimationClip>("2handclub_2");
            _customClips["2handclub_3"] = asset.LoadAsset<AnimationClip>("2handclub_3");

            _customClips["BuffWeapon"] = asset.LoadAsset<AnimationClip>("BuffWeapon");

            _weaponMaps["$ps_sunlightsword"] = new Dictionary<string, string>
            {
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

            _weaponMaps["$ps_abyssgreatsword"] = new Dictionary<string, string>
            {
                { "Greatsword Idle", "GS_Idle" },
                { "Greatsword Run", "GS_Sprint_Forward" },
                { "Greatsword Jog", "GS_Jog_Forward" },
                { "Greatsword Walking", "GS_Walk_Forward" },
                { "Greatsword Jog Strafe Left", "GS_Strafe_Left" },
                { "Greatsword Jog Strafe Right", "GS_Strafe_Right" },
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
                { "Greatsword Idle", "GS_Idle" },
                { "Greatsword Run", "GS_Sprint_Forward" },
                { "Greatsword Jog", "GS_Jog_Forward" },
                { "Greatsword Walking", "GS_Walk_Forward" },
                { "Greatsword Jog Strafe Left", "GS_Strafe_Left" },
                { "Greatsword Jog Strafe Right", "GS_Strafe_Right" },
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
                { "Knife_Secondary", "Dualswordsalt" }
            };

            _weaponMaps["$ps_DragonTooth"] = new Dictionary<string, string>
            {
                { "Attack1", "2handclub_1" },
                { "Attack2", "2handclub_2" },
                { "Attack3", "2handclub_3" }
            };
        }
        private static void Init(Player p)
        {
            if (p?.m_animator == null) return;

            var runtime = p.m_animator.runtimeAnimatorController;
            if (runtime == null) return;

            if (_override != null && _baseRuntime == runtime)
                return;

            _baseRuntime = runtime;
            _override = new AnimatorOverrideController(runtime);
            p.m_animator.runtimeAnimatorController = _override;

            _overrideList = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            _override.GetOverrides(_overrideList);

            _lastWeapon = null;
        }

        private static void Apply(Player p)
        {
            if (_override == null || p?.m_animator == null) return;

            var weapon = p.GetCurrentWeapon();
            if (weapon == null) return;

            var weaponName = weapon.m_shared.m_name;

            if (weaponName == _lastWeapon)
                return;

            _lastWeapon = weaponName;

            if (!_weaponMaps.TryGetValue(weaponName, out var map))
                return;

            bool changed = false;

            for (int i = 0; i < _overrideList.Count; i++)
            {
                var baseClip = _overrideList[i].Key;
                if (baseClip == null) continue;

                if (map.TryGetValue(baseClip.name, out var replacementName) &&
                    _customClips.TryGetValue(replacementName, out var replacement))
                {
                    if (_overrideList[i].Value != replacement)
                    {
                        _overrideList[i] = new KeyValuePair<AnimationClip, AnimationClip>(baseClip, replacement);
                        changed = true;
                    }
                }
            }

            if (changed)
                _override.ApplyOverrides(_overrideList);
        }

        private static void Refresh(Player p)
        {
            Init(p);
            Apply(p);
        }

        [HarmonyPatch(typeof(Player), "Start")]
        private static class StartPatch
        {
            static void Postfix(Player __instance)
            {
                if (__instance == Player.m_localPlayer)
                    Refresh(__instance);
            }
        }

        [HarmonyPatch(typeof(Player), "OnSpawned")]
        private static class SpawnPatch
        {
            static void Postfix(Player __instance)
            {
                if (__instance == Player.m_localPlayer)
                    Refresh(__instance);
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        private static class EquipPatch
        {
            static void Postfix(Humanoid __instance)
            {
                if (__instance is Player p && p == Player.m_localPlayer)
                    Refresh(p);
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        private static class UnequipPatch
        {
            static void Postfix(Humanoid __instance)
            {
                if (__instance is Player p && p == Player.m_localPlayer)
                    Refresh(p);
            }
        }
    }
}