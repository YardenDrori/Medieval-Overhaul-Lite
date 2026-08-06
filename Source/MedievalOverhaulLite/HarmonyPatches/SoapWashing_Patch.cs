using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MOExpandedLite.HarmonyPatches;

/// <summary>
/// Makes Dubs Bad Hygiene washes spend soap from the fixture's CompSoapConsumer.
///
/// DBH is a soft dependency and is never referenced at compile time, so the target is
/// resolved by name and the patch is silently skipped when the mod isn't active. This
/// runs from a static constructor rather than the Mod ctor because DBH's assembly may
/// not be loaded yet at that point.
/// </summary>
[StaticConstructorOnStartup]
public static class SoapWashing_Patch
{
  static SoapWashing_Patch()
  {
    TryPatch(new Harmony("blacksparrow.medievaloverhaullite"));
  }

  private static void TryPatch(Harmony harmony)
  {
    Type sanitationUtil = AccessTools.TypeByName("DubsBadHygiene.SanitationUtil");
    if (sanitationUtil == null)
    {
      return;
    }

    MethodInfo target = AccessTools.Method(
      sanitationUtil,
      "ApplyBathroomThought",
      new[] { typeof(Pawn), typeof(Thing) }
    );
    if (target == null)
    {
      Log.Warning(
        "[MO Expanded Lite] Dubs Bad Hygiene is active but SanitationUtil.ApplyBathroomThought was not found — washing will not consume soap."
      );
      return;
    }

    harmony.Patch(
      target,
      postfix: new HarmonyMethod(AccessTools.Method(typeof(SoapWashing_Patch), nameof(Postfix)))
    );
  }

  /// <summary>
  /// DBH calls this once per wash — from the finish toil of a shower and the opening
  /// toil of a bath — for every washing fixture, so a fixture without the soap comp
  /// (toilets, basins, wash buckets) is simply left alone.
  /// </summary>
  public static void Postfix(Pawn actor, Thing fixture)
  {
    CompSoapConsumer soap = (fixture as ThingWithComps)?.GetComp<CompSoapConsumer>();
    if (soap == null || !soap.TryConsumeSoap())
    {
      return;
    }

    ThoughtDef thought = soap.Props.washedThought;
    if (thought == null)
    {
      return;
    }

    actor?.needs?.mood?.thoughts?.memories?.TryGainMemory(thought);
  }
}
