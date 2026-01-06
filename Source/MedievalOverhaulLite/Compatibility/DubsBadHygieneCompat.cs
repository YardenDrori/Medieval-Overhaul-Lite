using RimWorld;
using Verse;

namespace MOExpandedLite.Compatibility
{
  /// <summary>
  /// Handles optional integration with Dubs Bad Hygiene water system
  /// DBH uses CompRefuelable to store water, much simpler than we thought!
  /// </summary>
  public static class DubsBadHygieneCompat
  {
    private static bool isLoaded;

    static DubsBadHygieneCompat()
    {
      isLoaded = ModsConfig.IsActive("dubwise.dubsbadhygiene");

      if (isLoaded)
      {
        Log.Message("[Medieval Overhaul Lite] DBH detected, water integration enabled");
      }
    }

    /// <summary>
    /// Check if building has enough water available
    /// </summary>
    public static bool HasWater(Building building, float amount)
    {
      if (!isLoaded)
        return true; // No DBH = always has water

      CompRefuelable waterStorage = building.GetComp<CompRefuelable>();
      if (waterStorage == null)
        return true; // No water comp = not using DBH water system

      return waterStorage.Fuel >= amount;
    }

    /// <summary>
    /// Try to consume water from building's storage
    /// </summary>
    public static bool TryConsumeWater(Building building, float amount)
    {
      CompRefuelable waterStorage = building.GetComp<CompRefuelable>();
      if (waterStorage == null)
        return true; // No water comp = always succeed

      if (waterStorage.Fuel < amount)
      {
        // Not enough water!
        waterStorage.ConsumeFuel(100);
        return false;
      }

      waterStorage.ConsumeFuel(amount);
      return true;
    }

    /// <summary>
    /// Check if building can currently work (has water OR DBH not loaded)
    /// Use this in WorkGiver checks to prevent jobs when no water
    /// </summary>
    public static bool CanWork(Building building, float waterNeeded)
    {
      if (!isLoaded)
        return true; // No DBH = always can work

      CompRefuelable waterStorage = building.GetComp<CompRefuelable>();
      if (waterStorage == null)
        return true; // No water system = always can work

      // Can work if has enough water
      return waterStorage.Fuel >= waterNeeded;
    }
  }
}
