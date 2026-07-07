using System.Reflection;
using HarmonyLib;
using Modules.Death;

namespace Integration
{
    [HarmonyPatch(typeof(Container), "Awake")]
    public static class TombstoneContainerAwakePatch
    {
        private static readonly FieldInfo InventoryWidthField = AccessTools.Field(typeof(Inventory), "m_width");
        private static readonly FieldInfo InventoryHeightField = AccessTools.Field(typeof(Inventory), "m_height");

        [HarmonyPostfix]
        private static void Postfix(Container __instance)
        {
            TombStone tombstone = __instance.GetComponent<TombStone>();
            if (tombstone == null)
                return;

            ZNetView nview = __instance.GetComponent<ZNetView>();
            ZDO zdo = nview?.GetZDO();
            if (zdo == null)
                return;

            int width = zdo.GetInt(AgentTombstoneResurrect.TombInvWidthHash);
            int height = zdo.GetInt(AgentTombstoneResurrect.TombInvHeightHash);

            if (width <= 0 || height <= 0)
                return;

            Inventory inventory = __instance.GetInventory();
            if (inventory == null)
                return;

            if (inventory.GetWidth() == width && inventory.GetHeight() == height)
                return;

            if (InventoryWidthField != null)
                InventoryWidthField.SetValue(inventory, width);

            if (InventoryHeightField != null)
                InventoryHeightField.SetValue(inventory, height);
        }
    }
}