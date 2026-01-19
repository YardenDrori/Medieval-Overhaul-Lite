using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MOExpandedLite;

public class Thing_TilledSoilManager : Thing
{
  // Parallel lists for serialization (RimWorld can't serialize Dictionary<int, List<IntVec3>>)
  private List<int> expirationTicks = new();
  private List<IntVec3> expirationCells = new();

  private int nextCheckTick = int.MaxValue;

  public void RegisterSoil(IntVec3 cell, int hoursUntilExpire)
  {
    // 2500 ticks per hour (60000 ticks per day / 24 hours)
    int expirationTick = Find.TickManager.TicksGame + (hoursUntilExpire * 2500);

    expirationTicks.Add(expirationTick);
    expirationCells.Add(cell);

    if (expirationTick < nextCheckTick)
      nextCheckTick = expirationTick;
  }

  public void UnregisterSoil(IntVec3 cell)
  {
    int index = expirationCells.IndexOf(cell);
    if (index >= 0)
    {
      expirationTicks.RemoveAt(index);
      expirationCells.RemoveAt(index);
    }
  }

  public override void TickLong()
  {
    if (Find.TickManager.TicksGame < nextCheckTick)
      return;

    int currentTick = Find.TickManager.TicksGame;

    // Count available bone meal ONCE to prevent race condition
    int availableBoneMeal = CountBoneMealInStorage();

    // Collect cells to expire (don't modify list while iterating)
    List<IntVec3> cellsToExpire = new();
    for (int i = 0; i < expirationTicks.Count; i++)
    {
      if (expirationTicks[i] <= currentTick)
        cellsToExpire.Add(expirationCells[i]);
    }

    // Process expirations
    foreach (IntVec3 cell in cellsToExpire)
    {
      if (Map.terrainGrid.TerrainAt(cell) == MOL_DefOf.MOL_SoilTilled)
      {
        // Get underlying terrain before removing
        TerrainDef underTerrain = Map.terrainGrid.UnderTerrainAt(cell);

        // Directly set to underlying terrain (no resource refund)
        // This avoids RemoveTopLayer which refunds materials
        if (underTerrain != null)
          Map.terrainGrid.SetTerrain(cell, underTerrain);
        else
          Map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);

        // Manually unregister since SetTerrain might not trigger VEF's PostRemove
        UnregisterSoil(cell);

        // Auto-renew if bone meal available
        if (availableBoneMeal > 0)
        {
          PlaceRebuildBlueprint(cell);
          availableBoneMeal--;
        }
      }
    }

    nextCheckTick = expirationTicks.Count > 0 ? expirationTicks.Min() : int.MaxValue;
  }

  private int CountBoneMealInStorage()
  {
    int count = 0;
    foreach (Thing thing in Map.listerThings.ThingsOfDef(MOL_DefOf.MOL_BoneMeal))
    {
      // Only count items not forbidden
      if (!thing.IsForbidden(Faction.OfPlayer))
        count += thing.stackCount;
    }

    // Subtract bone meal reserved by pending blueprints for our soil
    int pendingBlueprints = CountPendingSoilBlueprints();
    count -= pendingBlueprints;

    return System.Math.Max(0, count);
  }

  private int CountPendingSoilBlueprints()
  {
    int count = 0;
    foreach (Thing thing in Map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint))
    {
      if (thing.def.entityDefToBuild == MOL_DefOf.MOL_SoilTilled)
        count++;
    }
    return count;
  }

  private void PlaceRebuildBlueprint(IntVec3 cell)
  {
    GenConstruct.PlaceBlueprintForBuild(
      MOL_DefOf.MOL_SoilTilled,
      cell,
      Map,
      Rot4.North,
      Faction.OfPlayer,
      null
    );
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Collections.Look(ref expirationTicks, "expirationTicks", LookMode.Value);
    Scribe_Collections.Look(ref expirationCells, "expirationCells", LookMode.Value);
    Scribe_Values.Look(ref nextCheckTick, "nextCheckTick", int.MaxValue);

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      expirationTicks ??= new();
      expirationCells ??= new();
    }
  }
}
