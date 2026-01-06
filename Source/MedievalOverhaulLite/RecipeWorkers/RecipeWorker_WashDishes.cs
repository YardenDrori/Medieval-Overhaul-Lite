using System.Collections.Generic;
// using MOExpandedLite.Compatibility;
using RimWorld;
using Verse;

namespace MOExpandedLite
{
  public class RecipeWorker_WashDishes : RecipeWorker
  {
    public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
    {
      //no way this is correct but idk how to get the building info so might as well guess right? better than leaving empty i suppose
      Thing dishWasher = billDoer.jobs.curJob?.targetA.ToTargetInfo(billDoer.Map).Thing;

      if (dishWasher == null)
      {
        Log.Error(
          $"[Medieval Overhaul Lite] building could not be found from bill (cryptic i know too bad)"
        );
        return;
      }
      Building_WorkTable building = dishWasher as Building_WorkTable;
      if (building == null)
      {
        Log.Error(
          $"[Medieval Overhaul Lite] dishwasher is not a Building_WorkTable: {dishWasher.def.defName}"
        );
        return;
      }

      float waterPerWash = 2f;
      if (
        !MOExpandedLite.Compatibility.DubsBadHygieneCompat.TryConsumeWater(building, waterPerWash)
      )
      {
        return;
      }
    }
  }
}
