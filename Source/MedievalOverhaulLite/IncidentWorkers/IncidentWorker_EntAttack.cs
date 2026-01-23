using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MOExpandedLite;

public class IncidentWorker_EntAttack : IncidentWorker
{
  private const float CostScalingPerEnt = 1.2f;

  protected override bool TryExecuteWorker(IncidentParms parms)
  {
    Map map = (Map)parms.target;

    // Find trees near the spawn center (worker's position)
    const int radius = 25;
    List<Thing> treesNearPawn = new List<Thing>();
    foreach (
      Thing thing in GenRadial.RadialDistinctThingsAround(parms.spawnCenter, map, radius, true)
    )
    {
      if (thing is Plant plant && plant.def.plant.IsTree)
      {
        treesNearPawn.Add(plant);
      }
    }

    if (treesNearPawn.Count == 0)
    {
      // Fallback in case there weren't any trees near the worker
      treesNearPawn = map.listerThings.ThingsInGroup(ThingRequestGroup.Plant);
    }

    if (treesNearPawn.Count == 0)
    {
      // No trees on the map at all
      return false;
    }

    // Spawn ents near the trees using storyteller's points
    int entsSpawned = SpawnEntsNear(treesNearPawn, map, parms.points);

    if (entsSpawned == 0)
    {
      return false;
    }

    // Send letter to player
    SendStandardLetter(parms, new TargetInfo(parms.spawnCenter, map));
    return true;
  }

  private int SpawnEntsNear(List<Thing> trees, Map map, float points)
  {
    // Get available ent types
    PawnKindDef majorEntDark = PawnKindDef.Named("MOL_Schrat_Dark");
    PawnKindDef majorEntPlains = PawnKindDef.Named("MOL_Schrat_Plains");
    PawnKindDef minorEntDark = PawnKindDef.Named("MOL_Sapling_Dark");
    PawnKindDef minorEntPlains = PawnKindDef.Named("MOL_Sapling_Plains");

    List<PawnKindDef> entKindsPlains = new List<PawnKindDef> { majorEntPlains, minorEntPlains };
    List<PawnKindDef> entKindsDark = new List<PawnKindDef> { majorEntDark, minorEntDark };

    // Filter to only valid defs
    entKindsDark = entKindsDark.Where(k => k != null).ToList();
    entKindsPlains = entKindsPlains.Where(k => k != null).ToList();

    if (!entKindsDark.Any() || !entKindsPlains.Any())
    {
      Log.Warning("IncidentWorker_EntAttack: No ent PawnKindDefs found!");
      return 0;
    }

    int spawned = 0;
    float pointsRemaining = points;
    float costMultiplier = 1f;

    bool spawnDark = map.Biome == BiomeDef.Named("MOL_DarkForest");

    while (pointsRemaining > 0)
    {
      // Pick a random ent type we can afford (accounting for cost scaling)
      List<PawnKindDef> affordable;
      if (spawnDark)
      {
        affordable = entKindsDark
          .Where(k => k.combatPower * costMultiplier <= pointsRemaining)
          .ToList();
      }
      else
      {
        affordable = entKindsPlains
          .Where(k => k.combatPower * costMultiplier <= pointsRemaining)
          .ToList();
      }

      if (!affordable.Any())
      {
        break;
      }

      PawnKindDef entKind = affordable.RandomElement();

      // Pick a random tree to spawn near
      Thing targetTree = trees.RandomElement();
      Plant plant = targetTree as Plant;

      // Find a spawn location near the tree
      if (
        !CellFinder.TryFindRandomCellNear(
          targetTree.Position,
          map,
          1,
          (IntVec3 c) => c.Standable(map) && !c.Fogged(map),
          out IntVec3 spawnCell
        )
      )
      {
        // Try next tree
        continue;
      }

      // Spawn the ent
      Pawn ent = PawnGenerator.GeneratePawn(
        new PawnGenerationRequest(
          entKind,
          faction: null,
          context: PawnGenerationContext.NonPlayer,
          tile: map.Tile,
          forceGenerateNewPawn: true,
          allowDead: false,
          allowDowned: false
        )
      );

      GenSpawn.Spawn(ent, spawnCell, map, Rot4.Random);

      // Set energy comp to spawn back into this tree when depleted
      if (plant != null)
      {
        CompPlantEnergy energyComp = ent.TryGetComp<CompPlantEnergy>();
        if (energyComp != null)
        {
          energyComp.SetTreeData(plant.def, plant.Growth);
        }
      }

      // Destroy the tree and spawn effects
      IntVec3 treePosition = targetTree.Position;
      targetTree.Destroy(DestroyMode.Vanish);

      //TODO: Add sound effects

      // Spawn particle effects
      FleckMaker.ThrowDustPuffThick(
        treePosition.ToVector3Shifted(),
        map,
        2f,
        UnityEngine.Color.green
      );
      FleckMaker.ThrowSmoke(treePosition.ToVector3Shifted(), map, 1.5f);

      // Make ent aggressive
      ent.mindState.mentalStateHandler.TryStartMentalState(
        MentalStateDefOf.Manhunter,
        forceWake: true
      );

      // Deduct points with current multiplier, then increase cost for next ent
      pointsRemaining -= entKind.combatPower * costMultiplier;
      costMultiplier *= CostScalingPerEnt;
      spawned++;
    }

    return spawned;
  }
}
