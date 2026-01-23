using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MOExpandedLite;

/// <summary>
/// A spawner that looks like a shaking/rumbling tree before an Ent emerges.
/// Uses leaf and dust particles since we don't have ground emergence effects.
/// </summary>
public class EntSpawner : ThingWithComps
{
  public bool spawnDarkVariant;
  public ThingDef treeDefToRestore;
  public float treeGrowth;
  public Thing treeToDestroy; // The original tree, kept alive during animation
  public PawnKindDef entKindToSpawn; // Pre-decided what this spawner will spawn

  private int spawnTick;
  private Sustainer sustainer;

  private static readonly IntRange SpawnDelay = new IntRange(1200, 1560); // ~20-26 seconds

  public int TicksUntilSpawn => spawnTick - Find.TickManager.TicksGame;

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Values.Look(ref spawnDarkVariant, "spawnDarkVariant", false);
    Scribe_Values.Look(ref spawnTick, "spawnTick", 0);
    Scribe_Values.Look(ref treeGrowth, "treeGrowth", 0.5f);
    Scribe_Defs.Look(ref treeDefToRestore, "treeDefToRestore");
    Scribe_Defs.Look(ref entKindToSpawn, "entKindToSpawn");
    Scribe_References.Look(ref treeToDestroy, "treeToDestroy");
  }

  public override void SpawnSetup(Map map, bool respawningAfterLoad)
  {
    base.SpawnSetup(map, respawningAfterLoad);
    if (!respawningAfterLoad)
    {
      spawnTick = Find.TickManager.TicksGame + SpawnDelay.RandomInRange;
    }
    LongEventHandler.ExecuteWhenFinished(delegate
    {
      sustainer = SoundDefOf.Tunnel?.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
    });
  }

  protected override void Tick()
  {
    base.Tick();
    if (!Spawned)
    {
      return;
    }

    // Maintain sound
    sustainer?.Maintain();

    // Throw leaf/dust particles periodically to simulate tree shaking
    if (Find.TickManager.TicksGame % 15 == 0)
    {
      Vector3 loc = Position.ToVector3Shifted() + new Vector3(Rand.Range(-0.5f, 0.5f), 0, Rand.Range(-0.5f, 0.5f));
      FleckMaker.ThrowDustPuff(loc, Map, 0.5f);
    }
    if (Find.TickManager.TicksGame % 30 == 0)
    {
      // Simulate leaves/debris falling
      Vector3 loc = Position.ToVector3Shifted() + new Vector3(Rand.Range(-1f, 1f), 0, Rand.Range(-1f, 1f));
      FleckMaker.ThrowMicroSparks(loc, Map);
    }

    // Time to spawn
    if (Find.TickManager.TicksGame >= spawnTick)
    {
      sustainer?.End();
      Map map = Map;
      IntVec3 loc = Position;
      Destroy();
      SpawnEnt(map, loc);
    }
  }

  private void SpawnEnt(Map map, IntVec3 loc)
  {
    // Use the pre-decided ent kind
    if (entKindToSpawn == null)
    {
      Log.Warning("[Medieval Overhaul Lite] EntSpawner: No ent kind was set for this spawner!");
      return;
    }

    Pawn ent = PawnGenerator.GeneratePawn(
      new PawnGenerationRequest(
        entKindToSpawn,
        faction: null,
        context: PawnGenerationContext.NonPlayer,
        tile: map.Tile,
        forceGenerateNewPawn: true,
        allowDead: false,
        allowDowned: false
      )
    );

    GenSpawn.Spawn(ent, loc, map);

    // Set energy comp to restore tree when depleted
    if (treeDefToRestore != null)
    {
      CompPlantEnergy energyComp = ent.TryGetComp<CompPlantEnergy>();
      if (energyComp != null)
      {
        energyComp.SetTreeData(treeDefToRestore, treeGrowth);
      }
    }

    // Make ent aggressive
    ent.mindState.mentalStateHandler.TryStartMentalState(
      MentalStateDefOf.Manhunter,
      forceWake: true
    );

    // Now destroy the tree that was kept alive during animation
    if (treeToDestroy != null && !treeToDestroy.Destroyed)
    {
      treeToDestroy.Destroy(DestroyMode.Vanish);
    }

    // Final burst effect
    FleckMaker.ThrowDustPuffThick(loc.ToVector3Shifted(), map, 2f, Color.green);
    FleckMaker.ThrowSmoke(loc.ToVector3Shifted(), map, 1.5f);
  }

  protected override void DrawAt(Vector3 drawLoc, bool flip = false)
  {
    // Don't draw anything - we're invisible, just particles
  }

  public override string GetInspectString()
  {
    return "Emergence".Translate() + ": " + TicksUntilSpawn.ToStringTicksToPeriod();
  }
}
