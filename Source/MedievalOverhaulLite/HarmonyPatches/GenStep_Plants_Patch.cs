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
    Log.Message($"[MOL] Generate_Prefix called for biome: {map.Biome.defName}");
    if (
      biomePlantFoodPools.NullOrEmpty()
      || biomePlantDrugPools.NullOrEmpty()
      || biomePlantUtilPools.NullOrEmpty()
    )
    {
      Log.Message("[MOL] Initializing pools...");
      InitializePools();
    }

    // Roll pools for this map and store in MapComponent
    var mapComp = map.GetComponent<MapComponent_TileCrops>();
    if (mapComp == null)
    {
      Log.Error("MapComponent_TileCrops not found!");
      return;
    }

    BiomeDef biome = map.Biome;
    Log.Message($"[MOL] Rolling crops for {biome.defName}");

    // Roll food pool
    if (
      !biomePlantFoodPools.NullOrEmpty()
      && biomePlantFoodPools.ContainsKey(biome)
      && !biomePlantFoodPools[biome].NullOrEmpty()
    )
    {
      Log.Message($"[MOL] Food pool has {biomePlantFoodPools[biome].Count} options");
      int numToRoll = DetermineNumFoodCrops(biome);
      Log.Message($"[MOL] Rolling {numToRoll} food crops");
      for (int i = 0; i < numToRoll; i++)
      {
        ThingDef plant = biomePlantFoodPools[biome].RandomElementByWeight(kvp => kvp.Value).Key;
        mapComp.RegisterTilePlant(plant, 0.003f);
      }
    }
    if (
      !biomePlantDrugPools.NullOrEmpty()
      && biomePlantDrugPools.ContainsKey(biome)
      && !biomePlantDrugPools[biome].NullOrEmpty()
    )
    {
      int numToRoll = DetermineNumDrugCrops(biome);
      for (int i = 0; i < numToRoll; i++)
      {
        ThingDef plant = biomePlantDrugPools[biome].RandomElementByWeight(kvp => kvp.Value).Key;
        mapComp.RegisterTilePlant(plant, 0.002f);
      }
    }
    if (
      !biomePlantUtilPools.NullOrEmpty()
      && biomePlantUtilPools.ContainsKey(biome)
      && !biomePlantUtilPools[biome].NullOrEmpty()
    )
    {
      int numToRoll = 1;
      for (int i = 0; i < numToRoll; i++)
      {
        ThingDef plant = biomePlantUtilPools[biome].RandomElementByWeight(kvp => kvp.Value).Key;
        mapComp.RegisterTilePlant(plant, 0.001f);
      }
    }
  }

  private static int DetermineNumFoodCrops(BiomeDef biome)
  {
    HashSet<string> foodStaples =
    [
      "TemperateForest",
      "TemperateSwamp",
      "Desert",
      "BorealForest",
      "ColdBog",
      "Tundra",
      "Grasslands",
      "GlacialPlain",
      "LavaField",
    ];
    if (foodStaples.Contains(biome.defName))
    {
      return Rand.Range(0, 2);
    }
    else
    {
      return Rand.Range(1, 3);
    }
  }

  private static int DetermineNumDrugCrops(BiomeDef biome)
  {
    HashSet<string> foodStaples =
    [
      "TemperateForest",
      "TemperateSwamp",
      "Desert",
      "BorealForest",
      "ColdBog",
      "Tundra",
      "Grasslands",
      "GlacialPlain",
      "LavaField",
    ];
    if (foodStaples.Contains(biome.defName))
    {
      return Rand.Range(1, 2);
    }
    else
    {
      return Rand.Range(0, 1);
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

      // Skip if biome doesn't exist (e.g., DLC not installed)
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
        ["VCE_Cabbage"] = 1.5f,
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
        ["VCE_Onion"] = 2f,
        ["VCE_Pumpkin"] = 2f,
        ["VCE_Beet"] = 2f,
        ["Plant_Potato"] = 1.5f,
      },
      drugPools: new()
      {
        ["VBE_Plant_Tobacco"] = 2f,
        ["Plant_Smokeleaf_Wild"] = 1.75f,
        ["Plant_Hops"] = 1.25f,
      },
      utilPools: new() { ["Plant_Haygrass"] = 1f }
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
        ["VCE_Tomato"] = 1f,
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
        ["VCE_Tomato"] = 2f,
        ["VCE_Beet"] = 1.5f,
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
      }
    );

    // Arid Shrubland - Staple: Grapes
    AddPool(
      "AridShrubland",
      foodPools: new()
      {
        ["Plant_Corn"] = 2f,
        ["VCE_Onion"] = 2f,
        ["MOL_Plant_Garlic_Wild"] = 2f,
        ["VCE_Eggplant"] = 1.5f,
        ["VCE_Tomato"] = 1.5f,
        ["MOL_Plant_Lentils_Wild"] = 1f,
      },
      drugPools: new() { ["Plant_Psychoid_Wild"] = 3f, ["VBE_Plant_Tobacco"] = 2f },
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
        ["VCE_Peas"] = 1.5f,
        ["VCE_Onion"] = 1.5f,
      },
      drugPools: new()
      {
        ["Plant_Hops"] = 2.25f,
        ["Plant_Smokeleaf_Wild"] = 1.5f,
        ["MOL_Plant_Poppy_Wild"] = 1.25f,
      },
      utilPools: new() { ["Plant_Haygrass"] = 1f }
    );

    // Cold Bog - Staple: Cabbage
    AddPool(
      "ColdBog",
      foodPools: new()
      {
        ["Plant_Rice"] = 3f,
        ["Plant_Potato"] = 3f,
        ["VCE_Beet"] = 2f,
        ["VCE_Onion"] = 1f,
        ["VCE_Peas"] = 1f,
      },
      drugPools: new() { ["Plant_Hops"] = 3f, ["Plant_Smokeleaf_Wild"] = 2f },
      utilPools: new() { ["Plant_Haygrass"] = 1f }
    );

    // Tundra - Staple: Carrot
    AddPool(
      "Tundra",
      foodPools: new()
      {
        ["Plant_Potato"] = 2.5f,
        ["VCE_Peas"] = 1.5f,
        ["MOL_Plant_Lentils_Wild"] = 1f,
      },
      drugPools: new() { ["MOL_Plant_Poppy_Wild"] = 1f },
      utilPools: new() { ["Plant_Haygrass"] = 1f }
    );

    // Ice Sheet - no pools
    AddPool("IceSheet");

    // Sea Ice - no pools
    AddPool("SeaIce");

    // Glowforest (Odyssey) - Staple: Nutrifungus (no additional pools)
    AddPool("Glowforest");

    // Scarlands - Staple: Smokeleaf
    AddPool(
      "Scarlands",
      foodPools: new()
      {
        ["VCE_Cabbage"] = 3f,
        ["Plant_Potato"] = 2.5f,
        ["MOL_Plant_Garlic_Wild"] = 2f,
        ["VCE_Peas"] = 1.5f,
        ["VCE_Tomato"] = 1f,
      },
      drugPools: new() { ["MOL_Plant_Poppy_Wild"] = 1f }
    );

    // Grasslands - Staple: Corn
    AddPool(
      "Grasslands",
      foodPools: new()
      {
        ["VCE_Wheat"] = 2f,
        ["VCE_Pumpkin"] = 1.8f,
        ["VCE_Tomato"] = 1.8f,
        ["VCE_Cabbage"] = 1.5f,
        ["VCE_Onion"] = 1.2f,
        ["MOL_Plant_Lentils_Wild"] = 1f,
        ["MOL_Plant_Carrot_Wild"] = 0.7f,
      },
      drugPools: new()
      {
        ["VFEM2_Plant_Grape"] = 1.5f,
        ["VBE_Plant_Tobacco"] = 1.5f,
        ["Plant_Smokeleaf_Wild"] = 1.25f,
        ["Plant_Hops"] = 0.75f,
      },
      utilPools: new() { ["Plant_Haygrass"] = 1f }
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
      utilPools: new() { ["Plant_Haygrass"] = 1f }
    );

    // Lava Fields - Staple: Tomato
    AddPool(
      "LavaField",
      foodPools: new()
      {
        ["Plant_Corn"] = 4f,
        ["VCE_Pepper"] = 3.5f,
        ["VCE_Eggplant"] = 2.5f,
      },
      drugPools: new() { ["VBE_Plant_Coffee"] = 3f, ["Plant_Psychoid_Wild"] = 2f },
      utilPools: new() { ["VCE_Allspice"] = 1f }
    );

    // Aspen Forest (Regrowth) - Staple: Hops
    AddPool(
      "RG_AspenForest",
      foodPools: new()
      {
        ["VCE_Wheat"] = 2.5f,
        ["VCE_Pumpkin"] = 2f,
        ["VCE_Cabbage"] = 2f,
        ["VCE_Peas"] = 1.5f,
        ["MOL_Plant_Carrot_Wild"] = 1.2f,
        ["VCE_Onion"] = 0.8f,
      },
      drugPools: new() { ["MOL_Plant_Poppy_Wild"] = 3f, ["Plant_Smokeleaf_Wild"] = 2f },
      utilPools: new() { ["Plant_Haygrass"] = 1f }
    );
  }
}
