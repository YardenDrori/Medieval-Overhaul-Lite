using RimWorld;
using Verse;

namespace MOExpandedLite;

public class CompCreatesInfestations : ThingComp
{
  private int lastCreatedGolemsTick = -999999;

  private const float MinRefireDays = 7f;

  public bool CanCreateInfestationNow
  {
    get
    {
      CompDeepDrill comp = parent.GetComp<CompDeepDrill>();
      if (comp != null && !comp.UsedLastTick())
      {
        return false;
      }
      if (CantFireBecauseCreatedGolemsRecently)
      {
        return false;
      }
      return true;
    }
  }

  public bool CantFireBecauseCreatedGolemsRecently =>
    Find.TickManager.TicksGame <= lastCreatedGolemsTick + 420000;

  public override void PostExposeData()
  {
    Scribe_Values.Look(ref lastCreatedGolemsTick, "lastCreatedGolemsTick", -999999);
  }

  public void Notify_CreatedInfestation()
  {
    lastCreatedGolemsTick = Find.TickManager.TicksGame;
  }
}
