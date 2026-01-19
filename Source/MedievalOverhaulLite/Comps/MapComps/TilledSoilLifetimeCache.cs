using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MOExpandedLite;

public class MapComponent_TilledSoilLifetimeCache : MapComponent
{
  // csharpier-ignore-start
  Dictionary<int, List<IntVec3>> terrainCache = new Dictionary<int, List<IntVec3>>();
  // csharpier-ignore-end

  public MapComponent_TilledSoilLifetimeCache(Map map)
    : base(map) { }
}
