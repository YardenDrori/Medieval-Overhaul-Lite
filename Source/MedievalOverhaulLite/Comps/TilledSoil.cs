using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MOExpandedLite;

public class CompProperties_TilledSoil : CompProperties
{
  public int ticksToExpire = 900000;

  public CompProperties_TilledSoil()
  {
    this.compClass = typeof(CompTilledSoil);
  }
}

public class CompTilledSoil : ThingComp
{
  private CompProperties_TilledSoil Props => (CompProperties_TilledSoil)props;
  private int ticksToExpire;
  private int ticksPassed = 0;

  public override void PostSpawnSetup(bool respawningAfterLoad)
  {
    base.PostSpawnSetup(respawningAfterLoad);
    if (!respawningAfterLoad)
    {
      ticksPassed = 0;
    }
  }

  public override void PostDestroy(DestroyMode mode, Map previousMap)
  {
    base.PostDestroy(mode, previousMap);
  }

  public override void CompTickRare()
  {
    base.CompTickRare();
    ticksPassed += 250;
    if (ticksPassed >= ticksToExpire) { }
  }

  private void Expire() { }

  public override void PostExposeData()
  {
    base.PostExposeData();
    Scribe_Values.Look(ref ticksPassed, "ticksPassed", 0);
  }
}
