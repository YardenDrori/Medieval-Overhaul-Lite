using HarmonyLib;
using RimWorld;
using Verse;

namespace MOExpandedLite.HarmonyPatches;

[HarmonyPatch(typeof(Plant), nameof(Plant.PlantCollected))]
public static class PlantCollected_Patch
{
  [HarmonyPrefix]
  public static void Prefix(Plant __instance, out Map __state)
  {
    __state = __instance.Map;
  }

  [HarmonyPostfix]
  public static void Postfix(
    Plant __instance,
    Pawn by,
    PlantDestructionMode plantDestructionMode,
    Map __state
  )
  {
    if (__state == null || !__instance.def.plant.IsTree)
    {
      return;
    }
    float points = 0;
    points += __instance.def.plant.harvestYield;
    points += __instance.Growth * __instance.def.plant.growDays;
    MapComponent_TreesChoppedHandler treesChoppedHandler =
      __state.GetComponent<MapComponent_TreesChoppedHandler>();
    if (treesChoppedHandler == null)
    {
      Log.Error("[Medieval Overhaul Lite] Failed to find MapComponent_TreesChoppedHandler");
      return;
    }
    treesChoppedHandler.NotifyTreeChopped(points, by);
  }
}
