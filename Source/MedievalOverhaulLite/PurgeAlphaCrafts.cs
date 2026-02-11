using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace MOExpandedLite;

[StaticConstructorOnStartup]
public static class PurgeAlphaCrafts
{
  static PurgeAlphaCrafts()
  {
    // Run the test after a delay to ensure everything is loaded
    LongEventHandler.ExecuteWhenFinished(PurgeStart);
  }

  private static void PurgeStart()
  {
    Log.Message("========== STARTING ALPHA CRAFTS PURGE ==========");
    HashSet<String> thingsToKeep =
    [
      "AC_PerfumeHediff",
      "AC_Butter", //bakes fine, soup fine
      "AC_Ghee", //bakes lavish, soup lavish
      "AC_Vinegar", //sushi fine, soup fine
      "AC_BalsamicVinegar", //sushi lavish, soup lavish
      "AC_Oil", //vanilla fine, grill lavish
      //honey new item! grill fine
      //oil + vinegar - vanilla lavish
      // Olive Oil -- new item! Soup base
      // Wine grill base
      "AC_Perfume",
      "AC_Pickles",
      "AC_Jam",
      "AC_Soap",
    ];
    HashSet<String> thingsRemoved = new();
    thingsRemoved.UnionWith(RemoveThingDefs(thingsToKeep));
    thingsRemoved.UnionWith(RemoveHedifDefs(thingsToKeep));
    thingsRemoved.UnionWith(RemoveJobDefs(thingsToKeep));
    thingsRemoved.UnionWith(RemoveProcessDefs());
    thingsRemoved.UnionWith(RemoveRecipeDefs(thingsToKeep));
    thingsRemoved.UnionWith(RemoveThoughtDefs(thingsToKeep));
    thingsRemoved.UnionWith(RemoveVEFGraphicOffsets(thingsToKeep));

    // Remove VCE_Condiments category from all Alpha Crafts items
    RemoveCondimentsCategory();

    // Clean up orphaned graphics for removed items
    CleanupOrphanedGraphics(thingsRemoved);

    Log.Message($"Purged {thingsRemoved.Count} items from Alpha crafts");

    Resources.UnloadUnusedAssets(); // Unity's texture GC
    System.GC.Collect(); // C# GC for good measure
    Log.Message("========== FINISHED ALPHA CRAFTS PURGE ==========");

    // Now purge VCE items
    PurgeVCE();
  }

  private static HashSet<String> RemoveThingDefs(HashSet<String> thingsToKeep)
  {
    // Get the private Remove method using reflection
    var removeMethod = typeof(DefDatabase<ThingDef>).GetMethod(
      "Remove",
      BindingFlags.Static | BindingFlags.NonPublic
    );
    if (removeMethod == null)
    {
      Log.Error("[TEST] Could not find Remove method via reflection!");
      return null;
    }

    ///first pass thingDefs
    // int thingDefsRemoved = 0;
    HashSet<String> thingsRemoved = new();
    foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs.ToList())
    {
      if (!thing.defName.StartsWith("AC_") || thingsToKeep.Contains(thing.defName))
      {
        continue;
      }
      // Call Remove on the original def
      try
      {
        thingsRemoved.Add(thing.defName);
        removeMethod.Invoke(null, new object[] { thing });
        // thingDefsRemoved++;
      }
      catch (Exception ex)
      {
        thingsRemoved.Remove(thing.defName);
        Log.Error(
          $"[Medieval Overhaul Lite] Exception during Remove: {ex.Message}\n{ex.StackTrace}"
        );
        continue;
      }
    }
    return thingsRemoved;
  }

  private static HashSet<String> RemoveVEFGraphicOffsets(HashSet<String> thingsToKeep)
  {
    // Get the private Remove method using reflection
    var removeMethod = typeof(DefDatabase<VEF.Graphics.GraphicOffsets>).GetMethod(
      "Remove",
      BindingFlags.Static | BindingFlags.NonPublic
    );
    if (removeMethod == null)
    {
      Log.Error("[TEST] Could not find Remove method via reflection!");
      return null;
    }

    ///first pass thingDefs
    // int thingDefsRemoved = 0;
    HashSet<String> thingsRemoved = new();
    foreach (var thing in DefDatabase<VEF.Graphics.GraphicOffsets>.AllDefs.ToList())
    {
      if (!thing.defName.StartsWith("AC_") || thingsToKeep.Contains(thing.defName))
      {
        continue;
      }
      // Call Remove on the original def
      try
      {
        thingsRemoved.Add(thing.defName);
        removeMethod.Invoke(null, new object[] { thing });
        // thingDefsRemoved++;
      }
      catch (Exception ex)
      {
        thingsRemoved.Remove(thing.defName);
        Log.Error(
          $"[Medieval Overhaul Lite] Exception during Remove: {ex.Message}\n{ex.StackTrace}"
        );
        continue;
      }
    }
    return thingsRemoved;
  }

  private static HashSet<String> RemoveHedifDefs(HashSet<String> thingsToKeep)
  {
    // Get the private Remove method using reflection
    var removeMethod = typeof(DefDatabase<HediffDef>).GetMethod(
      "Remove",
      BindingFlags.Static | BindingFlags.NonPublic
    );
    if (removeMethod == null)
    {
      Log.Error("[TEST] Could not find Remove method via reflection!");
      return null;
    }

    ///first pass thingDefs
    // int thingDefsRemoved = 0;
    HashSet<String> thingsRemoved = new();
    foreach (var thing in DefDatabase<HediffDef>.AllDefs.ToList())
    {
      if (!thing.defName.StartsWith("AC_") || thingsToKeep.Contains(thing.defName))
      {
        continue;
      }
      // Call Remove on the original def
      try
      {
        thingsRemoved.Add(thing.defName);
        removeMethod.Invoke(null, new object[] { thing });
        // thingDefsRemoved++;
      }
      catch (Exception ex)
      {
        thingsRemoved.Remove(thing.defName);
        Log.Error(
          $"[Medieval Overhaul Lite] Exception during Remove: {ex.Message}\n{ex.StackTrace}"
        );
        continue;
      }
    }
    return thingsRemoved;
  }

  private static HashSet<String> RemoveJobDefs(HashSet<String> thingsToKeep)
  {
    // Get the private Remove method using reflection
    var removeMethod = typeof(DefDatabase<JobDef>).GetMethod(
      "Remove",
      BindingFlags.Static | BindingFlags.NonPublic
    );
    if (removeMethod == null)
    {
      Log.Error("[TEST] Could not find Remove method via reflection!");
      return null;
    }

    ///first pass thingDefs
    // int thingDefsRemoved = 0;
    HashSet<String> thingsRemoved = new();
    foreach (var thing in DefDatabase<JobDef>.AllDefs.ToList())
    {
      if (!thing.defName.StartsWith("AC_") || thingsToKeep.Contains(thing.defName))
      {
        continue;
      }
      // Call Remove on the original def
      try
      {
        thingsRemoved.Add(thing.defName);
        removeMethod.Invoke(null, new object[] { thing });
        // thingDefsRemoved++;
      }
      catch (Exception ex)
      {
        thingsRemoved.Remove(thing.defName);
        Log.Error(
          $"[Medieval Overhaul Lite] Exception during Remove: {ex.Message}\n{ex.StackTrace}"
        );
        continue;
      }
    }
    return thingsRemoved;
  }

  private static HashSet<String> RemoveProcessDefs()
  {
    // Get the private Remove method using reflection
    var removeMethod = typeof(DefDatabase<PipeSystem.ProcessDef>).GetMethod(
      "Remove",
      BindingFlags.Static | BindingFlags.NonPublic
    );
    if (removeMethod == null)
    {
      Log.Error("[TEST] Could not find Remove method via reflection!");
      return null;
    }

    ///first pass thingDefs
    // int thingDefsRemoved = 0;
    HashSet<String> thingsRemoved = new();
    foreach (var thing in DefDatabase<PipeSystem.ProcessDef>.AllDefs.ToList())
    {
      if (!thing.defName.StartsWith("AC_"))
      {
        continue;
      }
      // Call Remove on the original def
      try
      {
        thingsRemoved.Add(thing.defName);
        removeMethod.Invoke(null, new object[] { thing });
        // thingDefsRemoved++;
      }
      catch (Exception ex)
      {
        thingsRemoved.Remove(thing.defName);
        Log.Error(
          $"[Medieval Overhaul Lite] Exception during Remove: {ex.Message}\n{ex.StackTrace}"
        );
        continue;
      }
    }
    return thingsRemoved;
  }

  private static HashSet<String> RemoveRecipeDefs(HashSet<String> thingsToKeep)
  {
    // Get the private Remove method using reflection
    var removeMethod = typeof(DefDatabase<RecipeDef>).GetMethod(
      "Remove",
      BindingFlags.Static | BindingFlags.NonPublic
    );
    if (removeMethod == null)
    {
      Log.Error("[TEST] Could not find Remove method via reflection!");
      return null;
    }

    ///first pass thingDefs
    // int thingDefsRemoved = 0;
    HashSet<String> thingsRemoved = new();
    foreach (var thing in DefDatabase<RecipeDef>.AllDefs.ToList())
    {
      if (!thing.defName.StartsWith("AC_") || thingsToKeep.Contains(thing.defName))
      {
        continue;
      }
      // Call Remove on the original def
      try
      {
        thingsRemoved.Add(thing.defName);
        removeMethod.Invoke(null, new object[] { thing });
        // thingDefsRemoved++;
      }
      catch (Exception ex)
      {
        thingsRemoved.Remove(thing.defName);
        Log.Error(
          $"[Medieval Overhaul Lite] Exception during Remove: {ex.Message}\n{ex.StackTrace}"
        );
        continue;
      }
    }
    return thingsRemoved;
  }

  private static HashSet<String> RemoveThoughtDefs(HashSet<String> thingsToKeep)
  {
    // Get the private Remove method using reflection
    var removeMethod = typeof(DefDatabase<ThoughtDef>).GetMethod(
      "Remove",
      BindingFlags.Static | BindingFlags.NonPublic
    );
    if (removeMethod == null)
    {
      Log.Error("[TEST] Could not find Remove method via reflection!");
      return null;
    }

    ///first pass thingDefs
    // int thingDefsRemoved = 0;
    HashSet<String> thingsRemoved = new();
    foreach (var thing in DefDatabase<ThoughtDef>.AllDefs.ToList())
    {
      if (!thing.defName.StartsWith("AC_") || thingsToKeep.Contains(thing.defName))
      {
        continue;
      }
      // Call Remove on the original def
      try
      {
        thingsRemoved.Add(thing.defName);
        removeMethod.Invoke(null, new object[] { thing });
        // thingDefsRemoved++;
      }
      catch (Exception ex)
      {
        thingsRemoved.Remove(thing.defName);
        Log.Error(
          $"[Medieval Overhaul Lite] Exception during Remove: {ex.Message}\n{ex.StackTrace}"
        );
        continue;
      }
    }
    return thingsRemoved;
  }

  private static void RemoveCondimentsCategory()
  {
    try
    {
      // Find the VCE_Condiments category def
      var condimentsCategory = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("VCE_Condiments");
      if (condimentsCategory == null)
      {
        return;
      }

      int itemsModified = 0;
      foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs)
      {
        // Only process Alpha Crafts items that still exist (not purged)
        if (!thing.defName.StartsWith("AC_"))
        {
          continue;
        }

        // Check if this item has thingCategories and VCE_Condiments is in it
        if (thing.thingCategories != null && thing.thingCategories.Contains(condimentsCategory))
        {
          thing.thingCategories.Remove(condimentsCategory);
          itemsModified++;
        }
      }

      if (itemsModified > 0)
      {
        Log.Message(
          $"[Medieval Overhaul Lite] Removed VCE_Condiments category from {itemsModified} Alpha Crafts items"
        );
      }
    }
    catch (Exception ex)
    {
      Log.Error(
        $"[Medieval Overhaul Lite] Exception during condiments category removal: {ex.Message}\n{ex.StackTrace}"
      );
    }
  }

  private static void CleanupOrphanedGraphics(HashSet<String> removedDefNames)
  {
    try
    {
      // Access the internal allGraphics dictionary in GraphicDatabase
      var graphicDatabaseType = typeof(Verse.GraphicDatabase);
      var allGraphicsField = graphicDatabaseType.GetField(
        "allGraphics",
        BindingFlags.Static | BindingFlags.NonPublic
      );

      if (allGraphicsField == null)
      {
        Log.Warning(
          "[Medieval Overhaul Lite] Could not find GraphicDatabase.allGraphics field - skipping graphics cleanup"
        );
        return;
      }

      var allGraphics = allGraphicsField.GetValue(null) as System.Collections.IDictionary;
      if (allGraphics == null)
      {
        Log.Warning(
          "[Medieval Overhaul Lite] GraphicDatabase.allGraphics is null - skipping graphics cleanup"
        );
        return;
      }

      // Build list of graphic paths to remove (e.g., "Things/Item/AC_Ghee")
      HashSet<string> pathsToRemove = new();
      foreach (var defName in removedDefNames)
      {
        // Convert def names like AC_Ghee to potential texture paths
        pathsToRemove.Add($"Things/Item/{defName}");
        pathsToRemove.Add($"Things/Building/{defName}");
        pathsToRemove.Add($"Things/Plant/{defName}");
      }

      // Find and remove graphics matching removed item paths
      var keysToRemove = new System.Collections.ArrayList();
      foreach (System.Collections.DictionaryEntry entry in allGraphics)
      {
        var graphic = entry.Value as Verse.Graphic;
        if (graphic != null && graphic.path != null)
        {
          // Check if this graphic's path matches any removed item
          foreach (var pathToRemove in pathsToRemove)
          {
            if (graphic.path.StartsWith(pathToRemove))
            {
              keysToRemove.Add(entry.Key);
              break;
            }
          }
        }
      }

      // Remove the orphaned graphics
      int removedCount = 0;
      foreach (var key in keysToRemove)
      {
        allGraphics.Remove(key);
        removedCount++;
      }

      if (removedCount > 0)
      {
        Log.Message(
          $"[Medieval Overhaul Lite] Cleaned up {removedCount} orphaned graphics from GraphicDatabase"
        );
      }
    }
    catch (Exception ex)
    {
      Log.Error(
        $"[Medieval Overhaul Lite] Exception during graphics cleanup: {ex.Message}\n{ex.StackTrace}"
      );
    }
  }

  // ==================== VCE PURGE METHODS ====================

  private static void PurgeVCE()
  {
    Log.Message("========== STARTING VCE PURGE ==========");
    int totalRemoved = 0;

    // Items to remove
    HashSet<string> itemsToRemove = new()
    {
      // Deep fried items
      "VCE_DeepFriedBigMeat",
      "VCE_DeepFriedVegetables",
      "VCE_DeepFriedFish",
      "VCE_DeepFriedSushi",
      // Canned items
      "VCE_CannedMeat",
      "VCE_CannedProduce",
      "VCE_CannedFruit",
      "VCE_CannedAP",
      "VCE_CannedFish",
      // Buildings
      "VCE_DeepFrier",
      "VCE_CanningMachine",
      "VCE_CheesePress",
    };

    // Recipes to remove
    HashSet<string> recipesToRemove = new()
    {
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
    };

    // Research to remove
    HashSet<string> researchToRemove = new() { "VCE_DeepFrying", "VCE_Canning" };

    // Work givers to remove
    HashSet<string> workGiversToRemove = new() { "VCE_DoBillsFryer", "VCE_DoBillsCanning" };

    // Remove items
    totalRemoved += RemoveVCEThingDefs(itemsToRemove);

    // Remove recipes
    totalRemoved += RemoveVCERecipes(recipesToRemove);

    // Remove research
    totalRemoved += RemoveVCEResearch(researchToRemove);

    // Remove work givers
    totalRemoved += RemoveVCEWorkGivers(workGiversToRemove);

    // Note: VCE_CheeseGraphicOffsets is removed via XML patch to avoid static constructor timing issues

    Log.Message($"Purged {totalRemoved} items from VCE");
    Log.Message("========== FINISHED VCE PURGE ==========");
  }

  private static int RemoveVCEThingDefs(HashSet<string> itemsToRemove)
  {
    var removeMethod = typeof(DefDatabase<ThingDef>).GetMethod(
      "Remove",
      BindingFlags.Static | BindingFlags.NonPublic
    );
    if (removeMethod == null)
    {
      Log.Error("[Medieval Overhaul Lite] Could not find Remove method for ThingDef!");
      return 0;
    }

    int removed = 0;
    foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs.ToList())
    {
      if (!itemsToRemove.Contains(thing.defName))
      {
        continue;
      }

      try
      {
        removeMethod.Invoke(null, new object[] { thing });
        removed++;
      }
      catch (Exception ex)
      {
        Log.Error($"[Medieval Overhaul Lite] Failed to remove {thing.defName}: {ex.Message}");
      }
    }
    return removed;
  }

  private static int RemoveVCERecipes(HashSet<string> recipesToRemove)
  {
    var removeMethod = typeof(DefDatabase<RecipeDef>).GetMethod(
      "Remove",
      BindingFlags.Static | BindingFlags.NonPublic
    );
    if (removeMethod == null)
    {
      Log.Error("[Medieval Overhaul Lite] Could not find Remove method for RecipeDef!");
      return 0;
    }

    int removed = 0;
    foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefs.ToList())
    {
      if (!recipesToRemove.Contains(recipe.defName))
      {
        continue;
      }

      try
      {
        removeMethod.Invoke(null, new object[] { recipe });
        removed++;
      }
      catch (Exception ex)
      {
        Log.Error($"[Medieval Overhaul Lite] Failed to remove {recipe.defName}: {ex.Message}");
      }
    }
    return removed;
  }

  private static int RemoveVCEResearch(HashSet<string> researchToRemove)
  {
    var removeMethod = typeof(DefDatabase<ResearchProjectDef>).GetMethod(
      "Remove",
      BindingFlags.Static | BindingFlags.NonPublic
    );
    if (removeMethod == null)
    {
      Log.Error("[Medieval Overhaul Lite] Could not find Remove method for ResearchProjectDef!");
      return 0;
    }

    int removed = 0;
    foreach (ResearchProjectDef research in DefDatabase<ResearchProjectDef>.AllDefs.ToList())
    {
      if (!researchToRemove.Contains(research.defName))
      {
        continue;
      }

      try
      {
        removeMethod.Invoke(null, new object[] { research });
        removed++;
      }
      catch (Exception ex)
      {
        Log.Error($"[Medieval Overhaul Lite] Failed to remove {research.defName}: {ex.Message}");
      }
    }
    return removed;
  }

  private static int RemoveVCEWorkGivers(HashSet<string> workGiversToRemove)
  {
    var removeMethod = typeof(DefDatabase<WorkGiverDef>).GetMethod(
      "Remove",
      BindingFlags.Static | BindingFlags.NonPublic
    );
    if (removeMethod == null)
    {
      Log.Error("[Medieval Overhaul Lite] Could not find Remove method for WorkGiverDef!");
      return 0;
    }

    int removed = 0;
    foreach (WorkGiverDef workGiver in DefDatabase<WorkGiverDef>.AllDefs.ToList())
    {
      if (!workGiversToRemove.Contains(workGiver.defName))
      {
        continue;
      }

      try
      {
        removeMethod.Invoke(null, new object[] { workGiver });
        removed++;
      }
      catch (Exception ex)
      {
        Log.Error($"[Medieval Overhaul Lite] Failed to remove {workGiver.defName}: {ex.Message}");
      }
    }
    return removed;
  }

  private static int RemoveVCEGraphicOffsets(HashSet<string> graphicOffsetsToRemove)
  {
    var removeMethod = typeof(DefDatabase<VEF.Graphics.GraphicOffsets>).GetMethod(
      "Remove",
      BindingFlags.Static | BindingFlags.NonPublic
    );
    if (removeMethod == null)
    {
      Log.Error("[Medieval Overhaul Lite] Could not find Remove method for GraphicOffsets!");
      return 0;
    }

    int removed = 0;
    foreach (var offset in DefDatabase<VEF.Graphics.GraphicOffsets>.AllDefs.ToList())
    {
      if (!graphicOffsetsToRemove.Contains(offset.defName))
      {
        continue;
      }

      try
      {
        removeMethod.Invoke(null, new object[] { offset });
        removed++;
      }
      catch (Exception ex)
      {
        Log.Error($"[Medieval Overhaul Lite] Failed to remove {offset.defName}: {ex.Message}");
      }
    }
    return removed;
  }
}
