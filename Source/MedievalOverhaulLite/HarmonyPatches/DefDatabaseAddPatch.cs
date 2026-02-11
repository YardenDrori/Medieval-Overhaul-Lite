using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace MOExpandedLite.HarmonyPatches;

/// <summary>
/// Removes blacklisted defs from all DefDatabases BEFORE cross-references are resolved.
/// Patches the non-generic ResolveAllWantedCrossReferences — runs on each call to catch
/// both initially loaded defs and implied defs generated between resolution passes.
///
/// This avoids patching generic methods (which breaks Mono's JIT).
/// </summary>
[HarmonyPatch(
  typeof(DirectXmlCrossRefLoader),
  nameof(DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences)
)]
public static class DefRemovalPatch
{
  private static int removedCount;

  [HarmonyPrefix]
  static void Prefix()
  {
    int before = removedCount;
    RemoveBlacklistedDefs();
    int thisPass = removedCount - before;
    if (thisPass > 0)
    {
      Log.Message(
        $"[MO Expanded Lite] Removed {thisPass} blacklisted defs (total: {removedCount})"
      );
    }
  }

  private static void RemoveBlacklistedDefs()
  {
    foreach (Type defType in GenDefDatabase.AllDefTypesWithDatabases())
    {
      Type dbType;
      try
      {
        dbType = typeof(DefDatabase<>).MakeGenericType(defType);
      }
      catch
      {
        continue;
      }

      var allDefsProperty = dbType.GetProperty(
        "AllDefsListForReading",
        BindingFlags.Public | BindingFlags.Static
      );
      if (allDefsProperty == null)
        continue;

      var defsListObj = allDefsProperty.GetValue(null);
      if (defsListObj == null)
        continue;

      var removeMethod = dbType.GetMethod(
        "Remove",
        BindingFlags.Static | BindingFlags.NonPublic
      );
      if (removeMethod == null)
        continue;

      // Collect first, then remove — avoids any IList index issues on Mono.
      var toRemove = new List<Def>();
      foreach (object item in (IEnumerable)defsListObj)
      {
        if (item is Def def && DefBlacklist.ShouldBlockDef(def.defName))
          toRemove.Add(def);
      }

      foreach (Def def in toRemove)
      {
        try
        {
          removeMethod.Invoke(null, new object[] { def });
          removedCount++;
        }
        catch (Exception ex)
        {
          Log.Warning(
            $"[MO Expanded Lite] Failed to remove {def.defName} from DefDatabase<{defType.Name}>: {ex.InnerException?.Message ?? ex.Message}"
          );
        }
      }
    }
  }

  public static int GetRemovedCount() => removedCount;
}
