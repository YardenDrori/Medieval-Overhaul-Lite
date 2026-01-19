using System.Linq;
using RimWorld;
using Verse;

namespace MOExpandedLite;

public class MapComponent_TilledSoilLifetimeCache : MapComponent
{
  public Thing_TilledSoilManager Manager { get; private set; }

  public MapComponent_TilledSoilLifetimeCache(Map map)
    : base(map) { }

  public override void FinalizeInit()
  {
    base.FinalizeInit();

    Manager =
      map.listerThings.ThingsOfDef(MOL_DefOf.MOL_TilledSoilManager).FirstOrDefault()
      as Thing_TilledSoilManager;

    if (Manager == null)
    {
      Manager = (Thing_TilledSoilManager)ThingMaker.MakeThing(MOL_DefOf.MOL_TilledSoilManager);
      GenSpawn.Spawn(Manager, IntVec3.Zero, map);
    }
  }
}
