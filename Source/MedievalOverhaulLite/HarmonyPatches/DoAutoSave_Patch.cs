using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MOExpandedLite;

[HarmonyPatch(typeof(Autosaver), "DoAutosave")]
public static class Autosaver_Patch
{
  [HarmonyPostfix]
  public static void TriggerPlantBatch()
  {
    // Run plant spawning right after autosave
    foreach (Map map in Find.Maps)
    {
      if (map == null)
      {
        continue;
      }
      var spawner = map.wildPlantSpawner;
      if (spawner != null)
      {
        DoBatchSpawn(spawner);
      }
    }
  }

  private static void DoBatchSpawn(WildPlantSpawner spawner)
  {
    Map map = Traverse.Create(spawner).Field("map").GetValue<Map>();
    float plantDensityFactor = spawner.CurrentPlantDensityFactor;
    float wholeMapNumDesiredPlants = spawner.CurrentWholeMapNumDesiredPlants;

    int cellsToCheck = Mathf.CeilToInt(map.Area * 0.0001f) * 60000;

    for (int i = 0; i < cellsToCheck; i++)
    {
      IntVec3 c = map.cellsInRandomOrder.Get(i % map.Area);

      float mtb = (GoodRoofForCavePlant(spawner, c) ? 130f : map.BiomeAt(c).wildPlantRegrowDays);

      if (Rand.MTBEventOccurs(mtb, 60000f, 60000f) && CanRegrowAt(spawner, c))
      {
        spawner.CheckSpawnWildPlantAt(c, plantDensityFactor, wholeMapNumDesiredPlants);
      }
    }
  }

  // Helper methods same as before
  private static bool GoodRoofForCavePlant(WildPlantSpawner spawner, IntVec3 c)
  {
    return (bool)Traverse.Create(spawner).Method("GoodRoofForCavePlant", c).GetValue();
  }

  private static bool CanRegrowAt(WildPlantSpawner spawner, IntVec3 c)
  {
    return (bool)Traverse.Create(spawner).Method("CanRegrowAt", c).GetValue();
  }
}
