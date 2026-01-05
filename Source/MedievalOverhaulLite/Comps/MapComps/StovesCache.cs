using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MOExpandedLite
{
  public class MapComponent_StoveTracker : MapComponent
  {
    public HashSet<Building_WorkTable> allStoves = new HashSet<Building_WorkTable>();

    public MapComponent_StoveTracker(Map map)
      : base(map) { }
  }
}
