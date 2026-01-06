using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MOExpandedLite
{
  public class CompProperties_BowlStorage : CompProperties
  {
    public int capacity = 10;
    private static ThingDef dishTypeFallbackCached = null;
    public ThingDef dishType
    {
      get
      {
        if (dishTypeFallbackCached == null)
        {
          dishTypeFallbackCached = DefDatabase<ThingDef>.GetNamedSilentFail("MOL_PlateClean");
        }
        return dishTypeFallbackCached;
      }
    }

    public CompProperties_BowlStorage()
    {
      this.compClass = typeof(CompBowlStorage);
    }
  }

  public class CompBowlStorage : ThingComp, IThingHolder
  {
    private CompProperties_BowlStorage Props => (CompProperties_BowlStorage)props;
    private ThingOwner<Thing> bowls;
    private MapComponent_StoveTracker stoveTracker = null;
    private int bowlThreshold;

    public ThingDef DishType => Props.dishType;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
      stoveTracker = parent.Map?.GetComponent<MapComponent_StoveTracker>();
      bowlThreshold = Props.capacity * 0.3 > 4 ? (int)(Props.capacity * 0.3) : 4;
      if (stoveTracker != null)
      {
        stoveTracker.allStoves.Add((Building_WorkTable)parent);
      }
      else
      {
        Log.Error($"[Medieval Overhaul Lite] No MapComponent_StoveTracker found");
      }

      if (bowls == null)
      {
        bowls = new ThingOwner<Thing>(this);
      }
      base.PostSpawnSetup(respawningAfterLoad);
    }

    public override void PostExposeData()
    {
      base.PostExposeData();
      Scribe_Deep.Look(ref bowls, "bowls", this);
    }

    public override string CompInspectStringExtra()
    {
      if (bowls == null)
        return null;
      return $"Dishes: {bowls.Sum(thing => thing.stackCount)}/{Props.capacity}";
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
      base.PostDestroy(mode, previousMap);
      if (bowls != null && bowls.Sum(thing => thing.stackCount) > 0)
      {
        bowls.TryDropAll(parent.Position, previousMap, ThingPlaceMode.Near);
      }
      if (stoveTracker != null)
      {
        stoveTracker.allStoves.Remove((Building_WorkTable)parent);
      }
      else
      {
        Log.Error($"[Medieval Overhaul Lite] No MapComponent_StoveTracker found");
      }
    }

    public bool HasBowlForRecipe(RecipeDef recipe)
    {
      if (bowls == null)
      {
        Log.Error($"[Medieval Overhaul Lite] no ThingHolder");
        return false;
      }
      if (recipe == null || recipe.products.NullOrEmpty())
      {
        return true;
      }
      if (bowls.Sum(thing => thing.stackCount) >= recipe.products[0].count)
        return true;
      return false;
    }

    public int CapacityRemaining()
    {
      if (bowls == null)
      {
        Log.Error($"[Medieval Overhaul Lite] no ThingHolder");
        return 0;
      }
      return Props.capacity - bowls.Sum(thing => thing.stackCount);
    }

    public bool IsNeedRefuel()
    {
      if (bowls == null)
      {
        Log.Error($"[Medieval Overhaul Lite] no ThingHolder");
        return false;
      }
      return (bowls.Sum(thing => thing.stackCount) <= bowlThreshold);
    }

    public void AddBowls(Thing bowlsToAdd)
    {
      if (bowls == null)
      {
        Log.Error($"[Medieval Overhaul Lite] no ThingHolder");
        return;
      }
      if (bowls.Sum(thing => thing.stackCount) + bowlsToAdd.stackCount > Props.capacity)
      {
        Log.Error($"[Medieval Overhaul Lite] Attempted to add more bowls than availble capacity");
        return;
      }
      int bowlsAdded = bowls.TryAddOrTransfer(bowlsToAdd, bowlsToAdd.stackCount, true);
      if (bowlsAdded != bowlsToAdd.stackCount)
      {
        Log.Error(
          $"[Medieval Overhaul Lite] Failed to add sufficient bowls to ThingOwner attempted to add {bowlsToAdd.stackCount} but added {bowlsAdded}"
        );
        bowls.TryDropAll(parent.Position, parent.Map, ThingPlaceMode.Direct);
      }
    }

    public void ConsumeBowls(int count)
    {
      if (bowls == null)
      {
        Log.Error($"[Medieval Overhaul Lite] no ThingHolder");
        return;
      }
      if (bowls.Sum(thing => thing.stackCount) - count < 0)
      {
        Log.Error(
          $"[Medieval Overhaul Lite] Attempt at removing bowls resulted in negative number"
        );
        return;
      }

      int remaining = count;
      while (remaining > 0 && bowls.Count > 0)
      {
        Thing bowl = bowls[0];
        if (bowl.stackCount <= remaining)
        {
          // Remove the entire stack
          remaining -= bowl.stackCount;
          bowls.RemoveAt(0);
        }
        else
        {
          // Split the stack - reduce stackCount
          bowl.stackCount -= remaining;
          remaining = 0;
        }
      }
    }

    public void CookedRecipe(RecipeDef recipe)
    {
      if (!recipe.ProducedThingDef.HasModExtension<MOExpandedLite.NoBowlOnIngest>())
        ConsumeBowls(recipe.products[0].count);
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
      ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public ThingOwner GetDirectlyHeldThings()
    {
      return bowls;
    }
  }
}
