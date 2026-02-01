using HarmonyLib;
using RimWorld;
using Verse;

namespace MOExpandedLite;

public class Thing_PlantSpawnerTicker : Thing
{
  private const int lifeTime = Autosaver_Patch.ticksToSpreadOver;
  private const int callsPerTick = 60000 / lifeTime;
  private int ticksAlive = 0;

  private WildPlantSpawner cachedSpawner;
  private Traverse cachedTickMethod;

  public override void SpawnSetup(Map map, bool respawningAfterLoad)
  {
    base.SpawnSetup(map, respawningAfterLoad);
    cachedSpawner = map.wildPlantSpawner;
    if (cachedSpawner != null)
    {
      cachedTickMethod = Traverse.Create(cachedSpawner).Method("WildPlantSpawnerTickInternal");
    }
  }

  protected override void Tick()
  {
    WildPlantSpawnerTickInternal_Patch.allowedCalls = callsPerTick;

    if (cachedTickMethod != null)
    {
      for (int i = 0; i < callsPerTick; i++)
      {
        cachedTickMethod.GetValue();
      }
    }

    ticksAlive++;
    if (ticksAlive >= lifeTime)
    {
      kill();
    }
  }

  private void kill()
  {
    ticksAlive = 0;
    this.Destroy();
  }
}
