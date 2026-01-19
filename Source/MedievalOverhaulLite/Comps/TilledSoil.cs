using VEF.Maps;
using Verse;

namespace MOExpandedLite;

public class TerrainCompProperties_Expiring : TerrainCompProperties
{
  public int hoursToExpire = 360; // 15 days default

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
    manager?.RegisterSoil(parent.Position, Props.hoursToExpire);
  }

  public override void PostRemove()
  {
    var manager = parent.Map.GetComponent<MapComponent_TilledSoilLifetimeCache>()?.Manager;
    manager?.UnregisterSoil(parent.Position);
  }
}
