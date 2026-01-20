using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MOExpandedLite;

public enum SoilState
{
  Rich,     // 0-20 days, +20% fertility
  Moderate, // 20-30 days, +10% fertility
  Depleted  // 30+ days, +0% fertility (matches underlying)
}

public struct SoilData : IExposable
{
  public int transitionTick;
  public SoilState state;
  public int underlyingFertilityPercent;

  public void ExposeData()
  {
    Scribe_Values.Look(ref transitionTick, "transitionTick");
    Scribe_Values.Look(ref state, "state");
    Scribe_Values.Look(ref underlyingFertilityPercent, "underlyingFertilityPercent");
  }
}

public class Thing_TilledSoilManager : Thing
{
  // O(1) lookup, add, remove
  private Dictionary<IntVec3, SoilData> trackedSoil = new();

  // O(1) lookup, add, remove for depleted cells awaiting replenishment
  private HashSet<IntVec3> depletedCells = new();

  // Pending terrain swaps (processed on next TickLong to let VEF finish registration)
  private List<IntVec3> pendingSwapCells = new();
  private List<int> pendingSwapHours = new();

  private int nextCheckTick = int.MaxValue;

  public void RegisterSoil(IntVec3 cell, int hoursUntilTransition, SoilState state, int underlyingFertilityPercent)
  {
    // 2500 ticks per hour (60000 ticks per day / 24 hours)
    int transitionTick = Find.TickManager.TicksGame + (hoursUntilTransition * 2500);

    trackedSoil[cell] = new SoilData
    {
      transitionTick = transitionTick,
      state = state,
      underlyingFertilityPercent = underlyingFertilityPercent
    };

    if (transitionTick < nextCheckTick)
      nextCheckTick = transitionTick;
  }

  public void RegisterDepletedSoil(IntVec3 cell)
  {
    depletedCells.Add(cell);
  }

  public void UnregisterSoil(IntVec3 cell)
  {
    trackedSoil.Remove(cell);
    depletedCells.Remove(cell);
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

      // Simple calculation: underlying + 20%, rounded to 10%
      int underlyingPercent = Mathf.RoundToInt(baseFertility * 100f);
      underlyingPercent = (underlyingPercent / 10) * 10;
      int richPercent = underlyingPercent + 20;

      // Clamp to valid range
      richPercent = Mathf.Clamp(richPercent, 90, 170);

      string variantDefName = $"MOL_SoilTilled_{richPercent}";
      TerrainDef variantDef = DefDatabase<TerrainDef>.GetNamed(variantDefName, errorOnFail: false);

      if (variantDef != null)
      {
        Map.terrainGrid.SetTerrain(pos, variantDef);
        // The variant's TerrainComp will register itself with the manager
      }
      else
      {
        // Fallback: register this terrain for expiration as-is
        Log.Warning($"[MOL] Could not find terrain variant {variantDefName}, using base terrain");
        RegisterSoil(pos, hours, SoilState.Rich, underlyingPercent);
      }
    }

    pendingSwapCells.Clear();
    pendingSwapHours.Clear();
  }

  public override void TickLong()
  {
    // Process pending fertility swaps first (delayed to let VEF finish registration)
    ProcessPendingSwaps();

    // Process state transitions
    ProcessStateTransitions();

    // Process replenishment for depleted cells
    ProcessReplenishment();
  }

  private void ProcessStateTransitions()
  {
    if (Find.TickManager.TicksGame < nextCheckTick)
      return;

    int currentTick = Find.TickManager.TicksGame;

    // Collect cells to transition (don't modify dictionary while iterating)
    List<IntVec3> cellsToTransition = null;

    foreach (var kvp in trackedSoil)
    {
      if (kvp.Value.transitionTick <= currentTick)
      {
        cellsToTransition ??= new List<IntVec3>();
        cellsToTransition.Add(kvp.Key);
      }
    }

    if (cellsToTransition != null)
    {
      foreach (IntVec3 cell in cellsToTransition)
      {
        if (!trackedSoil.TryGetValue(cell, out SoilData data))
          continue;

        // Verify cell still has our soil
        TerrainDef currentTerrain = Map.terrainGrid.TerrainAt(cell);
        if (!IsTilledSoil(currentTerrain))
        {
          trackedSoil.Remove(cell);
          continue;
        }

        // Remove before transition (transition will re-add with new state)
        trackedSoil.Remove(cell);

        // Perform state transition
        switch (data.state)
        {
          case SoilState.Rich:
            TransitionToModerate(cell, data.underlyingFertilityPercent);
            break;
          case SoilState.Moderate:
            TransitionToDepleted(cell, data.underlyingFertilityPercent);
            break;
          // Depleted has no timer-based transition
        }
      }
    }

    // Recalculate next check tick
    nextCheckTick = trackedSoil.Count > 0
      ? trackedSoil.Values.Min(d => d.transitionTick)
      : int.MaxValue;
  }

  private void TransitionToModerate(IntVec3 cell, int underlyingPercent)
  {
    // Moderate: underlying + 10%
    int moderatePercent = underlyingPercent + 10;
    moderatePercent = Mathf.Clamp(moderatePercent, 80, 150);

    string variantDefName = $"MOL_SoilTilled_Moderate_{moderatePercent}";
    TerrainDef variantDef = DefDatabase<TerrainDef>.GetNamed(variantDefName, errorOnFail: false);

    if (variantDef != null)
    {
      Map.terrainGrid.SetTerrain(cell, variantDef);
      // VEF defers TerrainComp initialization, so register directly here
      // 240 hours = 10 days until depleted
      RegisterSoil(cell, 240, SoilState.Moderate, underlyingPercent);
    }
    else
    {
      Log.Warning($"[MOL] Could not find moderate variant {variantDefName}, skipping to depleted");
      TransitionToDepleted(cell, underlyingPercent);
    }
  }

  private void TransitionToDepleted(IntVec3 cell, int underlyingPercent)
  {
    // Depleted: underlying + 0%
    int depletedPercent = Mathf.Clamp(underlyingPercent, 70, 140);

    string variantDefName = $"MOL_SoilTilled_Depleted_{depletedPercent}";
    TerrainDef variantDef = DefDatabase<TerrainDef>.GetNamed(variantDefName, errorOnFail: false);

    if (variantDef != null)
    {
      Map.terrainGrid.SetTerrain(cell, variantDef);
    }
    else
    {
      Log.Warning($"[MOL] Could not find depleted variant {variantDefName}");
    }

    // VEF defers TerrainComp initialization, so register directly here
    RegisterDepletedSoil(cell);
  }

  private void ProcessReplenishment()
  {
    if (depletedCells.Count == 0)
      return;

    // Count available bone meal (accounting for cost of 2 per tile and pending blueprints)
    int availableBoneMeal = CountBoneMealInStorage();
    if (availableBoneMeal <= 0)
      return;

    // Process depleted cells
    List<IntVec3> cellsToRemove = null;

    foreach (IntVec3 cell in depletedCells)
    {
      if (availableBoneMeal <= 0)
        break;

      // Verify cell still has depleted soil
      TerrainDef currentTerrain = Map.terrainGrid.TerrainAt(cell);
      if (currentTerrain == null || !currentTerrain.defName.StartsWith("MOL_SoilTilled_Depleted"))
      {
        cellsToRemove ??= new List<IntVec3>();
        cellsToRemove.Add(cell);
        continue;
      }

      // Revert to underlying terrain
      TerrainDef underTerrain = Map.terrainGrid.UnderTerrainAt(cell);
      if (underTerrain != null)
        Map.terrainGrid.SetTerrain(cell, underTerrain);
      else
        Map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);

      // Place blueprint for new enriched soil
      PlaceRebuildBlueprint(cell);

      cellsToRemove ??= new List<IntVec3>();
      cellsToRemove.Add(cell);
      availableBoneMeal--;
    }

    // Remove processed cells from depleted set
    if (cellsToRemove != null)
    {
      foreach (IntVec3 cell in cellsToRemove)
        depletedCells.Remove(cell);
    }
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

    // Each tile costs 2 bone meal, so divide by 2
    int tilesAffordable = count / 2;

    // Subtract tiles reserved by pending blueprints
    int pendingBlueprints = CountPendingSoilBlueprints();
    tilesAffordable -= pendingBlueprints;

    return System.Math.Max(0, tilesAffordable);
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

    // Serialize dictionary as parallel lists (RimWorld's Scribe works better with lists)
    List<IntVec3> cells = null;
    List<SoilData> data = null;
    List<IntVec3> depleted = null;

    if (Scribe.mode == LoadSaveMode.Saving)
    {
      cells = trackedSoil.Keys.ToList();
      data = trackedSoil.Values.ToList();
      depleted = depletedCells.ToList();
    }

    Scribe_Collections.Look(ref cells, "trackedCells", LookMode.Value);
    Scribe_Collections.Look(ref data, "trackedData", LookMode.Deep);
    Scribe_Collections.Look(ref depleted, "depletedCells", LookMode.Value);
    Scribe_Collections.Look(ref pendingSwapCells, "pendingSwapCells", LookMode.Value);
    Scribe_Collections.Look(ref pendingSwapHours, "pendingSwapHours", LookMode.Value);
    Scribe_Values.Look(ref nextCheckTick, "nextCheckTick", int.MaxValue);

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      // Rebuild dictionary from lists
      trackedSoil = new Dictionary<IntVec3, SoilData>();
      if (cells != null && data != null)
      {
        for (int i = 0; i < cells.Count && i < data.Count; i++)
          trackedSoil[cells[i]] = data[i];
      }

      // Rebuild hashset from list
      depletedCells = depleted != null ? new HashSet<IntVec3>(depleted) : new HashSet<IntVec3>();

      pendingSwapCells ??= new List<IntVec3>();
      pendingSwapHours ??= new List<int>();
    }
  }
}
