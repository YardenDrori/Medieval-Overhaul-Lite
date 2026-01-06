using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MOExpandedLite
{
  public class JobDriver_FillBowl : JobDriver
  {
    private const TargetIndex StoveInd = TargetIndex.A;

    private const TargetIndex BowlInd = TargetIndex.B;

    private const int Duration = 200;

    protected Building_WorkTable Stove => (Building_WorkTable)job.GetTarget(TargetIndex.A).Thing;

    protected Thing Bowl => job.GetTarget(TargetIndex.B).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
      if (pawn.Reserve(Stove, job, 1, -1, null, errorOnFailed))
      {
        return pawn.Reserve(Bowl, job, 1, -1, null, errorOnFailed);
      }
      return false;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
      CompBowlStorage bowlStorage = Stove.TryGetComp<CompBowlStorage>();
      if (bowlStorage == null)
      {
        Log.Error($"[Medieval Overhaul Lite] {Stove.def.defName} does not have CompBowlStorage");
        yield break;
      }

      this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
      this.FailOnBurningImmobile(TargetIndex.A);
      AddEndCondition(() =>
        (bowlStorage.CapacityRemaining() > 0) ? JobCondition.Ongoing : JobCondition.Succeeded
      );
      yield return Toils_General.DoAtomic(
        delegate
        {
          job.count = bowlStorage.CapacityRemaining();
        }
      );
      Toil reserveBowl = Toils_Reserve.Reserve(TargetIndex.B);
      yield return reserveBowl;
      yield return Toils_Goto
        .GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
        .FailOnDespawnedNullOrForbidden(TargetIndex.B)
        .FailOnSomeonePhysicallyInteracting(TargetIndex.B);
      yield return Toils_Haul
        .StartCarryThing(
          TargetIndex.B,
          putRemainderInQueue: false,
          subtractNumTakenFromJobCount: true
        )
        .FailOnDestroyedNullOrForbidden(TargetIndex.B);
      yield return Toils_Haul.CheckForGetOpportunityDuplicate(
        reserveBowl,
        TargetIndex.B,
        TargetIndex.None,
        takeFromValidStorage: true
      );
      yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
      yield return Toils_General
        .Wait(200)
        .FailOnDestroyedNullOrForbidden(TargetIndex.B)
        .FailOnDestroyedNullOrForbidden(TargetIndex.A)
        .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
        .WithProgressBarToilDelay(TargetIndex.A);
      Toil toil = ToilMaker.MakeToil("MakeNewToils");
      toil.initAction = delegate
      {
        bowlStorage.AddBowls(Bowl);
      };
      toil.defaultCompleteMode = ToilCompleteMode.Instant;
      yield return toil;
    }
  }
}
