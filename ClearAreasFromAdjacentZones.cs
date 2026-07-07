using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace PungusSouls;

[HarmonyPatch(typeof(ZoneSystem), "PlaceVegetation")]
public class ClearAreasFromAdjacentZones
{
	[_003Cff97f7d9_002D7f48_002D44a2_002Da23f_002D4f039e0cea12_003ENullableContext(1)]
	private static void Prefix(ZoneSystem __instance, Vector2i zoneID, List<ZoneSystem.ClearArea> clearAreas)
	{
		for (int i = zoneID.x - 1; i <= zoneID.x + 1; i++)
		{
			for (int j = zoneID.y - 1; j <= zoneID.y + 1; j++)
			{
				if ((i != zoneID.x || j != zoneID.y) && __instance.m_locationInstances.TryGetValue(new Vector2i(i, j), out var value) && value.m_location.m_location.m_clearArea && !(value.m_location.m_exteriorRadius < 32f))
				{
					clearAreas.Add(new ZoneSystem.ClearArea(value.m_position, value.m_location.m_exteriorRadius));
				}
			}
		}
	}
}
