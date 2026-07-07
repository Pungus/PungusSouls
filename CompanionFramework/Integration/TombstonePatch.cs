using HarmonyLib;
using Modules.Death;

namespace Integration
{
    [HarmonyPatch(typeof(TombStone), "Interact")]
    public static class TombstoneInteractPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(TombStone __instance, Humanoid character, bool hold, ref bool __result)
        {
            AgentTombstoneResurrect resurrect = __instance.GetComponent<AgentTombstoneResurrect>();
            if (resurrect == null)
                return true;

            Player player = character as Player;
            if (player == null)
                return true;

            if (resurrect.TryStartResurrect(player, hold))
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}

[HarmonyPatch(typeof(TombStone), "GetHoverText")]
public static class TombstoneHoverTextPatch
{
    [HarmonyPostfix]
    private static void Postfix(TombStone __instance, ref string __result)
    {
        AgentTombstoneResurrect resurrect = __instance.GetComponent<AgentTombstoneResurrect>();
        if (resurrect == null)
            return;

        string text = resurrect.GetResurrectHoverText();
        if (!string.IsNullOrEmpty(text))
        {
            __result = Localization.instance.Localize(text);
        }
    }
}

[HarmonyPatch(typeof(TombStone), "UpdateDespawn")]
    public static class TombstonePreventDespawnPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(TombStone __instance)
        {
            AgentTombstoneResurrect resurrect = __instance.GetComponent<AgentTombstoneResurrect>();

            if (resurrect == null)
                return true;

            return false;
        }
    }
[HarmonyPatch(typeof(TombStone), "Awake")]
public static class TombstoneAwakeResurrectPatch
{
    [HarmonyPostfix]
    private static void Postfix(TombStone __instance)
    {
        ZNetView nview = __instance.GetComponent<ZNetView>();
        ZDO zdo = nview?.GetZDO();

        if (zdo == null)
            return;

        string owner = zdo.GetString(AgentTombstoneResurrect.RespawnOwnerHash);

        if (!string.IsNullOrEmpty(owner) && __instance.GetComponent<AgentTombstoneResurrect>() == null)
        {
            __instance.gameObject.AddComponent<AgentTombstoneResurrect>();
        }
    }
}