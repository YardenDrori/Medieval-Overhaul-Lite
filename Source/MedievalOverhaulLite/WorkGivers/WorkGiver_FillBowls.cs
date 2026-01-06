using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace MOExpandedLite
{
  public class WorkGiver_FillBowls : WorkGiver_Scanner
  {
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForUndefined();

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
      MapComponent_StoveTracker stoveTracker = pawn.Map.GetComponent<MapComponent_StoveTracker>();
      if (stoveTracker != null)
      {
        foreach (Building_WorkTable stove in stoveTracker.allStoves)
        {
          yield return stove;
        }
      }
      else
      {
        Log.Error("[Medieval Overhaul Lite] Failed to find MapComponent_StoveTracker");
        yield break;
      }
    }

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
      CompBowlStorage comp = t.TryGetComp<CompBowlStorage>();
      if (comp == null)
      {
        Log.Error($"[Medieval Overhaul Lite] No CompBowlStorage in {t.def.defName}");
        JobFailReason.IsSilent();
        return false;
      }
      if (!comp.IsNeedRefuel() && !forced)
      {
        JobFailReason.IsSilent();
        return false;
      }
      if (comp.CapacityRemaining() <= 0)
      {
        JobFailReason.Is("Already full");
        return false;
      }
      if (!pawn.CanReserve(t, 1, -1, null, forced))
      {
        return false;
      }
      if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
      {
        return false;
      }
      if (t.IsBurning())
      {
        return false;
      }

      ///Get the def of the dishtype either from the recipe if it is a building_worktable
      ///or from the fallback defined in the comp (default is a bowl)
      ThingDef dishType = null;
      Building_WorkTable stove = t as Building_WorkTable;
      if (stove != null)
      {
        dishType = stove
          .billStack.FirstShouldDoNow?.recipe.GetModExtension<RequireDishesToFunction>()
          ?.dishType;
      }
      if (dishType == null)
      {
        dishType = comp.DishTypeFallBack;
      }

      if (FindBowl(pawn, dishType) == null)
      {
        JobFailReason.Is("No Bowls in storage");
        return false;
      }
      return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
      CompBowlStorage comp = t.TryGetComp<CompBowlStorage>();
      if (comp == null)
      {
        Log.Error($"[Medieval Overhaul Lite] No CompBowlStorage in {t.def.defName}");
        return null;
      }

      ///Get the def of the dishtype either from the recipe if it is a building_worktable
      ///or from the fallback defined in the comp (default is a bowl)
      ThingDef dishType = null;
      Building_WorkTable stove = t as Building_WorkTable;
      if (stove != null)
      {
        dishType = stove
          .billStack.FirstShouldDoNow?.recipe.GetModExtension<RequireDishesToFunction>()
          ?.dishType;
      }
      if (dishType == null)
      {
        dishType = comp.DishTypeFallBack;
      }

      Thing thing = FindBowl(pawn, dishType);
      return JobMaker.MakeJob(JobDefOf_MedievalOverhaulLite.MOL_FillBowl, t, thing);
    }

    private Thing FindBowl(Pawn pawn, ThingDef dishType)
    {
      Predicate<Thing> validator = (Thing x) =>
        (!x.IsForbidden(pawn) && pawn.CanReserve(x)) ? true : false;
      return GenClosest.ClosestThingReachable(
        pawn.Position,
        pawn.Map,
        ThingRequest.ForDef(dishType),
        PathEndMode.ClosestTouch,
        TraverseParms.For(pawn),
        9999f,
        validator
      );
    }
  }
}
