using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MOExpandedLite;

[HarmonyPatch(typeof(GenStep_Plants), "Generate")]
public static class Generate_Patch
{
  private static Dictionary<BiomeDef, Dictionary<ThingDef, float>> biomePlantFoodPools = new();
  private static Dictionary<BiomeDef, Dictionary<ThingDef, float>> biomePlantDrugPools = new();
  private static Dictionary<BiomeDef, Dictionary<ThingDef, float>> biomePlantUtilPools = new();

  [HarmonyPrefix]
  public static void Generate_Prefix(Map map, GenStepParams parms)
  {
    if (
      biomePlantFoodPools.NullOrEmpty()
      || biomePlantDrugPools.NullOrEmpty()
      || biomePlantUtilPools.NullOrEmpty()
    )
    {
      InitializePools();
    }

    var mapComp = map.GetComponent<MapComponent_TileCrops>();
    if (mapComp == null)
    {
      Log.Error("[MOL] MapComponent_TileCrops not found!");
      return;
    }

    BiomeDef biome = map.Biome;

    // Roll food pool
    if (
      !biomePlantFoodPools.NullOrEmpty()
      && biomePlantFoodPools.ContainsKey(biome)
      && !biomePlantFoodPools[biome].NullOrEmpty()
    )
    {
      int numToRoll = DetermineNumFoodCrops(biome);
      HashSet<ThingDef> chosen = new HashSet<ThingDef>();

      for (int i = 0; i < numToRoll; i++)
      {
        if (biomePlantFoodPools[biome].TryRandomElementByWeight(kvp => kvp.Value, out var crop))
        {
          if (!chosen.Contains(crop.Key))
          {
            mapComp.RegisterTilePlant(crop.Key, 0.005f);
            chosen.Add(crop.Key);
          }
        }
      }
    }

    // Roll drug pool
    if (
      !biomePlantDrugPools.NullOrEmpty()
      && biomePlantDrugPools.ContainsKey(biome)
      && !biomePlantDrugPools[biome].NullOrEmpty()
    )
    {
      int numToRoll = DetermineNumDrugCrops(biome);
      HashSet<ThingDef> chosen = new HashSet<ThingDef>();

      for (int i = 0; i < numToRoll; i++)
      {
        if (biomePlantDrugPools[biome].TryRandomElementByWeight(kvp => kvp.Value, out var crop))
        {
          if (!chosen.Contains(crop.Key))
          {
            mapComp.RegisterTilePlant(crop.Key, 0.003f);
            chosen.Add(crop.Key);
          }
        }
      }
    }

    // Roll utility pool
    if (
      !biomePlantUtilPools.NullOrEmpty()
      && biomePlantUtilPools.ContainsKey(biome)
      && !biomePlantUtilPools[biome].NullOrEmpty()
    )
    {
      if (biomePlantUtilPools[biome].TryRandomElementByWeight(kvp => kvp.Value, out var crop))
      {
        mapComp.RegisterTilePlant(crop.Key, 0.003f);
      }
    }
  }

  private static int DetermineNumFoodCrops(BiomeDef biome)
  {
    HashSet<string> foodStaples =
    [
      "TemperateForest",
      "TemperateSwamp",
      "TropicalSwamp",
      "AridShrubland",
      "Desert",
      "BorealForest",
      "ColdBog",
      "Tundra",
      "Grasslands",
      "GlacialPlain",
      "LavaField",
      "Glowforest",
    ];

    if (foodStaples.Contains(biome.defName))
    {
      return Rand.RangeInclusive(0, 2);
    }
    else
    {
      return Rand.RangeInclusive(1, 3);
    }
  }

  private static int DetermineNumDrugCrops(BiomeDef biome)
  {
    HashSet<string> drugStaples =
    [
      "TropicalRainforest",
      "ExtremeDesert",
      "Scarlands",
      "RG_AspenForest",
      "MOL_DarkForest",
    ];

    if (drugStaples.Contains(biome.defName))
    {
      return Rand.RangeInclusive(0, 1);
    }
    else
    {
      return Rand.RangeInclusive(1, 2);
    }
  }

  private static void InitializePools()
  {
    void AddPool(
      string biomeName,
      Dictionary<string, float> foodPools = null,
      Dictionary<string, float> drugPools = null,
      Dictionary<string, float> utilPools = null
    )
    {
      BiomeDef biome = DefDatabase<BiomeDef>.GetNamed(biomeName, false);

      if (biome == null)
      {
        return;
      }

      biomePlantFoodPools[biome] = new Dictionary<ThingDef, float>();
      biomePlantDrugPools[biome] = new Dictionary<ThingDef, float>();
      biomePlantUtilPools[biome] = new Dictionary<ThingDef, float>();

      if (foodPools != null)
      {
        foreach (var kvp in foodPools)
        {
          ThingDef plant = DefDatabase<ThingDef>.GetNamed(kvp.Key, false);
          if (plant != null)
          {
            biomePlantFoodPools[biome][plant] = kvp.Value;
          }
        }
      }

      if (drugPools != null)
      {
        foreach (var kvp in drugPools)
        {
          ThingDef plant = DefDatabase<ThingDef>.GetNamed(kvp.Key, false);
          if (plant != null)
          {
            biomePlantDrugPools[biome][plant] = kvp.Value;
          }
        }
      }

      if (utilPools != null)
      {
        foreach (var kvp in utilPools)
        {
          ThingDef plant = DefDatabase<ThingDef>.GetNamed(kvp.Key, false);
          if (plant != null)
          {
            biomePlantUtilPools[biome][plant] = kvp.Value;
          }
        }
      }
    }

    // Temperate Forest - Staple: Wheat
    AddPool(
      "TemperateForest",
      foodPools: new()
      {
        ["Plant_Corn"] = 2.5f,
        ["VCE_Pumpkin"] = 2f,
        ["VCE_Tomato"] = 2f,
        ["VCE_Sunflower_Wild"] = 2f,
        ["VCE_TreeOlive_Wild"] = 1.8f,
        ["VCE_Cabbage"] = 1.5f,
        ["MOL_Plant_Lentils_Wild"] = 1.5f,
        ["MOL_Plant_Garlic_Wild"] = 1.5f,
        ["VCE_Onion"] = 1f,
        ["MOL_Plant_Carrot_Wild"] = 1f,
      },
      drugPools: new()
      {
        ["Plant_Hops"] = 2f,
        ["VBE_Plant_Tobacco"] = 1.75f,
        ["Plant_Smokeleaf_Wild"] = 1.25f,
      },
      utilPools: new() { ["Plant_Haygrass"] = 1f }
    );

    // Temperate Swamp - Staple: Rice
    AddPool(
      "TemperateSwamp",
      foodPools: new()
      {
        ["VCE_Cabbage"] = 2.5f,
        ["VCE_Beet"] = 2.5f,
        ["VCE_Onion"] = 2f,
        ["VCE_Pumpkin"] = 2f,
        ["Plant_Potato"] = 1.5f,
        ["VCE_Soybean"] = 1.5f,
      },
      drugPools: new()
      {
        ["VBE_Plant_Tobacco"] = 2f,
        ["Plant_Smokeleaf_Wild"] = 1.75f,
        ["Plant_Hops"] = 1.25f,
      },
      utilPools: new() { ["Plant_Haygrass"] = 1f, ["VCE_Soybean"] = 1f }
    );

    // Tropical Rainforest - Staple: Coffee Beans
    AddPool(
      "TropicalRainforest",
      foodPools: new()
      {
        ["Plant_Rice"] = 3f,
        ["VCE_Pumpkin"] = 2.5f,
        ["VCE_Pepper"] = 2f,
        ["VCE_Eggplant"] = 1.5f,
        ["VCE_Tomato"] = 1.5f,
      },
      drugPools: new() { ["Plant_Psychoid_Wild"] = 3f, ["VFEM2_Plant_Grape"] = 2f },
      utilPools: new()
      {
        ["Plant_Haygrass"] = 1f,
        ["VCE_Sugarcane"] = 1f,
        ["VCE_Allspice"] = 1f,
      }
    );

    // Tropical Swamp - Staple: Hot Pepper
    AddPool(
      "TropicalSwamp",
      foodPools: new()
      {
        ["Plant_Rice"] = 3.5f,
        ["VCE_Eggplant"] = 3f,
        ["VCE_Beet"] = 2.5f,
        ["VCE_Tomato"] = 2f,
        ["VCE_Soybean"] = 1.5f,
      },
      drugPools: new()
      {
        ["VBE_Plant_Tobacco"] = 1.75f,
        ["VBE_Plant_Coffee"] = 1.75f,
        ["Plant_Psychoid_Wild"] = 1.5f,
      },
      utilPools: new()
      {
        ["Plant_Haygrass"] = 1f,
        ["VCE_Sugarcane"] = 1f,
        ["VCE_Allspice"] = 1f,
        ["VCE_Soybean"] = 1f,
      }
    );

    // Arid Shrubland - Staple: Olives
    AddPool(
      "AridShrubland",
      foodPools: new()
      {
        ["Plant_Corn"] = 2f,
        ["VCE_Sunflower_Wild"] = 2f,
        ["VCE_Onion"] = 2f,
        ["MOL_Plant_Garlic_Wild"] = 2f,
        ["MOL_Plant_Lentils_Wild"] = 2f,
        ["VCE_Eggplant"] = 1.5f,
        ["VCE_Tomato"] = 1.5f,
      },
      drugPools: new()
      {
        ["Plant_Psychoid_Wild"] = 3f,
        ["VBE_Plant_Tobacco"] = 2f,
        ["VFEM2_Plant_Grape"] = 1.5f,
      },
      utilPools: new() { ["Plant_Haygrass"] = 1f, ["VCE_Allspice"] = 1f }
    );

    // Desert - Staple: Lentils
    AddPool(
      "Desert",
      foodPools: new() { ["Plant_Potato"] = 1f, ["MOL_Plant_Garlic_Wild"] = 1f },
      drugPools: new() { ["VFEM2_Plant_Grape"] = 1.1f, ["MOL_Plant_Poppy_Wild"] = 0.9f }
    );

    // Extreme Desert - Staple: Poppies (no pools)
    AddPool("ExtremeDesert");

    // Boreal Forest - Staple: Potato
    AddPool(
      "BorealForest",
      foodPools: new()
      {
        ["VCE_Wheat"] = 2.5f,
        ["VCE_Cabbage"] = 2.5f,
        ["MOL_Plant_Carrot_Wild"] = 2f,
        ["VCE_Peas"] = 2f,
        ["MOL_Plant_Garlic_Wild"] = 1.5f,
        ["VCE_Onion"] = 1.5f,
      },
      drugPools: new()
      {
        ["Plant_Hops"] = 2.25f,
        ["Plant_Smokeleaf_Wild"] = 1.5f,
        ["MOL_Plant_Poppy_Wild"] = 1.25f,
      },
      utilPools: new() { ["Plant_Haygrass"] = 1f, ["VCE_Wasabi"] = 1f }
    );

    // Cold Bog - Staple: Cabbage
    AddPool(
      "ColdBog",
      foodPools: new()
      {
        ["Plant_Rice"] = 3f,
        ["Plant_Potato"] = 3f,
        ["VCE_Beet"] = 2.5f,
        ["VCE_Peas"] = 2f,
        ["VCE_Onion"] = 1f,
      },
      drugPools: new() { ["Plant_Hops"] = 3f, ["Plant_Smokeleaf_Wild"] = 2f },
      utilPools: new() { ["Plant_Haygrass"] = 1f, ["VCE_Wasabi"] = 1f }
    );

    // Tundra - Staple: Carrot
    AddPool(
      "Tundra",
      foodPools: new()
      {
        ["Plant_Potato"] = 2.5f,
        ["VCE_Peas"] = 2f,
        ["MOL_Plant_Lentils_Wild"] = 1f,
      },
      drugPools: new() { ["MOL_Plant_Poppy_Wild"] = 1f },
      utilPools: new() { ["Plant_Haygrass"] = 1f, ["VCE_Wasabi"] = 1f }
    );

    // Ice Sheet - no pools
    AddPool("IceSheet");

    // Sea Ice - no pools
    AddPool("SeaIce");

    // Glowforest - Staple: Nutrifungus
    AddPool("Glowforest", drugPools: new() { ["MOL_Plant_Psicap"] = 1f });

    // Scarlands - Staple: Smokeleaf
    AddPool(
      "Scarlands",
      foodPools: new()
      {
        ["VCE_Cabbage"] = 3f,
        ["Plant_Potato"] = 2.5f,
        ["VCE_Sunflower_Wild"] = 2f,
        ["MOL_Plant_Garlic_Wild"] = 2f,
        ["VCE_Peas"] = 2f,
        ["VCE_Tomato"] = 1.5f,
      },
      drugPools: new() { ["MOL_Plant_Poppy_Wild"] = 1f }
    );

    // Grasslands - Staple: Sunflowers
    AddPool(
      "Grasslands",
      foodPools: new()
      {
        ["VCE_Wheat"] = 2f,
        ["Plant_Corn"] = 2f,
        ["VCE_Tomato"] = 2f,
        ["VCE_Pumpkin"] = 1.8f,
        ["MOL_Plant_Lentils_Wild"] = 1.5f,
        ["VCE_TreeOlive_Wild"] = 1.5f,
        ["VCE_Cabbage"] = 1.5f,
        ["VCE_Onion"] = 1.2f,
        ["MOL_Plant_Carrot_Wild"] = 0.7f,
      },
      drugPools: new()
      {
        ["VFEM2_Plant_Grape"] = 1.5f,
        ["VBE_Plant_Tobacco"] = 1.5f,
        ["Plant_Smokeleaf_Wild"] = 1.25f,
        ["Plant_Hops"] = 0.75f,
      },
      utilPools: new() { ["Plant_Haygrass"] = 1f, ["VCE_Soybean"] = 1f }
    );

    // Glacial Plain - Staple: Pea
    AddPool(
      "GlacialPlain",
      foodPools: new()
      {
        ["VCE_Wheat"] = 3.5f,
        ["MOL_Plant_Carrot_Wild"] = 3f,
        ["VCE_Cabbage"] = 2.5f,
        ["Plant_Potato"] = 1f,
      },
      drugPools: new() { ["MOL_Plant_Poppy_Wild"] = 1f },
      utilPools: new() { ["Plant_Haygrass"] = 1f, ["VCE_Wasabi"] = 1f }
    );

    // Lava Fields - Staple: Tomato
    AddPool(
      "LavaField",
      foodPools: new()
      {
        ["VCE_Pepper"] = 3.5f,
        ["Plant_Corn"] = 3f,
        ["VCE_Sunflower_Wild"] = 2.5f,
        ["VCE_Eggplant"] = 2.5f,
        ["VCE_TreeOlive_Wild"] = 2f,
      },
      drugPools: new() { ["VBE_Plant_Coffee"] = 3f, ["Plant_Psychoid_Wild"] = 2f },
      utilPools: new() { ["VCE_Allspice"] = 1f }
    );

    // Aspen Forest - Staple: Hops
    AddPool(
      "RG_AspenForest",
      foodPools: new()
      {
        ["VCE_Wheat"] = 2.5f,
        ["VCE_Pumpkin"] = 2f,
        ["VCE_Cabbage"] = 2f,
        ["VCE_Peas"] = 2f,
        ["MOL_Plant_Carrot_Wild"] = 1.2f,
        ["VCE_Onion"] = 0.8f,
      },
      drugPools: new() { ["MOL_Plant_Poppy_Wild"] = 3f, ["Plant_Smokeleaf_Wild"] = 2f },
      utilPools: new() { ["Plant_Haygrass"] = 1f, ["VCE_Wasabi"] = 1f }
    );

    // Dark Forest - Staple: Grapes
    AddPool(
      "MOL_DarkForest",
      foodPools: new()
      {
        ["Plant_Potato"] = 3f,
        ["VCE_Cabbage"] = 2.5f,
        ["VCE_Beet"] = 2.5f,
        ["VCE_Sunflower_Wild"] = 2f,
        ["VCE_Onion"] = 2f,
        ["MOL_Plant_Carrot_Wild"] = 2f,
        ["Plant_Rice"] = 1f,
      },
      drugPools: new() { ["Plant_Smokeleaf_Wild"] = 2f, ["Plant_Hops"] = 1f },
      utilPools: new() { ["Plant_Haygrass"] = 1f, ["VCE_Soybean"] = 1f }
    );
  }
}
