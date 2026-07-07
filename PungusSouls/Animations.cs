using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ItemManager;
using System.Reflection;



namespace PungusSouls
{
    [HarmonyPatch]
    public class Animations
    {
        internal const string ModName = "PungusSouls";
        internal const string Author = "Pungus";
        private const string ModGUID = Author + "." + ModName;
        private static readonly Dictionary<string, string> CoolAnimation = new();
        private static Dictionary<string, AnimationClip> _externalAnimations = new();
        private static bool _firstInit;
        internal static RuntimeAnimatorController MyNewAnimation;
        internal static RuntimeAnimatorController OrigAnimation;
        public void Awake()
        {
            AssetBundle asset = PrefabManager.RegisterAssetBundle("anims", "assets");

            CoolAnimation.Add("Dance", "MyCoolDance1");
            CoolAnimation.Add("swing_sledge", "GreatSwordSlashNew");

            _externalAnimations.Add("MyCoolDance1", asset.LoadAsset<AnimationClip>("MyCoolDance1.anim"));
            _externalAnimations.Add("GreatSwordSlashNew", asset.LoadAsset<AnimationClip>("GreatSwordSlashNew.anim"));

            Assembly assembly = Assembly.GetExecutingAssembly();
            Harmony harmony = new(ModGUID);
            harmony.PatchAll(assembly);
        }
       
        [HarmonyPatch(typeof(Player), nameof(Player.Start))]
        static class TESTPATCHPLAYERANIMS
        {
            static void Postfix(Player __instance)
            {
                if (!_firstInit)
                {
                    _firstInit = true;
                    OrigAnimation = MakeAoc(new Dictionary<string, string>(), __instance.m_animator.runtimeAnimatorController);
                    MyNewAnimation = MakeAoc(CoolAnimation, __instance.m_animator.runtimeAnimatorController);
                }
                if (Player.m_localPlayer)
                {
                    __instance.m_animator.runtimeAnimatorController = MyNewAnimation;
                }
            }
        }
        public static RuntimeAnimatorController MakeAoc(IReadOnlyDictionary<string, string> replacement, RuntimeAnimatorController original)
        {
            AnimatorOverrideController aoc = new AnimatorOverrideController(original);
            List<KeyValuePair<AnimationClip, AnimationClip>> anims = new();
            foreach (AnimationClip animation in aoc.animationClips)
            {
                string name = animation.name;
                if (replacement.ContainsKey(name))
                {
                    AnimationClip newClip = Object.Instantiate(_externalAnimations[replacement[name]]);
                    anims.Add(new KeyValuePair<AnimationClip, AnimationClip>(animation, newClip));
                }
                else
                {
                    anims.Add(new KeyValuePair<AnimationClip, AnimationClip>(animation, animation));
                }
            }

            aoc.ApplyOverrides(anims);
            return aoc;
        }
    }
}