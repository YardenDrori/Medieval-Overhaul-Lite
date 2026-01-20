using UnityEngine;
using VEF.Maps;
using Verse;

namespace MOExpandedLite;

public class TerrainCompProperties_Expiring : TerrainCompProperties
{
  public int hoursToExpire = 480; // Default 20 days for Rich state
  public bool isBaseVariant = false;
  public SoilState soilState = SoilState.Rich;

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

    // Only Rich state registers via TerrainComp (initial placement)
    // Moderate and Depleted states are registered directly by the manager during transitions
    // (VEF defers TerrainComp initialization, so manager handles it to ensure correct timing)
    if (Props.soilState == SoilState.Rich)
    {
      TerrainDef underTerrain = parent.Map.terrainGrid.UnderTerrainAt(parent.Position);
      float baseFertility = underTerrain?.fertility ?? 1f;
      int underlyingPercent = Mathf.RoundToInt(baseFertility * 100f);
      underlyingPercent = (underlyingPercent / 10) * 10; // Round to 10%

      manager.RegisterSoil(parent.Position, Props.hoursToExpire, Props.soilState, underlyingPercent);
    }
    // Moderate and Depleted: manager already registered us during transition
  }

  public override void PostRemove()
  {
    var manager = parent.Map.GetComponent<MapComponent_TilledSoilLifetimeCache>()?.Manager;
    manager?.UnregisterSoil(parent.Position);
  }
}
