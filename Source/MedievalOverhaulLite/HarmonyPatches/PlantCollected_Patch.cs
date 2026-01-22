using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MOExpandedLite.HarmonyPatches;

[HarmonyPatch(typeof(Plant), nameof(Plant.PlantCollected))]
public static class PlantCollected_Patch
{
  [HarmonyPostfix]
  public static void Postfix(Plant __instance, Pawn by, PlantDestructionMode plantDestructionMode)
  {
    if (!__instance.def.plant.IsTree)
    {
      return;
    }
    float points = 0;
    points += __instance.def.plant.harvestYield;
    points += __instance.Growth * __instance.def.plant.growDays;
    MapComponent_TreesChoppedHandler treesChoppedHandler =
      __instance.Map.GetComponent<MapComponent_TreesChoppedHandler>();
    if (treesChoppedHandler == null)
    {
      Log.Error("[Medieval Overhaul Lite] Failed to find MapComponent_TreesChoppedHandler");
      return;
    }
    treesChoppedHandler.NotifyTreeChopped(points, by);
    return;
  }
}
