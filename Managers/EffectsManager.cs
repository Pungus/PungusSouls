using HarmonyLib;
using UnityEngine;

namespace PungusSouls.PungusTools
{
    public class AnimationEventHandler : MonoBehaviour
    {
        private static GameObject BuffLightningPrefab;
        private static GameObject ActiveBuffVFX;

        public void ApplyWeaponBuff()
        {
            //Debug.Log("ApplyWeaponBuff CALLED");

            var player = GetComponentInParent<Player>();

            if (!player)
            {
                //Debug.Log("Player not found");
                return;
            }

            if (player != Player.m_localPlayer)
            {
                return;
            }

            ApplyVFX(player);
            ApplyStatus(player);
        }

        private void ApplyVFX(Player player)
        {
            if (ActiveBuffVFX)
            {
                Destroy(ActiveBuffVFX);
            }

            var weaponVisual = player.m_visEquipment?.m_rightItemInstance;

            if (!weaponVisual || !BuffLightningPrefab)
            {
                return;
            }

            var spawned = new EffectList
            {
                m_effectPrefabs = new[]
                {
            new EffectList.EffectData
            {
                m_prefab = BuffLightningPrefab,
                m_enabled = true,
                m_attach = true,
                m_follow = true,
                m_inheritParentRotation = true
            }
        }
            }.Create(
                weaponVisual.transform.position,
                weaponVisual.transform.rotation,
                weaponVisual.transform);

            if (spawned.Length > 0)
            {
                ActiveBuffVFX = spawned[0];
            }
        }

        private void ApplyStatus(Player player)
        {
            if (ObjectDB.instance == null)
            {
                //Debug.Log("ObjectDB not available");
                return;
            }

            var se = ObjectDB.instance.GetStatusEffect(
                "SE_Lightningbuff".GetStableHashCode());

            if (se == null)
            {
                //Debug.Log("SE_Lightningbuff NOT FOUND");
                return;
            }

            player.m_seman.AddStatusEffect(se, true);

            //Debug.Log("SE_Lightningbuff applied");
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class ZNetScene_Awake_Patch
        {
            private static void Postfix(ZNetScene __instance)
            {
                //Debug.Log("AnimationEventHandler patch running");

                BuffLightningPrefab = __instance.GetPrefab("buff_lightning");

                var playerPrefab = __instance.GetPrefab("Player");

                if (!playerPrefab)
                {
                    //Debug.Log("Player prefab not found");
                    return;
                }

                int attached = 0;

                foreach (var transform in playerPrefab.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.GetComponent<AnimationEventHandler>())
                    {
                        continue;
                    }

                    transform.gameObject.AddComponent<AnimationEventHandler>();
                    attached++;
                }
            }
        }
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyAttack))]
        private static class SEMan_ModifyAttack_Patch
        {
            private static void Postfix(
                SEMan __instance,
                Skills.SkillType skill,
                ref HitData hitData)
            {
                if (!__instance.HaveStatusEffect(
                        "SE_Lightningbuff".GetStableHashCode()))
                {
                    return;
                }

                hitData.m_damage.m_lightning += 40f;
            }
        }
    }
}