using RimWorld;
using Verse;

namespace MOExpandedLite;

[DefOf]
public static class ThingDefOf_MOExpandedLite
{
  public static ThingDef MOL_Bone;
  public static ThingDef MOL_Fat;

  static ThingDefOf_MOExpandedLite()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf_MOExpandedLite));
  }
}
