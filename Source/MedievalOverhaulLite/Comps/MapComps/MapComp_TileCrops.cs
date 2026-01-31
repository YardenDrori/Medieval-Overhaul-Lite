using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MOExpandedLite;

public class MapComponent_TileCrops : MapComponent
{
  private Dictionary<ThingDef, float> tilePlants = new();
  private BiomeDef biomeDef = null;

  public void RegisterTilePlant(ThingDef plantToRegister, float weight)
  {
    if (biomeDef == null)
    {
      biomeDef = this.map.Biome;
    }
    if (plantToRegister.IsPlant)
    {
      tilePlants.Add(plantToRegister, weight);
    }
  }

  public ThingDef GetRandomPlant()
  {
    return tilePlants.RandomElementByWeight(kvp => kvp.Value).Key;
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Collections.Look(ref tilePlants, "tilePlants", LookMode.Def, LookMode.Value);
    Scribe_Defs.Look(ref biomeDef, "biomeDef");
  }

  public MapComponent_TileCrops(Map map)
    : base(map) { }
}
