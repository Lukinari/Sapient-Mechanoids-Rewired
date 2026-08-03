using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SapientMechanoidFix
{
    /// <summary>
    /// Getting a job assigned (see WorkGiver_RepairMech_HasJobOnThing_Patch) isn't
    /// enough on its own - JobDriver_RepairMech's own repair toil unconditionally does
    /// Mech.needs.energy.CurLevel -= ... on every tick interval, with no null check,
    /// since every real mechanoid always has that need. A sapient mechanoid doesn't (see
    /// the other patch's doc comment for why), so the job would crash the first time it
    /// actually ticked.
    ///
    /// JobDriver_RepairMechRemote (the mechanitor-bandwidth long-range variant) extends
    /// this class and calls base.MakeNewToils() rather than reimplementing it, so
    /// patching the base method here covers both without needing a second patch.
    ///
    /// MakeNewToils only ever assigns a custom tickIntervalAction to the one toil that
    /// actually performs the repair (the earlier GotoThing toil, when present, doesn't
    /// set one) - checking for that is what identifies which yielded toil to wrap here,
    /// rather than hardcoding an index that could shift if vanilla reorders the toils.
    /// </summary>
    [HarmonyPatch(typeof(JobDriver_RepairMech), "MakeNewToils")]
    public static class JobDriver_RepairMech_MakeNewToils_Patch
    {
        private static readonly AccessTools.FieldRef<JobDriver_RepairMech, int> TicksToNextRepairRef =
            AccessTools.FieldRefAccess<JobDriver_RepairMech, int>("ticksToNextRepair");

        public static IEnumerable<Toil> Postfix(IEnumerable<Toil> values, JobDriver_RepairMech __instance)
        {
            foreach (Toil toil in values)
            {
                if (toil.tickIntervalAction != null)
                    WrapTickAction(toil, __instance);
                yield return toil;
            }
        }

        private static void WrapTickAction(Toil toil, JobDriver_RepairMech driver)
        {
            Action<int> original = toil.tickIntervalAction;
            toil.tickIntervalAction = delegate (int delta)
            {
                try
                {
                    Pawn mech = (Pawn)driver.job.GetTarget(TargetIndex.A).Thing;
                    if (mech?.needs?.energy != null)
                    {
                        original(delta); // Real energy need present - vanilla's own behavior is already correct, untouched.
                        return;
                    }

                    // Sapient mechanoid: replicate the rest of the original tick action
                    // exactly, just without the line that unconditionally drains
                    // needs.energy.CurLevel. MechRepairUtility.RepairTick doesn't touch
                    // that need either way, so nothing about the actual repair is being
                    // skipped - only the power-bar bookkeeping that doesn't apply here.
                    int ticksToNextRepair = TicksToNextRepairRef(driver) - delta;
                    if (ticksToNextRepair <= 0)
                    {
                        MechRepairUtility.RepairTick(mech, delta);
                        ticksToNextRepair = Mathf.RoundToInt(1f / driver.pawn.GetStatValue(StatDefOf.MechRepairSpeed) * 120f);
                    }
                    TicksToNextRepairRef(driver) = ticksToNextRepair;
                    driver.pawn.rotationTracker.FaceTarget(mech);
                    if (driver.pawn.skills != null)
                        driver.pawn.skills.Learn(SkillDefOf.Crafting, 0.05f * delta);
                }
                catch (Exception e)
                {
                    Log.ErrorOnce("[SapientMechanoidFix] JobDriver_RepairMech tick patch failed: " + e, 91274431);
                }
            };
        }
    }
}
