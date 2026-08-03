using System;
using System.Collections.Generic;
using BigAndSmall;
using RimWorld;
using Verse;

namespace SapientMechanoidFix
{
    public class CompProperties_SapientMechHitArmor : CompProperties
    {
        public HediffDef hediffToAdd;
        public int duration;
        public int limitOfTimes = -1;

        public CompProperties_SapientMechHitArmor()
        {
            compClass = typeof(CompSapientMechHitArmor);
        }
    }

    /// <summary>
    /// Ports Glitterworld Destroyer 5's CompHitArmor (GD3.dll) for our sapient War
    /// Queen/Work Queen: each hit grants (or refreshes) a short full-immunity hediff,
    /// consuming one of a limited number of charges that recharge while the mech is
    /// being repaired - same mechanic, same recharge hook
    /// (MechRepairUtility.RepairTick/CanRepair, see the patches for those), just a
    /// separate comp/hediff so it doesn't depend on GD5 being installed.
    ///
    /// Self-gates every effect to sapient mechs (IsMechanical() && !RaceProps.IsMechanoid)
    /// - present on the non-sapient originals too (see Patches/ReactiveArmor.xml) but
    /// always inert there, matching this mod's established pattern.
    /// </summary>
    public class CompSapientMechHitArmor : ThingComp
    {
        public int timesLeft = -1;
        public int repairTick;

        private CompProperties_SapientMechHitArmor Props => (CompProperties_SapientMechHitArmor)props;

        public bool CanRepair => Props.limitOfTimes >= 0 && timesLeft < Props.limitOfTimes;

        private bool IsSapient
        {
            get
            {
                Pawn pawn = parent as Pawn;
                return pawn != null && !pawn.RaceProps.IsMechanoid && pawn.IsMechanical();
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (Props.limitOfTimes >= 0 && timesLeft == -1)
                timesLeft = Props.limitOfTimes;
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            try
            {
                if (!IsSapient)
                    return;
                if (totalDamageDealt == 0f || !dinfo.Def.harmsHealth)
                    return;
                if (Props.limitOfTimes >= 0 && timesLeft == 0)
                    return;

                Pawn pawn = (Pawn)parent;
                if (!pawn.Spawned)
                    return;

                Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffToAdd);
                if (existing != null)
                {
                    HediffComp_Disappears disappears = existing.TryGetComp<HediffComp_Disappears>();
                    if (disappears != null)
                        disappears.ticksToDisappear = Props.duration;
                }
                else
                {
                    Hediff added = HediffMaker.MakeHediff(Props.hediffToAdd, pawn);
                    HediffComp_Disappears disappears = added.TryGetComp<HediffComp_Disappears>();
                    if (disappears != null)
                        disappears.ticksToDisappear = Props.duration;
                    pawn.health.AddHediff(added);
                }

                if (Props.limitOfTimes >= 0 && timesLeft > 0)
                    timesLeft--;
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[SapientMechanoidFix] Reactive armor (hit-armor) trigger failed: " + e, 91274445);
            }
        }

        public void Notify_RepairMech()
        {
            if (!IsSapient || Props.limitOfTimes < 0)
                return;

            repairTick++;
            if (repairTick < 10)
                return;

            repairTick = 0;
            timesLeft = Math.Min(timesLeft + 1, Props.limitOfTimes);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref timesLeft, "timesLeft", -1);
            Scribe_Values.Look(ref repairTick, "repairTick", 0);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
                yield return gizmo;

            if (!IsSapient || Props.limitOfTimes < 0)
                yield break;

            yield return new SapientMechHitArmorGizmo(this);
        }
    }
}
