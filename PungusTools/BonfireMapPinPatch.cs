using HarmonyLib;
using System.Collections.Generic;

namespace PungusSouls
{
    [HarmonyPatch(typeof(Minimap), "UpdatePins")]
    public static class BonfireMapPinPatch
    {
        private static void Postfix(Minimap __instance)
        {
            List<Minimap.PinData> pins = Traverse.Create(__instance)
                .Field("m_pins")
                .GetValue<List<Minimap.PinData>>();

            if (pins == null)
            {
                return;
            }

            foreach (Minimap.PinData pin in pins)
            {
                if (!BonfireManager.IsBonfirePin(pin))
                {
                    continue;
                }

                BonfireManager.ApplyBonfireIcon(pin);
            }
        }
    }
}
