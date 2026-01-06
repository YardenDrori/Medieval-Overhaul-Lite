using System;
using System.Reflection;
using Verse;

namespace MOExpandedLite.Compatibility
{
  public static class DubsBadHygieneCompat
  {
    private static bool isLoaded;
    private static MethodInfo tryUseWaterMethod;

    static DubsBadHygieneCompat()
    {
      Initialize();
    }

    private static void Initialize()
    {
      // Check if DBH is loaded
      isLoaded = ModsConfig.IsActive("dubwise.dubsbadhygiene");

      if (!isLoaded)
      {
        Log.Message("[Medieval Overhaul Lite] DBH not loaded, water integration disabled");
        return;
      }

      try
      {
        // Find DBH assembly
        Assembly dbhAssembly = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
          if (assembly.GetName().Name == "BadHygiene")
          {
            dbhAssembly = assembly;
            break;
          }
        }

        if (dbhAssembly == null)
        {
          Log.Warning(
            "[Medieval Overhaul Lite] DBH mod loaded but assembly not found"
          );
          return;
        }

        // Find PlumbingNet class
        Type plumbingNetType = dbhAssembly.GetType("DubsBadHygiene.PlumbingNet");
        if (plumbingNetType == null)
        {
          Log.Warning("[Medieval Overhaul Lite] Could not find PlumbingNet type in DBH");
          return;
        }

        // Find TryUseWater method
        tryUseWaterMethod = plumbingNetType.GetMethod(
          "TryUseWater",
          BindingFlags.Public | BindingFlags.Static
        );

        if (tryUseWaterMethod == null)
        {
          Log.Warning(
            "[Medieval Overhaul Lite] Could not find TryUseWater method in PlumbingNet"
          );
          return;
        }

        Log.Message("[Medieval Overhaul Lite] Successfully initialized DBH integration");
      }
      catch (Exception ex)
      {
        Log.Error($"[Medieval Overhaul Lite] Failed to initialize DBH compatibility: {ex}");
      }
    }

    /// <summary>
    /// Attempts to consume water from the plumbing network
    /// </summary>
    /// <param name="building">The building consuming water</param>
    /// <param name="amount">Amount of water to consume</param>
    /// <returns>True if water was consumed or DBH not loaded, false if not enough water</returns>
    public static bool TryConsumeWater(Building building, float amount)
    {
      if (!isLoaded || tryUseWaterMethod == null)
      {
        return true; // No DBH or failed init = always succeed
      }

      try
      {
        // Call PlumbingNet.TryUseWater(building, amount)
        object result = tryUseWaterMethod.Invoke(null, new object[] { building, amount });
        return (bool)result;
      }
      catch (Exception ex)
      {
        Log.Warning(
          $"[Medieval Overhaul Lite] Error calling DBH TryUseWater: {ex.Message}"
        );
        return true; // On error, don't block gameplay
      }
    }
  }
}
