using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MOExpandedLite;

public enum SoilState
{
  Rich,
  Weathered,
  Depleted,
}

public class Thing_TilledSoilManager : Thing
{
  private const int TicksPerHour = 2500;
  private const int DepletedCheckIntervalTicks = 30000; // Check depleted soil every half day

  // Tracked soil awaiting state transition
  private List<IntVec3> trackedCells = new();
  private List<int> trackedExpirationTicks = new();

  // Depleted soil cells (checked periodically for bonemeal renewal)
  private List<IntVec3> depletedCells = new();

  // Pending terrain swaps (processed next TickLong to let VEF finish registration)
  private List<IntVec3> pendingCells = new();
  private List<SoilState> pendingStates = new();

  private int nextExpirationTick = int.MaxValue;
  private int nextDepletedCheckTick;

  // ============ Public API ============

  /// <summary>
  /// Called when new enriched soil is placed. Queues swap to correct Rich variant.
  /// </summary>
  public void OnSoilPlaced(IntVec3 cell)
  {
    pendingCells.Add(cell);
    pendingStates.Add(SoilState.Rich);
  }

  /// <summary>
  /// Called when soil terrain is removed (by player or other means).
  /// </summary>
  public void OnSoilRemoved(IntVec3 cell)
  {
    RemoveFromTracking(cell);
    depletedCells.Remove(cell);

    int pendingIdx = pendingCells.IndexOf(cell);
    if (pendingIdx >= 0)
    {
      pendingCells.RemoveAt(pendingIdx);
      pendingStates.RemoveAt(pendingIdx);
    }
  }

  // ============ Core Logic ============

  public override void TickLong()
  {
    ProcessPendingSwaps();
    ProcessExpirations();
    ProcessDepletedSoilRenewal();
  }

  private void ProcessPendingSwaps()
  {
    for (int i = pendingCells.Count - 1; i >= 0; i--)
    {
      IntVec3 cell = pendingCells[i];
      SoilState targetState = pendingStates[i];

      if (!cell.InBounds(Map))
        continue;

      // Get the fertility we need to match
      int fertilityPercent = GetTargetFertilityPercent(cell, targetState);
      TerrainDef variantDef = GetVariantDef(targetState, fertilityPercent);

      if (variantDef == null)
      {
        Log.Warning(
          $"[MOL] Could not find terrain variant for {targetState} at {fertilityPercent}%"
        );
        continue;
      }

      // Swap to the correct variant
      Map.terrainGrid.SetTerrain(cell, variantDef);

      // Track based on new state
      if (targetState == SoilState.Depleted)
      {
        depletedCells.Add(cell);
      }
      else
      {
        int hoursToExpire = targetState == SoilState.Rich ? 480 : 240; // 20 days / 10 days
        TrackForExpiration(cell, hoursToExpire);
      }
    }

    pendingCells.Clear();
    pendingStates.Clear();
  }

  private void ProcessExpirations()
  {
    int currentTick = Find.TickManager.TicksGame;
    if (currentTick < nextExpirationTick)
      return;

    // Find all expired cells
    List<IntVec3> expiredCells = new();
    for (int i = 0; i < trackedCells.Count; i++)
    {
      if (trackedExpirationTicks[i] <= currentTick)
        expiredCells.Add(trackedCells[i]);
    }

    // Process each expiration
    foreach (IntVec3 cell in expiredCells)
    {
      RemoveFromTracking(cell);
      TransitionToNextState(cell);
    }

    // Update next check time
    nextExpirationTick =
      trackedExpirationTicks.Count > 0 ? trackedExpirationTicks.Min() : int.MaxValue;
  }

  private void TransitionToNextState(IntVec3 cell)
  {
    if (!cell.InBounds(Map))
      return;

    TerrainDef currentTerrain = Map.terrainGrid.TerrainAt(cell);
    SoilState? currentState = GetSoilStateFromDef(currentTerrain);

    if (currentState == null)
      return;

    SoilState nextState = currentState.Value switch
    {
      SoilState.Rich => SoilState.Weathered,
      SoilState.Weathered => SoilState.Depleted,
      _ => SoilState.Depleted,
    };

    // Queue the swap for next tick (consistent with initial placement)
    pendingCells.Add(cell);
    pendingStates.Add(nextState);
  }

  private void ProcessDepletedSoilRenewal()
  {
    int currentTick = Find.TickManager.TicksGame;
    if (currentTick < nextDepletedCheckTick || depletedCells.Count == 0)
      return;

    nextDepletedCheckTick = currentTick + DepletedCheckIntervalTicks;

    int availableBoneMeal = CountAvailableBoneMeal() / 3;
    if (availableBoneMeal <= 0)
      return;

    // Renew depleted cells (oldest first, up to available bonemeal)
    int toRenew = Mathf.Min(availableBoneMeal, depletedCells.Count);
    for (int i = 0; i < toRenew; i++)
    {
      IntVec3 cell = depletedCells[0];
      depletedCells.RemoveAt(0);

      if (!cell.InBounds(Map))
        continue;

      // Remove the depleted terrain
      TerrainDef underTerrain = Map.terrainGrid.UnderTerrainAt(cell);
      Map.terrainGrid.SetTerrain(cell, underTerrain ?? TerrainDefOf.Soil);

      // Place blueprint for renewal
      GenConstruct.PlaceBlueprintForBuild(
        MOL_DefOf.MOL_SoilTilled,
        cell,
        Map,
        Rot4.North,
        Faction.OfPlayer,
        null
      );
    }
  }

  // ============ Helper Methods ============

  private void TrackForExpiration(IntVec3 cell, int hours)
  {
    int expirationTick = Find.TickManager.TicksGame + (hours * TicksPerHour);

    trackedCells.Add(cell);
    trackedExpirationTicks.Add(expirationTick);

    if (expirationTick < nextExpirationTick)
      nextExpirationTick = expirationTick;
  }

  private void RemoveFromTracking(IntVec3 cell)
  {
    int idx = trackedCells.IndexOf(cell);
    if (idx >= 0)
    {
      trackedCells.RemoveAt(idx);
      trackedExpirationTicks.RemoveAt(idx);
    }
  }

  private int GetTargetFertilityPercent(IntVec3 cell, SoilState state)
  {
    TerrainDef underTerrain = Map.terrainGrid.UnderTerrainAt(cell);
    float baseFertility = underTerrain?.fertility ?? 1f;

    float bonus = state switch
    {
      SoilState.Rich => 0.2f,
      SoilState.Weathered => 0.1f,
      SoilState.Depleted => 0f,
      _ => 0f,
    };

    float targetFertility = baseFertility + bonus;
    // Round to nearest 10%
    return Mathf.RoundToInt(targetFertility * 10f) * 10;
  }

  private static TerrainDef GetVariantDef(SoilState state, int fertilityPercent)
  {
    string prefix = state switch
    {
      SoilState.Rich => "R",
      SoilState.Weathered => "W",
      SoilState.Depleted => "D",
      _ => "R",
    };

    string defName = $"MOL_SoilTilled{prefix}_{fertilityPercent}";
    return DefDatabase<TerrainDef>.GetNamed(defName, errorOnFail: false);
  }

  private static SoilState? GetSoilStateFromDef(TerrainDef terrain)
  {
    if (terrain?.defName == null)
      return null;

    if (terrain.defName.Contains("TilledR_"))
      return SoilState.Rich;
    if (terrain.defName.Contains("TilledW_"))
      return SoilState.Weathered;
    if (terrain.defName.Contains("TilledD_"))
      return SoilState.Depleted;

    return null;
  }

  private int CountAvailableBoneMeal()
  {
    int count = 0;
    foreach (Thing thing in Map.listerThings.ThingsOfDef(MOL_DefOf.MOL_BoneMeal))
    {
      if (!thing.IsForbidden(Faction.OfPlayer))
        count += thing.stackCount;
    }

    // Subtract bonemeal reserved by pending blueprints (2 per blueprint)
    foreach (Thing thing in Map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint))
    {
      if (thing.def.entityDefToBuild == MOL_DefOf.MOL_SoilTilled)
        count -= 3;
    }

    return Mathf.Max(0, count);
  }

  // ============ Serialization ============

  public override void ExposeData()
  {
    base.ExposeData();

    Scribe_Collections.Look(ref trackedCells, "trackedCells", LookMode.Value);
    Scribe_Collections.Look(ref trackedExpirationTicks, "trackedExpirationTicks", LookMode.Value);
    Scribe_Collections.Look(ref depletedCells, "depletedCells", LookMode.Value);
    Scribe_Collections.Look(ref pendingCells, "pendingCells", LookMode.Value);
    Scribe_Collections.Look(ref pendingStates, "pendingStates", LookMode.Value);
    Scribe_Values.Look(ref nextExpirationTick, "nextExpirationTick", int.MaxValue);
    Scribe_Values.Look(ref nextDepletedCheckTick, "nextDepletedCheckTick");

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      trackedCells ??= new();
      trackedExpirationTicks ??= new();
      depletedCells ??= new();
      pendingCells ??= new();
      pendingStates ??= new();
    }
  }
}
