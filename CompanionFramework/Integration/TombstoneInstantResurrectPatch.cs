using HarmonyLib;
using UnityEngine;

namespace Modules.Death
{
    [HarmonyPatch]
    public static class TombstoneInstantResurrectPatch
    {
        [HarmonyPatch(typeof(TombStone), "Interact")]
        [HarmonyPrefix]
        private static bool TombStoneInteractPrefix(TombStone __instance, Humanoid character, bool hold, bool alt)
        {
            if (__instance == null || character == null)
                return true;

            AgentTombstoneResurrect resurrect = __instance.GetComponent<AgentTombstoneResurrect>();

            if (resurrect == null)
                return true;

            if (hold)
                return false;

            if (alt)
                return true;

            Player player = character as Player ?? Player.m_localPlayer;

            if (resurrect.TryInstantResurrect(player, out string message))
                return false;

            if (!string.IsNullOrEmpty(message))
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, message);

            return false;
        }

        [HarmonyPatch(typeof(TombStone), "GetHoverText")]
        [HarmonyPostfix]
        private static void TombStoneHoverTextPostfix(TombStone __instance, ref string __result)
        {
            if (__instance == null)
                return;

            AgentTombstoneResurrect resurrect = __instance.GetComponent<AgentTombstoneResurrect>();

            if (resurrect == null)
                return;

            string hover = resurrect.GetResurrectHoverText();

            if (string.IsNullOrEmpty(hover))
                return;

            __result = hover;
        }
    }
}
