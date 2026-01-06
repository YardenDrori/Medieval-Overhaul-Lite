using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MOExpandedLite
{
  [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
  public static class RecipeProducts_StovesUseBowls_Patch
  {
    [HarmonyPostfix]
    public static void Postfix(
      RecipeDef recipeDef,
      Pawn worker,
      List<Thing> ingredients,
      Thing dominantIngredient,
      IBillGiver billGiver
    )
    {
      Building_WorkTable billGiverWorkTable = billGiver as Building_WorkTable;
      if (billGiverWorkTable == null)
      {
        return;
      }
      CompBowlStorage comp = billGiverWorkTable.TryGetComp<CompBowlStorage>();
      if (comp == null)
      {
        return;
      }
      comp.CookedRecipe(recipeDef);
    }
  }
}
