using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace MOExpandedLite.HarmonyPatches;

/// <summary>
/// Suppresses cross-reference error logs for blacklisted defs.
/// Patches WantedRefForObject.TryResolve to silently succeed for blacklisted defNames,
/// and filters Log.Error messages for any remaining cross-ref errors (list/dict refs).
/// </summary>
[HarmonyPatch]
public static class CrossRefSuppressor
{
  private static readonly Type WantedRefForObjectType =
    AccessTools.Inner(typeof(DirectXmlCrossRefLoader), "WantedRefForObject");

  private static readonly FieldInfo DefNameField =
    AccessTools.Field(WantedRefForObjectType, "defName");

  /// <summary>
  /// Patches WantedRefForObject.TryResolve — handles single-def cross-references.
  /// When a field references a blacklisted def, we pretend it resolved (field stays null).
  /// </summary>
  [HarmonyTargetMethod]
  static MethodBase TargetMethod()
  {
    return AccessTools.Method(WantedRefForObjectType, "TryResolve");
  }

  [HarmonyPrefix]
  static bool Prefix(object __instance, ref bool __result)
  {
    string defName = (string)DefNameField.GetValue(__instance);
    if (defName != null && DefBlacklist.ShouldBlockDef(defName))
    {
      // Pretend resolved — the field stays null, no error logged,
      // and the WantedRef is removed from the pending list.
      __result = true;
      return false;
    }
    return true;
  }
}

/// <summary>
/// Catches error/warning logs related to blacklisted defs. Covers:
/// - "Could not resolve cross-reference ... named AC_Foo ..."
/// - "Failed to find ... named AC_Foo ..."
/// - "Config error in AC_Foo: ..."
/// - "Exception in ConfigErrors() of AC_Foo: ..."
/// - "StatRequest for null def." (from broken refs on surviving defs)
/// </summary>
[HarmonyPatch(typeof(Log), nameof(Log.Error))]
[HarmonyPatch(new Type[] { typeof(string) })]
public static class CrossRefLogFilter
{
  [HarmonyPrefix]
  static bool Prefix(string text)
  {
    if (text == null)
      return true;

    // "StatRequest for null def" — caused by broken stat refs on removed defs
    if (text.StartsWith("StatRequest for null"))
      return false;

    // "Config error in AC_Foo: ..."
    if (text.StartsWith("Config error in "))
    {
      string afterIn = text.Substring(16);
      int endIdx = afterIn.IndexOfAny(new[] { ':', ' ' });
      string defName = endIdx >= 0 ? afterIn.Substring(0, endIdx) : afterIn;
      if (DefBlacklist.ShouldBlockDef(defName))
        return false;
    }

    // "Could not resolve cross-reference ... named AC_Foo ..."
    // "Failed to find ... named AC_Foo ..."
    // "Exception in ConfigErrors() of AC_Foo: ..."
    int namedIdx = text.IndexOf(" named ");
    if (namedIdx >= 0)
    {
      string afterNamed = text.Substring(namedIdx + 7);
      int endIdx = afterNamed.IndexOfAny(new[] { ' ', '(', '.' });
      string defName = endIdx >= 0 ? afterNamed.Substring(0, endIdx) : afterNamed;
      if (DefBlacklist.ShouldBlockDef(defName))
        return false;
    }

    int configIdx = text.IndexOf("ConfigErrors() of ");
    if (configIdx >= 0)
    {
      string afterOf = text.Substring(configIdx + 18);
      int endIdx = afterOf.IndexOfAny(new[] { ':', ' ' });
      string defName = endIdx >= 0 ? afterOf.Substring(0, endIdx) : afterOf;
      if (DefBlacklist.ShouldBlockDef(defName))
        return false;
    }

    return true;
  }
}
