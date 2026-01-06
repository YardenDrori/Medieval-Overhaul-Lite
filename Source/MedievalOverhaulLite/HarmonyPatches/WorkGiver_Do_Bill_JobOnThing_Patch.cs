using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace MOExpandedLite
{
  [HarmonyPatch(typeof(WorkGiver_DoBill), nameof(WorkGiver_DoBill.JobOnThing))]
  public static class JobOnThing_Patch
  {
    [HarmonyPostfix]
    public static Verse.AI.Job Postfix(Verse.AI.Job __result, Thing thing)
    {
      if (thing.def.building?.isMealSource == true)
      {
        Building_WorkTable stove = thing as Building_WorkTable;
        if (stove != null)
        {
          CompBowlStorage bowlStorage = thing.TryGetComp<CompBowlStorage>();
          if (bowlStorage != null)
          {
            if (!bowlStorage.HasBowlForRecipe(stove.billStack.FirstShouldDoNow?.recipe))
            {
              JobFailReason.Is("No clean bowls in storage");
              return null;
            }
          }
          else
          {
            Log.Warning(
              $"[Medieval Overhaul Lite] {thing.def.defName} has isMealSource property but is missing CompBowlStorage"
            );
            return __result;
          }
        }
        else
        {
          Log.Warning(
            $"[Medieval Overhaul Lite] {thing.def.defName} has proprty isMealSource but is not a Building_WorkTable"
          );
          return __result;
        }
      }
      return __result;
    }
  }
}
