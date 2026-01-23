using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MOExpandedLite;

public class IncidentWorker_DeepDrillGolemAttack : IncidentWorker
{
  private static List<Thing> tmpDrills = new List<Thing>();

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

    // Find spawn location near drill
    ThingDef spawnerDef = DefDatabase<ThingDef>.GetNamedSilentFail("MOL_GolemSpawner");
    if (spawnerDef == null)
    {
      Log.Error("[Medieval Overhaul Lite] Could not find MOL_GolemSpawner ThingDef");
      return false;
    }

    IntVec3 spawnLoc = CellFinder.FindNoWipeSpawnLocNear(
      deepDrill.Position,
      map,
      spawnerDef,
      Rot4.North,
      10,
      (IntVec3 x) => x.Walkable(map) && x.GetFirstThing(map, deepDrill.def) == null
    );

    if (spawnLoc == deepDrill.Position)
    {
      return false;
    }

    // Create and spawn the golem spawner with all points
    GolemSpawner golemSpawner = (GolemSpawner)ThingMaker.MakeThing(spawnerDef);
    golemSpawner.golemPoints = parms.points;
    GenSpawn.Spawn(golemSpawner, spawnLoc, map, WipeMode.FullRefund);

    // Notify the comp that we spawned an attack
    deepDrill.TryGetComp<CompCreatesGolemAttacks>()?.Notify_CreatedGolemAttack();

    SendStandardLetter(parms, new TargetInfo(deepDrill.Position, map));
    return true;
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
