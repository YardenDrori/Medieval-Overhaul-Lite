using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace MOExpandedLite
{
    // [StaticConstructorOnStartup]
    public static class TestDefDatabaseRemoval
    {
        // Disabled temporarily to avoid startup issues
        // static TestDefDatabaseRemoval()
        // {
        //     // Run the test after a delay to ensure everything is loaded
        //     LongEventHandler.ExecuteWhenFinished(RunTest);
        // }

        private static void RunTest()
        {
            Log.Message("========== STARTING DEF DATABASE REMOVAL TEST ==========");

            // Get the original RawCorn ThingDef
            ThingDef originalCorn = DefDatabase<ThingDef>.GetNamedSilentFail("RawCorn");

            if (originalCorn == null)
            {
                Log.Error("RawCorn ThingDef not found! Test cannot proceed.");
                return;
            }

            Log.Message($"[TEST] Found original RawCorn ThingDef: {originalCorn.defName}");
            LogDefDetails("BEFORE REMOVAL", originalCorn);

            Log.Message($"[TEST] Total ThingDefs before removal: {DefDatabase<ThingDef>.DefCount}");

            // Get the private Remove method using reflection
            var removeMethod = typeof(DefDatabase<ThingDef>).GetMethod("Remove", BindingFlags.Static | BindingFlags.NonPublic);

            if (removeMethod == null)
            {
                Log.Error("[TEST] Could not find Remove method via reflection!");
                return;
            }

            // Check if it's in database before removal
            bool existsBeforeRemove = DefDatabase<ThingDef>.GetNamedSilentFail("RawCorn") != null;
            Log.Message($"[TEST] RawCorn exists in database before Remove: {existsBeforeRemove}");

            // Call Remove on the original def
            try
            {
                Log.Message($"[TEST] Calling DefDatabase<ThingDef>.Remove() on RawCorn...");
                removeMethod.Invoke(null, new object[] { originalCorn });
                Log.Message($"[TEST] Remove() completed without exceptions");
            }
            catch (Exception ex)
            {
                Log.Error($"[TEST] Exception during Remove: {ex.Message}\n{ex.StackTrace}");
                return;
            }

            // Check what happened
            ThingDef currentCorn = DefDatabase<ThingDef>.GetNamedSilentFail("RawCorn");

            if (currentCorn == null)
            {
                Log.Message("[TEST] RawCorn is now NULL in database (successfully removed)");
            }
            else
            {
                Log.Warning($"[TEST] RawCorn still exists in database! Label: {currentCorn.label}");
                bool isSameInstance = ReferenceEquals(currentCorn, originalCorn);
                Log.Message($"[TEST] Is same instance as original: {isSameInstance}");
            }

            Log.Message($"[TEST] Total ThingDefs after removal: {DefDatabase<ThingDef>.DefCount}");

            // Check if the removed def object still has its graphic data
            Log.Message($"[TEST] Checking removed def object (originalCorn variable)...");
            LogDefDetails("AFTER REMOVAL", originalCorn);

            Log.Message("========== DEF DATABASE REMOVAL TEST COMPLETE ==========");
        }

        private static void LogDefDetails(string prefix, ThingDef def)
        {
            Log.Message($"[{prefix}] defName: {def.defName}");
            Log.Message($"[{prefix}] label: {def.label}");
            Log.Message($"[{prefix}] index: {def.index}");
            Log.Message($"[{prefix}] shortHash: {def.shortHash}");

            if (def.graphicData != null)
            {
                Log.Message($"[{prefix}] graphicData exists: TRUE");
                Log.Message($"[{prefix}] graphicData.texPath: {def.graphicData.texPath}");
                Log.Message($"[{prefix}] graphicData.graphicClass: {def.graphicData.graphicClass}");

                // Check if graphic is initialized
                try
                {
                    var graphic = def.graphicData.Graphic;
                    if (graphic != null)
                    {
                        Log.Message($"[{prefix}] Graphic exists: TRUE");
                        Log.Message($"[{prefix}] Graphic.MatSingle: {(graphic.MatSingle != null ? "EXISTS" : "NULL")}");

                        if (graphic.MatSingle != null && graphic.MatSingle.mainTexture != null)
                        {
                            var tex = graphic.MatSingle.mainTexture;
                            Log.Message($"[{prefix}] Texture: {tex.name} (width: {tex.width}, height: {tex.height})");
                        }
                        else
                        {
                            Log.Message($"[{prefix}] Texture: NULL or no mainTexture");
                        }
                    }
                    else
                    {
                        Log.Message($"[{prefix}] Graphic exists: FALSE (not yet initialized)");
                    }
                }
                catch (Exception ex)
                {
                    Log.Message($"[{prefix}] Error accessing graphic: {ex.Message}");
                }
            }
            else
            {
                Log.Message($"[{prefix}] graphicData exists: FALSE");
            }
        }
    }
}
