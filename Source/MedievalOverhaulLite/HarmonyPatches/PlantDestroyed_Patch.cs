using HarmonyLib;
using RimWorld;
using Verse;

namespace MOExpandedLite.HarmonyPatches;

[HarmonyPatch(typeof(Plant), nameof(Plant.Kill))]
public static class PlantDestroyed_Patch
{
  [HarmonyPrefix]
  public static void Prefix(Plant __instance, DamageInfo? dinfo)
  {
    // Only process if it's a tree
    if (__instance.def.plant == null || !__instance.def.plant.IsTree)
    {
      return;
    }

    // Check if killed by fire
    if (!dinfo.HasValue || dinfo.Value.Def != DamageDefOf.Flame)
    {
      return;
    }

    Map map = __instance.Map;
    if (map == null)
    {
      return;
    }

    // Get the handler component
    MapComponent_BurnedTreesHandler handler = map.GetComponent<MapComponent_BurnedTreesHandler>();
    if (handler == null)
    {
      Log.Error("[Medieval Overhaul Lite] Failed to find MapComponent_BurnedTreesHandler");
      return;
    }

    handler.NotifyTreeBurned();
  }
}
