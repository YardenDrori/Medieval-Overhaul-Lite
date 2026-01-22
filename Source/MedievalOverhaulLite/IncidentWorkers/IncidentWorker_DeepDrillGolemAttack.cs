using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MOExpandedLite;

public class IncidentWorker_DeepDrillGolemAttack : IncidentWorker
{
  private static List<Thing> tmpDrills = new List<Thing>();

  // Each subsequent golem costs 20% more
  private const float CostScalingPerGolem = 1.2f;

  protected override bool CanFireNowSub(IncidentParms parms)
  {
    if (!base.CanFireNowSub(parms))
    {
      return false;
    }
    Map map = (Map)parms.target;
    GetUsableDeepDrills(map, tmpDrills);
    return tmpDrills.Any();
  }

  protected override bool TryExecuteWorker(IncidentParms parms)
  {
    Map map = (Map)parms.target;
    tmpDrills.Clear();
    GetUsableDeepDrills(map, tmpDrills);

    if (!tmpDrills.TryRandomElement(out var deepDrill))
    {
      return false;
    }

    // Spawn golems near the drill using storyteller's points directly
    int golemsSpawned = SpawnGolemsNear(deepDrill.Position, map, parms.points);

    if (golemsSpawned == 0)
    {
      return false;
    }

    // Notify the comp that we spawned an attack
    deepDrill.TryGetComp<CompCreatesGolemAttacks>()?.Notify_CreatedGolemAttack();

    SendStandardLetter(parms, new TargetInfo(deepDrill.Position, map));
    return true;
  }

  private int SpawnGolemsNear(IntVec3 position, Map map, float points)
  {
    List<PawnKindDef> golemKinds = new List<PawnKindDef>
    {
      PawnKindDef.Named("MOL_Golem_Iron"),
      PawnKindDef.Named("MOL_Golem_Steel"),
      PawnKindDef.Named("MOL_Golem_Gold"),
      PawnKindDef.Named("MOL_Golem_Silver"),
      PawnKindDef.Named("MOL_Golem_Plasteel"),
    };

    // Filter to only valid defs (in case some aren't loaded)
    golemKinds = golemKinds.Where(k => k != null).ToList();

    if (!golemKinds.Any())
    {
      Log.Warning("IncidentWorker_DeepDrillGolemAttack: No golem PawnKindDefs found!");
      return 0;
    }

    int spawned = 0;
    float pointsRemaining = points;
    float costMultiplier = 1f;

    while (pointsRemaining > 0)
    {
      // Pick a random golem type we can afford (accounting for cost scaling)
      List<PawnKindDef> affordable = golemKinds
        .Where(k => k.combatPower * costMultiplier <= pointsRemaining)
        .ToList();

      if (!affordable.Any())
      {
        break;
      }

      PawnKindDef golemKind = affordable.RandomElement();

      // Find a spawn location
      if (
        !CellFinder.TryFindRandomCellNear(
          position,
          map,
          10,
          (IntVec3 c) => c.Standable(map) && !c.Fogged(map),
          out IntVec3 spawnCell
        )
      )
      {
        break;
      }

      // Spawn the golem
      Pawn golem = PawnGenerator.GeneratePawn(
        new PawnGenerationRequest(
          golemKind,
          faction: null,
          context: PawnGenerationContext.NonPlayer,
          tile: map.Tile,
          forceGenerateNewPawn: true,
          allowDead: false,
          allowDowned: false
        )
      );

      GenSpawn.Spawn(golem, spawnCell, map, Rot4.Random);

      // Make golem aggressive
      golem.mindState.mentalStateHandler.TryStartMentalState(
        MentalStateDefOf.Manhunter,
        forceWake: true
      );

      // Deduct points with current multiplier, then increase cost for next golem
      pointsRemaining -= golemKind.combatPower * costMultiplier;
      costMultiplier *= CostScalingPerGolem;
      spawned++;
    }

    return spawned;
  }

  public static void GetUsableDeepDrills(Map map, List<Thing> outDrills)
  {
    outDrills.Clear();
    List<Thing> deepDrills = map.listerThings.ThingsOfDef(ThingDefOf.DeepDrill);

    foreach (Thing drill in deepDrills)
    {
      CompCreatesGolemAttacks comp = drill.TryGetComp<CompCreatesGolemAttacks>();
      if (comp != null && comp.CanCreateGolemAttackNow)
      {
        outDrills.Add(drill);
      }
    }
  }
}
