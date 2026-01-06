using HarmonyLib;
using RimWorld;
using Verse;

namespace MOExpandedLite
{
  [HarmonyPatch(typeof(Bill), nameof(Bill.PawnAllowedToStartAnew))]
  public static class Bill_PawnAllowedToStartAnew_Patch
  {
    [HarmonyPostfix]
    public static void Postfix(Bill __instance, ref bool __result)
    {
      // If vanilla already said no, don't bother checking
      if (!__result)
        return;

      // Check if this bill's workbench requires bowls
      Building_WorkTable workTable = __instance.billStack?.billGiver as Building_WorkTable;
      if (workTable != null && (workTable.def.building?.isMealSource == true))
      {
        CompBowlStorage bowlStorage = workTable.TryGetComp<CompBowlStorage>();
        if (bowlStorage != null)
        {
          if (!bowlStorage.HasBowlForRecipe(__instance.recipe))
          {
            __result = false;
          }
        }
      }
    }
  }
}
