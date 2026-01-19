using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MOExpandedLite.HarmonyPatches;

[HarmonyPatch(typeof(FertilityGrid), nameof(FertilityGrid.FertilityAt))]
public static class FertilityGrid_FertilityAt_Patch
{
  public static bool Prefix(IntVec3 c, Map ___map, ref float __result)
  {
    if (___map.terrainGrid.TerrainAt(c) != MOL_DefOf.MOL_SoilTilled)
      return true;

    TerrainDef underTerrain = ___map.terrainGrid.UnderTerrainAt(c);
    float baseFertility = underTerrain?.fertility ?? 1f;

    // 120% of base fertility
    float raw = baseFertility * 1.2f;

    // Round down to nearest 10% (0.1)
    float rounded = Mathf.Floor(raw * 10f) / 10f;

    // Clamp to +20% to +30% over original
    float min = baseFertility + 0.2f;
    float max = baseFertility + 0.3f;
    __result = Mathf.Clamp(rounded, min, max);

    return false;
  }
}
