using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MOExpandedLite
{
  public class MapComponent_StoveTracker : MapComponent
  {
    private HashSet<Building_WorkTable> allStoves = new HashSet<Building_WorkTable>();

    private Building_WorkTable IsValidType(Thing thingToVerify)
    {
      Building_WorkTable stove = thingToVerify as Building_WorkTable;
      if (stove == null)
      {
        Log.Error(
          $"[Medieval Overhaul Lite] attempted to add a Thing ({thingToVerify.def.defName}) to MapComponent_StoveTracker which does not have nor inherit the Building_WorkTable thihgClass"
        );
        return null;
      }
      return stove;
    }

    public void AddStove(Thing thingToAdd)
    {
      Building_WorkTable stove = IsValidType(thingToAdd);
      if (stove != null)
      {
        allStoves.Add(stove);
      }
    }

    public void RemoveStove(Thing thingToRemove)
    {
      Building_WorkTable stove = IsValidType(thingToRemove);
      if (stove != null)
      {
        allStoves.Remove(stove);
      }
    }

    public HashSet<Building_WorkTable> AllStoves => allStoves;

    public MapComponent_StoveTracker(Map map)
      : base(map) { }
  }
}
