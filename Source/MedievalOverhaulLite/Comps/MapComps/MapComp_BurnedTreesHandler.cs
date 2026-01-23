using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MOExpandedLite;

public class MapComponent_BurnedTreesHandler : MapComponent
{
  private List<int> burnedTreeTicks = new List<int>();
  private int totalSaplingsSpawned = 0;

  // Cache for living trees to avoid expensive queries during rapid fires
  private List<Thing> cachedLivingTrees = null;
  private int cachedLivingTreesTick = -1;
  private const int CacheValidityTicks = 250; // ~4 seconds

  private const int TreeRecordExpirationTicks = 720000; // 12 in-game hours
  private const int MinimumBurnedTreesThreshold = 20;
  private const float BaseSpawnChance = 0.05f; // 5%
  private const float ChanceDecayMultiplier = 1.25f;

  public MapComponent_BurnedTreesHandler(Map map)
    : base(map) { }

  public void NotifyTreeBurned()
  {
    int currentTick = Find.TickManager.TicksGame;

    // Clean up expired records FIRST
    CleanupExpiredRecords(currentTick);

    // Reset counter if list is empty (12+ hours with no fires)
    if (burnedTreeTicks.Count == 0)
    {
      totalSaplingsSpawned = 0;
    }

    // Add burned tree timestamp
    burnedTreeTicks.Add(currentTick);

    // Try to spawn a sapling
    TrySpawnSapling();
  }

  private void CleanupExpiredRecords(int currentTick)
  {
    burnedTreeTicks.RemoveAll(tick => currentTick - tick > TreeRecordExpirationTicks);
  }

  private void TrySpawnSapling()
  {
    // Check if raining
    if (!IsRaining())
    {
      return;
    }

    // Check if we have enough burned trees
    if (burnedTreeTicks.Count < MinimumBurnedTreesThreshold)
    {
      return;
    }

    // Calculate spawn chance with diminishing returns
    float spawnChance = BaseSpawnChance / Mathf.Pow(ChanceDecayMultiplier, totalSaplingsSpawned);

    // Roll for spawn
    if (!Rand.Chance(spawnChance))
    {
      return;
    }

    // Find a random living tree to replace
    Thing targetTree = FindRandomLivingTree();
    if (targetTree == null)
    {
      return;
    }

    // Get appropriate sapling type for biome
    PawnKindDef saplingKind = GetSaplingKindForBiome();
    if (saplingKind == null)
    {
      Log.Warning("[Medieval Overhaul Lite] Could not find sapling PawnKindDef for spawning");
      return;
    }

    // Spawn the sapling
    SpawnSaplingAtTree(targetTree, saplingKind);

    // Increment counter
    totalSaplingsSpawned++;

    // Invalidate cache since we just modified the tree list
    cachedLivingTrees = null;
  }

  private bool IsRaining()
  {
    if (map.weatherManager == null)
    {
      return false;
    }

    WeatherDef currentWeather = map.weatherManager.curWeather;
    if (currentWeather == null)
    {
      return false;
    }

    // Check if weather has actual rain
    return currentWeather.rainRate > 0f;
  }

  private Thing FindRandomLivingTree()
  {
    int currentTick = Find.TickManager.TicksGame;

    // Use cached list if still valid
    if (cachedLivingTrees != null && currentTick - cachedLivingTreesTick < CacheValidityTicks)
    {
      // Remove any that became invalid
      cachedLivingTrees.RemoveAll(t => t == null || t.Destroyed);
      if (cachedLivingTrees.Count > 0)
      {
        return cachedLivingTrees.RandomElement();
      }
    }

    // Rebuild cache
    List<Thing> allPlants = map.listerThings.ThingsInGroup(ThingRequestGroup.Plant);

    cachedLivingTrees = allPlants
      .Where(thing =>
        thing is Plant plant
        && plant.def.plant != null
        && plant.def.plant.IsTree
        && !plant.Destroyed
        && plant.Growth >= 0.5f
      ) // At least 50% grown
      .ToList();

    cachedLivingTreesTick = currentTick;

    if (cachedLivingTrees.Count == 0)
    {
      return null;
    }

    return cachedLivingTrees.RandomElement();
  }

  private PawnKindDef GetSaplingKindForBiome()
  {
    BiomeDef biome = map.Biome;

    // Dark saplings ONLY in MOL_DarkForest biome
    if (biome.defName == "MOL_DarkForest")
    {
      return DefDatabase<PawnKindDef>.GetNamedSilentFail("MOL_SchratDark_Sapling");
    }

    // All other biomes get plains saplings
    return DefDatabase<PawnKindDef>.GetNamedSilentFail("MOL_SchratPlains_Sapling");
  }

  private void SpawnSaplingAtTree(Thing tree, PawnKindDef saplingKind)
  {
    IntVec3 treePosition = tree.Position;
    Plant plant = tree as Plant;

    // Store tree data for potential restoration
    ThingDef treeDef = tree.def;
    float treeGrowth = plant?.Growth ?? 0.5f;

    // Destroy the tree
    tree.Destroy(DestroyMode.Vanish);

    // Generate the sapling pawn
    Pawn sapling = PawnGenerator.GeneratePawn(
      new PawnGenerationRequest(
        saplingKind,
        faction: null, // Wild/neutral
        context: PawnGenerationContext.NonPlayer,
        tile: map.Tile,
        forceGenerateNewPawn: true,
        allowDead: false,
        allowDowned: false
      )
    );

    // Spawn the sapling at the tree's position
    GenSpawn.Spawn(sapling, treePosition, map, Rot4.Random);

    // Set energy comp to spawn tree back when depleted (same as Ent behavior)
    CompPlantEnergy energyComp = sapling.TryGetComp<CompPlantEnergy>();
    if (energyComp != null)
    {
      energyComp.SetTreeData(treeDef, treeGrowth);
    }

    // Visual effects
    FleckMaker.ThrowSmoke(treePosition.ToVector3Shifted(), map, 1f);
    FleckMaker.ThrowDustPuffThick(
      treePosition.ToVector3Shifted(),
      map,
      1.5f,
      UnityEngine.Color.green
    );
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Collections.Look(ref burnedTreeTicks, "burnedTreeTicks", LookMode.Value);
    Scribe_Values.Look(ref totalSaplingsSpawned, "totalSaplingsSpawned", 0);

    if (Scribe.mode == LoadSaveMode.PostLoadInit && burnedTreeTicks == null)
    {
      burnedTreeTicks = new List<int>();
    }
  }
}
