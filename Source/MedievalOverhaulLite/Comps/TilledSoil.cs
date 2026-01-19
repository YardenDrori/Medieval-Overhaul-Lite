using VEF.Maps;
using Verse;

namespace MOExpandedLite;

public class TerrainCompProperties_Expiring : TerrainCompProperties
{
  public int hoursToExpire = 720;
  public bool isBaseVariant = false;

  public TerrainCompProperties_Expiring()
  {
    compClass = typeof(TerrainComp_Expiring);
  }
}

public class TerrainComp_Expiring : TerrainComp
{
  public TerrainCompProperties_Expiring Props => (TerrainCompProperties_Expiring)props;

  public override void Initialize(TerrainCompProperties props)
  {
    base.Initialize(props);

    var manager = parent.Map.GetComponent<MapComponent_TilledSoilLifetimeCache>()?.Manager;
    if (manager == null)
      return;

    // If this is the base buildable variant, queue swap to correct fertility variant
    // We delay to next TickLong because VEF hasn't finished registering this terrain yet
    if (Props.isBaseVariant)
    {
      manager.QueueFertilitySwap(parent.Position, Props.hoursToExpire);
      return;
    }

    // Register for expiration tracking
    manager.RegisterSoil(parent.Position, Props.hoursToExpire);
  }

  public override void PostRemove()
  {
    var manager = parent.Map.GetComponent<MapComponent_TilledSoilLifetimeCache>()?.Manager;
    manager?.UnregisterSoil(parent.Position);
  }
}
