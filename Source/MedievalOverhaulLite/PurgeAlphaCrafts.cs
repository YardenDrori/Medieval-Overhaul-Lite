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
      // "AC_PickleVegetables",
      "AC_ChurnButter",
      "AC_ExtractEssence",
      // "AC_ExtractPerfume",
      // "AC_MakeSoap",
      // 6
      "AC_Butter",
      "AC_Essence",
      "AC_BalsamicVinegar",
      "AC_Oil",
      "AC_Perfume",
      // "AC_Pickles",
      // "AC_Jam",
      // "AC_Soap",
      "AC_Vinegar",
      // 10
      "AC_ConsumedBalsamicVinegar",
      "AC_ConsumedButter",
      "AC_ConsumedJam",
      "AC_ConsumedOil",
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
}
