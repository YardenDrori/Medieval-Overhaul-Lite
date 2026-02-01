using RimWorld;
using Verse;

namespace MOExpandedLite;

[DefOf]
public static class MOL_DefOf
{
  public static ThingDef MOL_TilledSoilManager;
  public static ThingDef MOL_PlantSpawner;
  public static ThingDef MOL_BoneMeal;
  public static TerrainDef MOL_SoilTilled;

  static MOL_DefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(MOL_DefOf));
  }
}
