using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace MOExpandedLite;

/// <summary>
/// Post-load cleanup that runs after all defs are loaded and resolved.
/// Most removal is done before cross-ref resolution — this handles:
/// - Final safety-net removal pass for any defs that survived (e.g. implied defs)
/// - VCE_Condiments category cleanup on kept AC_ items
/// - Orphaned graphics cleanup
/// </summary>
[StaticConstructorOnStartup]
public static class PostLoadCleanup
{
  static PostLoadCleanup()
  {
    LongEventHandler.ExecuteWhenFinished(RunCleanup);
  }

  private static void RunCleanup()
  {
    // Safety net: remove any blacklisted defs that survived (implied defs, late additions)
    int lateSweep = FinalRemovalSweep();
    Log.Message(
      $"[MO Expanded Lite] Total blacklisted defs removed: {HarmonyPatches.DefRemovalPatch.GetRemovedCount()}"
        + (lateSweep > 0 ? $" (+{lateSweep} in final sweep)" : "")
    );

    RemoveCondimentsCategory();
    CleanupOrphanedGraphics();
    CleanupModContentAssets();

    Resources.UnloadUnusedAssets();
    GC.Collect();
  }

  private static int FinalRemovalSweep()
  {
    int removed = 0;
    foreach (Type defType in GenDefDatabase.AllDefTypesWithDatabases())
    {
      try
      {
        Type dbType = typeof(DefDatabase<>).MakeGenericType(defType);

        var allDefsProperty = dbType.GetProperty(
          "AllDefsListForReading",
          BindingFlags.Public | BindingFlags.Static
        );
        if (allDefsProperty == null)
          continue;

        var defsList = allDefsProperty.GetValue(null) as IList;
        if (defsList == null || defsList.Count == 0)
          continue;

        var removeMethod = dbType.GetMethod(
          "Remove",
          BindingFlags.Static | BindingFlags.NonPublic
        );
        if (removeMethod == null)
          continue;

        for (int i = defsList.Count - 1; i >= 0; i--)
        {
          try
          {
            if (defsList[i] is Def def && DefBlacklist.ShouldBlockDef(def.defName))
            {
              removeMethod.Invoke(null, new object[] { def });
              HarmonyPatches.DefRemovalPatch.RemovedDefNames.Add(def.defName);
              removed++;
            }
          }
          catch { }
        }
      }
      catch { }
    }
    return removed;
  }

  private static void RemoveCondimentsCategory()
  {
    try
    {
      var condimentsCategory = DefDatabase<ThingCategoryDef>.GetNamedSilentFail(
        "VCE_Condiments"
      );
      if (condimentsCategory == null)
        return;

      int itemsModified = 0;
      foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs)
      {
        if (!thing.defName.StartsWith("AC_"))
          continue;

        if (thing.thingCategories != null && thing.thingCategories.Contains(condimentsCategory))
        {
          thing.thingCategories.Remove(condimentsCategory);
          itemsModified++;
        }
      }

      if (itemsModified > 0)
      {
        Log.Message(
          $"[MO Expanded Lite] Removed VCE_Condiments from {itemsModified} AC_ items"
        );
      }
    }
    catch (Exception ex)
    {
      Log.Error($"[MO Expanded Lite] Error removing condiments category: {ex}");
    }
  }

  /// <summary>
  /// Checks if a path (graphic path or texture content key) belongs to a removed def.
  /// Uses the actual set of removed defNames instead of blacklist rules — avoids
  /// accidentally nuking textures for whitelisted defs.
  /// </summary>
  private static bool PathBelongsToRemovedDef(string path)
  {
    if (string.IsNullOrEmpty(path))
      return false;

    var removed = HarmonyPatches.DefRemovalPatch.RemovedDefNames;

    // Check each path segment against actual removed defNames
    int start = 0;
    while (start < path.Length)
    {
      int slash = path.IndexOf('/', start);
      string segment = slash >= 0 ? path.Substring(start, slash - start) : path.Substring(start);
      if (segment.Length > 0)
      {
        // Check if segment starts with any removed defName
        foreach (string defName in removed)
        {
          if (segment.StartsWith(defName))
            return true;
        }
      }
      if (slash < 0)
        break;
      start = slash + 1;
    }

    return false;
  }

  private static void CleanupOrphanedGraphics()
  {
    try
    {
      var allGraphicsField = typeof(GraphicDatabase).GetField(
        "allGraphics",
        BindingFlags.Static | BindingFlags.NonPublic
      );
      if (allGraphicsField == null)
        return;

      var allGraphics = allGraphicsField.GetValue(null) as System.Collections.IDictionary;
      if (allGraphics == null)
        return;

      var keysToRemove = new List<object>();
      foreach (DictionaryEntry entry in allGraphics)
      {
        if (entry.Value is Graphic graphic && graphic.path != null &&
            PathBelongsToRemovedDef(graphic.path))
        {
          keysToRemove.Add(entry.Key);
        }
      }

      foreach (var key in keysToRemove)
        allGraphics.Remove(key);

      if (keysToRemove.Count > 0)
      {
        Log.Message(
          $"[MO Expanded Lite] Cleaned up {keysToRemove.Count} orphaned graphics"
        );
      }
    }
    catch (Exception ex)
    {
      Log.Error($"[MO Expanded Lite] Error cleaning up graphics: {ex}");
    }
  }

  /// <summary>
  /// Removes loaded textures from mod content holders.
  /// RimWorld loads ALL textures from each mod's Textures/ folder during LoadAllActiveMods,
  /// regardless of whether the corresponding defs exist. This purges those orphaned assets.
  /// Only removes textures whose paths match actually-removed defNames.
  /// </summary>
  private static void CleanupModContentAssets()
  {
    try
    {
      int removedTextures = 0;

      foreach (ModContentPack mod in LoadedModManager.RunningMods)
      {
        var holder = mod.GetContentHolder<Texture2D>();
        if (holder == null)
          continue;

        var keysToRemove = holder.contentList.Keys
          .Where(PathBelongsToRemovedDef)
          .ToList();

        foreach (string key in keysToRemove)
        {
          if (holder.contentList.TryGetValue(key, out Texture2D tex) && tex != null)
            UnityEngine.Object.Destroy(tex);

          holder.contentList.Remove(key);
          removedTextures++;
        }
      }

      if (removedTextures > 0)
      {
        Log.Message(
          $"[MO Expanded Lite] Destroyed {removedTextures} orphaned textures from mod content"
        );
      }
    }
    catch (Exception ex)
    {
      Log.Error($"[MO Expanded Lite] Error cleaning up mod content assets: {ex}");
    }
  }
}
