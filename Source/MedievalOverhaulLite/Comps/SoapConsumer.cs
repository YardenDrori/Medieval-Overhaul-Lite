using RimWorld;
using Verse;

namespace MOExpandedLite;

/// <summary>
/// Stock of soap held by a washing fixture (Dubs Bad Hygiene showers and bathtubs).
/// Built on CompRefuelable so hauling, the fill gizmo, inspect string and saving all
/// come from vanilla — the only custom behaviour is that soap is spent per wash
/// instead of over time.
/// </summary>
public class CompProperties_SoapConsumer : CompProperties_Refuelable
{
  public float soapPerUse = 1f;

  /// <summary>Memory granted to a pawn that washed with soap available. Optional.</summary>
  public ThoughtDef washedThought;

  public CompProperties_SoapConsumer()
  {
    compClass = typeof(CompSoapConsumer);

    // Soap is only spent by CompSoapConsumer.TryConsumeSoap, never by ticking.
    consumeFuelOnlyWhenUsed = true;
    fuelConsumptionRate = 0f;

    // Running dry just means no soap bonus, so don't nag the player about it.
    drawOutOfFuelOverlay = false;
    initialFuelPercent = 0f;
  }
}

public class CompSoapConsumer : CompRefuelable
{
  public new CompProperties_SoapConsumer Props => (CompProperties_SoapConsumer)props;

  /// <summary>
  /// Spends one wash worth of soap. Returns false when the fixture is dry — the wash
  /// still happens, it just doesn't earn the soap bonus.
  /// </summary>
  public bool TryConsumeSoap()
  {
    if (Fuel < Props.soapPerUse)
    {
      return false;
    }

    ConsumeFuel(Props.soapPerUse);
    return true;
  }
}
