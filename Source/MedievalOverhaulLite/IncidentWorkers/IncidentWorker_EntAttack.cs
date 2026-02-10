using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MOExpandedLite;

public class IncidentWorker_EntAttack : IncidentWorker
{
  protected override bool TryExecuteWorker(IncidentParms parms)
  {
    Map map = (Map)parms.target;

    // Find trees near the spawn center
    const int radius = 45;
    List<Thing> treesNearPawn = new List<Thing>();
    foreach (
      Thing thing in GenRadial.RadialDistinctThingsAround(parms.spawnCenter, map, radius, true)
    )
    {
      if (thing is Plant plant && plant.def.plant.IsTree && !plant.Destroyed)
      {
        treesNearPawn.Add(thing);
      }
    }

    if (treesNearPawn.Count == 0)
    {
      // Fallback - find any tree on the map
      foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Plant))
      {
        if (thing is Plant plant && plant.def.plant.IsTree && !plant.Destroyed)
        {
          treesNearPawn.Add(thing);
        }
      }
    }

    if (treesNearPawn.Count == 0)
    {
      return false;
    }

    // Get the spawner def
    ThingDef spawnerDef = DefDatabase<ThingDef>.GetNamedSilentFail("MOL_EntSpawner");
    if (spawnerDef == null)
    {
      Log.Error("[Medieval Overhaul Lite] Could not find MOL_EntSpawner ThingDef");
      return false;
    }

    bool spawnDark = map.Biome?.defName == "MOL_DarkForest";

    // Get available ent kinds
    PawnKindDef majorEnt = spawnDark
      ? PawnKindDef.Named("MOL_Schrat_Dark")
      : PawnKindDef.Named("MOL_Schrat_Plains");
    PawnKindDef minorEnt = spawnDark
      ? PawnKindDef.Named("MOL_SchratDark_Sapling")
      : PawnKindDef.Named("MOL_SchratPlains_Sapling");

    List<PawnKindDef> availableEnts = new List<PawnKindDef>();
    if (majorEnt != null)
      availableEnts.Add(majorEnt);
    if (minorEnt != null)
      availableEnts.Add(minorEnt);

    if (availableEnts.Count == 0)
    {
      Log.Warning("[Medieval Overhaul Lite] No ent PawnKindDefs found!");
      return false;
    }

    // Get minimum cost
    float minCost = availableEnts.Min(e => e.combatPower);

    float pointsRemaining = parms.points;
    int spawned = 0;
    List<Thing> spawnedSpawners = new List<Thing>();

    // Create spawners until we run out of points/trees
    while (pointsRemaining >= minCost && treesNearPawn.Count > 0)
    {
      // Pick ent type - prefer major ents 70% of the time
      PawnKindDef selectedEnt = null;

      // Check which ents we can afford
      bool canAffordMajor = majorEnt != null && majorEnt.combatPower <= pointsRemaining;
      bool canAffordMinor = minorEnt != null && minorEnt.combatPower <= pointsRemaining;

      if (canAffordMajor && Rand.Chance(0.95f))
      {
        selectedEnt = majorEnt;
      }
      else if (canAffordMinor)
      {
        selectedEnt = minorEnt;
      }
      else if (canAffordMajor)
      {
        selectedEnt = majorEnt;
      }

      // Can't afford any ent
      if (selectedEnt == null)
      {
        break;
      }

      // Pick a random tree
      Thing targetTree = treesNearPawn.RandomElement();
      treesNearPawn.Remove(targetTree); // Don't use same tree twice
      Plant plant2 = targetTree as Plant;

      // Store tree data (don't destroy yet - keep visible during animation)
      IntVec3 treePosition = targetTree.Position;
      ThingDef treeDef = targetTree.def;
      float treeGrowth = plant2?.Growth ?? 0.5f;

      // Create and spawn the ent spawner at tree position
      EntSpawner entSpawner = (EntSpawner)ThingMaker.MakeThing(spawnerDef);
      entSpawner.spawnDarkVariant = spawnDark;
      entSpawner.treeDefToRestore = treeDef;
      entSpawner.treeGrowth = treeGrowth;
      entSpawner.treeToDestroy = targetTree; // Keep the tree alive during animation
      entSpawner.entKindToSpawn = selectedEnt; // Set which ent this spawner will spawn
      GenSpawn.Spawn(entSpawner, treePosition, map);
      spawnedSpawners.Add(entSpawner);

      pointsRemaining -= selectedEnt.combatPower;
      spawned++;
    }

    if (spawned == 0)
    {
      return false;
    }

    SendStandardLetter(parms, new LookTargets(spawnedSpawners));
    return true;
  }
}
