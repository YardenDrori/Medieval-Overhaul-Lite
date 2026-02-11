using HarmonyLib;
using Verse;

namespace MOExpandedLite;

/// <summary>
/// Mod class — constructor runs during LoadAllActiveMods(), BEFORE defs are loaded.
/// This ensures our ResolveAllWantedCrossReferences prefix is active when DoPlayLoad runs.
/// </summary>
public class MOExpandedLite : Mod
{
  public MOExpandedLite(ModContentPack content)
    : base(content)
  {
    var harmony = new Harmony("blacksparrow.medievaloverhaullite");
    harmony.PatchAll();

    Log.Message("[MO Expanded Lite] Harmony patches applied (early init)");
  }
}
