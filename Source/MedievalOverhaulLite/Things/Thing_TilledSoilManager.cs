using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MOExpandedLite;

public class Thing_TilledSoilManager : Thing
{
  // Parallel lists for serialization (RimWorld can't serialize Dictionary<int, List<IntVec3>>)
  private List<int> expirationTicks = new();
  private List<IntVec3> expirationCells = new();

  // Pending terrain swaps (processed on next TickLong to let VEF finish registration)
  private List<IntVec3> pendingSwapCells = new();
  private List<int> pendingSwapHours = new();

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

  public void QueueFertilitySwap(IntVec3 cell, int hoursToExpire)
  {
    pendingSwapCells.Add(cell);
    pendingSwapHours.Add(hoursToExpire);
  }

  private void ProcessPendingSwaps()
  {
    if (pendingSwapCells.Count == 0)
      return;

    for (int i = 0; i < pendingSwapCells.Count; i++)
    {
      IntVec3 pos = pendingSwapCells[i];
      int hours = pendingSwapHours[i];

      if (!pos.InBounds(Map))
        continue;

      // Check if the base variant is still there
      TerrainDef currentTerrain = Map.terrainGrid.TerrainAt(pos);
      if (currentTerrain?.defName != "MOL_SoilTilled")
        continue;

      TerrainDef underTerrain = Map.terrainGrid.UnderTerrainAt(pos);
      float baseFertility = underTerrain?.fertility ?? 1f;

      // Calculate target fertility: 120% of base, rounded down to 10%, clamped to +20% to +30%
      float raw = baseFertility * 1.2f;
      float rounded = Mathf.Floor(raw * 10f) / 10f;
      float min = baseFertility + 0.2f;
      float max = baseFertility + 0.3f;
      float targetFertility = Mathf.Clamp(rounded, min, max);

      // Convert to percentage for def name (e.g., 1.2 -> 120)
      int fertilityPercent = Mathf.RoundToInt(targetFertility * 100f);
      fertilityPercent = Mathf.Clamp(fertilityPercent, 90, 170);
      fertilityPercent = (fertilityPercent / 10) * 10;

      string variantDefName = $"MOL_SoilTilled_{fertilityPercent}";
      TerrainDef variantDef = DefDatabase<TerrainDef>.GetNamed(variantDefName, errorOnFail: false);

      if (variantDef != null)
      {
        Map.terrainGrid.SetTerrain(pos, variantDef);
      }
      else
      {
        // Fallback: register this terrain for expiration as-is
        Log.Warning($"[MOL] Could not find terrain variant {variantDefName}, using base terrain");
        RegisterSoil(pos, hours);
      }
    }

    pendingSwapCells.Clear();
    pendingSwapHours.Clear();
  }

  public override void TickLong()
  {
    // Process pending fertility swaps first (delayed to let VEF finish registration)
    ProcessPendingSwaps();

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
      TerrainDef currentTerrain = Map.terrainGrid.TerrainAt(cell);
      if (IsTilledSoil(currentTerrain))
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

  private static bool IsTilledSoil(TerrainDef terrain)
  {
    return terrain != null && terrain.defName.StartsWith("MOL_SoilTilled");
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
    Scribe_Collections.Look(ref pendingSwapCells, "pendingSwapCells", LookMode.Value);
    Scribe_Collections.Look(ref pendingSwapHours, "pendingSwapHours", LookMode.Value);
    Scribe_Values.Look(ref nextCheckTick, "nextCheckTick", int.MaxValue);

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      expirationTicks ??= new();
      expirationCells ??= new();
      pendingSwapCells ??= new();
      pendingSwapHours ??= new();
    }
  }
}
