using VEF.Maps;
using Verse;

namespace MOExpandedLite;

public class TerrainCompProperties_Expiring : TerrainCompProperties
{
  // If true, this is the player-buildable base variant that gets swapped to a Rich variant
  public bool isBaseVariant = false;

  public TerrainCompProperties_Expiring()
  {
    compClass = typeof(TerrainComp_Expiring);
  }
}

public class TerrainComp_Expiring : TerrainComp
{
  private TerrainCompProperties_Expiring Props => (TerrainCompProperties_Expiring)props;

  public override void Initialize(TerrainCompProperties props)
  {
    base.Initialize(props);

    // Only the base buildable variant triggers registration
    // The actual R/W/D variants are managed entirely by the manager
    if (!Props.isBaseVariant)
      return;

    var manager = GetManager();
    manager?.OnSoilPlaced(parent.Position);
  }

  public override void PostRemove()
  {
    // Notify manager that this soil was removed (by player, construction, etc.)
    var manager = GetManager();
    manager?.OnSoilRemoved(parent.Position);
  }

  private Thing_TilledSoilManager GetManager()
  {
    return parent.Map?.GetComponent<MapComponent_TilledSoilLifetimeCache>()?.Manager;
  }
}
