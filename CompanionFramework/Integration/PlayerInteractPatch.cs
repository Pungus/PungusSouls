using HarmonyLib;
using UnityEngine;

namespace Integration
{

    [HarmonyPatch(typeof(Player), "Interact")]
    public static class Player_Interact_Patch
    {
        static bool Prefix(Player __instance, GameObject go, bool hold, bool alt)
        {
            if (go == null)
                return true;

            var agent = go.GetComponentInParent<AgentContainerComponent>();

            if (agent != null)
            {
                //Debug.Log("[Agent] ✅ Intercepted NPC interaction");

                agent.Interact(__instance, hold, alt);

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Hud), "InRadial")]
    public static class Agent_Hud_InRadial_Patch
    {
        static void Postfix(ref bool __result)
        {
            if (!__result && AgentContainerComponent.IsRadialOpen)
            {
                __result = true;
            }
        }
    }
    [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
    public static class Agent_Hud_UpdateCrosshair_Patch
    {
        static void Postfix(Hud __instance)
        {
            if (!AgentContainerComponent.IsRadialOpen)
                return;

            if (__instance.m_crosshair != null)
                __instance.m_crosshair.gameObject.SetActive(false);

            if (__instance.m_hoverName != null)
                __instance.m_hoverName.text = "";

            if (__instance.m_pieceHealthRoot != null)
                __instance.m_pieceHealthRoot.gameObject.SetActive(false);

            if (__instance.m_crosshairBow != null)
                __instance.m_crosshairBow.gameObject.SetActive(false);
        }
    }
    [HarmonyPatch(typeof(Player), "SetControls")]
    public static class Agent_Player_SetControls_Patch
    {
        static void Prefix(
            ref Vector3 movedir,
            ref bool attack,
            ref bool attackHold,
            ref bool secondaryAttack,
            ref bool secondaryAttackHold,
            ref bool block,
            ref bool blockHold,
            ref bool jump,
            ref bool crouch,
            ref bool run,
            ref bool autoRun,
            ref bool dodge)
        {
            if (!AgentContainerComponent.IsRadialOpen && Time.time > AgentContainerComponent.BlockInputUntil)
                return;

            movedir = Vector3.zero;
            attack = false;
            attackHold = false;
            secondaryAttack = false;
            secondaryAttackHold = false;
            block = false;
            blockHold = false;
            jump = false;
            crouch = false;
            run = false;
            autoRun = false;
            dodge = false;
        }
    }
    [HarmonyPatch(typeof(Character), "GetHoverText")]
    public static class Character_HoverText_Patch
    {
        static void Postfix(Character __instance, ref string __result)
        {
            var agent = __instance.GetComponent<AgentComponent>();

            if (agent == null)
                return;

            // ✅ Replace vanilla text completely
            __result =
                $"{__instance.GetHoverName()}\n" +
                "[E] Open Inventory\n" +
                "[Shift + E] Set Home Zone";
        }
    }
}