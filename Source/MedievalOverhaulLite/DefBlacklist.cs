using System.Collections.Generic;

namespace MOExpandedLite;

/// <summary>
/// Central blacklist configuration for defs that should be prevented from loading.
/// All collections are readonly after static init — thread-safe for parallel def loading.
/// </summary>
public static class DefBlacklist
{
  private static readonly string[] BlockedPrefixes = { "AC_" };

  private static readonly HashSet<string> Whitelisted = new()
  {
    //buildings
    "AC_Infuser",
    "AC_VinegarCask",
    "AC_OilPressWorkshop",
    "AC_ArtisanWorkshop",
    "AC_BottlingStation",
    "AC_DairyProcessingStation",
    //WorkGiverDefs
    "AC_DoBillsOilPressWorkshop",
    "AC_DoBillsDairyProcessingStation",
    "AC_DoBillsArtisanTable",
    "AC_DoBillsBottlingPlant",
    //hediffs
    "AC_PerfumeHediff",
    //items
    "AC_Butter", // bakes fine, soup fine
    "AC_Ghee", // bakes lavish, soup lavish
    "AC_Vinegar", // sushi fine, soup fine
    "AC_BalsamicVinegar", // sushi lavish, soup lavish
    "AC_Oil", // vanilla fine, grill lavish
    "AC_Perfume",
    "AC_Pickles",
    "AC_Jam",
    "AC_Soap",
  };

  private static readonly HashSet<string> Blacklisted = new()
  {
    //misc stuff with similar names making them viable for the whitelist
    "AC_ButteredPopcorn",
    "AC_TeaSachets",
    "AC_SkySyrup",
    "AC_RoyalSalve",
    "AC_PipeTobacco",
    "AC_JamonSerrano",
    "AC_IcedElixir",
    "AC_FlavouredKombucha",
    "AC_EggPickles",
    "AC_DesertSalve",
    "AC_CoffeeLiquor",
    "AC_ChichaMorada",
    "AC_Buttermilk",
    "AC_BlackMayonnaise",
    "AC_BlackGarlic",
    "AC_ArtisanalKombucha",
    // VCE ThingDefs - deep fried
    "VCE_DeepFriedBigMeat",
    "VCE_DeepFriedVegetables",
    "VCE_DeepFriedFish",
    "VCE_DeepFriedSushi",
    // VCE ThingDefs - canned
    "VCE_CannedMeat",
    "VCE_CannedProduce",
    "VCE_CannedFruit",
    "VCE_CannedAP",
    "VCE_CannedFish",
    // VCE Buildings
    "VCE_DeepFrier",
    "VCE_CanningMachine",
    "VCE_CheesePress",
    // VCE RecipeDefs
    "VCE_DeepFryMeats",
    "VCE_DeepFryVegetables",
    "VCE_DeepFryFish",
    "VCE_DeepFryAlienFish",
    "VCE_DeepFrySushi",
    "VCE_CanMeats",
    "VCE_CanProduce",
    "VCE_CanFruit",
    "VCE_CanEggs",
    "VCE_CanFish",
    "VCE_CanAlienFish",
    // VCE ResearchProjectDefs
    "VCE_DeepFrying",
    "VCE_Canning",
    // VCE WorkGiverDefs
    "VCE_DoBillsFryer",
    "VCE_DoBillsCanning",
  };

  public static bool ShouldBlockDef(string defName)
  {
    if (string.IsNullOrEmpty(defName))
      return false;

    // Exact whitelist match
    if (Whitelisted.Contains(defName))
      return false;

    // Explicit blacklist takes priority over fuzzy whitelist matching
    // (e.g. AC_ButteredPopcorn is blacklisted even though it starts with whitelisted AC_Butter)
    if (Blacklisted.Contains(defName))
      return true;

    // Catch RimWorld's duplicate-rename pattern: AC_Butter48347 is still AC_Butter
    foreach (var name in Whitelisted)
    {
      if (defName.StartsWith(name))
        return false;
    }

    for (int i = 0; i < BlockedPrefixes.Length; i++)
    {
      if (defName.StartsWith(BlockedPrefixes[i]))
        return true;
    }

    return false;
  }

  /// <summary>
  /// Checks if an asset path belongs to a blacklisted def.
  /// Uses StartsWith matching for the whitelist so texture variants like
  /// "AC_ButterA" or "AC_Butter_south" are recognized as belonging to whitelisted AC_Butter.
  /// </summary>
  public static bool ShouldBlockGraphicPath(string path)
  {
    if (string.IsNullOrEmpty(path))
      return false;

    // Check each path segment
    int start = 0;
    while (start < path.Length)
    {
      int slash = path.IndexOf('/', start);
      string segment = slash >= 0 ? path.Substring(start, slash - start) : path.Substring(start);
      if (segment.Length > 0 && IsGraphicSegmentBlocked(segment))
        return true;
      if (slash < 0)
        break;
      start = slash + 1;
    }

    return false;
  }

  /// <summary>
  /// Checks a single path segment (filename or folder name) against the blacklist.
  /// Uses StartsWith for whitelist matching so texture variants are preserved.
  /// e.g. "AC_ButterA" starts with whitelisted "AC_Butter" → NOT blocked.
  /// e.g. "AC_CheeseA" starts with no whitelisted name → blocked by AC_ prefix.
  /// </summary>
  private static bool IsGraphicSegmentBlocked(string segment)
  {
    // Check explicit blacklist (also with StartsWith for texture variants)
    foreach (var name in Blacklisted)
    {
      if (segment.StartsWith(name))
        return true;
    }

    // Check whitelist, if segment belongs to a whitelisted def, allow it.
    // Uses StartsWith so texture suffixes (A, B, _north, _south) don't break it.
    foreach (var name in Whitelisted)
    {
      if (segment.StartsWith(name))
        return false;
    }

    // Check blocked prefixes
    for (int i = 0; i < BlockedPrefixes.Length; i++)
    {
      if (segment.StartsWith(BlockedPrefixes[i]))
        return true;
    }

    return false;
  }
}
