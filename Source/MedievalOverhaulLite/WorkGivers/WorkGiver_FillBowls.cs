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
      return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
      Building_FermentingBarrel barrel = (Building_FermentingBarrel)t;
      Thing thing = FindWort(pawn, barrel);
      return JobMaker.MakeJob(JobDefOf.FillFermentingBarrel, t, thing);
    }

    private Thing FindWort(Pawn pawn, Building_FermentingBarrel barrel)
    {
      Predicate<Thing> validator = (Thing x) =>
        (!x.IsForbidden(pawn) && pawn.CanReserve(x)) ? true : false;
      return GenClosest.ClosestThingReachable(
        pawn.Position,
        pawn.Map,
        ThingRequest.ForDef(ThingDefOf.Wort),
        PathEndMode.ClosestTouch,
        TraverseParms.For(pawn),
        9999f,
        validator
      );
    }
  }
}
