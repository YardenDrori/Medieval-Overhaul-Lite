using RimWorld;
using Verse;

namespace MOExpandedLite;

public class CompProperties_PlantEnergy : CompProperties_MechPowerCell
{
  private ThingDef cachedThingToSpawnOnEmpty;
  public ThingDef thingToSpawnOnEmpty
  {
    get
    {
      if (cachedThingToSpawnOnEmpty == null)
      {
        cachedThingToSpawnOnEmpty = DefDatabase<ThingDef>.GetNamedSilentFail("Plant_TreeOak");
      }
      return cachedThingToSpawnOnEmpty;
    }
  }
  public float growthPercentage = 0.15f;

  public CompProperties_PlantEnergy()
  {
    compClass = typeof(CompPlantEnergy);
    totalPowerTicks = 20000;
    tooltipOverride =
      "Temporary animation triggered by disturbance and psychic phenomena granting limited autonamy. The animation will become ordinary matter once this energy dissipates.";
    showGizmoOnNonPlayerControlled = true;
    killWhenDepleted = false;
  }
}

public class CompPlantEnergy : CompMechPowerCell
{
  public new CompProperties_PlantEnergy Props => (CompProperties_PlantEnergy)props;

  // Runtime-set values (override Props defaults)
  private ThingDef treeTypeToSpawn;
  private float treeGrowthPercentage;

  public void SetTreeData(ThingDef treeType, float growthPercentage)
  {
    this.treeTypeToSpawn = treeType;
    this.treeGrowthPercentage = growthPercentage;
  }

  public override void PostExposeData()
  {
    base.PostExposeData();
    Scribe_Defs.Look(ref treeTypeToSpawn, "treeTypeToSpawn");
    Scribe_Values.Look(ref treeGrowthPercentage, "treeGrowthPercentage", 0f);
  }

  public override void CompTick()
  {
    base.CompTick();

    if (depleted)
    {
      // Use runtime values if set, otherwise fall back to Props
      ThingDef thingDef = treeTypeToSpawn ?? Props.thingToSpawnOnEmpty;
      float growth = treeTypeToSpawn != null ? treeGrowthPercentage : Props.growthPercentage;

      if (thingDef == null)
      {
        Log.Error(
          $"[Medieval Overhaul Lite] Failed to find thingDef to spawn for {parent.def.defName}"
        );
        return;
      }
      Thing thingToSpawnOnEmpty = ThingMaker.MakeThing(thingDef);
      if (thingToSpawnOnEmpty == null)
      {
        Log.Error($"[Medieval Overhaul Lite] Failed make Thing {thingDef.defName}");
        return;
      }
      if (thingToSpawnOnEmpty is Plant plantToSpawn)
      {
        plantToSpawn.Growth = growth * 0.5f;
      }
      Thing spawned = GenSpawn.Spawn(thingToSpawnOnEmpty, parent.Position, parent.Map);
      if (spawned != null)
      {
        parent.Destroy();
      }
    }
  }
}
