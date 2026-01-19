using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace MOExpandedLite.HarmonyPatches;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.ButcherProducts))]
public static class ButcherProducts_Patch
{
  private const float MEAT_TO_BONE_RATIO = 0.10f;
  private const float MEAT_TO_FAT_RATIO = 0.15f;
  private const float MEAT_NUTRITION = 0.05f;
  private const float BONE_NUTRITION = 0.03f;
  private const float FAT_NUTRITION = 0.08f;

  [HarmonyPostfix]
  public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result)
  {
    List<Thing> results = __result.ToList();
    Thing meatThing = results.FirstOrDefault(t => t.def.IsMeat);

    if (meatThing != null && meatThing.stackCount > 0)
    {
      int originalMeatCount = meatThing.stackCount;

      // Calculate byproduct counts
      int boneCount = (int)(originalMeatCount * MEAT_TO_BONE_RATIO);
      int fatCount = (int)(originalMeatCount * MEAT_TO_FAT_RATIO);

      // Calculate nutrition budget
      float totalOriginalNutrition = originalMeatCount * MEAT_NUTRITION;
      float byproductNutrition = (boneCount * BONE_NUTRITION) + (fatCount * FAT_NUTRITION);
      float remainingForMeat = totalOriginalNutrition - byproductNutrition;

      // New meat count
      int newMeatCount = (int)(remainingForMeat / MEAT_NUTRITION);

      // Calculate current total
      float currentTotal = (newMeatCount * MEAT_NUTRITION) + byproductNutrition;

      // Fill gap with bones until we reach original total
      while (currentTotal < totalOriginalNutrition)
      {
        boneCount++;
        currentTotal += BONE_NUTRITION;
      }

      // Update meat count
      meatThing.stackCount = newMeatCount;

      // Add byproducts
      if (boneCount > 0)
      {
        Thing bones = ThingMaker.MakeThing(ThingDefOf_MOExpandedLite.MOL_Bone);
        bones.stackCount = boneCount;
        results.Add(bones);
      }

      if (fatCount > 0)
      {
        Thing fat = ThingMaker.MakeThing(ThingDefOf_MOExpandedLite.MOL_Fat);
        fat.stackCount = fatCount;
        results.Add(fat);
      }
    }

    foreach (Thing thing in results)
    {
      yield return thing;
    }
  }
}
