using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace MOExpandedLite
{
    public class DeathActionWorker_MonsterSplit : DeathActionWorker
    {
        public override RulePackDef DeathRules => RulePackDefOf.Transition_DiedExplosive;

        public override bool DangerousInMelee => true;

        public override void PawnDied(Corpse corpse, Lord prevLord)
        {
            if (corpse?.Map == null)
            {
                return;
            }

            DeathSplitExtension modExtension = corpse.InnerPawn?.def?.GetModExtension<DeathSplitExtension>();
            if (modExtension == null || !Rand.Chance(modExtension.randomChance))
            {
                return;
            }

            int spawnCount = modExtension.createAmount.RandomInRange;
            List<Pawn> spawnedPawns = new List<Pawn>();

            for (int i = 0; i < spawnCount; i++)
            {
                if (modExtension.pawnKindDef == null)
                {
                    continue;
                }

                Pawn pawn = PawnGenerator.GeneratePawn(modExtension.pawnKindDef, null);
                pawn.ageTracker.AgeBiologicalTicks = 60L;
                spawnedPawns.Add(pawn);
            }

            if (modExtension.explosion)
            {
                DamageDef damageDef = modExtension.damageDef ?? DamageDefOf.Bomb;
                GenExplosion.DoExplosion(
                    corpse.Position,
                    corpse.Map,
                    modExtension.explosionRadius,
                    damageDef,
                    corpse.InnerPawn,
                    modExtension.explosionDamage,
                    modExtension.armorPen
                );
            }

            foreach (Pawn pawn in spawnedPawns)
            {
                IntVec3 spawnPos = CellFinder.RandomClosewalkCellNear(corpse.Position, corpse.Map, 3);
                GenSpawn.Spawn(pawn, spawnPos, corpse.Map, Rot4.Random);
                pawn.mindState?.mentalStateHandler?.TryStartMentalState(MentalStateDefOf.Manhunter);
            }
        }
    }
}
