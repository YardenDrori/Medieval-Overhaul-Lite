using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MOExpandedLite;

public class GolemSpawner : GroundSpawner
{
  public float golemPoints;
  private const int SpawnRadius = 2;
  private const float CostScalingPerGolem = 1.2f;

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Values.Look(ref golemPoints, "golemPoints", 0f);
  }

  protected override void Spawn(Map map, IntVec3 loc)
  {
    if (golemPoints <= 0f)
    {
      Log.Warning("[Medieval Overhaul Lite] GolemSpawner: golemPoints is " + golemPoints);
      return;
    }

    List<PawnKindDef> golemKinds = new List<PawnKindDef>
    {
      PawnKindDef.Named("MOL_Golem_Iron"),
      PawnKindDef.Named("MOL_Golem_Steel"),
      PawnKindDef.Named("MOL_Golem_Gold"),
      PawnKindDef.Named("MOL_Golem_Silver"),
      PawnKindDef.Named("MOL_Golem_Plasteel"),
    };

    // Filter to only valid defs
    golemKinds.RemoveAll(k => k == null);

    if (golemKinds.Count == 0)
    {
      Log.Warning("[Medieval Overhaul Lite] GolemSpawner: No golem PawnKindDefs found!");
      return;
    }

    float pointsLeft = golemPoints;
    if (pointsLeft < 350)
    {
      pointsLeft = 350;
    }
    float costMultiplier = 1f;
    int golemCount = 0;

    while (pointsLeft > 0f)
    {
      // Find affordable golems
      List<PawnKindDef> affordable = new List<PawnKindDef>();
      foreach (var kind in golemKinds)
      {
        if (kind.combatPower * costMultiplier <= pointsLeft)
        {
          affordable.Add(kind);
        }
      }

      if (affordable.Count == 0)
      {
        // Log.Message(
        //   "[Medieval Overhaul Lite] GolemSpawner: No affordable golems. pointsLeft="
        //     + pointsLeft
        //     + ", costMultiplier="
        //     + costMultiplier
        // );
        break;
      }

      PawnKindDef golemKind = affordable.RandomElement();
      float golemCost = golemKind.combatPower * costMultiplier;

      // Log.Message(
      //   "[Medieval Overhaul Lite] GolemSpawner: Spawning "
      //     + golemKind.label
      //     + " (cost: "
      //     + golemCost
      //     + ")"
      // );

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

      IntVec3 spawnCell = CellFinder.RandomClosewalkCellNear(loc, map, SpawnRadius);
      GenSpawn.Spawn(golem, spawnCell, map);

      // Make golem aggressive
      golem.mindState.mentalStateHandler.TryStartMentalState(
        MentalStateDefOf.Manhunter,
        forceWake: true
      );

      pointsLeft -= golemCost;
      if (pointsLeft < 0 && pointsLeft < 350)
      {
        pointsLeft = 350;
      }
      costMultiplier *= CostScalingPerGolem;
      golemCount++;
    }

    // Log.Message("[Medieval Overhaul Lite] GolemSpawner: Spawned " + golemCount + " golems total");

    // Visual effects
    FleckMaker.ThrowDustPuffThick(loc.ToVector3Shifted(), map, 2f, Color.gray);
    FleckMaker.ThrowSmoke(loc.ToVector3Shifted(), map, 1.5f);
  }
}
