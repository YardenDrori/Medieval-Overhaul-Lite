using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MOExpandedLite;

[HarmonyPatch(typeof(WildPlantSpawner), "CalculatePlantsWhichCanGrowAt")]
public static class CalculatePlantsWhichCanGrowAt_Patch
{
  [HarmonyPostfix]
  public static void CalculatePlantsWhichCanGrowAt_Postfix(
    IntVec3 c,
    List<ThingDef> outPlants,
    bool cavePlants,
    WildPlantSpawner __instance
  )
  {
    if (cavePlants)
      return; // Don't mess with cave plants

    Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
    var mapComp = map.GetComponent<MapComponent_TileCrops>();
    if (mapComp == null)
      return;

    // Add all pool crops that can grow at this location
    foreach (var kvp in mapComp.GetAllTilePlants())
    {
      ThingDef plant = kvp.Key;
      if (!outPlants.Contains(plant) && plant.CanEverPlantAt(c, map))
      {
        outPlants.Add(plant);
      }
    }
  }
}

[HarmonyPatch(typeof(WildPlantSpawner), "PlantChoiceWeight")]
public static class PlantChoiceWeight_Patch
{
  // public static HashSet<ThingDef> idk = new();

  [HarmonyPostfix]
  public static void PlantChoiceWeight_Postfix(
    ThingDef plantDef,
    ref float __result,
    WildPlantSpawner __instance
  )
  {
    Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
    var mapComp = map.GetComponent<MapComponent_TileCrops>();

    if (mapComp != null && __result == 0)
    {
      if (__result == 0)
      {
        float weight = mapComp.GetWeightForPlant(plantDef);
        __result = weight; // Override with our pool weight
      }
    }
  }
}

[HarmonyPatch(typeof(WildPlantSpawner), "WildPlantSpawnerTickInternal")]
public class WildPlantSpawnerTickInternal_Patch
{
  public static int allowedCalls = 0;

  [HarmonyPrefix]
  public static bool DisableVanillaSpawning()
  {
    if (allowedCalls > 0)
    {
      allowedCalls--;
      return true;
    }
    return false;
  }
}
