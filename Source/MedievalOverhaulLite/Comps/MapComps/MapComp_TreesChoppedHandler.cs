using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MOExpandedLite;

public class MapComponent_TreesChoppedHandler : MapComponent
{
  private float treePointsChopped = 0;
  private Dictionary<int, float> plantExpirationDict = new();

  public void NotifyTreeChopped(float points, Pawn worker)
  {
    points /= 1000;
    if (points < 0.01f)
      points = 0.01f;
    treePointsChopped += points;

    int currTick = Find.TickManager.TicksGame;

    if (plantExpirationDict.ContainsKey(currTick))
      plantExpirationDict[currTick] += points;
    else
      plantExpirationDict[currTick] = points;

    ExpirePlants(currTick);
    TrySpawnEnts(worker);
  }

  private void ExpirePlants(int currTick)
  {
    List<int> toRemove = new();
    foreach (var kvp in plantExpirationDict)
    {
      if (currTick >= kvp.Key + 90000)
      {
        treePointsChopped -= kvp.Value;
        toRemove.Add(kvp.Key);
      }
    }
    foreach (int key in toRemove)
    {
      plantExpirationDict.Remove(key);
    }
  }

  private void TrySpawnEnts(Pawn worker)
  {
    if (!ShouldSpawnEnts())
    {
      return;
    }
    SpawnEnts(worker);
  }

  private void SpawnEnts(Pawn worker)
  {
    // Fire the ent attack incident
    IncidentParms parms = StorytellerUtility.DefaultParmsNow(
      IncidentCategoryDefOf.ThreatBig,
      this.map
    );
    parms.forced = true;
    parms.target = this.map;
    parms.spawnCenter = worker.Position; // Pass the worker's position to the incident

    IncidentDef entAttack = IncidentDef.Named("MOL_EntAttack");
    if (entAttack != null)
    {
      entAttack.Worker.TryExecute(parms);
    }
    else
    {
      Log.Error("MOL_EntAttack IncidentDef not found!");
    }
  }

  private bool ShouldSpawnEnts()
  {
    if (Rand.Value > 0.01f + treePointsChopped)
    // if (Rand.Value > 0.5f)
    {
      return false;
    }
    return true;
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Values.Look(ref treePointsChopped, "treePointsChopped", 0f);
    Scribe_Collections.Look(
      ref plantExpirationDict,
      "plantExpirationDict",
      LookMode.Value,
      LookMode.Value
    );
    plantExpirationDict ??= new();
  }

  public MapComponent_TreesChoppedHandler(Map map)
    : base(map) { }
}
