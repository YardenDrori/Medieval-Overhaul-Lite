using HarmonyLib;
using RimWorld;
using Verse;

namespace MOExpandedLite
{
  [HarmonyPatch(typeof(Thing), "PostIngested")]
  public static class PostIngested_MakeBowl_Patch
  {
    //caching cause im cool like that tho idk if this is an expensive operation i assume it is as it invlovesa a string
    private static ThingDef bowlDef = DefDatabase<ThingDef>.GetNamed("MOL_PlateDirty");

    [HarmonyPostfix]
    public static void Postfix(Thing __instance, Pawn ingester)
    {
      if (
        __instance.def.ingestible.foodType.HasFlag(FoodTypeFlags.Meal)
        && __instance.def.ingestible.tableDesired
        && !__instance.def.HasModExtension<MOExpandedLite.NoBowlOnIngest>()
      )
      {
        Thing bowl = ThingMaker.MakeThing(bowlDef, null);
        bowl.stackCount = 1;
        GenPlace.TryPlaceThing(bowl, ingester.Position, ingester.Map, ThingPlaceMode.Direct);

        Verse.AI.Job haulJob = Verse.AI.HaulAIUtility.HaulToStorageJob(ingester, bowl, true);

        if (haulJob != null)
        {
          haulJob.count = bowl.stackCount;
          haulJob.targetA = bowl;

          ingester.jobs.jobQueue.EnqueueFirst(haulJob);
        }
      }
    }
  }
}
