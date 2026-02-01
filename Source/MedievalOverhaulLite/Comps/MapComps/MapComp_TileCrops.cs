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

    if (plantToRegister == null)
    {
      Log.Error("[MOL] Tried to register null plant!");
      return;
    }

    if (!plantToRegister.IsPlant)
    {
      Log.Error(
        $"[MOL] {plantToRegister.defName} is not a plant! Category: {plantToRegister.category}"
      );
      return;
    }

    if (!tilePlants.ContainsKey(plantToRegister))
    {
      tilePlants.Add(plantToRegister, weight);
      Log.Message($"[MOL] Successfully registered {plantToRegister.defName} with weight {weight}");
    }
    else
    {
      Log.Warning($"[MOL] {plantToRegister.defName} already registered, skipping");
    }
  }

  public Dictionary<ThingDef, float> GetAllTilePlants()
  {
    return tilePlants;
  }

  public float GetWeightForPlant(ThingDef plant)
  {
    if (!tilePlants.ContainsKey(plant))
    {
      Log.Warning(
        $"[Medieval Overhaul Lite] Attempted to replace weight of {plant.defName} which is not in the tilePlants Dictionary. This likely due to a configuration error with a biome setting a plant as a wildplant but with weight value of '0'"
      );
      return 0;
    }
    return tilePlants[plant];
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
