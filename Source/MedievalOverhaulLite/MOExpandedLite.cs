using HarmonyLib;
using Verse;

namespace MOExpandedLit
{
  [StaticConstructorOnStartup]
  public static class MOExpandedLite
  {
    static MOExpandedLite()
    {
      var harmony = new Harmony("blacksparrow.medievaloverhaullite");
      harmony.PatchAll();

      Log.Message("[MO Expanded Lite] Mod initialized successfully!");
    }
  }
}
