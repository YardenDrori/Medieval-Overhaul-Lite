using RimWorld;
using Verse;

namespace MOExpandedLite;

public class CompProperties_CreatesGolemAttacks : CompProperties
{
  public CompProperties_CreatesGolemAttacks()
  {
    compClass = typeof(CompCreatesGolemAttacks);
  }
}

public class CompCreatesGolemAttacks : ThingComp
{
  private int lastCreatedGolemAttackTick = -999999;

  // 7 days between attacks from the same drill (420000 ticks = 7 days)
  private const int MinRefireTicks = 420000;

  public bool CanCreateGolemAttackNow
  {
    get
    {
      // Only trigger if drill is actively being used
      CompDeepDrill comp = parent.GetComp<CompDeepDrill>();
      if (comp != null && !comp.UsedLastTick())
      {
        return false;
      }

      if (CantFireBecauseCreatedGolemAttackRecently)
      {
        return false;
      }

      return true;
    }
  }

  public bool CantFireBecauseCreatedGolemAttackRecently =>
    Find.TickManager.TicksGame <= lastCreatedGolemAttackTick + MinRefireTicks;

  public override void PostExposeData()
  {
    Scribe_Values.Look(ref lastCreatedGolemAttackTick, "lastCreatedGolemAttackTick", -999999);
  }

  public void Notify_CreatedGolemAttack()
  {
    lastCreatedGolemAttackTick = Find.TickManager.TicksGame;
  }
}
