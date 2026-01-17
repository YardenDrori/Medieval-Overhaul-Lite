using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace MOExpandedLite.HarmonyPatches;

/// <summary>
/// Harmony patch that intercepts Pawn.ButcherProducts to add fat and bones yield
/// </summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.ButcherProducts))]
public static class ButcherProducts_Patch
{
  private static float meatToFatRatio = 0.15f;
  private static float meatToBoneRatio = 0.1f;

  [HarmonyPostfix]
  public static IEnumerable<Thing> Postfix(
    IEnumerable<Thing> __result,
    Pawn __instance,
    Pawn butcher,
    float efficiency
  )
  {
    foreach (Thing thing in __result)
    {
      if (thing.def.IsMeat)
      {
        int meatCount = thing.stackCount;
        Thing bones = ThingMaker.MakeThing(ThingDefOf_MOExpandedLite.MOL_Bone);
        bones.stackCount = (int)(meatCount * meatToBoneRatio);
        yield return bones;
        Thing fat = ThingMaker.MakeThing(ThingDefOf_MOExpandedLite.MOL_Fat);
        fat.stackCount = (int)(meatCount * meatToFatRatio);
        yield return fat;
        thing.stackCount -= fat.stackCount;
      }
      yield return thing;
    }
  }
}
